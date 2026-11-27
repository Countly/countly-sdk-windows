"""Checks that every version source of the tag's packages carries the tag's version, and reads the release notes from
the changelogs."""

import re
from pathlib import Path

PLACEHOLDER = "XX.XX.XX"


def expected_version(plan, source):
    """The version a source must hold: the release version, or for an assembly file version ("form": "file") the
    four-part numeric form such as 26.8.1.0, which cannot carry an -rcN suffix."""
    return plan.file_version if source.get("form") == "file" else plan.version


def check_version_sources(repo_root, plan, config):
    """Returns problems; empty when every version source and, for final releases, every changelog heading matches, with
    no '## XX.XX.XX' heading left."""
    problems = []
    root = Path(repo_root)
    for package in plan.packages:
        spec = config["packages"][package.key]
        for source in spec["versionSources"]:
            path = root / source["file"]
            if not path.is_file():
                problems.append(f"{source['file']} does not exist")
                continue
            match = re.search(source["regex"], path.read_text(encoding="utf-8-sig"), re.MULTILINE)
            expected = expected_version(plan, source)
            if match is None:
                problems.append(f"{source['file']}: no version found")
            elif match.group(1).strip() != expected:
                problems.append(f"{source['file']}: version is {match.group(1).strip()}, the tag says {expected}")
        changelog = spec.get("changelog")
        if changelog and not plan.prerelease:
            path = root / changelog
            if not path.is_file():
                problems.append(f"{changelog} does not exist")
                continue
            text = path.read_text(encoding="utf-8-sig")
            if changelog_section(text, plan.version) is None:
                problems.append(f"{changelog}: no '## {plan.version}' heading")
            if changelog_section(text, PLACEHOLDER) is not None:
                problems.append(f"{changelog}: still has a '## {PLACEHOLDER}' heading; move its lines under '## {plan.version}'")
    return list(dict.fromkeys(problems))


def release_notes(repo_root, plan, config):
    """The notes of the plan's GitHub release, the changelog section of its version for each of its packages; a
    pre-release takes the '## XX.XX.XX' section when there is one. Returns the notes and the problems that leave a
    changelog without them."""
    notes = ""
    problems = []
    for package in plan.packages:
        changelog = config["packages"][package.key].get("changelog")
        if not changelog:
            continue
        path = Path(repo_root) / changelog
        if not path.is_file():
            problems.append(f"{changelog} does not exist")
            continue
        text = path.read_text(encoding="utf-8-sig")
        section = (changelog_section(text, PLACEHOLDER) if plan.prerelease else None) or changelog_section(text, plan.version)
        if section is None:
            problems.append(f"{changelog}: no '## {plan.version}' heading")
        notes += section or ""
    return notes, problems


def changelog_section(text, heading):
    """The lines under the '## <heading>' heading of a changelog, up to the next '## ' heading outside a fenced code
    block, without the blank lines around them; None when the changelog has no such heading."""
    lines = text.splitlines()
    start = next((index for index, line in enumerate(lines) if re.fullmatch(rf"## {re.escape(heading)}\s*", line)), None)
    if start is None:
        return None
    section = []
    fenced = False
    for line in lines[start + 1:]:
        if line.lstrip().startswith("```"):
            fenced = not fenced
        elif not fenced and line.startswith("## "):
            break
        section.append(line)
    return "\n".join(section).strip("\n") + "\n"
