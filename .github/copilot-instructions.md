# Copilot instructions

<!-- Repo-specific guidance goes here. Hand-edit freely above the synced
     block. The synced block at the bottom is overwritten by
     equinor/armada/scripts/sync-copilot.sh. -->

<!-- BEGIN SYNCED FROM equinor/armada@6696246 -->
<!-- AUTO-GENERATED — do not edit between these markers. Run scripts/sync-copilot.sh in armada to refresh. -->

## Flotilla–SARA system context

This repository is part of the Equinor Flotilla–SARA robotics system. Sibling
repositories that may be affected by changes here include:

- **isar** — mission API and `robot_interface` definition; publishes MQTT events.
- **isar-robot / isar-anymal / isar-taurob** — robot-specific implementations of
  the ISAR `robot_interface`.
- **flotilla** — operator backend (ASP.NET) + frontend (React) + Mosquitto
  broker; consumes ISAR MQTT messages.
- **sara** + **sara-anonymizer / sara-constant-level-oiler / sara-stid /
  sara-thermal-reading / sara-fence-detection / sara-timeseries** — inspection
  data platform; SARA orchestrates the analysis steps via Argo workflows and
  ingests `IsarInspectionResultMessage` events.
- **robotics-infrastructure / analytics-infrastructure** — Kustomize/ArgoCD
  deployments for the above.
- **armada** — integration-test harness that runs `flotilla`, `isar-robot` and
  `sara` together; also the canonical source of this shared Copilot config.

For the full architecture overview see the README in
[`equinor/armada`](https://github.com/equinor/armada).

## Pull request reviews

When asked to review a pull request, follow the workflow in
`.github/prompts/review-pr.prompt.md`. If that prompt file is not available in
your Copilot surface (e.g. some IDE integrations), apply this condensed
checklist instead:

1. **Cross-repo impact.** Does the diff touch a contract used by sibling repos?
   In particular: ISAR MQTT message models, the ISAR `robot_interface`, SARA
   Argo workflow step inputs/outputs, shared schemas, or deployment manifests
   under `*-infrastructure`. If so, name the sibling repo(s) that likely need a
   coordinated PR.
2. **Tests.** New/changed behaviour has unit or integration tests. Consider
   whether `armada` integration tests need updating as well.
3. **Security.** No secrets, credentials, or private endpoints committed. New
   outbound network calls or new dependencies are justified.
4. **Backward compatibility.** Public contracts (HTTP, MQTT topics/payloads,
   CLI flags, config keys, database schemas) remain compatible, or a migration
   path is documented.
5. **Observability.** Logs and metrics are present where the change affects
   runtime behaviour or failure modes.
6. **Style.** Matches the repo's language conventions (C# / TypeScript /
   Python / Kustomize) and passes the repo's existing lint/format gates.
7. **Docs.** README or in-repo docs updated when user-visible behaviour
   changes.
8. **Commits & PR hygiene.** Commit messages follow the repo's convention; the
   PR description explains the *why*, not just the *what*.

Output the review as four sections — **Summary**, **Blocking**, **Suggestions**,
**Questions** — and cite findings as `path:line`. Use `- _none_` for empty
sections. No emojis.

<!-- END SYNCED -->
