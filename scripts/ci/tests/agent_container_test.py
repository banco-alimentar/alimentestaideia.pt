from contextlib import redirect_stdout
import io
import json
import os
from pathlib import Path
import signal
import subprocess
import tarfile
import tempfile
import time
import unittest
from unittest.mock import patch


START = Path(__file__).resolve().parents[3] / "docker/azure-devops-agent/start.sh"
INSTALLER = START.with_name("install-dependencies.sh")


class AgentContainerTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.bin = self.root / "bin"
        self.bin.mkdir()
        self.events = self.root / "events"
        self.token = self.root / "pat"
        self.token.write_text("test-only-pat\n")
        self.env = {
            **os.environ,
            "PATH": f"{self.bin}:{os.environ['PATH']}",
            "AZP_URL": "https://dev.azure.com/example",
            "AZP_POOL": "test-pool",
            "AZP_AGENT_NAME": "test-agent",
            "AZP_TOKEN_FILE": str(self.token),
            "AZP_WORK": str(self.root / "work"),
            "EVENTS": str(self.events),
            "READY": str(self.root / "ready"),
            "AGENT_ARCHIVE": str(self.root / "package.tar.gz"),
        }
        self.env.pop("AZP_TOKEN", None)
        self.executable("id", 'echo "${FAKE_UID:-1001}"')
        self.executable("docker", 'exit "${DOCKER_EXIT:-0}"')
        self.executable("python3", 'echo https://example.invalid/package.tar.gz')
        self.executable("curl", 'cp "$AGENT_ARCHIVE" agent.tar.gz')
        self.executable("sleep", "/bin/sleep 0.01")
        with tarfile.open(self.env["AGENT_ARCHIVE"], "w:gz") as archive:
            self.archive_file(archive, "env.sh", "export AGENT_ENV_LOADED=yes\n")
            self.archive_file(
                archive,
                "config.sh",
                '#!/bin/bash\n'
                '[[ "$VSTS_AGENT_INPUT_TOKEN" == test-only-pat* ]] || exit 99\n'
                'printf "%s\\n" "$*" >> "$EVENTS"\n'
                'if [[ "$1" == remove ]]; then exit "${REMOVE_EXIT:-0}"; fi\n'
                'exit "${CONFIG_EXIT:-0}"\n',
            )
            self.archive_file(
                archive,
                "run.sh",
                '#!/bin/bash\n'
                'echo "run:$AGENT_ENV_LOADED:$*" >> "$EVENTS"\n'
                'if [[ "${HOLD_AGENT:-0}" == 1 ]]; then\n'
                '  trap \'echo stopped >> "$EVENTS"; exit 0\' TERM INT\n'
                '  touch "$READY"\n'
                '  while true; do /bin/sleep 0.05; done\n'
                'fi\n'
                'exit "${RUN_EXIT:-0}"\n',
            )

    def executable(self, name, body):
        path = self.bin / name
        path.write_text(f"#!/bin/bash\n{body}\n")
        path.chmod(0o755)

    @staticmethod
    def archive_file(archive, name, body):
        data = body.encode()
        info = tarfile.TarInfo(name)
        info.size = len(data)
        info.mode = 0o755
        archive.addfile(info, io.BytesIO(data))

    def run_agent(self, *args):
        return subprocess.run(
            ["bash", str(START), *args],
            cwd=self.root,
            env=self.env,
            capture_output=True,
            text=True,
            timeout=10,
        )

    def test_registration_run_once_and_removal_without_token_arguments(self):
        result = self.run_agent("--once")
        self.assertEqual(result.returncode, 0, result.stderr)
        events = self.events.read_text()
        self.assertIn("--pool test-pool --agent test-agent", events)
        self.assertIn("run:yes:--once\n", events)
        self.assertTrue(events.endswith("remove --unattended --auth pat\n"))
        self.assertNotIn("--replace", events)
        self.assertNotIn("test-only-pat", events + result.stdout + result.stderr)

    def test_listener_failure_preserves_exit_code_and_unregisters(self):
        self.env["RUN_EXIT"] = "42"
        result = self.run_agent()
        self.assertEqual(result.returncode, 42, result.stderr)
        self.assertIn("remove --unattended", self.events.read_text())

    def test_configuration_failure_attempts_cleanup_without_running(self):
        self.env["CONFIG_EXIT"] = "23"
        result = self.run_agent()
        self.assertEqual(result.returncode, 23, result.stderr)
        events = self.events.read_text()
        self.assertNotIn("run:", events)
        self.assertIn("remove --unattended", events)

    def test_failed_removal_retries_and_fails_visibly(self):
        self.env["REMOVE_EXIT"] = "1"
        result = self.run_agent()
        self.assertEqual(result.returncode, 1)
        self.assertEqual(self.events.read_text().count("remove --unattended"), 3)
        self.assertIn("remove 'test-agent' from 'test-pool' manually", result.stderr)

    def test_invalid_runtime_configuration_fails_before_download(self):
        cases = [
            ({"FAKE_UID": "0"}, "non-root"),
            ({"AZP_URL": ""}, "AZP_URL"),
            ({"AZP_POOL": ""}, "AZP_POOL"),
            ({"AZP_TOKEN_FILE": str(self.root / "missing")}, "PAT file"),
            ({"AZP_TOKEN": "do-not-use"}, "instead of exposing"),
            ({"DOCKER_EXIT": "1"}, "Cannot access Docker"),
        ]
        for overrides, message in cases:
            with self.subTest(overrides=overrides):
                original = self.env.copy()
                self.env.update(overrides)
                try:
                    result = self.run_agent()
                finally:
                    self.env = original
                self.assertNotEqual(result.returncode, 0)
                self.assertIn(message, result.stderr)
                self.assertFalse(self.events.exists())

    def test_sigterm_stops_listener_and_unregisters(self):
        self.env["HOLD_AGENT"] = "1"
        process = subprocess.Popen(
            ["bash", str(START)],
            cwd=self.root,
            env=self.env,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
        )
        try:
            deadline = time.monotonic() + 5
            while not Path(self.env["READY"]).exists():
                if process.poll() is not None or time.monotonic() > deadline:
                    self.fail("Agent did not become ready.")
                time.sleep(0.01)
            process.send_signal(signal.SIGTERM)
            _, stderr = process.communicate(timeout=5)
            self.assertEqual(process.returncode, 143, stderr)
            events = self.events.read_text()
            self.assertIn("stopped\n", events)
            self.assertIn("remove --unattended", events)
        finally:
            if process.poll() is None:
                process.kill()
            process.communicate()

    def discover_package(self, packages, **environment):
        source = START.read_text().split("<<'PY'\n", 1)[1].split("\nPY\n", 1)[0]
        response = io.BytesIO(json.dumps({"value": packages}).encode())
        output = io.StringIO()
        with patch.dict(os.environ, {**self.env, **environment}, clear=True), \
                patch("urllib.request.urlopen", return_value=response) as request, \
                redirect_stdout(output):
            exec(compile(source, str(START), "exec"), {})
        return output.getvalue().strip(), request.call_args

    def test_package_discovery_authenticates_and_selects_requested_version(self):
        url, call = self.discover_package(
            [{"downloadUrl": "https://example.invalid/agent.tar.gz"}],
            AZP_AGENT_VERSION="4.999.0",
        )
        self.assertEqual(url, "https://example.invalid/agent.tar.gz")
        request = call.args[0]
        self.assertIn("platform=linux-x64", request.full_url)
        self.assertIn("packageVersion=4.999.0", request.full_url)
        self.assertTrue(request.get_header("Authorization").startswith("Basic "))
        self.assertNotIn("test-only-pat", request.full_url)

    def test_package_discovery_rejects_missing_packages_and_insecure_urls(self):
        cases = [
            ([], {}, "No matching"),
            ([{"downloadUrl": "http://example.invalid/agent.tar.gz"}], {}, "HTTPS"),
            ([], {"AZP_URL": "http://dev.azure.com/example"}, "HTTPS"),
            ([], {"AZP_URL": "https://user:password@dev.azure.com/example"}, "credentials"),
        ]
        for packages, environment, message in cases:
            with self.subTest(environment=environment, packages=packages):
                with self.assertRaisesRegex(SystemExit, message):
                    self.discover_package(packages, **environment)


class InstallerArchitectureTests(unittest.TestCase):
    def run_installer(self, architecture):
        with tempfile.TemporaryDirectory() as temporary:
            bin_path = Path(temporary)
            for name, body in {
                "dpkg": 'echo "$TEST_ARCHITECTURE"',
                "apt-get": 'echo "apt-get called" >&2; exit 42',
            }.items():
                command = bin_path / name
                command.write_text(f"#!/bin/bash\n{body}\n")
                command.chmod(0o755)
            return subprocess.run(
                ["bash", str(INSTALLER)],
                env={
                    **os.environ,
                    "PATH": f"{bin_path}:{os.environ['PATH']}",
                    "TEST_ARCHITECTURE": architecture,
                    "TARGETARCH": "amd64",
                },
                capture_output=True,
                text=True,
                timeout=10,
            )

    def test_rejects_unsupported_architectures_before_installing_packages(self):
        for architecture in ["arm64", "i386"]:
            with self.subTest(architecture=architecture):
                result = self.run_installer(architecture)
                self.assertEqual(result.returncode, 1, result.stderr)
                self.assertIn(architecture, result.stderr)
                self.assertIn("--platform linux/amd64", result.stderr)
                self.assertNotIn("apt-get called", result.stderr)

    def test_amd64_reaches_package_installation(self):
        result = self.run_installer("amd64")
        self.assertEqual(result.returncode, 42, result.stderr)
        self.assertIn("apt-get called", result.stderr)


class ChromeDriverResolutionTests(unittest.TestCase):
    def resolve(self, manifest):
        source = INSTALLER.read_text().split("<<'PY'\n", 1)[1].split("\nPY\n", 1)[0]
        output = io.StringIO()
        with patch("sys.argv", ["-", "140.0.7339.80"]), \
                patch("builtins.open", return_value=io.StringIO(json.dumps(manifest))), \
                redirect_stdout(output):
            exec(compile(source, str(INSTALLER), "exec"), {})
        return output.getvalue().strip()

    @staticmethod
    def manifest(url):
        return {"builds": {"140.0.7339": {"downloads": {
            "chromedriver": [{"platform": "linux64", "url": url}],
        }}}}

    def test_resolves_matching_build_even_if_patch_differs(self):
        url = (
            "https://storage.googleapis.com/chrome-for-testing-public/"
            "140.0.7339.81/linux64/chromedriver-linux64.zip"
        )
        self.assertEqual(self.resolve(self.manifest(url)), url)

    def test_rejects_absent_build_and_mismatched_or_untrusted_downloads(self):
        cases = [
            ({"builds": {}}, "No ChromeDriver"),
            (self.manifest(
                "https://storage.googleapis.com/chrome-for-testing-public/"
                "141.0.7339.80/linux64/chromedriver-linux64.zip"
            ), "mismatched build"),
            (self.manifest("https://example.invalid/driver.zip"), "unexpected URL"),
        ]
        for manifest, message in cases:
            with self.subTest(manifest=manifest):
                with self.assertRaisesRegex(SystemExit, message):
                    self.resolve(manifest)


if __name__ == "__main__":
    unittest.main()
