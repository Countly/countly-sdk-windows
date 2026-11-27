import subprocess
import unittest

from checks import branch_problems, environment_problems, missing_checks


def runs(*pairs):
    """A check-runs API response from (name, conclusion) pairs."""
    return {"check_runs": [{"name": name, "conclusion": conclusion} for name, conclusion in pairs]}


class ChecksTest(unittest.TestCase):
    """The release branch, required checks and publish environment checks."""

    def test_a_required_check_must_have_runs_and_all_of_them_must_succeed(self):
        """A required check must have runs and all of them must succeed."""
        response = runs(("Build", "success"), ("Build", "success"), ("Tests", "success"), ("Tests", "failure"), ("Lint", None))
        self.assertEqual(missing_checks(response, ["Build", "Tests", "Lint", "Analyze"]), ["Tests", "Lint", "Analyze"])

    def test_a_commit_off_the_release_branch_is_refused(self):
        """A commit off the release branch is refused."""
        def run(args, **kwargs):
            """Answers merge-base --is-ancestor with "no"."""
            return subprocess.CompletedProcess(args, 1)
        self.assertEqual(branch_problems("abc", "staging", ".", run), ["abc is not on origin/staging; the tag must point to that branch"])
        self.assertEqual(branch_problems("abc", "staging", ".", lambda args, **kwargs: subprocess.CompletedProcess(args, 0)), [])

    def test_the_publish_environment_needs_reviewers_and_selected_refs(self):
        """The publish environment needs reviewers and selected refs."""
        protected = {"name": "nuget-release", "protection_rules": [{"type": "required_reviewers", "reviewers": [{"type": "User"}]}],
                     "deployment_branch_policy": {"protected_branches": False, "custom_branch_policies": True}}
        self.assertEqual(environment_problems(protected), [])
        self.assertEqual(environment_problems({"name": "nuget-release", "protection_rules": [], "deployment_branch_policy": None}), [
            "environment nuget-release does not require a reviewer to approve the publish job",
            "environment nuget-release does not limit deployments to selected branches and tags",
        ])
