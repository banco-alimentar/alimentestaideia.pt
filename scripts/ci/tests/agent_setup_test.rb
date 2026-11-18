#!/usr/bin/env ruby
# frozen_string_literal: true

require 'json'
require 'minitest/autorun'
require 'open3'
require 'tmpdir'

class AgentSetupTest < Minitest::Test
  SCRIPT = File.expand_path('../../setup-ubuntu-agent.sh', __dir__)
  DRIVER_URL = 'https://storage.googleapis.com/chrome-for-testing-public/140.0.7339.82/linux64/chromedriver-linux64.zip'

  def test_help_requires_no_privileges_or_host_changes
    output, error, status = Open3.capture3('bash', SCRIPT, '--help')
    assert status.success?, error
    assert_includes output, 'Ubuntu 24.04'
    assert_includes output, '--allow-docker-group'
  end

  def test_invalid_arguments_fail_before_host_changes
    [
      [[], 'Specify --agent-user'],
      [['--agent-user'], 'requires a username'],
      [['--agent-user', '--allow-docker-group'], 'requires a username'],
      [['--unknown'], 'Unknown option'],
      [['--agent-user', 'example-agent'], 'root-equivalent Docker access']
    ].each do |arguments, expected|
      _, error, status = Open3.capture3('bash', SCRIPT, *arguments)
      refute status.success?
      assert_includes error, expected
    end
  end

  def test_driver_resolution_matches_installed_chrome_build_not_latest_major
    output, error, status = resolve('140.0.7339.100', manifest)
    assert status.success?, error
    assert_equal DRIVER_URL, output.strip
  end

  def test_driver_resolution_rejects_missing_build_or_platform
    output, error, status = resolve('141.0.7339.100', manifest)
    refute status.success?
    assert_empty output
    assert_includes error, 'No ChromeDriver published'

    data = manifest
    data['builds']['140.0.7339']['downloads']['chromedriver'].first['platform'] = 'mac-arm64'
    _, error, status = resolve('140.0.7339.100', data)
    refute status.success?
    assert_includes error, 'No linux64 ChromeDriver'
  end

  def test_driver_resolution_rejects_unexpected_urls_versions_and_invalid_json
    [
      ['140.0.7339.100', manifest(DRIVER_URL.sub('storage.googleapis.com', 'example.test')), 'unexpected URL'],
      ['140.0.7339.100', manifest(DRIVER_URL.sub('140.0.7339.82', '141.0.7339.82')), 'mismatched build'],
      ['not-a-version', manifest, 'Invalid installed Chrome version'],
      ['140.0.7339.100', '{', 'JSONDecodeError']
    ].each do |version, data, expected|
      output, error, status = resolve(version, data)
      refute status.success?
      assert_empty output
      assert_includes error, expected
    end
  end

  private

  def manifest(url = DRIVER_URL)
    { 'builds' => { '140.0.7339' => { 'downloads' => { 'chromedriver' => [{ 'platform' => 'linux64', 'url' => url }] } } } }
  end

  def resolve(version, data)
    Dir.mktmpdir('agent-setup-test-') do |directory|
      path = File.join(directory, 'manifest.json')
      File.write(path, data.is_a?(String) ? data : JSON.generate(data))
      Open3.capture3('bash', '-c', 'source "$1"; resolve_chromedriver "$2" "$3"', '--', SCRIPT, version, path)
    end
  end
end
