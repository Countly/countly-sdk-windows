import subprocess
import tempfile
import unittest
from pathlib import Path

from pending import merge_problems, pending_releases
from tests.support import repository_config, write_core, write_ui

HEAD = "a" * 40
MERGE = "b" * 40
RELEASED = {"26.8.0", "26.8.0-ui"}


def git_runner(contains_head, trees):
    """A stand-in for subprocess.run answering the two git questions of merge_problems: whether the merge contains the
    head, and the tree of each commit."""
    def run(args, **kwargs):
        """Answers merge-base and rev-parse."""
        if "merge-base" in args:
            return subprocess.CompletedProcess(args, 0 if contains_head else 1)
        return subprocess.CompletedProcess(args, 0, stdout="".join(trees[arg.split("^")[0]] + "\n" for arg in args[-2:]))
    return run


class PendingTest(unittest.TestCase):
    """The releases a staging to master merge creates."""

    def setUp(self):
        """The configuration and an empty fake checkout."""
        self.config = repository_config()
        self.root = Path(tempfile.mkdtemp())

    def test_new_versions_are_released_core_first_with_their_changelog_sections_and_only_the_core_is_latest(self):
        """New versions are released core first with their changelog sections and only the core is latest."""
        write_core(self.root, "26.9.0", "## 26.9.0\n\n* Core change.\n\n## 26.8.0\n* Old.\n")
        write_ui(self.root, "26.9.0", "## 26.9.0\n* UI change.\n")
        releases, problems = pending_releases(self.config, self.root, "staging", RELEASED)
        self.assertEqual(problems, [])
        self.assertEqual([(release.tag, release.latest, release.prerelease) for release in releases], [("26.9.0", True, False), ("26.9.0-ui", False, False)])
        self.assertEqual([release.notes for release in releases], ["* Core change.\n", "* UI change.\n"])

    def test_tagged_versions_are_skipped_so_an_unchanged_ui_releases_nothing(self):
        """Tagged versions are skipped so an unchanged ui releases nothing."""
        write_core(self.root, "26.9.0", "## 26.9.0\n* Core change.\n")
        write_ui(self.root, "26.8.0", "## 26.8.0\n* Old UI.\n")
        releases, problems = pending_releases(self.config, self.root, "staging", RELEASED)
        self.assertEqual(([release.tag for release in releases], problems), (["26.9.0"], []))
        self.assertEqual(pending_releases(self.config, self.root, "staging", RELEASED | {"26.9.0"}), ([], []))

    def test_a_straggler_stops_every_release_of_the_merge(self):
        """A straggler stops every release of the merge."""
        write_core(self.root, "26.9.0", "## 26.9.0\n* Core change.\n", sdk="26.8.0")
        write_ui(self.root, "26.9.0", "## 26.8.0\n")
        _, problems = pending_releases(self.config, self.root, "staging", RELEASED)
        self.assertEqual(problems, [
            "countlyCommon/countlyCommon/CountlyBase.cs: version is 26.8.0, the tag says 26.9.0",
            "ui/Countly.UI.WebView2/CHANGELOG.md: no '## 26.9.0' heading",
        ])

    def test_the_merge_must_contain_the_head_and_hold_its_tree(self):
        """The merge must contain the head and hold its tree."""
        self.assertEqual(merge_problems(HEAD, MERGE, self.root, git_runner(True, {HEAD: "t1", MERGE: "t1"})), [])
        self.assertEqual(merge_problems(HEAD, MERGE, self.root, git_runner(False, {})), [
            f"{MERGE} does not contain {HEAD}; merge the release PR with a merge commit, not a squash or a rebase"])
        self.assertEqual(merge_problems(HEAD, MERGE, self.root, git_runner(True, {HEAD: "t1", MERGE: "t2"})), [
            "master holds changes staging lacks; merge master into staging, then update the release PR"])
