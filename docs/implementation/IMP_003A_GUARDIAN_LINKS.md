# IMP-003A — Guardian Links for Existing Persons

## Status

**DONE / PASS — 2026-09-28.** The local GuardianLink slice passed its required implementation, PostgreSQL/Testcontainers, HTTP, concurrency, retry, architecture and full-suite validation. This is not completion of Patient Relationships, minor registration, Person creation/resolution, non-self payer or production activation.

## Objective and scope

The slice adds Patients-owned GuardianLink create, paged Guardian-only history read, and end lifecycle for an existing PatientProfile and an existing current Person. It excludes Person creation, minor registration, payer links, other relationship kinds, frontend, Billing and Clinical.

## Canonical sources

`PROJECT_OS.md`, IMP-003 design, API-001 §27.1/§42, AUTH-001, DB-001, MODEL-001, STATE-001 and the IMP-001/002 result documents govern the implementation.

## Implemented foundation

`GuardianLink` is a Patients child with `[effectiveFrom,effectiveTo)`, primary flag, persisted logical version and creation/end evidence. The People contract is purpose-specific and exposes only ID, display name and birth date; it does not expose entities, DbContext or queries. The HTTP mappings are Create, `relationships?kind=GUARDIAN`, and End only. Create/List/End use active account, explicit-deny, permissions, Unit scope and `Cache-Control: no-store`.

The Patients transaction acquires `SELECT … FOR UPDATE` on the PatientProfile before rereading links. It rejects same-guardian and primary overlaps. End requires a strong single `If-Match`, returns 428 when absent and 412 for a stale link ETag, increments the link version and preserves the row.

## Persistence and migration

`Patients_AddGuardianLinkLifecycle` creates only `patients.guardian_link`, with its local FK to `patients.patient_profile`, period/version/evidence checks and local indexes. No People or Organization migration or cross-schema FK is added.

## Gates and limitations

End now resolves the Patient Person through the purpose-specific People contract and, for an active minor, rejects a requested end unless the union of the other GuardianLink periods covers continuously from the exclusive end date until the eighteenth birthday. This does not register minors or duplicate birth date in Patients.

The migration was validated in PostgreSQL real. The validation found and corrected its local FK declaration; `guardian_link` is now created in the `patients` schema, with the local PatientProfile FK and persisted end history/version.

The transactional retry proof is complete in PostgreSQL real. The full Guardian HTTP and concurrent Create/Create, Create/End and End/End PostgreSQL matrices are documented below and passed. Durable Audit, production IAM/activation, historical correction, minor registration, new-Person resolution and non-self payer remain gated; they are outside this completed local slice and continue to block external activation.

## Reliability policy

Create and End use a local Patients retry policy with at most **two** whole transaction attempts. It retries only PostgreSQL `40P01` (deadlock) and `40001` (serialization failure), because these SQLSTATEs prove the transaction was aborted. Before the second attempt, the previous transaction has been disposed, the EF change tracker is cleared, no transaction is held during the small delay, and the new attempt reacquires the PatientProfile lock and rechecks all invariants.

Constraint failures, overlap and principal conflicts, stale/missing ETags, validation, authentication and authorization never retry. Connection loss, timeout, or any failure raised by `CommitAsync` has an unknown commit outcome and never retries: the server logs only operation/category metadata, returns the existing sanitized 500 response, and does not claim rollback or success. The caller reconciles through `GET /api/v1/patients/{patientId}/relationships?kind=GUARDIAN`; this does not create idempotency. A lost HTTP response after a confirmed commit follows the existing contract: repeating Create can return overlap conflict and repeating End with the prior ETag returns 412.

## RELIABILITY-003A-TEST — PostgreSQL transactional retry proof

The proof uses the real `ManageGuardianLinks` Application flow and a Testcontainers PostgreSQL database. A test-only EF Core command interceptor replaces the first `SELECT … FOR UPDATE` command with PostgreSQL `RAISE EXCEPTION` carrying SQLSTATE `40001` or `40P01`. PostgreSQL therefore aborts the actual transaction at the same lock boundary used in production; no application endpoint, request header, host configuration, or production fault switch exists. The second attempt runs through the normal transaction, lock, query, invariant, `SaveChangesAsync`, and `CommitAsync` code.

The Create proof confirms two lock acquisitions, one persisted link only, initial version `1`, and no `idle in transaction` session after completion. The End proof begins from a persisted current link, confirms two lock acquisitions, reloads the persisted ETag/version, retains the historical row, sets the requested `effectiveTo`, and leaves version `2` exactly once. Both assertions query a fresh PostgreSQL DbContext rather than relying only on attempt counters.

The retry-limit proof injects `40001` on both lock attempts and confirms exactly two attempts, a sanitized `INTERNAL_ERROR`, no persisted GuardianLink, and no idle transaction. A test-only transaction interceptor throws a timeout immediately before `CommitAsync`; its result is intentionally not asserted as committed or rolled back, and the proof confirms one commit call, one lock acquisition, no automatic reexecution, and the sanitized error.

Normal overlap and primary conflicts, a stale ETag, and a PostgreSQL `23505` constraint error each execute once without retry. The application now also converts the final eligible-abort failure and other non-commit transaction failures to the same sanitized result instead of leaking an exception after the final attempt. Commit-phase failures remain separately classified as unknown and are never eligible for retry, even if an inner provider exception carries a retryable SQLSTATE.

Validated targeted result: `GuardianLinkRetryPostgreSqlTests` 7/7 passed against Testcontainers PostgreSQL. This is the fault-injection proof at the Application/Infrastructure boundary; the separate full HTTP and multi-request concurrency proofs are recorded below.

## IMP-003A-TEST-HTTP — GuardianLink HTTP validation

Executed on 2026-09-28 using the existing `Fisiofit.ApiTests` TestServer host, its test-only authentication handler, and a real PostgreSQL 18 Testcontainers database with the existing Organization, People and Patients migrations. Fixtures create only fictitious Clinic/Unit, Person and PatientProfile records directly in the isolated test database; every Guardian operation itself is invoked over HTTP and mutation assertions read a fresh PatientsDbContext.

`GuardianLinkEndpointsTests` adds 10 independent HTTP tests. The targeted matrix passed 10/10, and the complete ApiTests suite passed 22/22 (the earlier 12 plus these 10).

- Create: 201 body/ETag/version/persistence, no `Idempotency-Key` and no individual Location; malformed/invalid UUID, missing/non-current Person, missing Patient, invalid/inverted/retroactive periods; same-guardian overlap, overlapping primary conflict, zero-primary and adjacent half-open periods.
- List: minimized Guardian DTO, per-item strong ETag, history/pagination/default descending ordering, `effectiveOn` half-open boundaries and future state; empty, missing/other `kind`, invalid date/paging/route and unknown query parameter.
- End: successful persisted historical end/version increment, 428 missing If-Match, 412 stale/reused tag, malformed IDs/body, foreign-patient link concealment, invalid/retroactive period, and no physical deletion.
- Minor safety: ending the only current guardian of an active minor returns `422 GUARDIAN_COVERAGE_REQUIRED` without mutation; a non-primary alternative covering continuously to adulthood permits End.
- Security/privacy: all three mappings require authentication; missing permission, explicit deny and inactive account return 403; the approved scoped-resource concealment remains 404 for an out-of-scope Unit (API-001 §27.1.5), rather than a 403 that could enumerate patients. Create/List/End success and tested error responses carry `Cache-Control: no-store`; Guardian responses omit CPF, phone, birth date, clinical/financial fields, EF entities and internal errors. Tested Problem Details include canonical `type`, `status`, `code`, `traceId` and sanitized body behavior.

### HTTP corrections discovered and made

1. Guardian routes used `:guid` constraints, turning invalid route identifiers into framework 404s. The routes now parse identifiers inside the Guardian endpoint boundary and emit canonical 400 `VALIDATION_ERROR`.
2. Relationship-list query keys outside the approved whitelist were silently accepted. They now return canonical 400.
3. The list serialized an implementation-shaped nested `link` plus a top-level `displayName`. It now emits the approved flattened GuardianLink facts and `guardian: { displayName }` projection.
4. Guardian error responses, including automatic malformed-JSON failures, did not consistently emit `Cache-Control: no-store` and automatic Problem Details used a framework RFC URI. The host now applies `no-store` to Guardian paths and canonicalizes automatic Problem Details URI/code without adding a second error handler.

## IMP-003A-TEST-CONCURRENCY — real PostgreSQL concurrency matrix

Executed on 2026-09-28 in `GuardianLinkConcurrencyPostgreSqlTests` against the existing PostgreSQL 18 Testcontainers fixture. Each competing operation uses its own `PatientsDbContext`, connection, transaction and real `ManageGuardianLinks` instance. A test-only EF command interceptor pauses the first operation only *after* PostgreSQL has completed the production `SELECT … FOR UPDATE`; the second operation is then started with `Task.WhenAll` coordination. The test queries `pg_stat_activity` and requires a second `FOR UPDATE` session with `wait_event_type = 'Lock'` before it releases the first operation. This proves database contention rather than an in-memory/sequential simulation. Every scenario has a 15-second cancellation/await timeout and verifies the final state through a new DbContext; it also asserts that PostgreSQL has no `idle in transaction` sessions.

The targeted result is 7/7 passing:

- Create/Create, same guardian and overlapping periods: first Create persists; second returns `409 CONFLICT`; exactly one link remains.
- Create/Create, different overlapping primaries: only one primary link persists; the other returns `409 CONFLICT`.
- Create/Create, different overlapping non-primaries: both commits and two links persist.
- Create/End with End holding the PatientProfile lock first: ending the current link at `effectiveTo` then creates the same guardian at that exact boundary. The persisted intervals are adjacent `[from, boundary)` and `[boundary, null)`, history is retained and the ended row is version 2.
- Create/End with Create holding the lock first: the Create observes the existing open interval after its lock acquisition and returns `409 CONFLICT`; End then succeeds, so no invalid overlap is persisted.
- End/End with the same ETag: exactly one end succeeds; the competing request returns `412 CONCURRENCY_CONFLICT`; the persisted row has the requested `effectiveTo`, one end evidence record and version 2.
- Controlled minor fixture: two current legal links are ended concurrently. One end succeeds and the other returns `422 GUARDIAN_COVERAGE_REQUIRED`; a fresh read confirms one current link remains. This uses only direct fixture data and does not expose or add a public minor-registration endpoint.

No production behavior was changed for the matrix. The existing local Patients `READ COMMITTED` transaction, PatientProfile `SELECT … FOR UPDATE`, post-lock GuardianLink reread, overlap/primary/minor-coverage validation and post-lock ETag validation are the behavior under test. The pre-existing bounded retries remain limited to proven `40P01`/`40001` aborts before commit; commit-outcome-unknown errors remain non-retryable and no Guardian receipt/idempotency key was added.

No correction to the GuardianLink production implementation or migration was necessary: the matrix passed while exercising the existing locking and reread path. The only additions are the isolated PostgreSQL concurrency proof and this evidence record.

Final validation on 2026-09-28: `dotnet restore Fisiofit.slnx` passed; build passed with 0 warnings and 0 errors; `dotnet test Fisiofit.slnx` passed 118/118 — Unit 39, Integration 43 (PostgreSQL real/Testcontainers), API 22 and Architecture 14. The stated baseline matches exactly. Durable Audit, production IAM/activation, historical correction, minor registration, new-Person resolution and non-self payer remain gated. IMP-003A is **DONE / PASS** only for Guardian Links for Existing Persons.

## Next action

Review the roadmap for the operational MVP: real authentication, administrative UI, patients, professionals, scheduling, classes and assisted clinic operation. Keep GuardianLink dependencies, minor registration and non-self payer documented; do not promote a new feature to READY without approved design.
