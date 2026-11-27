"""What a release PR from staging into master releases: one GitHub release per tag rule of staging whose version has
no tag yet, on the PR's head commit, once the merge is checked to hold exactly that commit."""

import subprocess
from dataclasses import dataclass

import checks
import sources
from plan import checkout_tags, plan_for_tag


@dataclass(frozen=True)
class PendingRelease:
    """One GitHub release the merge creates."""

    tag: str
    prerelease: bool
    latest: bool
    notes: str

    def to_json(self):
        """Serializable form, written to pending.json for the workflow; the notes go to a file of their own."""
        return {"tag": self.tag, "prerelease": self.prerelease, "latest": self.latest}


def merge_problems(head, merge, repo_root, runner=subprocess.run):
    """Problems unless the merge commit contains the PR's head commit and holds exactly its tree, so the commit the
    releases are tagged on is what master holds."""
    if not checks.is_ancestor(head, merge, repo_root, runner):
        return [f"{merge} does not contain {head}; merge the release PR with a merge commit, not a squash or a rebase"]
    trees = runner(["git", "-C", str(repo_root), "rev-parse", f"{head}^{{tree}}", f"{merge}^{{tree}}"], capture_output=True, text=True, check=True).stdout.split()
    if trees[0] != trees[1]:
        return ["master holds changes staging lacks; merge master into staging, then update the release PR"]
    return []


def existing_tags(repo_root, runner=subprocess.run):
    """The tags of the checkout."""
    return set(runner(["git", "-C", str(repo_root), "tag", "--list"], capture_output=True, text=True, check=True).stdout.split())


def pending_releases(config, repo_root, branch, tags):
    """The releases the checkout makes from the branch, in tag rule order, and the problems that must stop all of them.
    A tag that exists is skipped; every other one needs its version sources and changelog ready. Only a final release of
    the plain version tag is marked latest. Raises PlanError when a version is unreadable."""
    releases = []
    problems = []
    for tag in checkout_tags(config, repo_root, branch):
        if tag in tags:
            continue
        plan = plan_for_tag(config, tag)
        notes, notes_problems = sources.release_notes(repo_root, plan, config)
        problems += sources.check_version_sources(repo_root, plan, config) + notes_problems
        releases.append(PendingRelease(tag, plan.prerelease, tag == plan.version and not plan.prerelease, notes))
    return releases, list(dict.fromkeys(problems))
