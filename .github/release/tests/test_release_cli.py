import json
import os
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

from tests.support import make_assembly, make_package, write_core, write_ui

RELEASE_DIR = Path(__file__).resolve().parents[1]


def run_cli(script, *args, output=""):
    """Runs release.py like the workflow does; GitHub's step output file is written only when `output` names one."""
    env = dict(os.environ, GITHUB_OUTPUT=output, GITHUB_STEP_SUMMARY="")
    return subprocess.run([sys.executable, str(script), *args], capture_output=True, text=True, env=env)


class ReleaseCliTest(unittest.TestCase):
    """release.py run the way the workflows run it."""

    def setUp(self):
        """A copy of the release tooling in an empty fake checkout."""
        self.folder = Path(tempfile.mkdtemp())
        self.root = self.folder / "repo"
        shutil.copytree(RELEASE_DIR, self.root / ".github" / "release", ignore=shutil.ignore_patterns("__pycache__", "tests"))
        self.script = self.root / ".github" / "release" / "release.py"

    def outputs(self, path):
        """The step outputs written to a GITHUB_OUTPUT file."""
        return dict(line.split("=", 1) for line in path.read_text(encoding="utf-8").splitlines())

    def test_plan_writes_the_environment_and_the_login_endpoints_of_each_target(self):
        """Plan writes the environment and the login endpoints of each target."""
        cases = [("production", "nuget-release", "https://www.nuget.org/api/v2/token", "true"), ("test", "nuget-test", "https://int.nugettest.org/api/v2/token", "false"),
                 ("dry-run", "", "", "false")]
        for target, environment, token_service, announce in cases:
            with self.subTest(target=target):
                output = self.folder / f"output-{target}"
                output.write_bytes(b"")
                result = run_cli(self.script, "plan", "--tag", "26.9.0", "--target", target, "--run", "12", "--prerelease", "false", "--out", str(self.folder / "plan.json"), output=str(output))
                self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
                self.assertEqual((self.outputs(output)["environment"], self.outputs(output)["token_service"], self.outputs(output)["announce"]), (environment, token_service, announce))

    def test_a_test_run_without_a_tag_rehearses_the_core_version_of_the_checkout(self):
        """A test run without a tag rehearses the core version of the checkout."""
        write_core(self.root, "26.9.0", "## 26.9.0\n")
        write_ui(self.root, "26.8.0", "## 26.8.0\n")
        result = run_cli(self.script, "plan", "--target", "test", "--run", "3", "--out", str(self.folder / "plan.json"))
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertEqual(json.loads((self.folder / "plan.json").read_text(encoding="utf-8"))["packageVersion"], "26.9.0-test.3")
        refused = run_cli(self.script, "plan", "--target", "production", "--out", str(self.folder / "plan.json"))
        self.assertEqual(refused.returncode, 1)
        self.assertIn("::error::a production release needs the tag it publishes", refused.stdout)

    def test_a_production_tag_must_match_the_github_release_type(self):
        """A production tag must match the github release type."""
        result = run_cli(self.script, "plan", "--tag", "26.9.0-rc1", "--target", "production", "--prerelease", "false", "--out", str(self.folder / "plan.json"))
        self.assertEqual(result.returncode, 1)
        self.assertIn("must be published as a GitHub pre-release", result.stdout)

    def test_stage_accepts_only_the_planned_packages_and_writes_the_manifest(self):
        """Stage accepts only the planned packages and writes the manifest."""
        run_cli(self.script, "plan", "--tag", "26.9.0", "--target", "dry-run", "--out", str(self.folder / "plan.json"))
        staging = self.folder / "staged"
        make_package(staging / "Countly.26.9.0.nupkg", "Countly", "26.9.0", {"lib/net45/Countly.dll": make_assembly()})
        result = run_cli(self.script, "stage", "--plan", str(self.folder / "plan.json"), "--staging", str(staging), "--commit", "c" * 40, "--manifest", str(self.folder / "manifest.json"))
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertEqual([entry["path"] for entry in json.loads((self.folder / "manifest.json").read_text(encoding="utf-8"))["files"]], ["Countly.26.9.0.nupkg"])
        make_package(staging / "Countly.26.8.0.nupkg", "Countly", "26.8.0", {})
        stale = run_cli(self.script, "stage", "--plan", str(self.folder / "plan.json"), "--staging", str(staging), "--manifest", str(self.folder / "manifest.json"))
        self.assertEqual(stale.returncode, 1)
        self.assertIn("::error::unexpected Countly.26.8.0.nupkg", stale.stdout)

    def test_a_dry_run_plan_has_no_feed_to_check_push_or_verify(self):
        """A dry run plan has no feed to check push or verify."""
        run_cli(self.script, "plan", "--tag", "26.9.0", "--target", "dry-run", "--out", str(self.folder / "plan.json"))
        for command in (["check-absent"], ["push", "--staging", str(self.folder), "--api-key-env", "UNSET_KEY"], ["verify-public", "--staging", str(self.folder)]):
            with self.subTest(command=command[0]):
                result = run_cli(self.script, command[0], "--plan", str(self.folder / "plan.json"), *command[1:])
                self.assertEqual(result.returncode, 1)
                self.assertIn("::error::the dry-run target publishes nowhere", result.stdout)

    def test_signing_refuses_an_empty_or_malformed_key_without_printing_it(self):
        """Signing refuses an empty or malformed key without printing it."""
        run_cli(self.script, "plan", "--tag", "26.9.0", "--target", "dry-run", "--out", str(self.folder / "plan.json"))
        for value, message in (("", "STRONG_NAME_KEY is empty"), ("not base64!", "STRONG_NAME_KEY is not valid base64")):
            with self.subTest(message=message):
                env = dict(os.environ, STRONG_NAME_KEY=value, GITHUB_STEP_SUMMARY="")
                result = subprocess.run([sys.executable, str(self.script), "sign", "--plan", str(self.folder / "plan.json"), "--staging", str(self.folder),
                                         "--key-env", "STRONG_NAME_KEY", "--manifest", str(self.folder / "signed.json")], capture_output=True, text=True, env=env)
                self.assertEqual(result.returncode, 1)
                self.assertIn(message, result.stdout)
                if value:
                    self.assertNotIn(value, result.stdout + result.stderr)

    def test_a_key_pasted_with_line_breaks_decodes(self):
        """A key pasted with line breaks decodes, and fails only later for not being the Countly key."""
        run_cli(self.script, "plan", "--tag", "26.9.0", "--target", "dry-run", "--out", str(self.folder / "plan.json"))
        env = dict(os.environ, STRONG_NAME_KEY="AAEC\nAwQF\n", GITHUB_STEP_SUMMARY="")
        result = subprocess.run([sys.executable, str(self.script), "sign", "--plan", str(self.folder / "plan.json"), "--staging", str(self.folder),
                                 "--key-env", "STRONG_NAME_KEY", "--manifest", str(self.folder / "signed.json")], capture_output=True, text=True, env=env)
        self.assertEqual(result.returncode, 1)
        self.assertNotIn("not valid base64", result.stdout)
