# frozen_string_literal: true

# Exercise the read-only CLI through its public process interface. Fixtures are
# generated locally so no gems, .NET build, datastore, or network is required.
require 'minitest/autorun'
require 'open3'
require 'tmpdir'
require 'fileutils'
require 'digest'
require 'cgi'

class TestResultReportingTest < Minitest::Test
  ROOT = File.expand_path('../../..', __dir__)
  VERIFIER = File.join(ROOT, 'scripts/ci/verify-test-results.py')
  PRIVATE_TEXT = 'PRIVATE_SENTINEL_token=super-secret-value'.freeze
  NAMESPACE = 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'

  def setup
    @directory = Dir.mktmpdir('trx-report-policy-')
  end

  def teardown
    FileUtils.remove_entry(@directory) if @directory && File.directory?(@directory)
  end

  def rows(count = 109)
    Array.new(count) do |index|
      {
        name: "#{PRIVATE_TEXT}.scenario_#{index}",
        test_id: format('00000000-0000-0000-0000-%012d', index + 1),
        execution_id: format('10000000-0000-0000-0000-%012d', index + 1),
        outcome: 'Passed'
      }
    end
  end

  def escape(value)
    CGI.escapeHTML(value.to_s)
  end

  def fixture(name = 'report.trx', results: rows, counters: {}, summary: 'Completed')
    counts = {
      total: results.length, executed: results.count { |row| row[:outcome] != 'NotExecuted' },
      passed: results.count { |row| row[:outcome] == 'Passed' },
      failed: results.count { |row| row[:outcome] == 'Failed' },
      error: 0, timeout: 0, aborted: 0, inconclusive: 0, passedButRunAborted: 0,
      notRunnable: 0, notExecuted: results.count { |row| row[:outcome] == 'NotExecuted' },
      disconnected: 0, warning: 0, completed: 0, inProgress: 0, pending: 0
    }.merge(counters)
    result_xml = results.map do |row|
      <<~XML
        <UnitTestResult executionId="#{escape(row[:execution_id])}" testId="#{escape(row[:test_id])}"
          testName="#{escape(row[:name])}" outcome="#{escape(row[:outcome])}" computerName="localhost"
          duration="00:00:00.0010000" startTime="2026-10-27T09:00:00Z" endTime="2026-10-27T09:00:00.001Z"
          testType="13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b" testListId="19431567-8539-422a-85d7-44ee4e166bda"
          relativeResultsDirectory="#{escape(row[:execution_id])}">
          <Output><StdOut>#{PRIVATE_TEXT}</StdOut></Output>
        </UnitTestResult>
      XML
    end.join
    definitions = results.map do |row|
      <<~XML
        <UnitTest name="#{escape(row[:name])}" id="#{escape(row[:test_id])}" storage="offline-tests.dll">
          <Execution id="#{escape(row[:execution_id])}" />
          <TestMethod codeBase="offline-tests.dll" adapterTypeName="executor://xunit/VsTestRunner2/netcore/"
            className="OfflineTests" name="Scenario" />
        </UnitTest>
      XML
    end.join
    entries = results.map do |row|
      "<TestEntry testId=\"#{escape(row[:test_id])}\" executionId=\"#{escape(row[:execution_id])}\" testListId=\"19431567-8539-422a-85d7-44ee4e166bda\" />"
    end.join
    document = <<~XML
      <?xml version="1.0" encoding="utf-8"?>
      <TestRun id="20000000-0000-0000-0000-000000000001" name="Offline checks" xmlns="#{NAMESPACE}">
        <Times creation="2026-10-27T09:00:00Z" queuing="2026-10-27T09:00:00Z" start="2026-10-27T09:00:00Z" finish="2026-10-27T09:01:00Z" />
        <Results>#{result_xml}</Results>
        <TestDefinitions>#{definitions}</TestDefinitions>
        <TestEntries>#{entries}</TestEntries>
        <TestLists><TestList name="All Loaded Results" id="19431567-8539-422a-85d7-44ee4e166bda" /></TestLists>
        <ResultSummary outcome="#{summary}">
          <Counters #{counts.map { |key, value| "#{key}=\"#{escape(value)}\"" }.join(' ')} />
        </ResultSummary>
      </TestRun>
    XML
    path = File.join(@directory, name)
    File.write(path, document)
    path
  end

  def run_verifier(*paths)
    # Include an unrelated sentinel report to prove that explicit paths, rather
    # than directory globs, determine which files are inspected and modified.
    before = Dir.glob(File.join(@directory, '*')).select { |path| File.file?(path) }
                .each_with_object({}) { |path, checksums| checksums[path] = Digest::SHA256.file(path).hexdigest }
    stdout, stderr, status = Open3.capture3('python3', VERIFIER, *paths, chdir: ROOT)
    before.each do |path, checksum|
      assert File.file?(path), 'Verifier removed an input file'
      assert_equal checksum, Digest::SHA256.file(path).hexdigest, 'Verifier modified an input file'
    end
    assert_equal before.keys.sort, Dir.glob(File.join(@directory, '*')).select { |path| File.file?(path) }.sort,
                 'Verifier must not create rewritten reports or sidecars'
    [stdout, stderr].each do |output|
      refute_includes output, PRIVATE_TEXT, 'Verifier output must not disclose test names or diagnostic payloads'
      refute_includes output, 'super-secret-value'
    end
    [stdout, stderr, status]
  end

  def assert_accepted(*paths)
    stdout, stderr, status = run_verifier(*paths)
    assert_equal 0, status.exitstatus, "Expected complete passing reports to succeed. stdout=#{stdout.inspect} stderr=#{stderr.inspect}"
  end

  def assert_rejected(*paths)
    stdout, stderr, status = run_verifier(*paths)
    assert_equal 1, status.exitstatus, "Expected invalid reports to fail with exit 1. stdout=#{stdout.inspect} stderr=#{stderr.inspect}"
    refute_empty stdout + stderr, 'Failure should provide a sanitized diagnostic'
  end

  def test_accepts_109_uniquely_labeled_passing_results
    assert_accepted fixture
  end

  def test_accepts_multiple_explicit_complete_reports_without_globbing_neighbors
    fixture('unrelated-invalid.trx', results: [])
    assert_accepted fixture('offline.trx'), fixture('local.trx', results: rows(60))
  end

  def test_rejects_the_109_rows_102_display_names_regression
    results = rows
    results.last(8).each { |row| row[:name] = "#{PRIVATE_TEXT}.identical_truncated_url" }
    assert_equal 109, results.length
    assert_equal 102, results.map { |row| row[:name] }.uniq.length
    assert_equal 109, results.map { |row| row[:test_id] }.uniq.length
    assert_equal 109, results.map { |row| row[:execution_id] }.uniq.length
    assert_rejected fixture(results: results)
  end

  def test_rejects_missing_path_arguments
    assert_rejected
  end

  def test_rejects_missing_file
    assert_rejected File.join(@directory, 'missing.trx')
  end

  def test_failure_diagnostics_do_not_echo_a_secret_in_the_input_path
    assert_rejected File.join(@directory, "#{PRIVATE_TEXT}.trx")
  end

  def test_rejects_a_directory_instead_of_a_report
    assert_rejected @directory
  end

  def test_rejects_empty_and_malformed_files_without_echoing_private_content
    ['', "<#{PRIVATE_TEXT}", "<TestRun xmlns=\"#{NAMESPACE}\"><Results>#{PRIVATE_TEXT}</TestRun>"].each_with_index do |content, index|
      path = File.join(@directory, "malformed-#{index}.trx")
      File.write(path, content)
      assert_rejected path
    end
  end

  def test_rejects_zero_result_rows_even_when_counters_agree
    assert_rejected fixture(results: [])
  end

  %w[Failed NotExecuted Skipped Canceled Cancelled Aborted Error Timeout Inconclusive Unknown].each do |outcome|
    define_method("test_rejects_#{outcome.downcase}_outcome_without_disclosing_diagnostics") do
      results = rows
      results.first[:outcome] = outcome
      assert_rejected fixture(results: results, summary: outcome == 'Failed' ? 'Failed' : 'Completed')
    end
  end

  %i[total executed passed failed notExecuted].each do |counter|
    define_method("test_rejects_mismatched_#{counter}_counter") do
      assert_rejected fixture(counters: { counter => counter == :failed || counter == :notExecuted ? 1 : 108 })
    end
  end

  %i[name test_id execution_id].each do |attribute|
    define_method("test_rejects_duplicate_#{attribute}") do
      results = rows
      results.last[attribute] = results.first.fetch(attribute)
      assert_rejected fixture(results: results)
    end
    define_method("test_rejects_missing_#{attribute}") do
      results = rows
      results.last[attribute] = ''
      assert_rejected fixture(results: results)
    end
  end

  def test_rejects_absent_or_malformed_counters
    [:missing, :non_numeric, :negative, :missing_total].each do |variant|
      path = fixture("counters-#{variant}.trx")
      xml = File.read(path)
      xml = case variant
            when :missing then xml.sub(/<Counters[^>]*\/>/, '')
            when :non_numeric then xml.sub('total="109"', 'total="invalid"')
            when :negative then xml.sub('total="109"', 'total="-1"')
            when :missing_total then xml.sub('total="109"', '')
            end
      File.write(path, xml)
      assert_rejected path
    end
  end

  %i[error timeout aborted inconclusive notRunnable warning inProgress pending].each do |counter|
    define_method("test_rejects_nonzero_#{counter}_counter_even_when_result_rows_pass") do
      assert_rejected fixture(counters: { counter => 1 })
    end
  end

  # Keep the result rows and counters unchanged while corrupting only the
  # definition/entry metadata. These substitutions target our generated XML,
  # never parse production TRX using regular expressions.
  {
    missing_definitions: ['TestDefinitions', ->(_body) { '' }],
    missing_definition: ['TestDefinitions', ->(body) { body.sub(/<UnitTest\b.*?<\/UnitTest>/m, '') }],
    extra_definition: ['TestDefinitions', ->(body) { body + body[/<UnitTest\b.*?<\/UnitTest>/m] }],
    wrong_definition_test_id: ['TestDefinitions', ->(body) { body.sub('00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-999999999999') }],
    duplicate_definition_test_id: ['TestDefinitions', ->(body) { body.sub('00000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000001') }],
    wrong_definition_execution_id: ['TestDefinitions', ->(body) { body.sub('10000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-999999999999') }],
    missing_definition_execution: ['TestDefinitions', ->(body) { body.sub(/<Execution\b[^>]*\/>/, '') }],
    missing_entries: ['TestEntries', ->(_body) { '' }],
    missing_entry: ['TestEntries', ->(body) { body.sub(/<TestEntry\b[^>]*\/>/, '') }],
    extra_entry: ['TestEntries', ->(body) { body + body[/<TestEntry\b[^>]*\/>/] }],
    wrong_entry_test_id: ['TestEntries', ->(body) { body.sub('00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-999999999999') }],
    wrong_entry_execution_id: ['TestEntries', ->(body) { body.sub('10000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-999999999999') }]
  }.each do |scenario, (element, change)|
    define_method("test_rejects_#{scenario}_despite_passing_results_and_correct_counters") do
      path = fixture
      document = File.read(path).sub(/(<#{element}>)(.*?)(<\/#{element}>)/m) do
        opening, body, closing = Regexp.last_match.captures
        opening + change.call(body) + closing
      end
      File.write(path, document)
      assert_rejected path
    end
  end

  def test_rejects_incomplete_or_failed_summary_even_when_result_rows_pass
    %w[Failed Aborted Canceled InProgress].each do |summary|
      assert_rejected fixture("summary-#{summary}.trx", summary: summary)
    end
  end

  def test_any_invalid_report_fails_a_multi_file_invocation_in_either_order
    valid = fixture('valid.trx')
    invalid = fixture('invalid.trx', counters: { total: 108 })
    assert_rejected valid, invalid
    assert_rejected invalid, valid
    assert_rejected valid, File.join(@directory, 'missing.trx')
  end

  def test_verification_succeeds_with_read_only_input_and_preserves_its_checksum
    path = fixture
    File.chmod(0444, path)
    assert_accepted path
  end
end
