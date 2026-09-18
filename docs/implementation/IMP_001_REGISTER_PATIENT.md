# IMP-001 — Register Patient

## 1. Status

`DONE — PASS` em 2026-09-18. O status cobre somente o cadastro e a consulta administrativa backend de paciente adulto, `payerMode=SELF`, em Unit existente e ACTIVE.

## 2. Objective

Implementar o primeiro vertical slice funcional de People e Patients por `POST /api/v1/patients` e `GET /api/v1/patients/{patientId}`, preservando ownership, persistência e transações locais por bounded context.

## 3. Inputs

Foram aplicados PROJECT_OS, AI Profile, IMP-001 Design, baseline IMP-000, Context/Ownership Maps, Domain Events, ARC-003, DB-001, API-001, AUTH-001, STATE-001, MODEL-001, MODEL-005 e ADR-001 a ADR-007. O código e os quatro projetos de teste existentes foram inspecionados antes da implementação.

## 4. Scope

O slice aceita nome completo, nascimento, telefone estruturado, CPF opcional, Unit principal, início do relacionamento e `payerMode=SELF`. Cria somente Person, ContactPoint de telefone e PatientProfile ACTIVE. Não inclui update, delete, busca/listagem, guardian, pagador distinto, emergência, Staff, agenda, Clinical, Billing, Finance, Documents ou frontend.

## 5. Domain Implementation

IDs usam UUIDv7. Datas civis usam `DateOnly`. A maioridade é calculada em `relationshipStartedOn`; menor de 18 anos falha antes de People. Status ACTIVE é controlado pelo servidor. CPF e telefone são normalizados e revalidados pelo owner People.

## 6. People Context

People materializa `Person`, `ContactPoint` PHONE e seu receipt owner-local. Person guarda nome, nascimento, CPF normalizado opcional e record state CURRENT. O telefone principal ACTIVE é child entity e não é copiado para Patients. CPF existente em outra Person retorna conflito tipado sem revelar a identidade conflitante.

## 7. Patients Context

Patients materializa `PatientProfile` com `personId`, `primaryUnitId`, `administrativeStatus` e `relationshipStartedOn`. `personId` e `primaryUnitId` são referências UUID opacas, sem navigation ou FK cross-schema. Patients owns `RegisterPatient`, o receipt HTTP e a composição do GET.

## 8. Organization Dependency

O workflow usa `IValidateUnitForPatientRegistration` antes de People e novamente imediatamente antes da transação final Patients. O GET usa o contrato mínimo `IGetUnitForPatientRead`. Nenhum handler Patients acessa `OrganizationDbContext` ou o schema Organization.

## 9. Orchestration

`Patients.Application.RegisterPatient` executa: autorização e prevalidation; validação inicial da Unit; claim local Patients; command público People; checkpoint local de `personId`; leitura mínima People; revalidação da Unit; criação do profile e conclusão do receipt na mesma transação Patients. O Host apenas configura middleware e delega ao módulo Registry.

## 10. Idempotency

`Idempotency-Key` é obrigatória, limitada a 200 caracteres ASCII visíveis e scoped por actor + `RegisterPatient` + key. O hash SHA-256 usa representação determinística length-prefixed de nome, datas, telefone, CPF normalizado/null, UUID, start date e payer mode normalizado. Mesmo hash retoma/reproduz; hash diferente retorna `IDEMPOTENCY_CONFLICT`.

## 11. Failure Recovery

Falha antes do commit People não cria Person. Commit People com resposta perdida é recuperado pela operation key derivada `{workflowId}/person-step/v1`. Falha após People preserva Person e libera o lease para retry. Profile e receipt COMPLETED são confirmados atomicamente, permitindo replay após perda da resposta HTTP. Requests concorrentes da mesma key elegem uma execução pelo unique/lease local.

## 12. Persistence

`PeopleDbContext` mapeia somente `people.person`, `people.contact_point` e `people.command_receipt`. `PatientsDbContext` mapeia somente `patients.patient_profile` e `patients.command_receipt`. Organization permanece no `OrganizationDbContext`. Não há shared DbContext, repository genérico, TransactionScope, join ou transaction cross-context.

## 13. Migrations

- Organization: `20260917010742_Organization_InitialUnitBaseline`, history `organization.__organization_migrations_history`.
- People: `20260918180533_People_InitialPatientRegistrationSlice`, history `people.__people_migrations_history`.
- Patients: `20260918180545_Patients_InitialPatientRegistrationSlice`, history `patients.__patients_migrations_history`.

As migrations People e Patients alteram somente seus schemas e não criam FK para outro contexto.

## 14. HTTP API

POST retorna `201 Created`, `Location` e `{ patientId, personId, status }`. Replay retorna o mesmo 201/body/Location e `Idempotency-Replayed: true`. GET retorna o read administrativo com Person, telefone, CPF ausente ou mascarado, Unit, status e início; usa `Cache-Control: no-store`.

## 15. Authorization

POST exige conta autenticada/ACTIVE, ausência de explicit deny, `patients.profile.create`, `people.person.create` e Unit grant. GET exige `patients.profile.read`, `people.person.read` e scope da Unit do profile. People revalida suas permissions nos próprios contratos. O Host normal não possui login fake, principal hardcoded ou bypass de Development.

## 16. Problem Details

Falhas HTTP usam `application/problem+json` RFC 9457 com `code`, `traceId` e `errors` para validação. Foram mapeados auth, validation, key ausente, mismatch/in-progress, Unit missing/inactive, minor, payer distinto, CPF/profile duplicate, not found, dependency e internal error. Respostas não expõem SQL, constraint, stack, CPF integral ou tipo interno.

## 17. Testing

UnitTests cobrem domínio, CPF, maioridade e hash canônico. IntegrationTests usam PostgreSQL 18/Testcontainers para migrations, schemas, constraints, contracts, transactions locais, replay, recovery e races. ApiTests usam Host real/TestServer e PostgreSQL real. ArchitectureTests verificam namespaces, contratos, DbContexts, migrations e ausência de orchestration no Host.

## 18. PostgreSQL Validation

As três migrations foram aplicadas em banco PostgreSQL descartável. Foram validados histories separados, persistência de Person/phone/profile, CPF partial unique com múltiplos NULL, unique de profile, receipts, ausência de FK Patients→People/Organization e descarte dos containers.

## 19. Architecture Validation

Patients consome People e Organization somente via ModuleContracts. People não depende de Patients. Cada DbContext e migration declara apenas o schema owner. Domain/Infrastructure continuam internal; `InternalsVisibleTo` foi ampliado apenas para o projeto ApiTests que precisa preparar o banco do test host.

## 20. Gated Branches

Continuam gated: menor/GuardianLink, payer diferente/ResponsiblePayerLink, ativação externa/produção sem IAM concreto, durable business Audit e política definitiva de retenção dos receipts.

## 21. Out of Scope

Não foram implementados update/delete/merge, relacionamentos, contatos extras, Staff, CRM, Scheduling, Pilates, Clinical, Plans, Billing, Finance, Communication, Documents, Reports, frontend, outbox ou eventos novos.

## 22. Risks / Limitations

O lease de execução usa janela fixa de 30 segundos e a retenção dos receipts permanece decisão operacional. Uma Person pode permanecer legitimamente sem PatientProfile após falha ou Unit inativada; retry pela mesma key recupera o fluxo, mas troca de payload/Unit exige um fluxo futuro explícito. Produção externa continua bloqueada por IAM, Audit e provisioning operacional.

## 23. Validation Results

- prerequisite IMP-000: 24/24 baseline tests PASS antes da implementação;
- restore: PASS;
- build: PASS, zero warnings/errors;
- UnitTests: PASS, 19/19;
- IntegrationTests PostgreSQL: PASS, 23/23;
- ApiTests PostgreSQL/HTTP: PASS, 9/9;
- ArchitectureTests: PASS, 11/11;
- full suite: PASS, 62/62;
- migrations list: uma migration por Organization, People e Patients;
- health/startup sem migration automática: PASS;
- `git diff --check`: PASS.

## 24. Consequences / Next Slice

People e Patients agora possuem a primeira superfície funcional, mas não estão completos. O menor próximo slice coerente é **Patient Search/List**, inicialmente como design/DoR e depois implementação separada; relacionamentos/guardian devem permanecer uma slice própria devido às regras de autoridade e menoridade.
