#!/usr/bin/env python3
"""Entry point of the release workflows: one subcommand per workflow step."""

import argparse
import binascii
import base64
import json
import os
import platform
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

import build
import checks
import consumers
import contract
import feed
import manifest as manifest_module
import nupkg
import pending
import sign
import sources
from plan import TARGETS, PlanError, checkout_tags, for_target, load_config, plan_for_tag, plan_from_json

HERE = Path(__file__).resolve().parent
REPO_ROOT = HERE.parents[1]
WINDOWS = platform.system() == "Windows"
PROBE_TIMEOUT_SECONDS = 300


class StepFailed(Exception):
    """A command of a step failed; its output is already in the log."""


def summary(lines):
    """Prints lines and appends them to the job summary when running in GitHub Actions."""
    for line in lines:
        print(line)
    path = os.environ.get("GITHUB_STEP_SUMMARY")
    if path:
        with open(path, "a", encoding="utf-8") as handle:
            handle.write("\n".join(lines) + "\n")


def outputs(values):
    """Writes step outputs when running in GitHub Actions."""
    path = os.environ.get("GITHUB_OUTPUT")
    if path:
        with open(path, "a", encoding="utf-8") as handle:
            for key, value in values.items():
                handle.write(f"{key}={value}\n")


def finish(problems, title):
    """Reports problems as error annotations and exits with 1 when there are any; reports success otherwise."""
    if problems:
        for problem in problems:
            print(f"::error::{problem}")
        summary([f"### {title}: failed"] + [f"- {problem}" for problem in problems])
        sys.exit(1)
    summary([f"### {title}: passed"])


def read_plan(path):
    """Loads config.json and the plan written by the plan step."""
    config = load_config()
    return config, plan_from_json(config, json.loads(Path(path).read_text(encoding="utf-8")))


def public_key(config):
    """The Countly strong-name public key blob committed in config.json."""
    return bytes.fromhex(config["strongName"]["publicKey"])


def staged_file(staging, package, plan):
    """Path of one staged package file."""
    return Path(staging) / package.file_name(plan.package_version)


def load_contract(package):
    """The committed contract file of a package."""
    return json.loads((REPO_ROOT / package.contract).read_text(encoding="utf-8"))


def feed_of(config, plan, title):
    """The feed the plan publishes to; a dry run has none, so a step that needs one stops with that reason."""
    if plan.target not in config["feeds"]:
        finish([f"the {plan.target} target publishes nowhere; plan a test or production release for this step"], title)
    return config["feeds"][plan.target]


def run(command, cwd=None, env=None):
    """Runs a build command with its output in the log; raises StepFailed when it fails."""
    print("$ " + " ".join(command), flush=True)
    executable = shutil.which(command[0]) or command[0]
    result = subprocess.run([executable] + command[1:], cwd=cwd, env=env)
    if result.returncode != 0:
        raise StepFailed(f"{command[0]} {command[1]} failed with exit code {result.returncode}")


def cmd_plan(args):
    """Maps the tag to a plan for the target, writes plan.json and the step outputs, and checks the GitHub release type.
    A test or dry run without a tag rehearses the core release of the checkout's version."""
    config = load_config()
    try:
        tag = args.tag
        if not tag:
            if args.target == "production":
                raise PlanError("a production release needs the tag it publishes")
            tag = checkout_tags(config, REPO_ROOT, config["tagRules"][0]["branch"])[0]
        plan = for_target(plan_for_tag(config, tag), args.target, args.run)
    except PlanError as error:
        finish([str(error)], "Tag")
        return
    Path(args.out).write_bytes(json.dumps(plan.to_json(), indent=2).encode("utf-8"))
    target_feed = config["feeds"].get(plan.target, {})
    outputs({
        "tag": plan.tag,
        "environment": target_feed.get("environment", ""),
        "token_service": target_feed.get("tokenService", ""),
        "audience": target_feed.get("audience", ""),
        "announce": str(plan.target == "production" and not plan.prerelease).lower(),
        "changelog": config["packages"][plan.packages[0].key].get("changelog", ""),
    })
    lines = [f"### Plan for {plan.tag}", f"- version {plan.version}{' (release candidate)' if plan.prerelease else ''}", f"- branch {plan.branch}",
             f"- target {plan.target}{' (' + target_feed['name'] + ')' if target_feed else ''}"]
    if args.commit:
        lines.append(f"- commit {args.commit}")
    summary(lines + [f"- {package.id} {plan.package_version}" for package in plan.packages])
    problems = []
    if plan.target == "production":
        if args.prerelease not in ("true", "false"):
            problems.append("a production release needs a published GitHub release for this tag")
        elif (args.prerelease == "true") != plan.prerelease:
            problems.append("a tag ending in -rcN must be published as a GitHub pre-release, and a pre-release needs such a tag")
    finish(problems, "GitHub release")


def cmd_checkout_tags(args):
    """Writes the tags the checkout would be released as from --branch, as the JSON list the release checks run on."""
    try:
        tags = checkout_tags(load_config(), REPO_ROOT, args.branch)
    except PlanError as error:
        finish([str(error)], "Checkout tags")
        return
    outputs({"tags": json.dumps(tags)})
    summary([f"### Tags {args.branch} would release"] + [f"- {tag}" for tag in tags or ["none"]])


def cmd_pending(args):
    """Lists the GitHub releases a release PR from staging into master creates and writes their notes, after checking
    the merge; fails on anything that would stop one of them, so nothing is created."""
    config = load_config()
    problems = pending.merge_problems(args.head, args.merge, REPO_ROOT)
    releases = []
    if not problems:
        try:
            releases, problems = pending.pending_releases(config, REPO_ROOT, args.branch, pending.existing_tags(REPO_ROOT))
        except (PlanError, OSError) as error:
            problems = [str(error)]
    if not problems:
        notes = Path(args.notes)
        notes.mkdir(parents=True, exist_ok=True)
        for release in releases:
            (notes / f"{release.tag}.md").write_bytes(release.notes.encode("utf-8"))
        Path(args.out).write_bytes(json.dumps({"releases": [release.to_json() for release in releases]}, indent=2).encode("utf-8"))
        lines = [f"- {release.tag}{' (pre-release)' if release.prerelease else ''}" for release in releases]
        summary(["### Releases of this merge"] + (lines or ["- none, every version is tagged already"]))
    finish(problems, "Release plan")


def cmd_check_branch(args):
    """The tag commit must be on the plan's release branch."""
    _, plan = read_plan(args.plan)
    finish(checks.branch_problems(args.commit, plan.branch, REPO_ROOT), "Release branch")


def cmd_check_sources(args):
    """Version numbers and changelog headings must match the tag."""
    config, plan = read_plan(args.plan)
    finish(sources.check_version_sources(REPO_ROOT, plan, config), "Version numbers")


def cmd_check_absent(args):
    """The package versions must not be on the target feed yet; a feed never takes a version twice."""
    config, plan = read_plan(args.plan)
    target_feed = feed_of(config, plan, "Not published yet")
    problems = []
    for package in plan.packages:
        if feed.fetch(feed.package_url(target_feed["flatContainer"], package.id, plan.package_version)) is not None:
            problems.append(f"{package.id} {plan.package_version} is already on {target_feed['name']}")
    finish(problems, "Not published yet")


def cmd_check_required(args):
    """The required status checks must have succeeded on the tag commit."""
    config = load_config()
    runs = json.loads(Path(args.check_runs).read_text(encoding="utf-8"))
    finish([f"required check '{name}' did not succeed on the tag commit" for name in checks.missing_checks(runs, config["requiredChecks"])], "Required checks")


def cmd_check_environment(args):
    """The environment of the publish job must make a reviewer approve it and limit which refs deploy to it."""
    environment = json.loads(Path(args.environment).read_text(encoding="utf-8"))
    finish(checks.environment_problems(environment), "Publish environment")


def cmd_build(args):
    """Compiles each package of the plan delay-signed with the Countly public key and packs it into the staging folder:
    the core from its three projects into the nuspec's layout, the UI with dotnet pack. --skip-compile packs the core
    from the build outputs already in the checkout."""
    config, plan = read_plan(args.plan)
    work = Path(args.work).resolve()
    staging = Path(args.staging).resolve()
    staging.mkdir(parents=True, exist_ok=True)
    key = build.write_public_key(config, work / "countly-public.snk")
    try:
        for package in plan.packages:
            spec = config["packages"][package.key]
            if "nuspec" not in spec:
                run(build.pack_project_command(config, package.key, plan, REPO_ROOT, key, staging), cwd=REPO_ROOT)
                continue
            if not args.skip_compile:
                for output in build.built_outputs(config, package.key, REPO_ROOT):
                    if output.is_file():
                        output.unlink()
                for command in build.compile_commands(config, package.key, REPO_ROOT, key, work):
                    run(command, cwd=REPO_ROOT)
            layout = work / "layout" / package.key
            shutil.rmtree(layout, ignore_errors=True)
            missing = build.assemble_layout(config, package.key, REPO_ROOT, layout)
            if missing:
                finish([f"{package.id}: {problem}" for problem in missing], "Build")
            nuspec = build.stage_nuspec(config, package.key, plan, REPO_ROOT, layout)
            run(build.pack_nuspec_command(build.write_pack_project(work), nuspec, layout, staging), cwd=work)
    except (StepFailed, build.BuildError) as error:
        finish([str(error)], "Build")
    summary([f"### Built {', '.join(package.id for package in plan.packages)} {plan.package_version}, delay-signed with the Countly public key"])


def cmd_stage(args):
    """The staging folder must hold exactly the plan's packages, each with its id and version; writes the manifest."""
    _, plan = read_plan(args.plan)
    staging = Path(args.staging)
    expected = {package.file_name(plan.package_version) for package in plan.packages}
    present = {path.name for path in staging.iterdir() if path.is_file()} if staging.is_dir() else set()
    problems = [f"missing {name}" for name in sorted(expected - present)] + [f"unexpected {name}" for name in sorted(present - expected)]
    for package in plan.packages:
        path = staged_file(staging, package, plan)
        if path.is_file():
            metadata = nupkg.package_metadata(path)
            if (metadata["id"], metadata["version"]) != (package.id, plan.package_version):
                problems.append(f"{path.name} holds {metadata['id']} {metadata['version']}")
    if not problems:
        Path(args.manifest).write_bytes(json.dumps(manifest_module.build_manifest(staging, plan.tag, args.commit), indent=2).encode("utf-8"))
    finish(problems, "Staged packages")


def cmd_contract(args):
    """Compares each staged package with its contract file and checks its assemblies' identity; --capture prints the
    actual contracts instead."""
    config, plan = read_plan(args.plan)
    key = public_key(config)
    problems = []
    lines = []
    for package in plan.packages:
        path = staged_file(args.staging, package, plan)
        actual = contract.actual_contract(path)
        if args.capture:
            print(json.dumps(actual, indent=2))
            continue
        expected = load_contract(package)
        found = contract.compare(expected, actual) + contract.identity_problems(path, plan, key, args.commit)
        problems += [f"{package.id}: {problem}" for problem in found]
        lines += [f"#### {package.id} {plan.package_version}", "", "| | Contract | Built |", "|---|---|---|"]
        lines += [f"| {name} | {promised} | {built} |" for name, promised, built in contract.summary_rows(expected, actual)] + [""]
    if args.capture:
        return
    summary(["### What the release promises integrators"] + lines)
    finish(problems, "Contract")


def cmd_skip_verification(args):
    """Makes .NET Framework on this runner load the delay-signed Countly assemblies, so the clean projects can run them."""
    config = load_config()
    try:
        tools = [sign.find_sn(x64=True), sign.find_sn(x64=False)]
        for command in sign.skip_verification_commands(config["strongName"]["token"], tools):
            run(command)
    except (sign.SigningError, StepFailed) as error:
        finish([str(error)], "Strong-name verification skipping")
    summary([f"### This runner skips strong-name verification for the token {config['strongName']['token']}"])


def run_probe(project, alias, plan, expectation, promised, token):
    """Runs the probe built for one framework and returns what it printed and the problems found; the core probe sends
    a session and an event to a local recording server, and the requests must arrive."""
    command = consumers.probe_command(project, alias)
    app_key = f"release-probe-{alias}"
    server = consumers.RecordingServer() if "sdkName" in expectation else None
    try:
        if server:
            with server:
                result = subprocess.run(command + [server.url, app_key], capture_output=True, text=True, timeout=PROBE_TIMEOUT_SECONDS)
        else:
            result = subprocess.run(command, capture_output=True, text=True, timeout=PROBE_TIMEOUT_SECONDS)
    except subprocess.TimeoutExpired:
        return {}, [f"the probe did not finish within {PROBE_TIMEOUT_SECONDS} seconds"]
    print(result.stdout + result.stderr)
    values = consumers.parse_probe(result.stdout)
    problems = [] if result.returncode == 0 else [f"the probe exited with {result.returncode}"]
    problems += consumers.probe_problems(values, plan, expectation, promised, token)
    if server:
        problems += consumers.delivery_problems(server.requests, app_key, plan, expectation)
        values["requests"] = str(len([request for request in server.requests if request.get("app_key") == app_key]))
    return values, problems


def cmd_consumer(args):
    """Restores each package of the plan into a clean project per framework of its contract, from the staging folder or
    from the target feed, checks the libraries NuGet chose and where it took them from, and runs each build this
    machine can run."""
    config, plan = read_plan(args.plan)
    token = config["strongName"]["token"]
    work = Path(args.work).resolve()
    problems = []
    rows = []
    for package in plan.packages:
        promised = load_contract(package)
        expectations = promised["consumers"]
        project_dir = work / package.key
        shutil.rmtree(project_dir, ignore_errors=True)
        shutil.copytree(REPO_ROOT / package.consumer, project_dir)
        project = next(project_dir.glob("*.csproj"))
        staged = None
        if args.source == "staged":
            release_source = str(Path(args.staging).resolve())
            staged = staged_file(release_source, package, plan)
        else:
            release_source = feed_of(config, plan, "Clean projects")["source"]
        packages_folder = project_dir / "packages"
        (project_dir / "NuGet.config").write_text(consumers.nuget_config(release_source, config["dependencySource"], package.id, packages_folder), encoding="utf-8")
        env = consumers.build_environment(os.environ, sorted(expectations), packages_folder, project_dir / "http-cache")
        try:
            run(consumers.build_command(project, plan.package_version), cwd=project_dir, env=env)
        except StepFailed as error:
            problems.append(f"{package.id}: the clean project did not build: {error}")
            continue
        assets = json.loads((project_dir / "obj" / "project.assets.json").read_text(encoding="utf-8"))
        found = consumers.asset_problems(assets, package.id, plan.package_version, expectations)
        found += consumers.source_problems(packages_folder, package.id, plan.package_version, release_source, staged)
        for alias, expectation in sorted(expectations.items()):
            if not consumers.runnable(alias, WINDOWS):
                rows.append(f"| {package.id} | {alias} | {expectation['lib']} | not run on {platform.system()} | |")
                continue
            values, probe_found = run_probe(project, alias, plan, expectation, promised, token)
            found += [f"{alias}: {problem}" for problem in probe_found]
            loaded = values.get("sdkName") or values.get("core", "").split(",")[0] or "?"
            sent = f"{values['requests']} requests arrived" if "requests" in values else values.get("webView2", "")
            rows.append(f"| {package.id} | {alias} | {expectation['lib']} | {values.get('runtime', '?')}, {loaded} | {sent} |")
        problems += [f"{package.id}: {problem}" for problem in found]
    summary([f"### Clean projects from {'the staged packages' if args.source == 'staged' else config['feeds'][plan.target]['name']}", "",
             "| Package | Framework | Library | Runtime and flavor | Delivery or WebView2 |", "|---|---|---|---|---|"] + rows + [""])
    finish(problems, "Clean projects")


def cmd_verify_manifest(args):
    """The downloaded staging folder must be exactly what the earlier job produced."""
    data = json.loads(Path(args.manifest).read_text(encoding="utf-8"))
    finish(manifest_module.verify_manifest(args.staging, data), "Manifest")


def cmd_sign(args):
    """Signs every assembly of the staged packages with the strong-name key pair from the --key-env variable (a base64
    .snk), checks each signature, and writes the manifest of the signed packages. The key is written to a temporary
    folder that is removed afterwards, and must be the Countly key."""
    config, plan = read_plan(args.plan)
    encoded = os.environ.get(args.key_env, "")
    if not encoded.strip():
        finish([f"{args.key_env} is empty: add the strong-name key pair (.snk, base64) as a secret of the publish environment"], "Strong-name signatures")
    work = Path(tempfile.mkdtemp(prefix="countly-sign-"))
    problems = []
    try:
        key_file = work / "countly.snk"
        # A secret pasted from `base64` output carries line breaks, which strict decoding would refuse.
        key_file.write_bytes(base64.b64decode("".join(encoded.split()), validate=True))
        sn = sign.find_sn()
        expected = public_key(config)
        if sign.public_key_of(key_file, sn, work) != expected:
            problems.append(f"the key in {args.key_env} is not the Countly strong-name key; its public key differs from config.json")
        else:
            for package in plan.packages:
                found = sign.sign_package(staged_file(args.staging, package, plan), key_file, expected, config["strongName"]["token"], sn, work)
                problems += [f"{package.id}: {problem}" for problem in found]
    except binascii.Error:
        problems.append(f"{args.key_env} is not valid base64")
    except sign.SigningError as error:
        problems.append(str(error))
    finally:
        shutil.rmtree(work, ignore_errors=True)
    if not problems:
        Path(args.manifest).write_bytes(json.dumps(manifest_module.build_manifest(args.staging, plan.tag, args.commit), indent=2).encode("utf-8"))
    finish(problems, "Strong-name signatures")


def cmd_push(args):
    """Pushes each package of the plan to the target feed with the API key in the --api-key-env variable."""
    config, plan = read_plan(args.plan)
    target_feed = feed_of(config, plan, "Push")
    api_key = os.environ.get(args.api_key_env, "")
    if not api_key:
        finish([f"{args.api_key_env} is empty; the NuGet login step did not produce a key"], "Push")
    problems = []
    lines = [f"### Pushed to {target_feed['name']}"]
    for package in plan.packages:
        path = staged_file(args.staging, package, plan)
        command = ["dotnet", "nuget", "push", str(path), "--source", target_feed["source"], "--api-key", api_key, "--timeout", "600"]
        print(f"$ dotnet nuget push {path.name} --source {target_feed['source']}", flush=True)
        if subprocess.run([shutil.which("dotnet") or "dotnet"] + command[1:]).returncode != 0:
            problems.append(f"{package.id} {plan.package_version}: the push to {target_feed['name']} failed")
            break
        lines.append(f"- {package.id} {plan.package_version}: {target_feed['gallery']}{package.id}/{plan.package_version}")
    summary(lines)
    finish(problems, "Push")


def cmd_verify_public(args):
    """Waits until the target feed serves each pushed package and checks it is exactly the pushed one."""
    config, plan = read_plan(args.plan)
    target_feed = feed_of(config, plan, "Served by the feed")
    problems = []
    for package in plan.packages:
        url = feed.package_url(target_feed["flatContainer"], package.id, plan.package_version)
        body = feed.wait_for(url, args.timeout_minutes * 60, 30)
        if body is None:
            problems.append(f"{package.id} {plan.package_version} was not on {target_feed['name']} after {args.timeout_minutes} minutes")
            continue
        problems += [f"{package.id}: {problem}" for problem in feed.published_problems(staged_file(args.staging, package, plan), body)]
    finish(problems, f"Served by {target_feed['name']}")


def main(argv=None):
    """Parses the subcommand and runs it."""
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)

    def command(name, handler, *arguments):
        """Adds a subcommand with (flag, options) arguments."""
        sub = commands.add_parser(name)
        for flag, options in arguments:
            sub.add_argument(flag, **options)
        sub.set_defaults(handler=handler)

    required = {"required": True}
    command("plan", cmd_plan, ("--tag", {"default": ""}), ("--target", {"choices": TARGETS, "required": True}), ("--commit", {"default": ""}),
            ("--prerelease", {"default": "unknown"}), ("--run", {"default": ""}), ("--out", required))
    command("checkout-tags", cmd_checkout_tags, ("--branch", required))
    command("pending", cmd_pending, ("--head", required), ("--merge", required), ("--branch", required), ("--out", required), ("--notes", required))
    command("check-branch", cmd_check_branch, ("--plan", required), ("--commit", required))
    command("check-sources", cmd_check_sources, ("--plan", required))
    command("check-absent", cmd_check_absent, ("--plan", required))
    command("check-required", cmd_check_required, ("--check-runs", required))
    command("check-environment", cmd_check_environment, ("--environment", required))
    command("build", cmd_build, ("--plan", required), ("--work", required), ("--staging", required), ("--skip-compile", {"action": "store_true"}))
    command("stage", cmd_stage, ("--plan", required), ("--staging", required), ("--commit", {"default": ""}), ("--manifest", required))
    command("contract", cmd_contract, ("--plan", required), ("--staging", required), ("--commit", {"default": ""}), ("--capture", {"action": "store_true"}))
    command("skip-verification", cmd_skip_verification)
    command("consumer", cmd_consumer, ("--plan", required), ("--source", {"choices": ["staged", "feed"], "required": True}), ("--staging", {"default": ""}), ("--work", required))
    command("verify-manifest", cmd_verify_manifest, ("--manifest", required), ("--staging", required))
    command("sign", cmd_sign, ("--plan", required), ("--staging", required), ("--key-env", required), ("--commit", {"default": ""}), ("--manifest", required))
    command("push", cmd_push, ("--plan", required), ("--staging", required), ("--api-key-env", required))
    command("verify-public", cmd_verify_public, ("--plan", required), ("--staging", required), ("--timeout-minutes", {"type": int, "default": 60}))
    args = parser.parse_args(argv)
    args.handler(args)


if __name__ == "__main__":
    main()
