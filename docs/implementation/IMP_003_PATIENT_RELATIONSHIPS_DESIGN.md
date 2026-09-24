# IMP-003 — Patient Relationships Design

## 1. Status

**DONE — PASS (design only).**

This document selects and fully delimits the first implementation slice, but implements no code, migration, endpoint, or production activation.

Selected next slice: **IMP-003A — Guardian Links for Existing Persons**.

Implementation readiness: **READY** for the delimited local backend slice after API-001 §27.1 and §42 published the definitive Create/List/End contracts on 2026-09-23. This reconciliation does not start IMP-003A. External/production activation remains **GATED** by production IAM and durable Audit. Minor registration and different-payer registration remain independently **GATED**.

Sources: `PROJECT_OS.md` source hierarchy and gates; `MODEL_001_PEOPLE_PATIENTS_STAFF_ORGANIZATION.md`; `DB_001_LOGICAL_DATA_MODEL.md`; `API_001_APPLICATION_API_CONTRACTS.md`; `AUTH_001_PERMISSIONS_POLICIES.md`.

## 2. Objective

Define the minimum evolution of Patients needed to represent relationships between a `PatientProfile` and other people while preserving bounded-context ownership, temporal history, privacy, authorization, and the already delivered IMP-001/002 behavior.

The design distinguishes all four canonical concepts and selects only one complete lifecycle for the next implementation. It does not treat the existence of a database row as completion of minor registration or third-party payer workflows.

Sources: `docs/domain/01-people-patients.md`; `MODEL_001`; `MODEL_005_INTEGRATED_CONCEPTUAL_MODEL.md`; IMP-001/002 design and result documents.

## 3. Canonical Inputs

The following inputs were read and govern this design:

- `PROJECT_OS.md`, including the latest IMP-002 handoff;
- `docs/ai/FISIOFIT_AI_PROFILE.md`;
- `docs/architecture/CONTEXT_MAP.md`, `OWNERSHIP_MAP.md`, `ARC_003_PHYSICAL_ARCHITECTURE.md`, and `DOMAIN_EVENTS.md`;
- `docs/database/DB_001_LOGICAL_DATA_MODEL.md`;
- `docs/api/API_001_APPLICATION_API_CONTRACTS.md`;
- `docs/security/AUTH_001_PERMISSIONS_POLICIES.md`;
- `docs/state-machines/STATE_001_STATE_MACHINES.md`;
- `docs/modeling/MODEL_001_PEOPLE_PATIENTS_STAFF_ORGANIZATION.md` and `MODEL_005_INTEGRATED_CONCEPTUAL_MODEL.md`;
- `docs/domain/01-people-patients.md`, `GLOSSARY.md`, and `BUSINESS_PARAMETERS.md`;
- `docs/business-rules/RULES_INDEX.md`, `docs/processes/PROCESS_INDEX.md`, and `docs/decisions/DECISIONS.md`;
- IMP-001 and IMP-002 design/result documents;
- relevant ADRs: ADR-001 module boundaries, ADR-002 persistence isolation, ADR-003 module communication, ADR-004 event reliability, ADR-005 Clinical isolation, ADR-006 UUID identifiers, and ADR-007 HTTP contracts. Authorization is governed by AUTH-001; time and persisted concurrency versions by DB-001.

Where a canonical source is silent, this document uses `OPEN_DECISION` or a deliberately narrow slice restriction. It does not promote an assumption to a general domain rule.

## 4. Current Implementation Baseline

The inspected implementation has three separate Registry bounded contexts despite their shared physical assembly:

- Organization owns `Clinic`, `Unit`, `OrganizationDbContext`, Unit validation, and Unit read contracts;
- People owns `Person`, `ContactPoint`, CPF normalization/uniqueness, `PeopleDbContext`, and purpose-specific create/read/search contracts;
- Patients owns `PatientProfile`, registration receipts, `PatientsDbContext`, patient registration/detail/search workflows, and patient endpoints.

The physical database currently contains Organization's Clinic/Unit baseline, People's `person`, `contact_point`, and local registration receipt, and Patients' `patient_profile` and local registration receipt. No patient relationship entity or table exists. There is no cross-schema foreign key or shared `DbContext`.

`POST /api/v1/patients` supports only adult + `SELF`, requires `Idempotency-Key`, validates Unit before People writes, and coordinates local People then Patients transactions with receipts. It rejects minors and non-self payers before creating a Person. `GET /api/v1/patients/{id}` composes Patients, People, and Organization through contracts. `GET /api/v1/patients` applies permission and `UNIT_SCOPE`, then composes scoped Patients candidates with People filtering/paging and Organization Unit reads. Read responses use `Cache-Control: no-store` and mask CPF; list also minimizes phone and omits birth date/person ID.

The current People creation contract creates for patient registration and does not provide a global People lookup or a relationship-purpose create/reuse operation. The current Patients receipt is specific to `RegisterPatient`; it must not be repurposed implicitly.

Sources: inspected `src/backend/Fisiofit.Api`, `Fisiofit.ModuleContracts`, Registry Organization/People/Patients, migrations, and `tests/backend`; IMP-001 and IMP-002 result documents.

## 5. Relationship Concepts

| Concept | Meaning | Canonical owner | Distinguishing effect |
|---|---|---|---|
| `GuardianLink` | Legal responsibility relationship to a patient | Patients | Legal representation; does not imply payer, administrative authority, clinical consent, or portal access |
| `AdministrativeResponsibleLink` | Delegated administrative responsibilities for stated purposes | Patients | Administrative actions only; no automatic legal or financial authority |
| `ResponsiblePayerLink` | Person financially responsible for future patient facts during a period | Patients | Future financial responsibility; does not rewrite Contract/Receivable/Payment snapshots |
| `EmergencyContact` | Contact to use for emergency communication | Patients | Contactability and priority only; no legal, administrative, clinical, or financial authority |

The four meanings require separate models. No generic `patient_relationship` table is approved. One `Person` may be referenced by any combination of the four links without duplication of civil identity.

Sources: `MODEL_001` relationship sections and invariants; `DB_001` Patients logical model; `OWNERSHIP_MAP`; `01-people-patients.md`.

## 6. Slice Alternatives

### A. Guardian lifecycle for an existing patient

Dependencies are an existing `PatientProfile`, an existing canonical `Person`, a minimal People validation/read contract, a Patients migration, and Patients authorization. It yields immediate value for legal-representative history and is a direct predecessor to minor registration. It does not require modifying `POST /patients`, creating a Person, writing another context, or integrating Billing. Temporal overlap, principality, lifecycle, concurrency control, and resource authorization can be delivered in isolation. This is the selected, narrowed alternative.

### B. Responsible payer lifecycle for an existing patient

It requires an existing/reused Person, Patients persistence, `patients.payer.change`, temporal replacement, mandatory idempotency, and the adopted `ResponsiblePayerChanged` contract. Its real end-to-end value depends on future Contract/Billing consumption and snapshot rules. A payer link alone cannot unlock non-self-payer registration or rewrite earlier obligations. It is independently deliverable, but less suitable as the first relationship slice because its downstream contract is broader.

### C. Minor registration with guardian

It must create or resolve both the minor's Person and guardian's Person, create `PatientProfile` and at least one current `GuardianLink` together in Patients, validate Unit and scope, preserve retries across local transactions, and prevent a valid minor profile without a guardian. It necessarily changes or adds to the patient-registration workflow. It is not a small first relationship slice and remains gated by legal/consent decisions and production Audit/IAM.

### D. Administrative responsible lifecycle

It can be optional for adults, temporal, multiple, and principal by communication purpose. However, canonical `OQ-M001-005` leaves the catalog of authorized administrative actions open. Without that catalog, neither invariants nor least-privilege response semantics are closed. The slice is **BLOCKED** on that decision.

### E. Emergency contact lifecycle

It has clear priority and non-authority semantics but canonical `OQ-M001-001` leaves open whether every contact must be a `Person` or may be a structured external contact. That decision changes ownership attributes, contracts, validation, and migration shape. The slice is **BLOCKED** on that decision.

### F. People resolution/creation as a prerequisite slice

A purpose-specific create-or-resolve capability would support brand-new guardians, payers, and administrative responsible people, including CPF conflicts and weak duplicate signals. It is useful, but by itself does not create a patient relationship and therefore is not selected as the first Patient Relationships slice. It follows IMP-003A before workflows that require a not-yet-registered Person.

No artificial ranking was used. Selection follows bounded scope, canonical closure, and end-to-end completeness within an explicit precondition.

Sources: `MODEL_001` open questions and relationship rules; `API_001` CMD-018 through CMD-021 and QRY-064; `DOMAIN_EVENTS.md`; IMP-001 gates.

## 7. Selected First Slice

**IMP-003A — Guardian Links for Existing Persons** provides a complete backend lifecycle for a GuardianLink whose patient and guardian Person already exist:

1. create a present or future GuardianLink;
2. list current and historical guardian links for the patient;
3. end a current link without physical deletion;
4. preserve multiple guardians and at most one overlapping primary legal guardian;
5. validate the guardian Person through a minimal public People contract;
6. authorize all operations against the patient's primary Unit;
7. protect creation/ending against concurrent duplicates and lost updates through PatientProfile serialization, local constraints, and `If-Match` on end.

The slice deliberately does not create a new Person and does not claim to complete minor registration. Its precondition is an already-known `guardianPersonId`. This is a complete lifecycle for that supported input, not a partial minor-registration flow.

Sources: `MODEL_001` GuardianLink; `DB_001` temporal conventions and DB-INV-009; `API_001` CMD-018/QRY-064; `AUTH_001` Patients permission catalog.

## 8. In Scope

- Patients domain model and persistence for `GuardianLink` only;
- purpose-specific People validation/read contract for an existing Person;
- create, list, and end operations;
- current and historical periods;
- multiple guardians and primary legal guardian invariant;
- resource authorization using patient `primaryUnitId` and `UNIT_SCOPE`;
- masked/minimized response DTOs, `no-store`, sanitized logs;
- local PostgreSQL, API, architecture, and IMP-001/002 regression tests;
- internal `GuardianLinked` domain event in the same local transaction/outbox only if the existing event mechanism is introduced by the implementation; no external consumer is required for DoD.

## 9. Out of Scope

- creating, searching globally, merging, or editing a `Person`;
- minor patient registration;
- changing `POST /api/v1/patients`;
- administrative responsible, payer, or emergency-contact persistence/endpoints;
- clinical consent, portal access, clinical permissions, Contract, Billing, Receivables, or Payments;
- UI/frontend;
- physical deletion or general historical correction;
- cross-context transactions, joins, foreign keys, or shared persistence;
- production activation and an improvised AuditLog.

## 10. Gated / Deferred

- Minor registration remains **GATED** until its full workflow and legal/consent prerequisites are designed and implemented.
- Different payer registration remains **GATED** until payer lifecycle plus Contract/Billing forward-use semantics are delivered.
- Administrative responsible remains **GATED** by `OQ-M001-005`.
- Emergency contact remains **GATED** by `OQ-M001-001`.
- New-Person creation/reuse for relationships is **DEFERRED** to a dedicated People + relationship orchestration slice.
- Retroactive creation, historical correction, and backdating are **GATED** pending business authority and Audit requirements. IMP-003A rejects `effectiveFrom` before the current business date.
- Atomic guardian substitution required for an active minor is **DEFERRED** to the minor-registration sequence. Adults can end then create; no minor is introduced by IMP-003A.
- IAM for production, durable Audit, and external activation remain **GATED**.

Sources: `PROJECT_OS.md`; `MODEL_001` open questions; `AUTH_001`; `DECISIONS.md`; IMP-001 design/result.

## 11. People Ownership

People remains the sole owner of civil identity, CPF, name, birth date, contact points, deduplication signals, and Person lifecycle. Patients may retain only the guardian Person identifier plus relationship-specific facts.

IMP-003A introduces no People entity or migration. It consumes a purpose-specific contract that answers whether an exact `personId` resolves to a current canonical Person and supplies the minimum display projection. It exposes no EF entity and no general repository.

No Patient workflow may query `PeopleDbContext`, `people.*`, or People internals.

Sources: `OWNERSHIP_MAP`; `CONTEXT_MAP`; ADR-001/002; `MODEL_001`; `ARC_003`.

## 12. Patients Ownership

Patients owns `PatientProfile`, GuardianLink identity, patient reference, guardian Person reference, optional relationship label, effective period, primaryity, lifecycle transition, and operational evidence. Only `PatientsDbContext` writes these records.

The link is a child of `PatientProfile`; the database has a local foreign key to `patients.patient_profile` and stores `guardian_person_id` as an external identifier with no physical cross-schema foreign key.

Sources: `MODEL_001`; `DB_001`; `OWNERSHIP_MAP`; ADR-002.

## 13. Person Resolution / Creation

IMP-003A behavior is explicit:

1. **Existing Person by `personId`:** People resolves the exact ID. A current canonical Person succeeds; missing/inactive/non-canonical outcomes fail closed before Patients writes. If future Person merge aliases exist, People—not Patients—returns the canonical ID and resolution status.
2. **Person not registered:** the request does not accept inline civil data. It fails with a typed `PERSON_NOT_FOUND`/`PERSON_CREATION_REQUIRED` outcome. A later slice supplies purpose-specific create/reuse orchestration.
3. **CPF already belongs to another Person:** not reachable through the IMP-003A request because CPF is not accepted. In the future creation slice, People must return the existing strong-identity conflict/candidate; it must not create a second Person.
4. **CPF absent:** outside IMP-003A. A future People command may create only under People rules and must preserve weak-duplicate review.
5. **Divergent civil data:** outside IMP-003A; future resolution must return conflict/review and must not overwrite canonical civil data through a relationship command.
6. **Possible duplicate by name/phone:** never sufficient for automatic reuse or merge. Future flow returns a review signal.
7. **Concurrent requests:** IMP-003A serializes every GuardianLink mutation for the same patient by locking the local PatientProfile row and relies on local constraints as defense in depth. Future Person creation must separately use People-local CPF constraints/receipts.

No global People search endpoint is invented. A caller can use a known Person ID obtained through an authorized existing workflow; discoverability for unrelated Persons is a later, purpose-specific design concern.

Sources: People ownership and dedup rules in `MODEL_001`, `DB_001`, IMP-001 design, and inspected People implementation.

## 14. GuardianLink

Canonical definition: a temporal legal-responsibility relationship between `PatientProfile` and another `Person`.

- owner: Patients;
- Person origin: People canonical `Person`, referenced by ID;
- patient reference: local child of `PatientProfile`;
- cardinality: zero or more for an adult, one or more current links for a valid active minor; multiple guardians are supported;
- canonical attributes: `guardianLinkId`, `patientProfileId`, `guardianPersonId`, optional `relationshipToPatient`, `effectiveFrom`, nullable `effectiveTo`, `isPrimaryLegalGuardian`, creation/end evidence. Because no relationship-label catalog is canonically approved, IMP-003A does not accept or persist `relationshipToPatient`; that optional attribute is deferred rather than represented as arbitrary text;
- lifecycle: scheduled/current/history derived from period; no physical delete;
- primaryity: at most one overlapping primary legal guardian for the same patient;
- invariant: guardian Person differs from the patient's Person in IMP-003A;
- permission: `patients.guardian.manage` for create/end and authorized Patients read permission for view, always resource-scoped;
- patient lifecycle: create requires an `ACTIVE` PatientProfile; authorized list and end remain available for an inactive profile so history can be inspected and an outstanding link can be closed without reactivating the patient;
- event: `GuardianLinked` is canonical `DOMAIN_EVENT / INTERNAL_ONLY`; no authorization is derived from it;
- impact: future minor-registration and authorized clinical policies may consult the link, but IMP-003A grants no clinical/portal authority;
- Contracts/Billing: no automatic payer role and no financial snapshot impact.

`relationshipToPatient` reconciliation is closed for this slice: MODEL-001 declares it optional (`relationshipToPatient?`); DB-001 lists a relationship attribute but does not make it a logical mandatory invariant; CMD-018 inputs are patient/person, period and primaryity and do not require it; RULES_INDEX, DECISIONS and the domain document approve no vocabulary. No canonical catalog exists. It is a kinship/general-relationship descriptor, not proof of legal authority: `MOTHER`/`FATHER`, if ever approved, could not by itself create legal responsibility, and a legal guardian need not be a parent. Legal authority exists only through GuardianLink. Therefore IMP-003A neither accepts nor persists this field, and its future addition requires an approved catalog but no change to the GuardianLink legal meaning.

Replacing a guardian does not overwrite `guardianPersonId`. It ends the old link and creates another. Atomic substitution is mandatory before minors can be supported; it is not needed to make the selected adult/existing-patient lifecycle complete.

An expired link is historical, cannot authorize current action, and remains queryable to authorized administrators. API-001 does not require an idempotency key for `ManageGuardian`: a repeated end with a stale `If-Match` returns `412`, while an end against an already-ended current version returns a domain conflict without a second effect.

Sources: `MODEL_001` GuardianLink and INV-PAC rules; `STATE_001`; `DOMAIN_EVENTS.md`; `AUTH_001`.

## 15. AdministrativeResponsibleLink

Canonical definition: a temporal link granting explicitly enumerated administrative responsibilities, separate from legal guardianship and payment responsibility.

- owner: Patients;
- Person origin: People `Person`;
- cardinality: potentially multiple, optional for adults, with at most one primary for a defined communication purpose where approved;
- attributes: responsible Person, period, authorized administrative actions, communication primaryity, status/lifecycle;
- authority: only the approved administrative-action catalog; never automatic portal, clinical, legal, or financial permission;
- temporal behavior: history is preserved; ending does not mutate earlier acts;
- minor relation: may be useful but is not a substitute for `GuardianLink`;
- Contracts/Billing: may perform approved administrative steps only; does not become payer by implication.

`OPEN_DECISION / GATED`: `OQ-M001-005` must close the exact action catalog, whether primaryity is global or per purpose, and when it is mandatory. No table or endpoint is designed for implementation yet.

Sources: `MODEL_001`; `AUTH_001`; `API_001` CMD-019.

## 16. ResponsiblePayerLink

Canonical definition: a temporal financial-responsibility link from a patient beneficiary to a payer Person.

- owner: Patients;
- cardinality: historical 0..N, with at most one overlapping primary current payer;
- attributes: payer Person, optional relationship label, period, primaryity, lifecycle/evidence;
- Person may be the patient or another Person;
- change ends the prior period and creates a new period; it never overwrites history;
- adopted event: `ResponsiblePayerChanged`, containing minimum identifiers and effective date;
- Contract/Billing: future Contracts/Receivables use the applicable payer; existing Contract, Receivable, Payment, and accounting snapshots remain unchanged.

The link alone changes no already existing financial contract. Different-payer patient registration remains gated until Person resolution/creation, patient workflow, payer link creation, failure recovery, Contract/Billing consumption, and authorization are complete.

Sources: `MODEL_001`; `STATE_001`; `DB_001` DB-INV-009; `DOMAIN_EVENTS.md`; `API_001` CMD-020.

## 17. EmergencyContact

Canonical definition: a prioritized contact used for emergency communication, not an authority role.

- owner: Patients;
- multiplicity: one or more may be ordered by priority; exact mandatory cardinality is not approved;
- expected attributes: optional Person reference, priority, relationship label, lifecycle/status, and a reachable telephone when the final representation is approved;
- access: only authorized, purpose-bound users; disclosure is minimized and logged safely;
- relation to other links: the same Person can also have another explicit link, but emergency status alone grants no legal, administrative, clinical, portal, or financial authority.

`OPEN_DECISION / GATED` (`OQ-M001-001`): whether it must reference an existing Person or may store a structured external contact. Consequently, mandatory telephone ownership, attributes, constraints, and endpoints are not implementation-ready.

Sources: `MODEL_001`; `DB_001`; `API_001` CMD-021.

## 18. Minor Registration Dependencies

IMP-001 correctly rejects minors. IMP-003A does not remove that gate because every currently creatable patient is adult and the slice does not atomically create a minor profile with a guardian.

To remove the gate, a later design must close and implement all of the following:

- create/resolve the minor Person;
- create/resolve at least one distinct guardian Person;
- validate Unit before writes and enforce `UNIT_SCOPE`;
- in one Patients-local transaction create `PatientProfile` plus at least one effective GuardianLink, so no valid minor profile exists without a current guardian;
- define guardian primaryity and atomic replacement without a guardianless window;
- coordinate People-local transactions and Patients-local transaction with operation-specific receipts, retries, and no distributed transaction;
- define payer mode (SELF is not automatically valid for a minor), optional administrative responsible, and downstream Contract/Billing behavior;
- close legal consent/authority evidence and durable Audit requirements;
- preserve the existing adult/SELF request and regression behavior.

The likely public API is a separate minor workflow or a discriminated extension only after its contract is approved. The current POST must not be relaxed merely to reuse it.

Sources: IMP-001 design/result; `MODEL_001` INV-PAC-004; `API_001`; `AUTH_001`.

## 19. Payer Registration Dependencies

Different-payer registration requires more than a `ResponsiblePayerLink` row:

- payer Person create/reuse resolution;
- current primary payer invariant and temporal replacement;
- idempotent patient + payer workflow with local transaction boundaries;
- explicit response and recovery when People commits but Patients fails;
- adopted `ResponsiblePayerChanged` publication semantics;
- Contract/Billing rule for future use and snapshot preservation;
- permissions, Unit scope, privacy, and durable Audit for external activation.

Until those are complete, `POST /api/v1/patients` continues rejecting non-`SELF` payer mode exactly as IMP-001 specifies.

Sources: `MODEL_001`; `DOMAIN_EVENTS.md`; IMP-001; `API_001` CMD-020.

## 20. Temporal Model

All effective periods use DB-001's half-open convention:

- `effectiveFrom` is inclusive;
- `effectiveTo` is exclusive or null;
- effective on date `d` means `effectiveFrom <= d && (effectiveTo is null || d < effectiveTo)`;
- `effectiveTo` must be greater than `effectiveFrom`.

For IMP-003A:

- current and future creation is allowed; `effectiveFrom` before the current business date is rejected as `RETROACTIVE_RELATIONSHIP_NOT_SUPPORTED` pending a later approved correction/backdating design;
- an active record is one effective on the evaluated date; historical is one whose `effectiveTo <= evaluatedDate`; future is one whose `effectiveFrom > evaluatedDate`;
- API exposes a derived `temporalState` (`FUTURE`, `CURRENT`, `HISTORICAL`) from the period; it does not introduce a second independently mutable lifecycle truth;
- end sets `effectiveTo` and evidence; it never changes guardian Person or start date;
- replacing means end + new row; for adults it may be two explicit operations, while future minor replacement must be atomic;
- equivalent links for the same patient and guardian cannot overlap;
- multiple different guardians may overlap, but primary periods may not overlap;
- direct correction of past dates/person is not exposed in this slice.
- an end command must never create an uncovered interval for an ACTIVE minor: using the patient's birth date resolved from People at command time and the existing IMP-001 boundary of 18 completed years, the post-command union of the other GuardianLink periods must cover continuously from the requested `effectiveTo` until the patient reaches adulthood; otherwise the command fails closed. If the patient is already adult on `effectiveTo`, this minor-only guard does not apply. Reactivation of a minor must independently require a current guardian.

This preserves “the present does not rewrite the past.”

Sources: `DB_001` temporal convention; `STATE_001` temporal entities; `MODEL_001`.

## 21. Cardinality / Principal Relationship

GuardianLink supports multiple links and history. IMP-003A enforces:

- unique link ID;
- guardian and patient Person must differ;
- no overlapping period for the same `(patientProfileId, guardianPersonId)`;
- no two overlapping `isPrimaryLegalGuardian = true` periods for a patient;
- non-primary guardians may overlap;
- at most one overlapping primary for the legal-guardian purpose; zero primary guardians is valid because no canonical source requires exactly one;
- adults may have zero current guardians;
- an ACTIVE minor requires at least one guardian throughout minority; IMP-003A does not create minors, but its end command already fails closed if it would violate that future-safe invariant.

Enforcement allocation:

- database: local patient FK, period check, unique IDs, exact-start duplicate key, indexes, and unique protection for open-ended current primary where representable;
- Patients transaction: use PostgreSQL `READ COMMITTED`, lock the parent `PatientProfile` row with `SELECT ... FOR UPDATE`, then query the now-current committed link set, validate same-guardian overlap, primary overlap and minor coverage, and write the link/end atomically;
- Application: request shape, supported dates, self-reference, People outcome, patient state, and authorization;
- `GATED`: general backdating/correction and minor registration/atomic substitution.

Every create and end path must acquire the same parent-row lock before reading link periods. Under PostgreSQL `READ COMMITTED`, a transaction that waited for the lock performs its invariant queries after the prior commit and therefore sees that mutation. Create/Create, Create/End, and End/End are serialized for one patient without a distributed transaction or cross-context access. Date/evidence checks, local FK, uniqueness, and any representable partial/exclusion constraint remain database defenses; correctness of finite/future interval coordination does not depend only on a pre-insert overlap query. Deadlock or serialization-class transient failures retry the whole Patients transaction a bounded number of times; domain conflicts and stale `If-Match` are returned and are never blindly retried.

Sources: `DB_001` DB-INV-009 and concurrency strategy; `MODEL_001`; `STATE_001`.

## 22. Application Workflow

### Create

1. Authenticate ACTIVE account; apply explicit deny and `patients.guardian.manage` before business data.
2. Validate route/request shape and dates. `Idempotency-Key` is not required by API-001 and is not introduced by this slice.
3. Load PatientProfile in Patients, fail closed if missing, and authorize its `primaryUnitId` against `UNIT_SCOPE`.
4. Call People's exact-ID validation contract; reject missing/non-current Person and canonicalize only on People instruction.
5. Reject the patient's own Person ID.
6. In one Patients-local `READ COMMITTED` transaction, lock the PatientProfile row, recheck state/resource identity and temporal overlaps, insert GuardianLink, and optionally persist the internal event/outbox record.
7. Return `201 Created` with minimized representation and `ETag` for the created link. If the response is lost, a retry may deterministically return an overlap/conflict; API-001 does not promise replay of the original response.

### List

1. Authenticate/authorize read permission and scope before composition.
2. Execute canonical `GetPatientRelationships` with `kind=GUARDIAN`, optional `effectiveOn`, and offset pagination; absence of `effectiveOn` returns the authorized GuardianLink history.
3. Resolve distinct guardian IDs through a batch, purpose-specific People read contract.
4. Fail closed on broken references; return minimized, masked DTOs with `no-store`.

### End

1. Authenticate and require `patients.guardian.manage`; require `If-Match` for the GuardianLink version.
2. Load patient and link, apply Unit scope, validate the effectiveTo request shape, and resolve the patient's birth date from People for the minor-safety guard.
3. In one Patients-local `READ COMMITTED` transaction, lock PatientProfile, reload the link/version and all relevant periods, reject stale `If-Match` before state/temporal domain checks, validate the requested exclusive effectiveTo, recheck overlaps and continuous guardian coverage through adulthood for an ACTIVE minor, then set end evidence atomically.
4. Return the ended representation and new `ETag`; never delete. A future minor substitution command must end/create atomically and is outside IMP-003A.

`POST /api/v1/patients` is untouched.

## 23. Public Contracts

Contracts follow the real purpose-specific `Fisiofit.ModuleContracts` style; no EF type or generic repository is exposed.

### People contracts

Conceptual names (final namespace/style follows current code):

- `IValidatePersonForGuardianLink.ValidateAsync(personId)` → `Found(canonicalPersonId, lifecycleState)` or typed `NotFound`/`NotCurrent`;
- `IGetPatientBirthDateForGuardianSafety.GetAsync(patientPersonId)` → the minimum `birthDate` fact needed to evaluate the 18-completed-years guard, or typed missing/unavailable; it exposes no other civil data and any missing fact fails the end command closed;
- `IGetPeopleForGuardianLinkRead.GetByIdsAsync(distinctPersonIds)` → minimal records with `personId` and `displayName` needed by the composing Patients handler; no CPF or phone.

The GuardianLink read contract omits CPF, phone and birth date. Only the separate internal guardian-safety contract supplies birth date when required; the HTTP response never includes it. Neither contract returns Person EF entities.

### Patients contracts

- `CreateGuardianLinkCommand` and result;
- `GetGuardianLinksQuery` and result;
- `EndGuardianLinkCommand` and result.

Patients endpoints call Patients Application services only. People contracts are orchestrated inside the module boundary, and no `IPersonRepository` is introduced.

Sources: inspected ModuleContracts conventions; `ARC_003`; ADR-001; IMP-001/002 designs.

## 24. HTTP Contracts

### Definitive API-001 contracts

The 2026-09-23 reconciliation is published in [API-001 §27.1](../api/API_001_APPLICATION_API_CONTRACTS.md#271-imp-003a--guardian-http-contract-reconciliation) and its endpoint catalog (§42). `API-001-IMP-003A-ROUTE-MAPPING` is **CLOSED**. These are contracts for future implementation, not delivered endpoints.

| Operation | Canonical contract / route | Request | Success |
|---|---|---|---|
| Create | CMD-018: `POST /api/v1/patients/{patientId}/guardians` | `guardianPersonId`, `effectiveFrom`, optional nullable `effectiveTo`, `isPrimary` | 201 GuardianLinkResult + ETag; no-store |
| List | QRY-064: `GET /api/v1/patients/{patientId}/relationships?kind=GUARDIAN` | optional `effectiveOn`, page/pageSize, sort | 200 paginated PatientRelationshipView with link ID, version and per-item etag; no-store |
| End | CMD-018: `POST /api/v1/patients/{patientId}/guardians/{guardianLinkId}/end` | `effectiveTo`; required If-Match | 200 GuardianLinkResult + new ETag; no-store |

Reconciled details:

- `isPrimary` is the HTTP name of the existing conceptual `isPrimaryLegalGuardian` attribute. The earlier JSON name is superseded; domain meaning is unchanged. No relationship label or end reason is accepted.
- Create and End neither require nor consume `Idempotency-Key`; no GuardianLink receipt exists. Create requires no If-Match because the child does not yet exist.
- No individual GET GuardianLink route is published, so Create does not emit a Location pointing to an unapproved route. The authorized QRY-064 item supplies its ID, version and exact ETag for End.
- Without `effectiveOn`, List returns historical, current and future periods, not just today's links. Default order is effectiveFrom descending then guardianLinkId ascending; sort whitelist is effectiveFrom ascending/descending. Defaults are page 1/pageSize 25, maximum 100. Only GUARDIAN is supported; other kinds are 400, not implemented relationship flows.
- Empty collection is 200; missing/concealed PatientProfile is 404. The minimized guardian display projection omits CPF, phone and birth date. Create/End return only owned link facts; list adds display name, kind and temporalState.
- End applies once to a current link, accepts today/future exclusive end, never rewrites the past or extends a pre-existing finite end. Future/historical/already-ended links conflict. The continuous minor-coverage guard in section 20 remains mandatory.
- A positive logical version is persisted on each GuardianLink, starts at 1 and increments atomically on mutation. ETag is a strong opaque validator for that link identity/version, not a parent lock, parent version or composite People projection. The client copies the per-item `etag` from QRY-064 to If-Match; it does not synthesize a token from `version`.
- End rechecks the persisted link version after locking/reloading and uses a conditional versioned update in the same transaction. Missing If-Match is **428 PRECONDITION_REQUIRED**, stale/mismatched token is **412 CONCURRENCY_CONFLICT**. Malformed, weak, wildcard or multiple tokens are 400. Two Ends with the same token cannot both mutate the link.
- API-001 §27.1.7 is definitive for RFC 9457 errors, including 409 overlap/state conflict, 422 invalid period/self-guardian/retroactivity/minor-coverage and sanitized 500. Authorization precedes existence/version disclosure.

API-001 §27.1 also records the approval provenance of patients.profile.read and all retry outcomes. No other relationship endpoint, generic Person creation, registration of minors, Billing or Clinical is introduced by this reconciliation.

## 25. Authorization

Every operation requires an authenticated ACTIVE account; explicit deny wins; deny-by-default applies.

- create: `patients.guardian.manage` + resource access to the patient's primary Unit;
- list: existing approved `patients.profile.read` permission + resource access to primary Unit;
- end: `patients.guardian.manage` + resource access;
- future replace: `patients.guardian.manage` + resource access, but it is not in IMP-003A.

People validation/composition also requires the existing `people.person.read` grant, revalidated by People as in IMP-001/002. AUTH-001 §9 explicitly lists `patients.guardian.manage` and `people.person.read`. Its §§8.1/13/26 approve scoped administrative reading, but §9 omits the literal `patients.profile.read`; IMP-001-DESIGN §5 authorized its concretization and approved IMP-002-DESIGN §5 explicitly adopted it. API-001 §27.1.5 records this provenance; no new permission or grant is invented.

The resource is the `PatientProfile`, and `UNIT_SCOPE` is evaluated with `primaryUnitId`. A cross-scope missing/forbidden resource must not leak existence. The actor context is revalidated at the People composition boundary as in IMP-001/002.

Owner/Secretary personas may receive the permission through approved policy; Developer/IT receives no business authority by default. A GuardianLink does not grant the guardian a user account, portal access, clinical permission, consent authority, or administrative access automatically.

Sources: `AUTH_001`; `API_001` §27.1.5 and permission catalog; IMP-001/002 approved authorization contracts.

## 26. Privacy / Audit

Create/end responses contain only link identifiers, period, primaryity, version and ETag. List adds guardian display name, kind and derived temporal state. CPF, birth date and phone are omitted because this lifecycle does not require them. A future communication use case must justify a separate purpose-specific projection; GuardianLink alone does not authorize phone disclosure.

All responses use `Cache-Control: no-store`. Logs contain correlation, operation, actor ID, patient/link IDs, outcome, and timing—not CPF, phone, names, request bodies, idempotency keys, or unredacted Problem Details. Third-party Person data is protected under the same Unit-scoped resource authorization as the patient.

Create and end require durable business-audit evidence: actor, timestamp, before/after period, primaryity, request correlation, and outcome. IMP-003A stores the minimum aggregate/history evidence (`createdAt`, `createdByActorId`, `endedAt`, `endedByActorId`), but this is not a substitute for the canonical durable Audit capability. Operational logging must never be called business audit.

Local behavior can be tested. External/production activation remains gated until durable Audit and production IAM are available.

Sources: `API_001` §27.1 and §49; `AUTH_001`; `DB_001` audit requirements; `PROJECT_OS.md` gates.

## 27. Idempotency

API-001 is definitive: `CMD-018 ManageGuardian` has idempotency `no`, and the endpoint catalog marks guardian creation `—`. Therefore neither create nor end requires or consumes `Idempotency-Key`, no GuardianLink command receipt is added, and the existing `RegisterPatient` receipt remains untouched.

Retry behavior is explicit:

- create response lost: retry is a new evaluation of the same intention, not automatically a new link; duplicate/overlap returns 409 if committed, while date rollover may first produce 422 retroactivity. Reconcile all QRY-064 history pages before deciding another intent; no replay of the original 201 is promised;
- end response lost: a retry with the prior `If-Match` returns `412` after the first commit; it does not apply a second end;
- transport/client retry policy must inspect the result rather than assume exactly-once response replay;
- concurrency correctness comes from the PatientProfile lock, link version, and local constraints, not from idempotency receipts.

Changing `CMD-018` to require an idempotency key would be a separate API contract change and is not necessary for the approved lifecycle.

## 28. Transaction Boundaries

IMP-003A performs no People write.

- People read/validation is a separate contract call with no transaction shared with Patients.
- Patients performs the link mutation, evidence, version change, and optional local outbox event atomically in one Patients-local transaction.
- The transaction uses PostgreSQL `READ COMMITTED`; it locks the PatientProfile row before reading GuardianLink periods. All create/end handlers use this same lock order: PatientProfile first, then GuardianLink rows.
- After a waiter acquires the parent lock, its subsequent queries observe the preceding commit and re-evaluate same-guardian overlap, primary overlap, version and minor coverage.
- No `TransactionScope`, distributed transaction, cross-context unit of work, cross-schema join, or compensating deletion exists.
- Organization is not called because Unit ownership and state are already represented by the existing PatientProfile resource; authorization uses its `primaryUnitId` and actor scope.

If a future policy requires revalidating current Unit state for link mutations, that must use the existing Organization public contract before the Patients transaction and be added explicitly; it is not needed by current canonical CMD-018 rules.

## 29. Partial Failure / Retry

Selected-slice windows:

- **People validation succeeds, Patients fails before commit:** no People state changed; the request can be retried, and no link exists.
- **create commits, HTTP response is lost:** no receipt replay; duplicate/overlap protection prevents a second equivalent link. Reconcile through QRY-064; 409 is usual, but a later business date may first reject retroactivity with 422 (API-001 §27.1.6).
- **end commits, HTTP response is lost:** reuse of the old `If-Match` returns `412`; no second mutation occurs.
- **equivalent or overlapping creates:** parent lock and temporal checks allow one and return conflict for the other.
- **Person becomes non-current between People read and Patients commit:** the accepted command records the canonical ID validated at command time. There is no cross-context atomicity; later Person lifecycle resolution is owned by People. No Patient link is silently repointed.
- **end races with create/end:** both lock the same PatientProfile row in the same order and re-evaluate periods/version/minor coverage after the lock; one observes the other's committed result.
- **deadlock/transient serialization failure:** retry the entire Patients transaction with a small bounded policy; never retry `409`, `412`, authorization failures, or invariant failures as transient.

Future new-Person workflow has two local transactions: People create/reuse then Patients link. If People commits and Patients fails, the Person remains canonical and reusable; it is never deleted as compensation. Operation receipts must resume at Patients. This future behavior is documented but not implemented by IMP-003A.

Sources: ADR-001; `DB_001`; IMP-001 failure-window pattern.

## 30. Persistence Impact

### PatientsDbContext

Add only `guardian_link`. IMP-003A does not change `patients.command_receipt`.

Proposed `patients.guardian_link` columns:

- `guardian_link_id uuid` primary key;
- `patient_profile_id uuid` not null, local FK;
- `guardian_person_id uuid` not null, external reference without FK;
- `effective_from date` not null;
- `effective_to date` null;
- `is_primary_legal_guardian boolean` not null;
- `created_at timestamptz`, `created_by_actor_id uuid` not null;
- `ended_at timestamptz`, `ended_by_actor_id uuid` nullable with coherent all-or-none constraint;
- persisted positive incremental logical `version`, initially 1, used to emit/validate the link ETag and condition the atomic End update; see API-001 §27.1.4. The parent lock is not a replacement for this column.

No independent mutable status column is required: API `temporalState` is derived from the half-open period, preventing time-driven drift. If a later design persists canonical `ACTIVE/ENDED`, it must be derived/generated or transactionally constrained to the dates, never independently editable.

Minimum constraints/indexes:

- check `effective_to IS NULL OR effective_to > effective_from`;
- coherent ending-evidence check;
- unique `(patient_profile_id, guardian_person_id, effective_from)`;
- index `(patient_profile_id, effective_from, effective_to)`;
- index `(guardian_person_id)` for governed reference analysis;
- partial index for current/open primary lookup and database protection where possible;
- all general overlap checks remain serialized in the Patients transaction.

### PeopleDbContext

No entity or schema change. **NO PEOPLE MIGRATION.** Only purpose-specific Application/ModuleContracts reads are added during implementation.

### OrganizationDbContext

No change. **NO ORGANIZATION MIGRATION.** Existing patient Unit ownership and authorization context are sufficient.

Sources: `DB_001` Patients tables/index rules; ADR-002; current migrations.

## 31. Migrations

One future Patients-owned migration is required, tentatively `Patients_AddGuardianLinkLifecycle`:

1. preserve the existing Patients migration and `__EFMigrationsHistory_Patients` history;
2. create only `patients.guardian_link` plus local constraints/indexes;
3. do not alter `patients.command_receipt` or existing IMP-001 rows;
4. do not create the other three future relationship tables;
5. do not add cross-schema FKs or touch Organization/People migration history;
6. no backfill is needed because current IMP-001 profiles are adults and relationships do not exist.

Operational order: deploy migration before application version; application remains backward-compatible with existing adult/self-payer data. Rollback must not be used after relationship data exists without an explicit preservation/export plan.

This design execution creates no migration.

## 32. Backward Compatibility

IMP-003A must not alter the request/response or behavior of:

- `POST /api/v1/patients`: adult + SELF, Unit validation, CPF uniqueness, existing receipt/replay semantics, minor/non-self gates, and Problem Details;
- `GET /api/v1/patients/{id}`: current composition, masking, scope, and no-store; relationships are not embedded automatically;
- `GET /api/v1/patients`: existing search/filter/sort/paging/count, composition, masking, and no-store.

Relationship reads use their own route. No eager loading or new PII is added to current patient responses. Existing migrations and receipts remain valid.

Sources: IMP-001/002 design and result documents.

## 33. Test Plan

### Unit / Application

- period validity, derived temporal state, self-reference rejection;
- existing/current canonical Person succeeds; missing/non-current fails before write;
- multiple non-primary guardians allowed; same-guardian and primary overlaps rejected;
- create, list, end, ended conflict, present/future creation, retroactive rejection;
- exactly zero or one overlapping primary accepted; no test requires exactly one primary;
- end of an ACTIVE minor is rejected when the resulting periods do not cover continuously through adulthood; adult end is allowed; missing birth date fails closed;
- permission, ACTIVE account, explicit deny, Unit scope, resource-not-found behavior;
- persisted link version starts at 1 and increments on End; QRY-064 per-item ETag matches that version and is usable unchanged in If-Match; token from a different link is rejected;
- end version/If-Match, 428 missing / 412 stale / 400 invalid precondition, and absence of GuardianLink command receipts;
- minimal response mapping and broken People reference fail-closed.

### Integration with real PostgreSQL

- Patients migration applies in its own history table and preserves existing data/receipts;
- FK only to `patients.patient_profile`; no cross-schema FK;
- constraints and indexes exist;
- present/future/history periods persist correctly;
- `patients.command_receipt` remains unchanged;
- equivalent/primary-overlap races yield one success and one conflict;
- Create/Create, Create/End, and End/End races acquire the same parent lock, re-read after waiting, and preserve finite/future overlaps plus minor coverage;
- bounded retry covers only transient database failures;
- People and Organization schemas remain unchanged and DbContexts map only their owner schema.

### API

- create/list/end success through the reconciled routes;
- no Idempotency-Key consumption for create/end; lost-response retry behavior follows API-001 §27.1.6, including date rollover and history reconciliation;
- validation, 428 for missing / 412 for stale If-Match on end, 400 for unsupported token forms, unknown Person, patient/link not found, overlap, ended conflict, and minor-without-guardian rejection;
- anonymous, inactive account, missing permission, explicit deny, wrong Unit scope;
- response PII minimized/masked and `Cache-Control: no-store`;
- Problem Details sanitized and no cross-scope existence leak.

### Architecture

- Patients source contains no People/Organization DbContext or schema access;
- People contracts expose DTOs only, not EF entities;
- API Host contains no workflow/DbContext;
- no shared/generic Person repository, cross-schema join/FK, or distributed transaction;
- Registry physical assembly still preserves internal bounded-context boundaries.

### Regression

- all IMP-001 unit/integration/API/architecture tests;
- all IMP-002 unit/integration/API/architecture tests;
- explicit adult/SELF, minor rejection, different-payer rejection, CPF uniqueness, Unit validation, scope, masking, paging/search/composition, idempotency, and Problem Details cases.

No tests for creation of new Persons, minor registration, payer/admin/emergency persistence, Billing, portal access, or UI belong to IMP-003A.

## 34. Expected File Plan

Future IMP-003A implementation is expected to touch only the necessary files under:

- `Fisiofit.ModuleContracts/People` for purpose-specific validation/read DTOs;
- Registry `People/Application` for contract implementations;
- Registry `Patients/Domain`, `Application`, `Infrastructure`, and endpoints;
- Registry DI registration;
- Patients-only migration and snapshot;
- Unit, Integration, API, and Architecture tests;
- generated API contracts must follow the already-reconciled `API_001_APPLICATION_API_CONTRACTS.md` §27.1/§42 without changing CMD-018 idempotency;
- `PROJECT_OS.md` and an IMP-003A result document.

It must not rewrite IMP-001/002 handlers or create unrelated relationship entities.

## 35. Definition of Done

IMP-003A is done only when:

- create/list/end work end-to-end for existing patient + existing canonical Person;
- all ownership and no-cross-schema rules hold;
- temporal history, same-guardian overlap, and primary overlap are concurrency-safe;
- no physical delete or personId overwrite occurs;
- API-001's non-idempotent `ManageGuardian` contract is preserved; concurrency uses parent locking, local constraints, and end `If-Match`;
- permission, ACTIVE account, explicit deny, Unit scope, and resource authorization pass;
- PII is minimal/masked, responses are no-store, and logs are sanitized;
- Patients migration applies in real PostgreSQL without changing People/Organization;
- Create/List/End conform to the definitive API-001 §27.1/§42 mappings and error/precondition contracts;
- `GuardianLinked` remains internal-only and grants no authority;
- IMP-001/002 full regression passes;
- minor and different-payer gates remain enforced;
- durable Audit/production IAM gates are still declared for activation;
- implementation result and PROJECT_OS handoff are updated.

## 36. Risks / Open Decisions

- `OPEN_DECISION`: approved administrative-action catalog (`OQ-M001-005`).
- `OPEN_DECISION`: Person-backed versus structured external emergency contact (`OQ-M001-001`).
- `OPEN_DECISION`: general historical correction/backdating policy and required evidence.
- `OPEN_DECISION`: exact controlled vocabulary for `relationshipToPatient`; IMP-003A excludes the field, so this does not block its implementation. A later additive slice must approve the catalog before accepting it.
- `CLOSED API-001-IMP-003A-ROUTE-MAPPING`: API-001 §27.1/§42 publishes QRY-064 read and CMD-018 Create/End, including persisted version/ETag, 428/412, authorization and retries (2026-09-23).
- Risk: caller discoverability of an existing guardian Person is limited because no global People search is approved. Mitigation: explicit existing-ID precondition and next People resolution/creation slice.
- Risk: finite/future overlap constraints depend on all writes using the Application transaction. Mitigation: lock parent, integration-test races, prohibit alternate writers, and evaluate a PostgreSQL exclusion constraint only if operational extension policy is approved.
- Risk: link existence could be mistaken for consent or portal authority. Mitigation: explicit policy denial and no authorization side effects.

No mandatory local implementation decision remains open after API-001 reconciliation. The other open decisions do not block this narrow slice because their affected fields/flows are explicitly excluded; production IAM and durable Audit remain activation gates.

## 37. Subsequent Slice Sequence

1. **IMP-003A — Guardian Links for Existing Persons** — READY for a separate local backend implementation task; not started by this reconciliation.
2. **IMP-003B-DESIGN — Relationship Person Resolution / Creation** — design purpose-specific People create/reuse, CPF conflict, weak duplicates, and People→Patients recovery; not READY until designed.
3. **IMP-003C-DESIGN — Minor Registration with Guardian** — atomic PatientProfile + GuardianLink, payer mode, consent/legal evidence, replacement, and complete retry workflow; BLOCKED by IMP-003A, IMP-003B, and legal/Audit decisions.
4. **IMP-003D-DESIGN — Responsible Payer Lifecycle** — payer changes plus Contract/Billing forward-use; BLOCKED pending dedicated design and downstream contracts.
5. **IMP-003E-DESIGN — Administrative Responsible** — BLOCKED by `OQ-M001-005`.
6. **IMP-003F-DESIGN — Emergency Contact** — BLOCKED by `OQ-M001-001`.

Sequence does not mark undesigned implementation as READY. Responsible payer may be scheduled before minor registration if business priority changes, but its gates remain unchanged.

## 38. Implementation Readiness

**IMP-003A is READY.** Domain scope, optional-field exclusion, cardinality, temporal semantics, minor-safe end, persisted concurrency version, authorization, persistence and tests are implementable. API-001 §27.1/§42 now publishes the definitive contracts summarized in section 24. Implementation is the next separate task and was not started here.

The compatible catalog correction is complete; no additional product or architecture decision is required for the delimited local backend implementation. External/production activation still remains gated by durable Audit and production IAM. This slice does not unlock minor registration or non-self payer registration.

All later slices listed above are design candidates or blocked, not implementation-ready.

## 39. Validation

Design validation checklist:

- all four concepts differentiated: PASS;
- People/Patients ownership and one-Person/multiple-roles rule: PASS;
- one minimum complete slice selected: PASS;
- existing-Person behavior and future new-Person cases explicit: PASS;
- temporal/cardinality/principality rules: PASS;
- API divergence reconciled explicitly in API-001 §27.1/§42, with no new permission or unrelated endpoint: PASS;
- canonical non-idempotency, ETag concurrency, authorization, privacy, transactions, and recovery: PASS;
- Patients-only persistence/migration impact: PASS;
- IMP-001/002 backward compatibility and regression plan: PASS;
- minor/payer and production gates preserved: PASS;
- no code or migration implemented by this design: PASS.

Final result: **IMP-003-DESIGN — PASS**.

Implementation readiness: **IMP-003A — READY** after the 2026-09-23 API-001 reconciliation. No implementation was started. Production activation, minor/different-payer registration, receipt retention and historical correction gates remain unchanged.
