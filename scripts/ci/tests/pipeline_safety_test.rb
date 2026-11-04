# frozen_string_literal: true

# Run from any directory: ruby scripts/ci/tests/pipeline_safety_test.rb
# This deliberately limited interpreter checks repository policy, not Azure's
# service-side YAML compiler. Unknown expressions fail instead of being executed.
require 'minitest/autorun'
require 'yaml'
require 'strscan'
require 'open3'

module PipelinePolicy
  ROOT = File.expand_path('../../..', __dir__)
  TEMPLATE = 'azure-pipelines/templates/selenium-job.yml'
  UNHEALTHY = %w[Failed Skipped Canceled SucceededWithIssues].freeze

  class Expression
    def initialize(source, context = {}, dependencies = {}, success = true)
      @scanner = StringScanner.new(source.to_s.strip.sub(/\A\$\{\{\s*/, '').sub(/\s*\}\}\z/, ''))
      @context, @dependencies, @success = context, dependencies, success
    end

    def value
      result = expression
      whitespace
      raise ArgumentError, "Unsupported expression near #{@scanner.rest.inspect}" unless @scanner.eos?

      result
    end

    private

    def whitespace
      @scanner.skip(/\s*/)
    end

    def expression
      whitespace
      return @scanner.matched[1...-1].gsub("''", "'") if @scanner.scan(/'(?:[^']|'')*'/)
      return @context[@scanner[1]] if @scanner.scan(/variables\[['"]([^'"]+)['"]\]/)
      return @context[@scanner[1]] if @scanner.scan(/variables\.([\w.]+)/)
      return @dependencies[@scanner[1]] if @scanner.scan(/dependencies\.([\w]+)\.result/)
      return @scanner.matched.downcase == 'true' if @scanner.scan(/(?:true|false)\b/i)

      name = @scanner.scan(/[a-zA-Z]+/)
      raise ArgumentError, "Unsupported expression near #{@scanner.rest.inspect}" unless name

      whitespace
      raise ArgumentError, "Expected ( after #{name}" unless @scanner.scan(/\(/)

      arguments = []
      whitespace
      unless @scanner.peek(1) == ')'
        loop do
          arguments << expression
          whitespace
          break unless @scanner.scan(/,/)
        end
      end
      raise ArgumentError, "Expected ) after #{name}" unless @scanner.scan(/\)/)

      case name.downcase
      when 'and' then arguments.all? { |argument| argument == true }
      when 'or' then arguments.any? { |argument| argument == true }
      when 'not' then !arguments.fetch(0)
      when 'eq' then arguments.fetch(0) == arguments.fetch(1)
      when 'ne' then arguments.fetch(0) != arguments.fetch(1)
      when 'in' then arguments.drop(1).include?(arguments.fetch(0))
      when 'succeeded' then @success
      when 'always' then true
      else raise ArgumentError, "Unsupported function #{name}"
      end
    end
  end

  def read_yaml(path)
    absolute = File.expand_path(path, ROOT)
    assert File.file?(absolute), "Missing required YAML: #{path}"
    # Psych's YAML 1.1 parser interprets GitHub's unquoted `on` as true.
    YAML.safe_load(File.read(absolute), permitted_classes: [], permitted_symbols: [], aliases: false)
  end

  def context(reason = 'IndividualCI', branch = 'refs/heads/developer')
    { 'Build.Reason' => reason, 'Build.SourceBranch' => branch }
  end

  def expand(value, path, variables)
    case value
    when Array
      value.flat_map { |item| expanded = expand(item, path, variables); expanded.is_a?(Array) ? expanded : [expanded] }
    when Hash
      if value.size == 1 && value.keys.first.to_s =~ /\A\$\{\{\s*if\s+(.+?)\s*\}\}\z/m
        return [] unless Expression.new(Regexp.last_match(1), variables).value == true

        return expand(value.values.first, path, variables)
      end
      if value.key?('template')
        target = File.expand_path(value.fetch('template'), File.dirname(File.expand_path(path, ROOT)))
        assert target.start_with?(ROOT + '/'), 'Template must stay inside the repository'
        template = read_yaml(target)
        parameters = Array(template['parameters']).each_with_object({}) do |parameter, defaults|
          defaults[parameter.fetch('name')] = parameter['default']
        end.merge(value.fetch('parameters', {}))
        content = substitute_parameters(template.fetch('jobs') { template.fetch('steps') }, parameters)
        return expand(content, target, variables)
      end
      value.each_with_object({}) { |(key, child), result| result[key] = expand(child, path, variables) }
    else value
    end
  end

  def pipeline(path, variables = context)
    expand(read_yaml(path), path, variables)
  end

  def substitute_parameters(value, parameters)
    case value
    when Array then value.map { |child| substitute_parameters(child, parameters) }
    when Hash then value.each_with_object({}) { |(key, child), result| result[key] = substitute_parameters(child, parameters) }
    when String
      value.gsub(/\$\{\{\s*parameters\.([\w]+)\s*\}\}/) { parameters.fetch(Regexp.last_match(1)).to_s }
    else value
    end
  end

  def stages(path, variables = context)
    pipeline(path, variables).fetch('stages')
  end

  def hashes(value)
    case value
    when Hash then [value] + value.values.flat_map { |child| hashes(child) }
    when Array then value.flat_map { |child| hashes(child) }
    else []
    end
  end

  def dependency_names(stage)
    Array(stage['dependsOn'])
  end

  def job_steps
    jobs = read_yaml(TEMPLATE).fetch('jobs')
    assert_equal 1, jobs.length
    assert_equal 'Selenium', jobs.first['job']
    jobs.first.fetch('steps')
  end

  def script(step)
    step['bash'] || step['script'] || step.dig('inputs', 'script') || ''
  end
end

class PipelineSafetyTest < Minitest::Test
  include PipelinePolicy

  PIPELINES = {
    'azure-pipelines/developer-debug.yml' => %w[developer],
    'azure-pipelines/core-build.yml' => %w[developer master]
  }.freeze
  REQUIRED_DEPENDENCIES = {
    'Build' => %w[BrowserRegression],
    'DeployDeveloper' => %w[Build BrowserRegression],
    'DeployFunctionDeveloper' => %w[DeployDeveloper Build BrowserRegression]
  }.freeze

  def test_github_triggers_include_documentation_only_prs_pushes_and_plain_dispatch
    workflow = read_yaml('.github/workflows/alimentestaideia.yaml')
    events = workflow['on'] || workflow[true]
    assert_kind_of Hash, events
    %w[pull_request push].each do |event|
      assert_kind_of Hash, events[event], "Missing #{event} trigger"
      assert_equal %w[developer master], events[event].fetch('branches').sort
      %w[paths paths-ignore branches-ignore].each { |key| refute events[event].key?(key), "#{event} must not filter #{key}" }
    end
    assert events.key?('workflow_dispatch')
    assert [nil, {}].include?(events['workflow_dispatch']), 'workflow_dispatch must be plain'
  end

  def test_github_uses_read_only_permissions
    workflow = read_yaml('.github/workflows/alimentestaideia.yaml')
    assert_equal({ 'contents' => 'read' }, workflow['permissions'])
    workflow.fetch('jobs').each_value do |job|
      permissions = job['permissions']
      next unless permissions

      assert permissions == 'read-all' || (permissions.is_a?(Hash) && permissions.values.all? { |value| %w[read none].include?(value) }),
             'Job permissions must not grant writes'
    end
  end

  def test_github_required_aggregate_runs_always_and_fails_closed
    workflow = read_yaml('.github/workflows/alimentestaideia.yaml')
    aggregate = workflow.fetch('jobs').values.find { |job| job['name'] == 'Required build and tests' }
    refute_nil aggregate, 'Missing stable Required build and tests aggregate job'
    assert_equal %w[build test], Array(aggregate['needs']).sort
    assert_match(/\A(?:\$\{\{\s*)?always\(\)(?:\s*\}\})?\z/, aggregate['if'].to_s)
    refute_equal true, aggregate['continue-on-error']
    checks = aggregate.fetch('steps').select { |step| step.key?('run') }
    refute_empty checks, 'Aggregate must check upstream results'
    # Exercise the actual aggregate shell with substituted GitHub results. Only
    # shell builtins are accepted here; this never invokes actions or network tools.
    %w[success failure cancelled skipped].repeated_permutation(2) do |build, test|
      values = { 'needs.build.result' => build, 'needs.test.result' => test }
      outcomes = checks.map do |step|
        refute_equal true, step['continue-on-error']
        assert [nil, 'always()', '${{ always() }}'].include?(step['if']), 'Aggregate checks must not skip failed or canceled dependencies'
        source = step.fetch('run').gsub(/\$\{\{\s*(needs\.(?:build|test)\.result)\s*\}\}/) { values.fetch(Regexp.last_match(1)) }
        refute_match(/\$\{\{/, source, 'Unsupported aggregate expression')
        refute_match(/\$\(|`/, source, 'Aggregate must not execute command substitutions')
        syntax = source.gsub(/'[^']*'|"(?:[^"\\]|\\.)*"/m, 'quoted')
        assert_match(/\A[\s\w\[\]"'$!=&|;:,().{}\\<>\/-]*\z/, syntax, 'Unsupported aggregate shell characters')
        commands = syntax.gsub(/\[\[.*?\]\]/m, 'test').scan(/(?:\A|\n|;|&&|\|\|)\s*([a-zA-Z][\w-]*)/).flatten
        assert_empty commands - %w[if then else elif fi exit echo printf set true false test], 'Aggregate must use shell builtins only'
        environment = (aggregate['env'] || {}).merge(step['env'] || {}).transform_values do |value|
          value.to_s.gsub(/\$\{\{\s*(needs\.(?:build|test)\.result)\s*\}\}/) { values.fetch(Regexp.last_match(1)) }
        end
        _, _, status = Open3.capture3(environment.merge('PATH' => '', 'BASH_ENV' => ''), '/bin/bash', '--noprofile', '--norc', '-c', source)
        status.success?
      end
      assert_equal(build == 'success' && test == 'success', outcomes.all?, "Aggregate results: build=#{build}, test=#{test}")
    end
  end

  def test_standalone_is_pr_and_manual_only_and_expands_the_shared_job
    path = 'azure-pipeline-selenium-ui-tests.yml'
    document = read_yaml(path)
    assert_equal 'none', document['trigger']
    assert_equal true, document.dig('pr', 'autoCancel')
    assert_equal %w[developer master], document.dig('pr', 'branches', 'include').sort
    refute document['pr'].key?('paths'), 'Documentation-only PRs must run'
    assert_shared_job_reference(document.fetch('jobs'), path)
    assert_equal ['Selenium'], pipeline(path).fetch('jobs').map { |job| job['job'] }
  end

  PIPELINES.each do |path, approved|
    label = File.basename(path, '.yml').tr('-', '_')

    define_method("test_#{label}_has_shared_browser_gate_first_and_no_pr_trigger") do
      document = read_yaml(path)
      assert_equal 'none', document['pr']
      assert_equal approved.sort, document.dig('trigger', 'branches', 'include').sort
      browser = document.fetch('stages').first
      assert_equal 'BrowserRegression', browser['stage']
      assert_shared_job_reference(browser.fetch('jobs'), path)
      expanded = stages(path).first
      assert_equal ['Selenium'], expanded.fetch('jobs').map { |job| job['job'] }
      assert_equal [], dependency_names(expanded)
      refute expanded.key?('condition'), 'Browser validation must not inherit deployment branch/reason guards'
    end

    define_method("test_#{label}_compile_time_guards_omit_all_credentialed_stages_for_untrusted_contexts") do
      scenarios = [context('PullRequest'), context('PullRequest', 'refs/heads/master'),
                   context('Manual', 'refs/pull/123/merge'), context('PullRequest', 'refs/pull/123/merge'),
                   context('IndividualCI', 'refs/heads/feature/safety'), context('Manual', 'refs/heads/developer-extra'),
                   context('Manual', 'refs/tags/developer'), context('Manual', 'refs/tags/master'),
                   {}, { 'Build.Reason' => 'Manual' }, { 'Build.SourceBranch' => 'refs/heads/developer' }]
      %w[Schedule BuildCompletion ResourceTrigger Unknown].each { |reason| scenarios << context(reason) }
      scenarios << context('Manual', 'refs/heads/master') unless approved.include?('master')
      scenarios.each do |variables|
        expanded = pipeline(path, variables)
        assert_equal ['BrowserRegression'], expanded.fetch('stages').map { |stage| stage['stage'] },
                     "#{path} must omit credentialed stages at compile time for #{variables.inspect}"
        assert_no_credentials(expanded)
      end
    end

    define_method("test_#{label}_trusted_builds_preserve_stages_and_dependency_graph") do
      approved.product(%w[Manual IndividualCI BatchedCI]).each do |branch, reason|
        selected = stages(path, context(reason, "refs/heads/#{branch}"))
        expected = path.include?('developer-debug') ? %w[BrowserRegression Build DeployDeveloper DeployFunctionDeveloper] : %w[BrowserRegression Build]
        assert_equal expected, selected.map { |stage| stage['stage'] }
        selected.drop(1).each do |stage|
          assert_equal REQUIRED_DEPENDENCIES.fetch(stage.fetch('stage')).sort, dependency_names(stage).sort,
                       "#{path}: #{stage['stage']} must depend on every required gate"
        end
      end
    end

    define_method("test_#{label}_runtime_guards_repeat_branch_reason_and_exact_success_requirements") do
      selected = stages(path)
      credentialed = selected.reject { |stage| stage['stage'] == 'BrowserRegression' }
      refute_empty credentialed
      credentialed.each do |stage|
        condition = stage['condition']
        refute_nil condition, "#{path}: #{stage['stage']} needs a runtime guard"
        assert_match(/\bsucceeded\(\)/i, condition)
        dependencies = REQUIRED_DEPENDENCIES.fetch(stage.fetch('stage')).each_with_object({}) { |name, result| result[name] = 'Succeeded' }
        approved.product(%w[IndividualCI BatchedCI Manual]).each do |branch, reason|
          assert_equal true, Expression.new(condition, context(reason, "refs/heads/#{branch}"), dependencies).value
        end
        [context('PullRequest'), context('Manual', 'refs/pull/123/merge'), context('Manual', 'refs/heads/feature/test'),
         context('Manual', 'refs/tags/developer'), {}, { 'Build.SourceBranch' => 'refs/heads/developer' }].each do |variables|
          assert_equal false, Expression.new(condition, variables, dependencies).value,
                       "#{stage['stage']} allowed #{variables.inspect}"
        end
        unless approved.include?('master')
          assert_equal false, Expression.new(condition, context('Manual', 'refs/heads/master'), dependencies).value
        end
        %w[Schedule BuildCompletion ResourceTrigger Unknown].each do |reason|
          assert_equal false, Expression.new(condition, context(reason), dependencies).value,
                       "#{stage['stage']} allowed unapproved reason #{reason}"
        end
        dependencies.each_key do |dependency|
          (UNHEALTHY + [nil]).each do |result|
            assert_equal false, Expression.new(condition, context, dependencies.merge(dependency => result)).value,
                         "#{stage['stage']} allowed #{dependency}=#{result.inspect}"
          end
        end
        assert_equal false, Expression.new(condition, context, dependencies, false).value, 'succeeded() must participate in the guard'
      end
    end
  end

  def test_shared_job_is_isolated_and_secret_free
    template = read_yaml(TEMPLATE)
    job = template.fetch('jobs').first
    assert_equal 'ubuntu-24.04', job.dig('pool', 'vmImage')
    assert_operator job.fetch('timeoutInMinutes'), :>, 0
    assert_operator job.fetch('cancelTimeoutInMinutes'), :>, 0
    checkout = job_steps.find { |step| step['checkout'] == 'self' }
    refute_nil checkout
    assert_equal false, checkout['persistCredentials']
    assert_no_credentials(template)
    refute hashes(template).any? { |entry| entry.key?('download') || entry.key?('publish') || entry['task'].to_s =~ /(?:Download|Publish).*Artifact/i },
           'Shared validation must not consume or publish release artifacts'
    sdk = job_steps.find { |step| step['task'] == 'UseDotNet@2' }
    refute_nil sdk
    assert_equal '10.0.x', sdk.dig('inputs', 'version')
  end

  def test_shared_job_runs_policy_before_dotnet_build_then_offline_before_local
    steps = job_steps
    build = steps.index { |step| script(step).match?(/\bdotnet\s+build\b/) || (step['task'] == 'DotNetCoreCLI@2' && step.dig('inputs', 'command') == 'build') }
    refute_nil build
    %w[pipeline_safety_test.rb test_result_reporting_test.rb].each do |filename|
      policy = steps.index { |step| script(step).match?(/\bruby\s+scripts\/ci\/tests\/#{Regexp.escape(filename)}\b/) }
      refute_nil policy, "Shared job must run #{filename}"
      assert_operator policy, :<, build
    end
    offline = steps.index { |step| script(step).match?(/scripts\/test-selenium\.sh\s+offline\b/) }
    local = steps.index { |step| script(step).match?(/scripts\/test-selenium\.sh\s+local\b/) }
    refute_nil offline
    refute_nil local
    assert_operator build, :<, offline
    assert_operator offline, :<, local
    [offline, local].each do |index|
      assert_match(/RunConfiguration\.TreatNoTestsAsError=true/, script(steps[index]))
      refute_equal true, steps[index]['continueOnError']
      assert_match(/--logger\s+['"]?trx/, script(steps[index]))
    end
    env = (read_yaml(TEMPLATE).fetch('jobs').first['variables'] || {}).merge(steps[local].fetch('env'))
    assert_equal 'Local', env['SeleniumTest__Target']
    assert_equal 'true', env['SeleniumTest__Headless'].to_s
    assert_equal 'true', env['SeleniumTest__Local__SelfContained'].to_s
    assert_match(%r{\Ahttp://(?:127\.0\.0\.1|localhost):8025/?\z}, env['SeleniumTest__Local__MailpitBaseUrl'])
    assert_equal '1025', env['SeleniumTest__Local__MailpitSmtpPort'].to_s
    steps.each do |step|
      refute_match(/scripts\/test-selenium\.sh\s+developer\b|Category=LivePayment/, script(step))
    end
  end

  def test_shared_cleanup_runs_always_before_failure_sensitive_result_publication
    steps = job_steps
    cleanup = steps.index { |step| script(step).match?(/docker\s+compose\b[^\n]*\bdown\b/) }
    refute_nil cleanup, 'Mailpit must be cleaned up'
    assert_equal 'always()', steps[cleanup]['condition']
    assert_match(/--volumes/, script(steps[cleanup]))
    assert_match(/--project-name/, script(steps[cleanup]))
    start = steps.index { |step| script(step).match?(/docker\s+compose\b[^\n]*\bup\b/) }
    refute_nil start
    assert_operator start, :<, cleanup
    assert_equal steps[start].dig('env', 'MAILPIT_PROJECT'), steps[cleanup].dig('env', 'MAILPIT_PROJECT')
    refute_nil steps[cleanup].dig('env', 'MAILPIT_PROJECT')
    assert_match(/(?:\{1\.\.\d+\}|\bseq\s+\d+|timeout\s+\d+)/, script(steps[start]), 'Readiness must be bounded')
    publications = steps.each_index.select { |index| steps[index]['task'] == 'PublishTestResults@2' }
    refute_empty publications
    verifier = steps.index { |step| script(step).match?(/\bpython3\s+scripts\/ci\/verify-test-results\.py\b/) }
    refute_nil verifier, 'Both reports must be verified after cleanup'
    assert_operator cleanup, :<, verifier
    assert_equal 'always()', steps[verifier]['condition']
    refute_equal true, steps[verifier]['continueOnError']
    refute_match(/\|\|\s*(?:true|exit\s+0)\b/, script(steps[verifier]), 'Report verification failure must fail the job')
    assert_match(/offline[^\s'"]*\.trx/, script(steps[verifier]))
    assert_match(/local[^\s'"]*\.trx/, script(steps[verifier]))
    publications.each do |index|
      assert_operator verifier, :<, index
      assert_equal 'always()', steps[index]['condition']
      refute_equal true, steps[index]['continueOnError']
      inputs = steps[index].fetch('inputs')
      assert_equal 'VSTest', inputs['testResultsFormat']
      %w[failTaskOnFailedTests failTaskOnFailureToPublishResults failTaskOnMissingResultsFile].each do |key|
        assert_equal true, inputs[key], "Publication must enable #{key}"
      end
      assert_match(/\.trx/, inputs.fetch('testResultsFiles'))
    end
  end

  def test_release_artifact_and_project_named_zip_contracts_are_preserved
    PIPELINES.each_key do |path|
      entries = hashes(pipeline(path))
      assert entries.any? { |entry| entry['publish'] && entry['artifact'] == 'drop' }, "#{path} must publish drop"
      projects = %w[BancoAlimentar.AlimentaEstaIdeia.Web BancoAlimentar.AlimentaEstaIdeia.Function]
      projects << 'BancoAlimentar.AlimentaEstaIdeia.Sas.Web.TenantManagement' if path.include?('core-build')
      projects.each do |project|
        publish = entries.find { |entry| entry['task'] == 'DotNetCoreCLI@2' && entry.dig('inputs', 'command') == 'publish' && entry.dig('inputs', 'projects') == "#{project}/#{project}.csproj" }
        refute_nil publish, "Missing publish contract for #{project}"
        assert_equal true, publish.dig('inputs', 'zipAfterPublish')
        assert_equal true, publish.dig('inputs', 'modifyOutputPath')
      end
      next unless path.include?('developer-debug')

      assert entries.any? { |entry| entry.dig('inputs', 'Package') == '$(Pipeline.Workspace)/drop/BancoAlimentar.AlimentaEstaIdeia.Web.zip' }
      assert entries.any? { |entry| entry.dig('inputs', 'package') == '$(Pipeline.Workspace)/drop/BancoAlimentar.AlimentaEstaIdeia.Function.zip' }
    end
  end

  def test_expression_interpreter_rejects_unknown_code_and_requires_complete_input
    assert_raises(ArgumentError) { Expression.new("system('anything')").value }
    assert_raises(ArgumentError) { Expression.new("eq('a', 'a'); true").value }
    assert_equal false, Expression.new("eq(variables['missing'], 'Succeeded')").value
    assert_equal true, Expression.new("and(ne(variables['Build.Reason'], 'PullRequest'), in(variables['Build.SourceBranch'], 'refs/heads/developer', 'refs/heads/master'))", context).value
  end

  private

  def assert_shared_job_reference(jobs, path)
    assert_equal 1, jobs.length, "#{path} should reference one shared browser job"
    refute_nil jobs.first['template'], "#{path} must use the shared job template"
    resolved = File.expand_path(jobs.first.fetch('template'), File.dirname(File.join(ROOT, path)))
    assert_equal File.join(ROOT, TEMPLATE), resolved
  end

  def assert_no_credentials(document)
    hashes(document).each do |entry|
      refute entry.key?('group'), 'Browser-only execution must not import variable groups'
      refute_match(/\A(?:Azure|replacetokens|DownloadSecureFile)/i, entry['task'].to_s, 'Browser-only execution must not retrieve Azure credentials')
      %w[azureSubscription ConnectedServiceName ConnectedServiceNameARM environment].each do |key|
        refute entry.key?(key), "Browser-only execution must not reference #{key}"
      end
      entry.each do |key, value|
        next unless value.is_a?(String)

        refute_match(/\$\([^)]*(?:ServiceConnection|KeyVault|Password|Secret|AccessToken)[^)]*\)/i, value,
                     "Browser-only #{key} must not reference credential variables")
      end
    end
  end
end
