"""Maps a release tag to the NuGet packages it publishes, using the repository's .github/release/config.json."""

import dataclasses
import json
import re
from dataclasses import dataclass
from pathlib import Path
from typing import Tuple

FINAL_VERSION = r"\d+\.\d+\.\d+"
RELEASE_VERSION = FINAL_VERSION + r"(?:-rc\d+)?"
RC_PATTERN = re.compile(r"-rc\d+$")
CONFIG_PATH = Path(__file__).resolve().parent / "config.json"
TARGETS = ("production", "test", "dry-run")


class PlanError(Exception):
    """A tag or repository state that must stop the release."""


@dataclass(frozen=True)
class Package:
    """One NuGet package of the repository."""

    key: str
    id: str
    contract: str
    consumer: str

    def file_name(self, version):
        """Name of the package file, e.g. Countly.26.8.1.nupkg."""
        return f"{self.id}.{version}.nupkg"


@dataclass(frozen=True)
class Plan:
    """What one tag publishes, and where. The package version is the release version, except on the test target,
    where each run publishes a copy with its own prerelease version because a NuGet feed never takes a version twice."""

    tag: str
    version: str
    prerelease: bool
    branch: str
    packages: Tuple[Package, ...]
    target: str = "dry-run"
    package_version: str = ""

    @property
    def numeric_version(self):
        """The version without its -rcN suffix, the part an assembly file version can hold."""
        return RC_PATTERN.sub("", self.version)

    @property
    def file_version(self):
        """The assembly file version of the release, e.g. 26.8.1.0."""
        return f"{self.numeric_version}.0"

    def to_json(self):
        """Serializable form, handed between workflow jobs as plan.json."""
        return {
            "tag": self.tag, "version": self.version, "prerelease": self.prerelease, "branch": self.branch,
            "packages": [package.key for package in self.packages], "target": self.target, "packageVersion": self.package_version,
        }


def load_config(path=CONFIG_PATH):
    """Reads config.json."""
    return json.loads(Path(path).read_text(encoding="utf-8"))


def package_from_config(config, key):
    """Builds the Package described by config['packages'][key]."""
    spec = config["packages"][key]
    return Package(key, spec["id"], spec["contract"], spec["consumer"])


def plan_for_tag(config, tag):
    """Maps a tag to its plan for a dry run; raises PlanError for a tag outside the repository's grammar (ASCII digits only)."""
    for rule in config["tagRules"]:
        prefix, suffix = rule["tag"].split("{version}")
        match = re.fullmatch(f"{re.escape(prefix)}(?P<version>{RELEASE_VERSION}){re.escape(suffix)}", tag, re.ASCII)
        if match:
            version = match.group("version")
            packages = tuple(package_from_config(config, key) for key in rule["packages"])
            return Plan(tag, version, bool(RC_PATTERN.search(version)), rule["branch"], packages, "dry-run", version)
    raise PlanError(f'"{tag}" is not a release tag of {config["repository"]}')


def for_target(plan, target, run_number=""):
    """The plan published to a target. A test run publishes the release as {version}-test.{run}, or {version}.test.{run}
    for a release candidate, so every rehearsal gets a version the test feed has not seen; production and dry runs keep
    the release version."""
    if target not in TARGETS:
        raise PlanError(f"unknown target {target}; expected one of {', '.join(TARGETS)}")
    package_version = plan.version
    if target == "test":
        if not re.fullmatch(r"\d+", str(run_number), re.ASCII):
            raise PlanError("a test release needs the workflow run number, which makes its package version unique")
        separator = "." if "-" in plan.version else "-"
        package_version = f"{plan.version}{separator}test.{run_number}"
    return dataclasses.replace(plan, target=target, package_version=package_version)


def checkout_tags(config, repo_root, branch):
    """Tags the checkout would be released as from the branch: one per tag rule of that branch, carrying the version
    held by the first version source of the rule's first package. Raises PlanError when that version is unreadable."""
    tags = []
    for rule in config["tagRules"]:
        if rule["branch"] != branch:
            continue
        source = config["packages"][rule["packages"][0]]["versionSources"][0]
        path = Path(repo_root) / source["file"]
        match = re.search(source["regex"], path.read_text(encoding="utf-8-sig"), re.MULTILINE) if path.is_file() else None
        if match is None:
            raise PlanError(f"{source['file']}: no version found")
        tags.append(rule["tag"].format(version=match.group(1).strip()))
    return tags


def plan_from_json(config, data):
    """Rebuilds a plan written by Plan.to_json."""
    packages = tuple(package_from_config(config, key) for key in data["packages"])
    return Plan(data["tag"], data["version"], data["prerelease"], data["branch"], packages, data["target"], data["packageVersion"])
