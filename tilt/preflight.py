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

BROKER_DIR = os.path.join(FLOTILLA_ROOT, "broker")
# The same two paths setup.sh writes, so the interactive and the Tilt route share
# one set of local credentials whichever one produced it. Both are gitignored.
CREDENTIALS_DIR = os.path.join(BROKER_DIR, ".local-credentials")
BROKER_ENV = os.path.join(BROKER_DIR, ".env")
CREDENTIALS_GENERATOR = os.path.join(BROKER_DIR, "scripts", "generate-mqtt-credentials.sh")

# Every name a client may reach the broker by; the certificate is valid for these
# and nothing else. Kept identical to setup.sh so either route yields the same
# certificate. The Tilt stacks connect on localhost; `broker` and
# host.docker.internal cover connections from inside a container.
BROKER_HOSTNAMES = ["broker", "localhost", "host.docker.internal", "IP:127.0.0.1"]

# Regenerate well before expiry, so rotating is never urgent.
CERTIFICATE_RENEW_SECONDS = 30 * 24 * 60 * 60

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


def _certificate_is_current() -> bool:
    """Whether generated credentials exist and the certificate is not near expiry."""
    required = [
        os.path.join(CREDENTIALS_DIR, name)
        for name in ("server-cert.pem", "server-key.pem", "ca-cert.pem", "passwords")
    ]
    required.append(BROKER_ENV)
    if not all(os.path.isfile(path) for path in required):
        return False

    result = _run([
        "openssl", "x509",
        "-in", os.path.join(CREDENTIALS_DIR, "server-cert.pem"),
        "-noout",
        "-checkend", str(CERTIFICATE_RENEW_SECONDS),
    ])
    return result is not None and result.returncode == 0


def _pem_body(name: str) -> str:
    """The base64 body of a generated PEM, on one line, without BEGIN/END."""
    with open(os.path.join(CREDENTIALS_DIR, name)) as pem:
        return "".join(
            line.strip() for line in pem if not line.startswith("-----")
        )


def _write_broker_env() -> None:
    """Write broker/.env from the generated credentials, as setup.sh does.

    Compose reads this through env_file, which cannot hold a multi-line value, so
    the PEM bodies go in on a single line; broker/entrypoint.sh reassembles them.
    """
    with open(os.path.join(CREDENTIALS_DIR, "mqtt-passwords.env")) as passwords:
        mqtt_passwords = passwords.read().strip()

    with open(BROKER_ENV, "w") as env_file:
        env_file.write("TLS_SERVER_KEY='%s'\n" % _pem_body("server-key.pem"))
        env_file.write("TLS_SERVER_CERT='%s'\n" % _pem_body("server-cert.pem"))
        env_file.write("TLS_CA_CERT='%s'\n" % _pem_body("ca-cert.pem"))
        env_file.write("%s\n" % mqtt_passwords)
    os.chmod(BROKER_ENV, 0o600)


def check_mqtt_credentials() -> Check:
    """Ensure this machine has its own broker credentials, generating if needed.

    The broker image ships a certificate and password file shared by every
    environment. Generating a set here means a laptop never holds a credential a
    deployment also uses, and lets the stack run with TLS on. Regenerated only
    when missing or close to expiry: the passwords reach the clients through the
    Tilt environment, so rotating them restarts every one of them.
    """
    if not os.path.isfile(CREDENTIALS_GENERATOR):
        return Check(
            "mqtt-credentials", False, "missing %s" % CREDENTIALS_GENERATOR, "Repository"
        )

    if _certificate_is_current():
        return Check(
            "mqtt-credentials",
            True,
            "broker/.local-credentials present and not near expiry",
            "Repository",
        )

    result = _run(
        [CREDENTIALS_GENERATOR, "-o", CREDENTIALS_DIR] + BROKER_HOSTNAMES, timeout=120
    )
    if result is None or result.returncode != 0:
        detail = result.stderr.strip() if result is not None else "openssl or bash missing"
        return Check(
            "mqtt-credentials",
            False,
            "generate-mqtt-credentials.sh failed: %s" % (detail or "no error details"),
            "Repository",
        )

    _write_broker_env()
    return Check(
        "mqtt-credentials", True, "generated broker/.local-credentials", "Repository"
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
        check_mqtt_credentials(),
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
