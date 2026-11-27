"""A NuGet feed seen from outside, the way an integrator's restore sees it: whether a package version exists, waiting
for a pushed one to become available, and comparing what the feed serves with what was pushed."""

import io
import time
import urllib.error
import urllib.request

import nupkg


def fetch(url, timeout=60):
    """Downloads a URL; returns None for 404 and raises for any other error."""
    request = urllib.request.Request(url, headers={"User-Agent": "countly-sdk-release"})
    try:
        with urllib.request.urlopen(request, timeout=timeout) as response:
            return response.read()
    except urllib.error.HTTPError as error:
        if error.code == 404:
            return None
        raise


def package_url(flat_container, package_id, version):
    """The address of a package file in a feed's flat container, which serves lowercase ids and versions."""
    lower_id = package_id.lower()
    lower_version = version.lower()
    return f"{flat_container.rstrip('/')}/{lower_id}/{lower_version}/{lower_id}.{lower_version}.nupkg"


def wait_for(url, timeout_seconds, interval_seconds, fetcher=fetch, clock=time.monotonic, sleep=time.sleep):
    """The body of the URL once it stops answering 404, or None when that takes longer than the timeout. A pushed
    package is served only after the gallery has validated and indexed it, which takes minutes."""
    deadline = clock() + timeout_seconds
    while True:
        body = fetcher(url)
        if body is not None:
            return body
        if clock() >= deadline:
            return None
        sleep(interval_seconds)


def published_problems(pushed_path, published):
    """Problems unless the feed serves the pushed package: every entry with the same content, nothing missing and
    nothing added except the repository signature a gallery puts on every package."""
    pushed = nupkg.entry_hashes(pushed_path)
    served = nupkg.entry_hashes(io.BytesIO(published))
    problems = [f"missing {name}" for name in sorted(set(pushed) - set(served))]
    problems += [f"unexpected {name}" for name in sorted(set(served) - set(pushed))]
    problems += [f"changed {name}" for name in sorted(set(pushed) & set(served)) if pushed[name] != served[name]]
    return problems
