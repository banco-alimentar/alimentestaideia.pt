# frozen_string_literal: true

# Run from any directory: ruby scripts/ci/tests/pipeline_safety_test.rb
# This deliberately limited interpreter checks repository policy, not Azure's
# service-side YAML compiler. Unknown expressions fail instead of being executed.
require 'minitest/autorun'
require 'yaml'
require 'strscan'

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
      if (match = /\A\$\{\{\s*parameters\.([\w]+)\s*\}\}\z/.match(value))
        parameters.fetch(match[1])
      else
        value.gsub(/\$\{\{\s*parameters\.([\w]+)\s*\}\}/) { parameters.fetch(Regexp.last_match(1)).to_s }
      end
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

  def test_azure_validation_is_pr_only_and_includes_both_branches
    path = 'azure-pipeline-selenium-ui-tests.yml'
    document = read_yaml(path)
    assert_equal 'none', document['trigger']
    assert_equal true, document.dig('pr', 'autoCancel')
    assert_equal %w[developer master], document.dig('pr', 'branches', 'include').sort
    refute document['pr'].key?('paths'), 'Documentation-only PRs must run'
    assert_equal '23 6 * * 1', document.dig('schedules', 0, 'cron')
    assert_equal ['master'], document.dig('schedules', 0, 'branches', 'include')

    raw_jobs = document.fetch('jobs')
    assert_shared_job_reference([raw_jobs.fetch(1)], path)
    jobs = pipeline(path).fetch('jobs')
    assert_equal %w[DotNetTests Selenium DependencyAudit], jobs.map { |job| job['job'] }
    assert_equal 'Default', jobs.first.dig('pool', 'name')
    assert_equal 'Default', jobs.last.dig('pool', 'name')
    assert_equal ['DotNetTests'], jobs[1]['dependsOn']
    assert_match(/succeededOrFailed/, jobs[1]['condition'])
  end

  def test_pr_validation_runs_all_test_projects_and_publishes_coverage
    job = pipeline('azure-pipeline-selenium-ui-tests.yml').fetch('jobs').find { |entry| entry['job'] == 'DotNetTests' }
    refute_nil job
    test_script = job.fetch('steps').find { |step| step['displayName'] == 'Run all five test projects and collect coverage' }.fetch('bash')
    %w[
      BancoAlimentar.AlimentaEstaIdeia.Repository.Tests/BancoAlimentar.AlimentaEstaIdeia.Repository.Tests.csproj
      BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tests/BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tests.csproj
      BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests/BancoAlimentar.AlimentaEstaldeia.Web.Integration.Tests.csproj
      BancoAlimentar.AlimentaEstaIdeia.Function.Tests/BancoAlimentar.AlimentaEstaIdeia.Function.Tests.csproj
      BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Tests/BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Tests.csproj
    ].each { |project| assert_includes test_script, project }
    assert_match(/test_failure=1/, test_script, 'A failed test project must fail the validation job')
    assert_match(/XPlat Code Coverage/, test_script)
    assert job.fetch('steps').any? { |step| step['task'] == 'PublishTestResults@2' }
    assert job.fetch('steps').any? { |step| step['task'] == 'PublishCodeCoverageResults@2' }
    assert job.fetch('steps').any? { |step| step['task'] == 'PublishPipelineArtifact@1' }
  end

  def test_dependency_audit_runs_for_schedule_and_manual_queue
    jobs = pipeline('azure-pipeline-selenium-ui-tests.yml').fetch('jobs')
    audit = jobs.find { |entry| entry['job'] == 'DependencyAudit' }
    refute_nil audit
    assert_match(/Build.Reason.*Schedule/, audit['condition'])
    assert_match(/Build.Reason.*Manual/, audit['condition'])
    scripts = audit.fetch('steps').filter_map { |step| step['bash'] }
    assert scripts.any? { |source| source.include?('dotnet list') && source.include?('--vulnerable') }
    assert scripts.any? { |source| source.include?('dotnet list') && source.include?('--outdated') }
    assert scripts.all? { |source| !source.include?('Tools.Tests') }, 'Audit must not reference the removed standalone Tools.Tests project'
    assert_equal 'Default', audit.dig('pool', 'name')
  end

  def test_github_actions_build_and_audit_workflows_are_removed
    refute File.exist?(File.join(ROOT, '.github/workflows/alimentestaideia.yaml'))
    refute File.exist?(File.join(ROOT, '.github/workflows/dependency-audit.yml'))
  end

  def test_all_azure_yaml_jobs_use_the_default_pool
    paths = Dir.glob('azure-pipelines/**/*.yml', base: ROOT) + ['azure-pipeline-selenium-ui-tests.yml']
    paths.each do |path|
      pools = hashes(read_yaml(path)).select { |entry| entry.key?('pool') }.map { |entry| entry.fetch('pool') }
      next if pools.empty? && path.end_with?('templates/report-disk-usage.yml')

      refute_empty pools, "#{path} must declare its agent pool"
      pools.each do |pool|
        assert_equal 'Default', pool['name'], "#{path} must use the Default pool"
        refute pool.key?('vmImage'), "#{path} must not request a Microsoft-hosted image"
      end
    end
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
    assert_equal 'Default', job.dig('pool', 'name')
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
