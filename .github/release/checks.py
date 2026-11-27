"""Checks before a release: the tag commit's release branch, its required status checks and the publish environment."""

import subprocess


def is_ancestor(commit, ref, repo_root, runner=subprocess.run):
    """Whether the commit is the ref or one of its ancestors."""
    return runner(["git", "-C", str(repo_root), "merge-base", "--is-ancestor", commit, ref], capture_output=True).returncode == 0


def branch_problems(commit, branch, repo_root, runner=subprocess.run):
    """Problems unless the commit is an ancestor of origin/<branch>."""
    return [] if is_ancestor(commit, f"origin/{branch}", repo_root, runner) else [f"{commit} is not on origin/{branch}; the tag must point to that branch"]


def missing_checks(check_runs, required):
    """Required check names that did not succeed on the commit, from the GitHub check-runs API response: a name without
    any run, or with any run that did not succeed or has not finished. A commit can carry several runs of one check,
    for example one from the push to the release branch and one from a pull request whose head it is, which tests a
    merge with another branch; each of them must have passed."""
    problems = []
    for name in required:
        conclusions = [run.get("conclusion") for run in check_runs.get("check_runs", []) if run["name"] == name]
        if not conclusions or any(conclusion != "success" for conclusion in conclusions):
            problems.append(name)
    return problems


def environment_problems(environment):
    """Problems unless the deployment environment, as the GitHub environments API returns it, makes a reviewer approve
    the publish job and limits deployments to selected branches and tags. GitHub creates an environment a job names on
    first use with neither rule, and once the strong-name key is added to it the job would sign and push unattended.
    Releases run on tags, which a protected-branches-only policy would refuse after the build, so only selected rules
    count."""
    name = environment.get("name")
    rules = environment.get("protection_rules") or []
    problems = []
    if not any(rule.get("type") == "required_reviewers" and rule.get("reviewers") for rule in rules):
        problems.append(f"environment {name} does not require a reviewer to approve the publish job")
    if not (environment.get("deployment_branch_policy") or {}).get("custom_branch_policies"):
        problems.append(f"environment {name} does not limit deployments to selected branches and tags")
    return problems
