# IMP-000 — Organization / Unit Baseline

## 1. Status

`BLOCKED_BY_TEST_ENVIRONMENT`. Implementação, build, UnitTests e ArchitectureTests concluídos; a validação PostgreSQL real permanece bloqueada porque o ambiente não possui Docker nem endpoint compatível disponível. IMP-000 não está `DONE` e IMP-001 permanece `BLOCKED_BY_PREREQUISITE`.

## 2. Objective

Materializar o owner mínimo de Organization para Clinic e Unit e disponibilizar uma validação persistida de Unit para o futuro cadastro de paciente.

## 3. Inputs

Foram usados PROJECT_OS, AI Profile, Context Map, Ownership Map, ARC-003, DB-001, API-001, MODEL-001, MODEL-005, AUTH-001, BOOT-001, IMP-001-DESIGN, ADR-001 a ADR-007 e o último handoff.

## 4. Scope

Somente Clinic, Unit, persistence PostgreSQL owner, migration inicial, contrato interno de validação, registration no Registry e testes. Não há endpoint, CRUD administrativo, frontend ou dados de produção.

## 5. Domain

Organization mantém os namespaces Domain, Application e Infrastructure próprios dentro do assembly Registry. IDs são UUID; novas entidades usam UUIDv7 sem inferir ordenação de negócio. O lifecycle aprovado é `ACTIVE`/`INACTIVE`, sem soft delete genérico.

## 6. Persistence

EF Core e Npgsql mapeiam exclusivamente `organization.clinic` e `organization.unit`. A FK local obrigatória Unit→Clinic usa `RESTRICT`, preservando Units históricas contra exclusão em cascata. Checks limitam status a `ACTIVE` e `INACTIVE`; o único índice adicional é o da FK `unit.clinic_id`.

## 7. OrganizationDbContext

`OrganizationDbContext` é interno ao owner e contém apenas `DbSet<Clinic>` e `DbSet<Unit>`. Não há People, Patients, Staff, DbContext global, repository público ou navegação cross-context.

## 8. Migration

Migration owner: `Organization_InitialUnitBaseline`. History table: `organization.__organization_migrations_history`.

Comandos exatos, com `ConnectionStrings__Database` fornecida por variável de ambiente ou user secrets:

```bash
dotnet ef migrations add Organization_InitialUnitBaseline \
  --context OrganizationDbContext \
  --project src/backend/Modules/Fisiofit.Modules.Registry/Fisiofit.Modules.Registry.csproj \
  --startup-project src/backend/Modules/Fisiofit.Modules.Registry/Fisiofit.Modules.Registry.csproj \
  --output-dir Organization/Infrastructure/Migrations/Organization

dotnet ef migrations list \
  --context OrganizationDbContext \
  --project src/backend/Modules/Fisiofit.Modules.Registry/Fisiofit.Modules.Registry.csproj \
  --startup-project src/backend/Modules/Fisiofit.Modules.Registry/Fisiofit.Modules.Registry.csproj

dotnet ef database update \
  --context OrganizationDbContext \
  --project src/backend/Modules/Fisiofit.Modules.Registry/Fisiofit.Modules.Registry.csproj \
  --startup-project src/backend/Modules/Fisiofit.Modules.Registry/Fisiofit.Modules.Registry.csproj
```

## 9. Public Contract

`IValidateUnitForPatientRegistration` recebe o UUID opaco da Unit e retorna `Valid`, `NotFound` ou `Inactive`. O resultado válido inclui somente `unitId`, `clinicId` e nome. Não expõe entity, DbContext, IQueryable ou detalhes de persistence.

## 10. Contract Implementation

A implementação pertence a Organization Application, consulta `OrganizationDbContext` com `AsNoTracking` e projeta somente os campos necessários. `UNIT_SCOPE` continua separado: é autorização do caller e não é confundido com existência/status institucional.

## 11. Registry Integration

`AddRegistryModule(IConfiguration)` registra `OrganizationDbContext` e a implementação scoped do contrato. `Fisiofit.Api` permanece composition root e somente passa configuration; não contém regra ou SQL.

## 12. PostgreSQL Testing

IntegrationTests usam `Testcontainers.PostgreSql` somente no projeto de testes, container PostgreSQL 18 descartável, database isolado, migration no setup e disposal pelo fixture. Os dados são fictícios e restritos ao teste.

As provas cobrem migration/schema/history, persistência, FK e delete restritivo, ACTIVE/INACTIVE/NOT_FOUND, ausência de People/Patients e model mapping exclusivo do schema Organization.

## 13. Architecture Enforcement

ArchitectureTests varrem fontes de Patients, People e Staff e rejeitam referências a `Organization.Infrastructure` ou `OrganizationDbContext`. Outro teste inspeciona a superfície pública do contrato e rejeita tipos Registry/EF expostos.

## 14. Configuration

A connection string usa a chave padrão `ConnectionStrings:Database`. Nenhum valor é versionado. Exemplo de variável local:

```bash
export ConnectionStrings__Database='Host=localhost;Database=fisiofit;Username=<user>;Password=<secret>'
```

Não há migration automática no startup nem `Database.EnsureCreated()`.

## 15. Validation Results

- `dotnet restore Fisiofit.slnx`: PASS.
- `dotnet build Fisiofit.slnx --no-restore --disable-build-servers`: PASS, zero warnings/errors.
- UnitTests: PASS, 9/9.
- ArchitectureTests: PASS, 5/5.
- IntegrationTests: BLOCKED; 1 smoke test anterior passou e 8 testes Organization não puderam iniciar porque `unix:///var/run/docker.sock` não existe.
- Migration aplicada em PostgreSQL real: BLOCKED pelo mesmo motivo.

## 16. Out of Scope

Room, calendar, holiday, People, Patients, Staff, demais domínios, endpoints, CRUD, IAM, Audit improvisado, frontend, docker-compose, seed e provisioning de produção não foram implementados.

## 17. Risks / Limitations

O risco pendente é exclusivamente a falta de evidência executada contra PostgreSQL neste ambiente. Até que Docker esteja disponível e a suite completa passe, a migration não pode ser considerada validada e IMP-000 não pode ser promovido a `DONE`.

## 18. Consequences for IMP-001

O contrato e owner necessários estão implementados, mas o gate formal continua fechado. Após disponibilizar Docker, executar `dotnet test Fisiofit.slnx`; se os IntegrationTests passarem, atualizar este documento e PROJECT_OS, marcar IMP-000 `DONE` e somente então promover IMP-001 para `READY_WITH_GATED_BRANCHES`, preservando minors, payer diferente, IAM externo e Audit durável como gates.
