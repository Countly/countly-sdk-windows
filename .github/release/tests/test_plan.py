import tempfile
import unittest
from pathlib import Path

from plan import PlanError, checkout_tags, for_target, plan_for_tag, plan_from_json
from tests.support import repository_config, write_core, write_ui


class PlanTest(unittest.TestCase):
    """Mapping tags and targets to plans."""

    def setUp(self):
        """The repository configuration."""
        self.config = repository_config()

    def test_the_plain_tag_releases_the_core_and_the_ui_tag_releases_the_ui(self):
        """The plain tag releases the core and the ui tag releases the ui."""
        core = plan_for_tag(self.config, "26.9.0")
        ui = plan_for_tag(self.config, "26.9.0-ui")
        self.assertEqual(([package.id for package in core.packages], core.version, core.branch), (["Countly"], "26.9.0", "staging"))
        self.assertEqual(([package.id for package in ui.packages], ui.version, ui.prerelease), (["Countly.UI.WebView2"], "26.9.0", False))

    def test_a_release_candidate_is_a_prerelease_whose_file_version_drops_the_suffix(self):
        """A release candidate is a prerelease whose file version drops the suffix."""
        for tag, version in (("26.9.0-rc2", "26.9.0-rc2"), ("26.9.0-rc2-ui", "26.9.0-rc2")):
            with self.subTest(tag=tag):
                plan = plan_for_tag(self.config, tag)
                self.assertEqual((plan.version, plan.prerelease, plan.numeric_version, plan.file_version), (version, True, "26.9.0", "26.9.0.0"))

    def test_tags_outside_the_grammar_are_refused(self):
        """Tags outside the grammar are refused."""
        for tag in ("v26.9.0", "26.9", "ui-26.9.0", "26.9.0-beta1", "26.9.0-ui-rc1", "٢٦.9.0"):
            with self.subTest(tag=tag), self.assertRaises(PlanError):
                plan_for_tag(self.config, tag)

    def test_a_test_target_gets_a_unique_prerelease_version_and_production_keeps_the_release_version(self):
        """A test target gets a unique prerelease version and production keeps the release version."""
        plan = plan_for_tag(self.config, "26.9.0")
        self.assertEqual(for_target(plan, "test", "42").package_version, "26.9.0-test.42")
        self.assertEqual(for_target(plan_for_tag(self.config, "26.9.0-rc1"), "test", "42").package_version, "26.9.0-rc1.test.42")
        self.assertEqual(for_target(plan, "production").package_version, "26.9.0")
        self.assertEqual(for_target(plan, "dry-run").package_version, "26.9.0")

    def test_a_test_target_needs_a_run_number_and_unknown_targets_are_refused(self):
        """A test target needs a run number and unknown targets are refused."""
        plan = plan_for_tag(self.config, "26.9.0")
        for target, run in (("test", ""), ("test", "4a"), ("staging", "1")):
            with self.subTest(target=target, run=run), self.assertRaises(PlanError):
                for_target(plan, target, run)

    def test_the_plan_survives_the_trip_through_plan_json(self):
        """The plan survives the trip through plan json."""
        plan = for_target(plan_for_tag(self.config, "26.9.0-rc1-ui"), "test", "7")
        self.assertEqual(plan_from_json(self.config, plan.to_json()), plan)

    def test_checkout_tags_read_the_nuspec_for_the_core_and_the_project_for_the_ui(self):
        """Checkout tags read the nuspec for the core and the project for the ui."""
        root = Path(tempfile.mkdtemp())
        write_core(root, "26.9.0", "## 26.9.0\n")
        write_ui(root, "26.8.0", "## 26.8.0\n")
        self.assertEqual(checkout_tags(self.config, root, "staging"), ["26.9.0", "26.8.0-ui"])
        self.assertEqual(checkout_tags(self.config, root, "master"), [])

    def test_an_unreadable_version_stops_the_plan(self):
        """An unreadable version stops the plan."""
        with self.assertRaises(PlanError):
            checkout_tags(self.config, Path(tempfile.mkdtemp()), "staging")
