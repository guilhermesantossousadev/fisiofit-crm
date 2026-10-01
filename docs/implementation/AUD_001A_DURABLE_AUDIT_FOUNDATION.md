# AUD-001A — Durable Administrative Audit Foundation

**Status:** DONE / PASS — validated in this worktree on 2026-10-01.  
**Owner:** Privacy & Audit (`Fisiofit.Modules.Audit`).  
**Scope:** foundation internal for future IAM-001A; no IAM producer, HTTP API, query API, Patients integration, outbox, retention, legal hold or frontend.

## Architecture and persistence

`AuditRecord` is private to the Audit module. `AuditDbContext` owns schema `audit`, table `audit.audit_record`, and its own migration history table. The record has opaque external IDs only; there are no cross-context foreign keys, shared contexts or distributed transactions. The composition root only registers the module.

The owner-local initial migration creates the table, a unique `evidence_id`, and only the approved indexes: actor/time, action/time, resource/time, non-null Unit/time, correlation and occurred time.

## Contract and evidence

`IRecordAdministrativeEvidence` in `ModuleContracts.PrivacyAudit` takes a closed `AdministrativeEvidence` type and returns a small disposition. It has no EF, `DbContext`, HTTP/request types, claims, arbitrary objects, dictionaries or query surface.

Actor is `USER_ACCOUNT` with only opaque `UserAccountId`, or the approved `SYSTEM`, `BOOTSTRAP` and `BACKGROUND_PROCESS` identities. The action catalogue is a stable, code-owned IAM-ready list; resource type/id and optional Unit are opaque. Result is `SUCCESS`, `DENIED` or `FAILED`. `occurredAt` must be UTC; the server controls `recordedAt`.

Metadata is the closed, versioned vocabulary approved in AUD-001-DESIGN: permission code, status transition, grant scope, session scope and opaque session ID. It is serialized as JSONB. The public surface cannot represent passwords, hashes, credentials, CPF, email, phone, address, cookie, Authorization, token, raw body or stack trace.

## Reliability semantics

`evidenceId` is the idempotency key. A first successful write is `Recorded`; a duplicate with semantically equal evidence is `AlreadyRecorded`; a duplicate with different immutable semantics is `Conflict`. PostgreSQL's unique constraint is authoritative. No global lock is used.

Validation failures are explicit. A PostgreSQL failure before a known write is `PersistenceFailure`; a failed/cancelled commit is intentionally `UnknownOutcome`, so a future IAM caller must reconcile using the same evidence ID instead of assuming rollback or blindly repeating a business mutation. No automatic business-operation retry is implemented.

`AuditDbContext` rejects tracked update/delete operations on `AuditRecord`; the application contract exposes writing only.

## Validated tests and gates

Environment: .NET SDK 10.0.400 (`/opt/homebrew/bin/dotnet`, macOS arm64), Docker Desktop 4.91.0 / Engine 29.8.0 arm64, PostgreSQL Testcontainers `postgres:18-alpine`. `global.json` is absent; NuGet uses only `https://api.nuget.org/v3/index.json`.

The initial restore/build hang was caused by the execution sandbox blocking NuGet DNS and the .NET CLI evaluation. The feed returned HTTP 200 and all validation commands passed when run with the required network/process access; no SDK, NuGet cache or project configuration was changed.

Commands executed successfully:

```bash
dotnet restore Fisiofit.slnx --verbosity minimal
dotnet build Fisiofit.slnx --no-restore --verbosity minimal
dotnet test tests/backend/Fisiofit.UnitTests/Fisiofit.UnitTests.csproj --no-build --no-restore
dotnet test tests/backend/Fisiofit.ArchitectureTests/Fisiofit.ArchitectureTests.csproj --no-build --no-restore
dotnet test tests/backend/Fisiofit.IntegrationTests/Fisiofit.IntegrationTests.csproj --no-build --no-restore
dotnet build Fisiofit.slnx
dotnet test Fisiofit.slnx
```

The final canonical build completed with 0 warnings and 0 errors. The final canonical test run passed 133/133: Unit 47/47, Architecture 15/15, Integration 49/49 and API 22/22.

Unit tests cover valid records, actor/action/result validation, UTC/schema rules, the closed metadata boundary, sensitive-data exclusion and semantic equality. Architecture tests assert owner-local persistence and a contract with no EF/unstructured inputs.

PostgreSQL/Testcontainers tests apply the migration to a clean database and prove the `audit` schema, Audit migration history, all table indexes, no foreign keys, persistence, unique `evidence_id`, idempotent replay, conflict, concurrent same-ID writes, append-only behavior, explicit pre-commit persistence failure, and cancelled-commit `UnknownOutcome` reconciliation by `evidenceId`. Two concurrent equivalent writes resulted in one persisted row, with `Recorded` and `AlreadyRecorded` dispositions.

Two AUD-001A defects were corrected during validation: PostgreSQL `jsonb` text normalization incorrectly made equivalent metadata conflict, and an EF `InvalidOperationException` wrapping an unavailable Npgsql connection escaped instead of returning `PersistenceFailure`. Semantic comparison now uses structured metadata, applies PostgreSQL timestamp precision, and maps that known pre-commit failure explicitly.

IAM-001A may consume the contract. Assisted use and production remain blocked by the documented IAM, Patients audit, web, retention/legal-hold, backup/restore and operational gates.
