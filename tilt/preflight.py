#!/usr/bin/env python3
# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""Preflight checks for the flotilla local (Tilt) stack.

Run standalone (human output):   uv run --script tilt/preflight.py
Machine-readable (for robotics):  uv run --script tilt/preflight.py --json

Exit code 0 if all required checks pass, 1 otherwise. The robotics
local-orchestration preflight shells out to this with --json and folds the
result into its own report.
"""

from __future__ import annotations

import argparse
import json
import os
import shutil
import socket
import subprocess
import sys
from dataclasses import dataclass, asdict

TILT_DIR = os.path.dirname(os.path.abspath(__file__))
FLOTILLA_ROOT = os.path.dirname(TILT_DIR)

# ANSI colors (mirrors robotics/local-orchestration/preflight.py)
GREEN = "\033[32m"
YELLOW = "\033[33m"
RED = "\033[31m"
BOLD = "\033[1m"
RESET = "\033[0m"

CHECK = f"{GREEN}\u2714{RESET}"  # green checkmark
FAIL = f"{RED}\u2718{RESET}"  # red X

# Host ports the Tilt stack binds. Kept in sync with the Tiltfile / compose.
REQUIRED_PORTS = {
    8000: "flotilla-backend",
    3001: "flotilla-frontend",
    1883: "mqtt-broker",
    5432: "postgres",
}
MIN_DOTNET_MAJOR = 10


@dataclass
class Check:
    name: str
    ok: bool
    detail: str
    section: str
    required: bool = True


def _run(cmd: list[str], timeout: int = 20) -> subprocess.CompletedProcess | None:
    try:
        return subprocess.run(cmd, capture_output=True, text=True, timeout=timeout)
    except (FileNotFoundError, subprocess.TimeoutExpired):
        return None


def check_docker() -> Check:
    if shutil.which("docker") is None:
        return Check("docker", False, "docker not found on PATH", "Tooling")
    res = _run(["docker", "info"])
    ok = res is not None and res.returncode == 0
    return Check(
        "docker", ok, "daemon running" if ok else "docker daemon not reachable", "Tooling"
    )


def check_dotnet() -> Check:
    if shutil.which("dotnet") is None:
        return Check("dotnet", False, "dotnet SDK not found on PATH", "Tooling")
    res = _run(["dotnet", "--version"])
    if res is None or res.returncode != 0:
        return Check("dotnet", False, "could not query dotnet version", "Tooling")
    version = res.stdout.strip()
    try:
        major = int(version.split(".")[0])
    except (ValueError, IndexError):
        return Check("dotnet", False, "unparseable version: %s" % version, "Tooling")
    ok = major >= MIN_DOTNET_MAJOR
    return Check(
        "dotnet", ok, "%s (need >= %d.x)" % (version, MIN_DOTNET_MAJOR), "Tooling"
    )


def check_tool(binary: str) -> Check:
    path = shutil.which(binary)
    detail = "%s (%s)" % (binary, path) if path else "%s not found on PATH" % binary
    return Check(binary, path is not None, detail, "Tooling")


def check_az_login() -> Check:
    if shutil.which("az") is None:
        return Check("az-login", False, "azure-cli (az) not found on PATH", "Azure")
    res = _run(["az", "account", "show", "--only-show-errors"])
    ok = res is not None and res.returncode == 0
    return Check(
        "az-login", ok, "logged in" if ok else "not logged in -- run: az login", "Azure"
    )


def check_broker_context() -> Check:
    path = os.path.join(FLOTILLA_ROOT, "broker", "Dockerfile")
    ok = os.path.isfile(path)
    return Check(
        "broker-context",
        ok,
        "broker/Dockerfile present" if ok else "missing %s" % path,
        "Repository",
    )


def check_port(port: int, owner: str) -> Check:
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as sock:
        sock.settimeout(1)
        in_use = sock.connect_ex(("127.0.0.1", port)) == 0
    return Check(
        "port-%d" % port,
        not in_use,
        "%d free (%s)" % (port, owner) if not in_use else "%d in use (needed by %s)" % (port, owner),
        "Ports",
    )


def run_checks() -> list[Check]:
    checks = [
        check_docker(),
        check_dotnet(),
        check_tool("node"),
        check_tool("pnpm"),
        check_tool("uv"),
        check_az_login(),
        check_broker_context(),
    ]
    checks += [check_port(p, owner) for p, owner in sorted(REQUIRED_PORTS.items())]
    return checks


def _print_human(checks: list[Check], failed: list[Check]) -> None:
    print(f"\n{BOLD}Preflight checks for local flotilla stack{RESET}")
    print("=" * 50)

    section = None
    for c in checks:
        if c.section != section:
            section = c.section
            print(f"\n{BOLD}{section}{RESET}")
        mark = CHECK if c.ok else FAIL
        print(f"  {mark} {c.detail}")

    print("\n" + "=" * 50)
    passed = len(checks) - len(failed)
    summary = f"Results: {GREEN}{passed} passed{RESET}"
    if failed:
        summary += f", {RED}{len(failed)} error{'s' if len(failed) != 1 else ''}{RESET}"
    print(summary)

    if failed:
        print(f"\n{RED}{BOLD}Errors:{RESET}")
        for c in failed:
            print(f"  {FAIL} {c.name}: {c.detail}")
        print(f"\n{RED}Fix the errors above before running 'make run'.{RESET}\n")
    else:
        print(f"\n{GREEN}All checks passed. Ready to start!{RESET}\n")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--json", action="store_true", help="emit JSON for tooling")
    args = parser.parse_args()

    checks = run_checks()
    failed = [c for c in checks if c.required and not c.ok]

    if args.json:
        print(json.dumps({
            "component": "flotilla",
            "ok": not failed,
            "checks": [asdict(c) for c in checks],
        }))
    else:
        _print_human(checks, failed)

    return 0 if not failed else 1


if __name__ == "__main__":
    raise SystemExit(main())
