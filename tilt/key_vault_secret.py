#!/usr/bin/env python3
# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""Fetch an Azure Key Vault secret with bounded retries."""

from __future__ import annotations

import argparse
import subprocess
import sys
import time


def fetch_secret(
    vault: str,
    name: str,
    *,
    emit_value: bool = False,
    attempts: int = 3,
    timeout: int = 20,
    initial_delay: float = 1.0,
) -> tuple[bool, str]:
    """Return success and any final error without reading the secret value."""
    last_error = "Azure CLI failed without error details"

    for attempt in range(attempts):
        try:
            result = subprocess.run(
                [
                    "az",
                    "keyvault",
                    "secret",
                    "show",
                    "--vault-name",
                    vault,
                    "--name",
                    name,
                    "--query",
                    "value",
                    "-o",
                    "tsv",
                    "--only-show-errors",
                ],
                stdout=None if emit_value else subprocess.DEVNULL,
                stderr=subprocess.PIPE,
                text=True,
                timeout=timeout,
            )
            if result.returncode == 0:
                return True, ""
            last_error = result.stderr.strip() or last_error
        except FileNotFoundError:
            return False, "Azure CLI executable 'az' was not found"
        except subprocess.TimeoutExpired:
            last_error = f"Azure CLI timed out after {timeout} seconds"

        if attempt < attempts - 1:
            time.sleep(initial_delay * (2**attempt))

    return False, last_error


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--vault-name", required=True)
    parser.add_argument("--name", required=True)
    args = parser.parse_args()

    ok, error = fetch_secret(args.vault_name, args.name, emit_value=True)
    if not ok:
        print("Failed to fetch secret from Azure Key Vault.", file=sys.stderr)
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
