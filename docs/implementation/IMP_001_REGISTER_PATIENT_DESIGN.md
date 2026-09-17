# IMP-001 — Register Patient Vertical Slice Design

## 1. Status

`IMP-001-DESIGN — DONE / PASS` em 2026-09-16, após revisão corretiva final.

Este documento autoriza somente a futura implementação backend de cadastro e consulta de paciente descrita aqui. Não implementa código, banco, migration ou frontend. O subfluxo aprovado é o cadastro de uma pessoa adulta que não exige GuardianLink nem pagador diferente; os branches dependentes desses vínculos permanecem bloqueados.

Readiness de implementação: **BLOCKED_BY_PREREQUISITE** até `IMP-000 — ORGANIZATION / UNIT BASELINE` materializar Clinic/Unit reais e o contrato público de validação. Isso não reprova o design; impede iniciar IMP-001 com Unit hardcoded, fake, configurada como source of truth ou aceita sem validação.

## 2. Objective

Entregar o menor slice verificável de `PROC-PAC-001`:

- `POST /api/v1/patients`, criando `Person` em People e `PatientProfile` em Patients;
- `GET /api/v1/patients/{patientId}`, compondo detalhes administrativos de Patients, People e Organization;
- provar que contexts no mesmo assembly `Fisiofit.Modules.Registry` preservam domínio, Application, Infrastructure, schema, `DbContext`, migrations e transactions separados.

O slice não completa VS-01: colocar o paciente em turma fixa continua posterior.

## 3. Inputs

Fontes normativas lidas: `PROJECT_OS.md` e último handoff; perfil de IA; Context Map; Ownership Map; Domain Events; ARC-003; DB-001; API-001; AUTH-001; STATE-001; MODEL-001; MODEL-005; Glossary; Rules Index; Process Index; ADR-001 a ADR-007; BOOT-001; e o domínio People/Patients referenciado pelas regras. A estrutura real de Registry, ModuleContracts, Host e quatro projetos backend de teste também foi inspecionada.

Decisões aplicáveis: `RB-PPL-001..004`, `RB-PAC-001..004`, `RB-SEC-001..002`, `RB-AUD-001`, `DB-INV-001`, `DB-INV-002`, `DB-INV-004`, `DB-INV-041` e `ARC-INV-001..009`, `019`, `023`, `024`.

## 4. Slice Boundary

### IN_SCOPE

- backend only;
- criação de uma nova Person canônica, com nome completo, data de nascimento, telefone principal válido e CPF opcional;
- criação de PatientProfile ACTIVE para essa Person, com unidade principal e início do relacionamento;
- paciente que pode ser cadastrado sem GuardianLink e cujo pagador é a própria Person;
- autorização de Secretária/Recepção ou Proprietária/Gestora com permissions explícitas e `UNIT_SCOPE` correspondente;
- idempotência obrigatória, falha parcial recuperável, PostgreSQL real, duas migrations independentes e testes;
- consulta administrativa por `patientId`, composta por contratos públicos.

### OUT_OF_SCOPE

- update, deactivate, delete, merge e fuzzy matching;
- guardian, responsável administrativo, pagador diferente e contato de emergência;
- profissional, agenda, turma, matrícula, contrato, Billing, Finance, Clinical e Documents;
- frontend funcional, OpenAPI completo do catálogo, outbox/inbox e eventos como mecanismo do workflow;
- autenticação/IAM lifecycle, UI de auditoria, Docker/CI e observabilidade completa.

### GATED

- início de IMP-001: bloqueado até `IMP-000 — ORGANIZATION / UNIT BASELINE` estar DONE;
- cadastro de menor: exige GuardianLink vigente; não relaxar a regra;
- pagador diferente: exige criar/selecionar outra Person e `ResponsiblePayerLink`;
- vincular automaticamente uma Person pré-existente encontrada por CPF: exige fluxo explícito de seleção/confirmação, não merge automático;
- ativação externa/produção: exige mecanismo concreto de autenticação, sessão/revogação e atribuição de Unit conforme gates de IAM/AUTH.

### DEFERRED

- frontend `patients/register` e jornada de busca/seleção de Person existente;
- demais operações People/Patients e expansão de audit/retention;
- VS-01 turma fixa.

## 5. Actors / Authorization

- Atores permitidos: `SECRETARY_RECEPTION` e `OWNER_MANAGER`, nunca por role isolada.
- POST exige simultaneamente `people.person.create`, `patients.profile.create`, `UNIT_SCOPE(primaryUnitId)`, conta ativa e ausência de explicit deny.
- GET exige leitura administrativa de paciente no escopo da unidade (`people.person.read` + read de profile de Patients, concretizada na implementação como permission Patients específica, sem criar “authenticated user” genérico).
- `DEVELOPER_IT` recebe `DENY`; acesso técnico não concede cadastro. `PHYSIOTHERAPIST` não cria profile neste slice e só poderá ler subset assistencial futuro sob `ASSIGNED_PATIENT`.
- Cada owner revalida a permission que governa sua operação. Patients não “empresta” ao caller a permission de People.
- POST e GET tratam PII; body não entra em log. CPF e telefone não entram em telemetry/audit genérico.
- A fronteira de autorização é real em Application: deny-by-default, permissions reais, `UNIT_SCOPE`, explicit deny e revalidação pelo owner. O Host apenas fornece o current actor.
- ApiTests/IntegrationTests podem instalar um authentication handler/principal **exclusivamente de teste**, com grants/scopes explícitos por cenário. Esse componente fica nos projetos de teste, não é registrado pelo Host normal e não é habilitado por ambiente `Development`, header inseguro, admin universal ou bypass.
- IAM concreto continua gated para ativação externa/produção. Sem provider/sessão/revogação/grants reais, o endpoint não é publicado externamente, embora a policy real possa ser testada pelo seam isolado.
- `SENSITIVE_AUDIT` continua requisito canônico, mas IMP-001 não materializa Audit nem cria tabela/emissor improvisado. Enquanto a superfície pública do owner Audit não existir, ativação externa/produção permanece gated. Logging operacional sanitizado (trace, métricas e diagnóstico sem PII) não é business audit e não satisfaz `RB-AUD-001`.

## 6. Minimum Input

Classificação do request do slice:

| Campo/conceito | Classificação | Owner | Decisão |
|---|---|---|---|
| `fullName` | `REQUIRED_IN_SLICE` | People | não vazio após normalização; sem inventar partes separadas do nome |
| `birthDate` | `REQUIRED_IN_SLICE` | People | data civil, não futura; necessária à ativação e à fronteira de menor |
| `phone` | `REQUIRED_IN_SLICE` | People/ContactPoint | objeto estruturado e validado; persistido uma vez em People |
| `cpf` | `OPTIONAL_IN_SLICE` | People | normalizado; válido e único quando presente; `null` quando ausente |
| `primaryUnitId` | `REQUIRED_IN_SLICE` | Patients referencia Organization | Unit ativa e dentro do scope do ator |
| `relationshipStartedOn` | `REQUIRED_IN_SLICE` | Patients | data civil; nome canônico do “start date” |
| `administrativeStatus` | `REQUIRED_IN_SLICE`, server-controlled | Patients | criação aprovada resulta `ACTIVE`; cliente não envia status |
| `guardian` | `GATED` | Patients | obrigatório para menor ativo, fora deste slice |
| payer diferente | `GATED` | Patients | vínculo explícito obrigatório, fora deste slice |
| payer igual ao paciente | `REQUIRED_IN_SLICE` como semântica | Patients | request declara `payerMode: "SELF"`; não exige link para outra Person |
| e-mail/endereço/documento alternativo | `OUT_OF_SCOPE` | People | não são requisitos gerais de ativação |
| emergência/administrativo/clínico | `OUT_OF_SCOPE` | respectivos owners | não entram no payload |

Representação do telefone:

```json
{
  "countryCode": "55",
  "areaCode": "11",
  "number": "999999999"
}
```

O contrato não aceita string livre ambígua. Normalização e validação pertencem a People. A forma física pode persistir valor normalizado e componentes conforme o VO; não se copia telefone para Patients.

## 7. Person Ownership

People é o único owner de `Person` e `ContactPoint`. Somente `PeopleDbContext` cria e consulta `people.person` e `people.contact_point`. Full name, birth date, CPF e phone nunca são colunas de `patients.patient_profile`.

A operação de People recebe os dados civis mínimos, normaliza CPF/telefone, faz deduplicação forte por CPF, cria Person e telefone na mesma transaction People ou retorna outcome explícito. O resultado expõe somente `personId`, outcome e conflito minimizado; nunca uma entity.

## 8. PatientProfile Ownership

Patients é o único owner de `PatientProfile`. `CreatePatientProfile` recebe `personId` opaco, `primaryUnitId`, `relationshipStartedOn` e a semântica `payerMode=SELF` já validada. Cria o profile ACTIVE em uma transaction `PatientsDbContext`.

Antes do commit, Patients usa o read contract de People para confirmar Person canônica/CURRENT e requisitos de ativação e o contract de Organization para Unit ativa. A validação não autoriza Patients a ler schemas alheios.

Unique local em `patients.patient_profile.person_id` garante um profile por Person. Retry que encontra o mesmo profile para o mesmo `workflowId` retorna o resultado existente; profile pertencente a outra operação produz conflito explícito.

## 9. Organization Dependency

O request contém `primaryUnitId`. Patients Application chama o contrato síncrono owner `ValidateUnitForPatientRegistration`, especialização mínima compatível com o catálogo `GetUnitAndCalendar`. O resultado expõe somente `unitId`, `clinicId`, `name`, `status` e uma versão/token de atualidade, ou outcomes `NOT_FOUND`, `INACTIVE` e `FORBIDDEN_FOR_SCOPE`. A Unit deve existir, estar ACTIVE e pertencer ao `UNIT_SCOPE` do ator.

Hoje o skeleton não possui `OrganizationDbContext`, tabelas ou handler real; portanto IMP-001 não tem como validar legitimamente uma Unit. Hardcode, UUID arbitrário, configuração como source of truth, Unit fake em produção, leitura direta do schema ou mapeamento de `organization.unit` no `PatientsDbContext` são proibidos.

Decisão: **alternativa B**, predecessor `IMP-000 — ORGANIZATION / UNIT BASELINE`. É menor e menos arriscada que inserir um terceiro domínio/migration no Register Patient. Como `INV-ORG-001` exige que toda Unit pertença exatamente a uma Clinic, o predecessor materializa o mínimo de Clinic e Unit no owner Organization:

- `OrganizationDbContext`, schema/history `organization` e modelos somente de `Clinic` e `Unit`;
- `organization.clinic` (`id`, `name`, `status`, `time_zone_id`) e `organization.unit` (`id`, `clinic_id`, `name`, `status`), com FK **interna** Unit→Clinic;
- lifecycle ACTIVE/INACTIVE e contrato público `ValidateUnitForPatientRegistration`;
- uma migration Organization independente e integration tests PostgreSQL;
- nenhuma UI, CRUD completo, Room, Holiday ou Calendar.

Não há estratégia canônica aprovada de seed de dados de negócio. Logo IMP-000 não inclui seed genérico/de produção. Testes podem criar Clinic/Unit persistidas por fixture exclusiva do owner Organization. Ambientes não teste só podem ativar IMP-001 depois de possuir uma Unit real provisionada por mecanismo Organization aprovado; configuração e inserção por Patients nunca substituem esse owner.

## 10. Orchestration Owner

O endpoint é registrado pela superfície Registry, mas seu owner lógico é **Patients Application**. O use case de borda chama-se `RegisterPatient`; ele compõe os commands owner existentes sem criar o bounded context “Registration”. O Host autentica, correlaciona e mapeia HTTP; não contém regra ou workflow.

- initiator: cliente autenticado chama `POST /api/v1/patients`;
- orchestration: `Patients.Application.RegisterPatient`;
- People command: `CreatePerson` por public contract;
- Patients command: `CreatePatientProfile` local;
- Organization query: valida Unit ativa/escopo;
- falha: nenhum rollback cross-context; Person confirmada permanece válida;
- retry: o receipt Patients recupera o workflow e a operation key interna estável faz People devolver o mesmo `personId`.

Ordem para reduzir órfãos: validação sintática/authorization/Unit e gates conhecidos → People transaction → People read result → Patients transaction → resposta. A Unit é revalidada imediatamente antes da transaction Patients; mudança concorrente resulta em erro explícito, não escrita inconsistente.

## 11. People Public Contract

Contrato owner-namespaced mínimo:

```text
CreatePersonForPatientRegistration(
  fullName,
  birthDate,
  phone,
  cpf?,
  operationKey,
  peopleRequestHash,
  actorContext,
  correlationId)

=> Created(personId)
 | Replayed(personId)
 | CpfConflict(conflictReference)
 | ValidationFailure
 | Forbidden
 | DependencyFailure
```

O nome é uma especialização pública do command canônico `CreatePerson`, não um service genérico. `operationKey` é uma chave interna purpose-bound (`{patientsWorkflowId}/person-step/v1`) derivada de forma determinística pelo workflow Patients; não é a `Idempotency-Key` HTTP e não torna People owner do request externo. `peopleRequestHash` cobre apenas o subrequest canônico de People e permite rejeitar reuso divergente da operation key. `conflictReference` não precisa expor `personId`, CPF ou dados da outra pessoa.

Read contract para ativação/GET:

```text
GetPersonPatientRegistrationData(personId)
=> canonicalPersonId, recordState, fullName, birthDate,
   primaryPhone, cpfDisplay?
```

O contrato é purpose-specific, autorizado e não expõe entity, ContactPoint collection ou storage fields.

## 12. Patients Application Contract

Contrato local de owner:

```text
CreatePatientProfile(
  personId,
  primaryUnitId,
  relationshipStartedOn,
  payerMode=SELF,
  workflowId,
  actorContext,
  correlationId)
=> patientId, administrativeStatus
```

`personId` é referência externa opaca. O handler valida Person por contrato público e Unit por contrato público; ele não instancia `Person`, não acessa `PeopleDbContext` e não inicia transaction abrangente.

`RegisterPatient` é a orchestration facade do POST e não é aggregate/domain service. Ela não transfere ownership: delega criação a People e criação do profile a Patients.

## 13. POST Contract

`POST /api/v1/patients`

Headers obrigatórios:

- `Authorization`/principal autenticado pela superfície configurada;
- `Idempotency-Key`: token opaco não vazio, com tamanho/charset operacional documentado na implementação;
- correlation/trace gerado ou propagado pelo Host.

Request:

```json
{
  "fullName": "Ana Souza",
  "birthDate": "1990-04-12",
  "cpf": "12345678909",
  "phone": {
    "countryCode": "55",
    "areaCode": "11",
    "number": "999999999"
  },
  "primaryUnitId": "uuid",
  "relationshipStartedOn": "2026-09-16",
  "payerMode": "SELF"
}
```

`cpf` pode ser omitido/null. Outros `payerMode` são rejeitados com o gate específico, não ignorados.

Success: `201 Created`, header `Location: /api/v1/patients/{patientId}` e corpo:

```json
{
  "patientId": "uuid",
  "personId": "uuid",
  "status": "ACTIVE"
}
```

`personId` é incluído porque o cliente administrativo precisa correlacionar a identidade People e porque o resultado de recuperação parcial deve ser determinístico; continua opaco.

Replay concluído retorna o mesmo status lógico `201`, body e `Location`, podendo incluir header `Idempotency-Replayed: true`. Não devolve aggregate inteiro.

Status aplicáveis: `201`, `400`, `401`, `403`, `404` para Unit ocultada/inexistente, `409`, `422`, `500`; dependência indisponível é Problem Details sanitizado e não cria fallback permissivo. A implementação pode usar `503` para `DEPENDENCY_UNAVAILABLE` se a baseline HTTP for estendida de modo consistente; até isso ser formalizado, não expõe internals como 500 detail.

## 14. GET Contract

`GET /api/v1/patients/{patientId}`

Success `200 OK`:

```json
{
  "patientId": "uuid",
  "personId": "uuid",
  "fullName": "Ana Souza",
  "birthDate": "1990-04-12",
  "phone": {
    "countryCode": "55",
    "areaCode": "11",
    "number": "999999999"
  },
  "cpf": { "status": "PRESENT", "masked": "***.***.***-09" },
  "primaryUnit": {
    "unitId": "uuid",
    "name": "Unidade Centro",
    "status": "ACTIVE"
  },
  "administrativeStatus": "ACTIVE",
  "relationshipStartedOn": "2026-09-16"
}
```

CPF ausente retorna `{ "status": "ABSENT" }`; valor integral não é retornado neste read default. `200` é PII e deve usar `Cache-Control: no-store`. `404 RESOURCE_NOT_FOUND` cobre inexistência ou ocultação anti-enumeração; `401/403` seguem auth. Nenhum dado clínico, payer/guardian, audit, schema ou storage aparece.

## 15. Read Composition

Patients Application é o composer do read:

1. carrega `PatientProfile` pelo `PatientsDbContext` após autorização/scoping;
2. chama `GetPersonPatientRegistrationData(personId)` em People;
3. chama o read mínimo de Unit em Organization;
4. monta `PatientAdministrativeDetails` e retorna ao adapter HTTP.

Não há `JOIN people.person`, view cross-schema, navegação EF, repository de sibling nem query do Host. Para este slice, chamadas síncronas in-process são mais simples que criar projection. Se volume/latência futura justificar, uma projection explícita e reconstruível poderá substituir a composição sem virar owner.

## 16. Idempotency

POST exige `Idempotency-Key`; a opção “O” de API-001 é tornada obrigatória para este workflow específico devido ao double-submit e à falha entre transactions.

- **owner externo:** `patients.command_receipt` é a única autoridade sobre a `Idempotency-Key` HTTP e sobre o resultado do workflow `RegisterPatient`;
- scope externo: `actorId + Patients + RegisterPatient + key`;
- canonical request externo: JSON semanticamente normalizado, incluindo fullName, birthDate, CPF normalizado ou null, phone, unitId, start date e payerMode; ordem/whitespace não alteram hash;
- o claim do receipt Patients é atômico. Ele mantém `workflowId`, hash, estado `IN_PROGRESS|PERSON_CONFIRMED|COMPLETED`, lease/version da tentativa, `personId?`, `patientId?` e o response mínimo quando concluído; lease expirada permite retomada do mesmo workflow, nunca criação de outro;
- mesma key/hash em `COMPLETED`: replay do mesmo `patientId`, `personId`, status e Location;
- mesma key/hash parcial: retoma o mesmo `workflowId`; nunca abre outro workflow lógico;
- mesma key com hash externo diferente: `409 IDEMPOTENCY_CONFLICT` antes de chamar People;
- duas chamadas concorrentes com mesma key/hash: a unique/claim Patients e serialização local elegem uma execução; a concorrente recebe replay se já concluída ou `409 IDEMPOTENCY_REQUEST_IN_PROGRESS` com retry permitido, sem iniciar People em paralelo como outra operação;
- key diferente com mesmo CPF não é retry e segue a decisão autoritativa de People para CPF conflict;
- retenção concreta continua decisão operacional, mas o endpoint não ativa sem janela publicada que cubra retries e o estado parcial.

### Responsabilidades dos receipts

- `patients.command_receipt`: receipt do HTTP/workflow completo; possui key externa, canonical hash externo, progress, correlações e response replayável.
- `people.command_receipt`: deduplicação **owner-local** de `CreatePersonForPatientRegistration`; possui `operationKey`, hash do subrequest People e `personId/outcome`. Não conhece nem interpreta a key HTTP como autoridade externa.

Mismatch externo é decidido por Patients antes de People. Mismatch do `peopleRequestHash` para a mesma operation key é uma inconsistência do workflow: People não escreve, Patients encerra a tentativa com conflito sanitizado e a operação exige investigação; nunca se cria P2 para “corrigir” a divergência.

Após criar/obter o receipt Patients, o orchestrator deriva de forma determinística `operationKey = {workflowId}/person-step/v1`. Assim, HTTP `K1` sempre leva ao mesmo workflow e ao mesmo step People, mas as tabelas não competem pela mesma autoridade. O `correlationId` liga telemetry/audit; não substitui nenhuma chave de deduplicação.

## 17. Transaction Boundaries

| Step | Context | DbContext | Transaction |
|---|---|---|---|
| authorize request / validate shape | Host + owners | none | none |
| claim/replay external key and workflow | Patients | `PatientsDbContext` | `T0`, local e atômica sobre receipt Patients |
| validate active Unit and scope | Organization | `OrganizationDbContext` read | nenhuma transaction de escrita |
| create/replay Person + phone + People receipt | People | `PeopleDbContext` | `T1`, local e atômica |
| record `PERSON_CONFIRMED/personId` | Patients | `PatientsDbContext` | `T1P`, local; recovery does not depend on this update succeeding |
| revalidate Person minimum | People public read | `PeopleDbContext` read | none |
| revalidate Unit immediately before profile | Organization public read | `OrganizationDbContext` read | none |
| create/replay PatientProfile + complete workflow receipt | Patients | `PatientsDbContext` | `T2`, local e atômica |
| compose create response | Patients Application | none | none |
| GET profile | Patients | `PatientsDbContext` read | none |
| GET Person details | People | `PeopleDbContext` read | none |
| GET Unit summary | Organization | `OrganizationDbContext` read | none |

`T0`, `T1`, `T1P` e `T2` são transações owner-local; em especial `T1 != T2`. Nenhum `TransactionScope`, shared Unit of Work, shared EF model ou distributed transaction é permitido.

Fluxo:

```text
HTTP POST
  -> Patients.Application.RegisterPatient
  -> T0 PatientsDbContext (claim/replay workflow receipt)
  -> Organization public read (Unit/scope)
  -> People public CreatePerson contract (derived operationKey)
  -> T1 PeopleDbContext commit (Person + ContactPoint + receipt)
  -> T1P PatientsDbContext (persist personId/progress, best recovery checkpoint)
  -> Patients local CreatePatientProfile
  -> T2 PatientsDbContext commit (PatientProfile + completed workflow receipt)
  -> 201 response
```

## 18. Partial Failure

Estratégia escolhida: **A — commits locais sequenciais, Person permanece legítima**.

Se T1 conclui e T2 falha, a Person não é apagada nem compensada. People admite Person sem PatientProfile e não existe regra que autorize apagar identidade recém-criada. A falha é registrada com correlation e erro sanitizado; o cliente recebe falha, não falso sucesso. O receipt People mantém `personId` e hash para recuperação.

Estratégia B (saga com compensação) é rejeitada para IMP-001: não há compensação de domínio legítima para apagar Person e o custo não se justifica. Estratégia C (persistência atômica cross-context) é rejeitada por ARC-003/ADR-002: violaria transactions e ownership por DbContext.

Failure windows normativas:

| Janela | Resultado | Recuperação |
|---|---|---|
| F1 — falha antes do commit People | nenhuma Person/phone/receipt People é criada | receipt Patients permanece incompleto; retry usa a mesma operation key e tenta People |
| F2 — People commitou, mas resposta/call se perdeu | Person + phone + receipt People existem, Patients pode não conhecer `personId` | retry chama People com a mesma operation key; People replays o mesmo `personId` |
| F3 — People concluiu, Patients falhou | Person permanece legítima; não há profile ou conclusão falsa | retry recupera/reutiliza `personId`, revalida e tenta T2 |
| F4 — Patients commitou, resposta HTTP se perdeu | profile e receipt `COMPLETED` existem atomicamente | replay Patients devolve exatamente o mesmo 201/body/Location |
| F5 — duas requests concorrentes com mesma key | somente um claim/workflow lógico vence | a outra recebe replay ou `IDEMPOTENCY_REQUEST_IN_PROGRESS`; não cria segunda Person/profile |
| F6 — requests diferentes com o mesmo CPF | People unique/receipt decide; no máximo uma Person canônica vence | a perdedora recebe CPF conflict e não cria PatientProfile |

## 19. Retry / Recovery

Ao repetir a mesma key/hash:

1. Patients, owner externo, localiza/claim o receipt pela key e valida o hash completo.
2. Se `COMPLETED`, reproduz o resultado; se outro executor detém o claim, não inicia outra execução.
3. Se parcial, deriva novamente a mesma `{workflowId}/person-step/v1`.
4. People consulta seu receipt local e retorna o mesmo `personId`; isso cobre inclusive F2, mesmo que Patients não tenha persistido o checkpoint.
5. Patients registra/recupera `personId`, revalida Person e Unit atuais.
6. Patients cria o profile ou reconhece resultado compatível da mesma workflow e completa receipt + profile em T2.
7. Qualquer retry posterior reproduz o resultado original.

Se a Unit ficou inativa entre tentativas, o retry falha explicitamente e a Person continua recuperável; reusar a mesma key com outro `unitId` é mismatch. Uma ação futura deverá permitir concluir com novo payload/key por fluxo explícito de Person existente; IMP-001 não faz isso silenciosamente.

Reconciliation operacional deve localizar People receipts sem Patients completion após limiar configurado. Não cria profile automaticamente; apenas sinaliza recuperação.

## 20. CPF / Deduplication

- CPF ausente: permitido; `null`, nunca fake. Outros sinais (nome+nascimento/telefone) podem registrar indicação de revisão, mas não bloqueiam nem fazem merge automático neste slice, salvo conflito inequívoco definido pelo owner.
- CPF novo: normaliza/valida e cria Person; partial unique protege race.
- CPF já pertence à mesma Person: somente quando o receipt da mesma operation key People prova a Person criada/reutilizada por esse workflow; retorna replay e continua.
- CPF pertence a outra Person: `409 CPF_ALREADY_REGISTERED`/`PERSON_IDENTITY_CONFLICT`, sem revelar dados da Person e sem criar profile ou merge.
- duas requests concorrentes com o mesmo CPF: uma vence a unique local; a outra traduz a violação para conflito seguro ou replay se key/hash for o mesmo.

Fuzzy matching sofisticado, seleção de Person existente e merge ficam fora. Telefone/nome/nascimento são sinais, não autoridade para auto-merge.

## 21. Minor / Guardian Boundary

Menores estão **GATED**, não “opcionalmente sem guardian”. MODEL-001/STATE-001 exigem GuardianLink vigente para PatientProfile ACTIVE menor, e guardian pertence a Patients, referenciando outra Person. Como guardian está fora do escopo exato, IMP-001 aceita somente pessoa com **18 anos completos ou mais em `relationshipStartedOn`**. Esse limiar é uma regra operacional estreita do slice para separar o branch adulto; não resolve consentimento, representação ou requisitos clínicos/jurídicos de menores.

A implementação calcula a idade a partir de `birthDate` e `relationshipStartedOn`, incluindo corretamente o aniversário no calendário civil. Pessoa com menos de 18 anos retorna `422 MINOR_REQUIRES_GUARDIAN_FLOW`, antes da criação de Person. Data ambígua/inválida falha em validation; não existe fallback permissivo nem idade informada pelo cliente.

Guardian será slice separado que cria/seleciona a Person responsável e GuardianLink na mesma transaction Patients do profile/link, sem escrita People por Patients.

## 22. Events

- `PersonCreated` existe, mas é `DOMAIN_EVENT / INTERNAL_ONLY`; pode ser produzido localmente se o domínio o usar, sem outbox ou workflow cross-context.
- `PatientProfileCreated` é `BOTH / ADOPTED`; para IMP-001 não há consumer necessário à resposta nem projeção indispensável. Não se introduz dispatcher/outbox apenas por infraestrutura futura.
- `PatientActivated` é fato canônico de ativação. A criação ACTIVE pode produzir `PatientProfileCreated` e `PatientActivated` conforme o modelo do owner, mas eles não coordenam People→Patients e não mudam transaction boundaries.
- nenhum evento novo `PatientRegistered`, `RegistrationStarted` ou `PersonProfileLinked` é criado.

## 23. Persistence Scope

Estruturas que o predecessor IMP-000 deve materializar antes de IMP-001:

- `organization.clinic`;
- `organization.unit`;
- schema e migration history próprios de Organization.

Estruturas materializadas pelo futuro IMP-001:

- `people.person`;
- `people.contact_point`;
- `people.command_receipt` para deduplicação owner-local da operation key do step People;
- `patients.patient_profile`;
- `patients.command_receipt` para claim, progresso parcial e replay do workflow HTTP, como estrutura indispensável de DB-INV-041;
- schemas e migration history próprios de `people` e `patients`.

IMP-001 não cria nem altera tabelas Organization. Seus testes usam Clinic/Unit **persistidas** pela migration IMP-000 e preparadas por fixture de teste owner-controlled; não simulam Unit em configuração. Nenhuma FK externa liga PatientProfile a Unit.

Não materializar address, relationship, merge, guardian, admin responsible, payer, emergency contact, inbox/outbox ou outras tabelas dos schemas.

## 24. DbContexts

- `PeopleDbContext`: modela somente `people.person`, `people.contact_point`, `people.command_receipt`; migrations/history em `people`.
- `PatientsDbContext`: modela somente `patients.patient_profile`, `patients.command_receipt`; migrations/history em `patients`.
- models, configurations, repositories e transactions são internos e separados, mesmo no mesmo assembly.
- nenhum navigation property entre Person e PatientProfile; `person_id` é UUID opaco sem FK.
- Organization é acessada por contract, nunca injetando `OrganizationDbContext` no handler Patients.

Architecture tests devem rejeitar referências `Patients.*` a `People.Domain`, `People.Infrastructure` ou `PeopleDbContext`, e o inverso.

## 25. Migration Plan

Predecessor obrigatório:

1. `Organization_InitialUnitBaseline` (IMP-000): cria schema/history Organization, `clinic`, `unit`, FK interna Unit→Clinic, checks/índices locais.

Migrations do IMP-001, somente depois:

1. `People_InitialPatientRegistrationSlice`: cria schema/history People e somente `person`, `contact_point`, `command_receipt`, constraints/índices locais.
2. `Patients_InitialPatientRegistrationSlice`: cria schema/history Patients e somente `patient_profile`, `command_receipt`, constraints/índices locais.

As três migrations são owner-local e independentes em DDL; somente a ordem operacional IMP-000 → People → Patients é exigida. Não há FK cross-schema. Deploy interrompe em falha e só habilita o endpoint depois do contrato Organization e das duas migrations IMP-001 estarem compatíveis.

## 26. Constraints

- `people.person.cpf_normalized` partial unique quando não nulo;
- CPF ausente permanece nulo; check/validation proíbe sentinel/fake;
- full name e birth date mínimos para o fluxo ativo;
- `people.contact_point`: kind PHONE, valor normalizado válido, ACTIVE e principal para o propósito administrativo; unicidade/principalidade local coerente;
- FK local `contact_point.person_id -> person.id`;
- `patients.patient_profile.person_id` unique, sem FK cross-schema;
- `primary_unit_id`, `person_id`, `relationship_started_on` e status requeridos no profile ACTIVE;
- status permitido `ACTIVE|INACTIVE`; criação do slice somente ACTIVE;
- `patients.command_receipt` unique por `(actor_id, RegisterPatient, idempotency_key)`; possui o workflow externo e rejeita hash divergente;
- `people.command_receipt` unique por `(caller_context, operation, operation_key)`; possui apenas deduplicação local e rejeita hash divergente do subrequest;
- UUID opaco; datas civis como date; timestamps técnicos UTC;
- nenhuma constraint de phone/birth duplicada em Patients; required phone e maioridade são invariantes de workflow validadas por contract People/policy.

## 27. Error Mapping

Todos os erros usam RFC 9457 com `code`, `traceId` e `errors` quando aplicável.

| Code | HTTP | Situação |
|---|---:|---|
| `VALIDATION_ERROR` | 400 | shape, UUID, data, nome, telefone ou CPF inválido |
| `UNAUTHORIZED` | 401 | principal ausente/inválido |
| `FORBIDDEN` | 403 | falta permission, Unit scope ou explicit deny |
| `RESOURCE_NOT_FOUND` | 404 | patient/Unit inexistente ou ocultado |
| `IDEMPOTENCY_KEY_REQUIRED` | 400 | POST sem key |
| `IDEMPOTENCY_CONFLICT` | 409 | mesma key, request canônico diferente |
| `IDEMPOTENCY_REQUEST_IN_PROGRESS` | 409 | mesma key/hash já está sob execução lógica; retry permitido |
| `CPF_ALREADY_REGISTERED` | 409 | CPF pertence a outra Person |
| `PERSON_ALREADY_HAS_PROFILE` | 409 | unique por Person fora do mesmo replay |
| `UNIT_INACTIVE` | 422 | Unit existe, mas não aceita novo profile |
| `MINOR_REQUIRES_GUARDIAN_FLOW` | 422 | branch exige GuardianLink |
| `DEPENDENCY_UNAVAILABLE` | 503 quando formalizado | owner contract indisponível; sem fallback |
| `INTERNAL_ERROR` | 500 | falha inesperada sanitizada |

Constraint names, SQL, stack e dados conflitantes não entram no response.

## 28. Test Plan

### Unit

- Person aceita CPF ausente e rejeita CPF/phone inválidos; normaliza valores;
- ContactPoint principal e Person são consistentes;
- PatientProfile exige ids/start date/status válidos e não contém identidade civil;
- cálculo de idade bloqueia pessoa com menos de 18 anos sem guardian; adulto segue;
- payerMode diferente de SELF é gated.

### Application

- sucesso completa Unit → People → Patients;
- duplicate CPF de outra Person conflita sem criar profile;
- CPF ausente funciona; invalid request não chama owners;
- Unit inexistente/inativa/fora do scope falha antes de Person quando possível;
- falha Patients após commit People preserva Person;
- retry parcial reutiliza `personId` e conclui profile;
- retry completo reproduz mesmo resultado;
- mesma key/payload diferente conflita;
- F2 reenvia a mesma operation key após resposta People perdida e recupera `personId`;
- duas requests concorrentes com a mesma key executam um único workflow;
- requests/key diferentes com o mesmo CPF deixam uma única Person e nenhum profile perdedor;
- GET compõe os três owners e mascara CPF;
- denied actor não causa qualquer write.

### Integration — PostgreSQL real

- Organization baseline persiste Clinic/Unit, aplica FK interna e retorna ACTIVE/INACTIVE/NOT_FOUND pelo contract;
- People persiste Person + phone + receipt atomicamente;
- Patients persiste profile + receipt atomicamente;
- CPF partial unique sob race;
- unique PatientProfile por `person_id` sob race;
- receipt unique/hash mismatch;
- migrations People/Patients aplicam e revertem/validam independentemente conforme política de teste;
- catálogos EF não mapeiam schema sibling e não há FK cross-schema;
- falha T2 deixa T1 consultável e retry fecha o workflow.

Estratégia: **Testcontainers para PostgreSQL** como padrão de CI/local reproduzível, com versão pinada compatível com produção. Container local manual é fallback apenas para diagnóstico; banco compartilhado/manual não é fonte dos testes. Nenhuma dependência é adicionada nesta tarefa.

### API

- POST 201 + body mínimo + Location;
- replay idempotente; missing/mismatch key;
- GET 200, 404, PII/no-store;
- validation 400, CPF/profile conflict 409, minor/Unit 422;
- 401 e 403, inclusive Developer/IT e Unit errada;
- authentication handler/principal existe apenas no test host e não pode ser registrado pelo Host normal;
- Problem Details content type/codes e ausência de internals.

### Architecture

- People/Patients internals não são públicos/importados pelo sibling;
- cada DbContext mapeia somente seu schema;
- migrations alteram somente owner schema;
- endpoint/Host não injeta DbContext nem contém orchestration de negócio;
- ModuleContracts não contém entities/repositories/DbContext;
- nenhuma transaction/EF navigation/FK cruza contexts.

## 29. Expected File Plan

Árvore provável do predecessor IMP-000; nomes finais podem variar sem mudar os boundaries:

```text
src/backend/Modules/Fisiofit.Modules.Registry/
  Organization/
    Domain/Clinic.cs
    Domain/Unit.cs
    Application/ValidateUnitForPatientRegistration.cs
    Infrastructure/OrganizationDbContext.cs
    Infrastructure/Configurations/{Clinic,Unit}Configuration.cs
    Infrastructure/Migrations/Organization/*
src/backend/Fisiofit.ModuleContracts/
  Organization/ValidateUnitForPatientRegistration.cs
tests/backend/Fisiofit.UnitTests/Registry/Organization/*
tests/backend/Fisiofit.IntegrationTests/Registry/OrganizationUnitBaselineTests.cs
tests/backend/Fisiofit.ArchitectureTests/RegistryOrganizationBoundaryTests.cs
```

Árvore provável do IMP-001, somente depois do predecessor:

```text
src/backend/Modules/Fisiofit.Modules.Registry/
  RegistryModule.cs                         # registration/mapping only
  People/
    Domain/Person.cs
    Domain/ContactPoint.cs
    Domain/Cpf.cs
    Domain/PhoneNumber.cs
    Application/CreatePersonForPatientRegistration.cs
    Application/GetPersonPatientRegistrationData.cs
    Infrastructure/PeopleDbContext.cs
    Infrastructure/Configurations/{Person,ContactPoint,CommandReceipt}Configuration.cs
    Infrastructure/Migrations/People/*
  Patients/
    Domain/PatientProfile.cs
    Application/RegisterPatient.cs
    Application/CreatePatientProfile.cs
    Application/GetPatientDetails.cs
    Infrastructure/PatientsDbContext.cs
    Infrastructure/Configurations/{PatientProfile,CommandReceipt}Configuration.cs
    Infrastructure/Migrations/Patients/*
  Endpoints/PatientsEndpoints.cs             # or Patients/Application HTTP adapter per established module convention

src/backend/Fisiofit.ModuleContracts/
  People/CreatePersonForPatientRegistration.cs
  People/GetPersonPatientRegistrationData.cs

src/backend/Fisiofit.Api/
  Program.cs                                 # composition/auth/error pipeline wiring only
  appsettings*.json                          # connection/auth test config, no secrets

tests/backend/Fisiofit.UnitTests/
  Registry/People/*
  Registry/Patients/*
tests/backend/Fisiofit.IntegrationTests/
  Registry/PeoplePersistenceTests.cs
  Registry/PatientsPersistenceTests.cs
  Registry/RegisterPatientRecoveryTests.cs
  Support/PostgreSqlFixture.cs
tests/backend/Fisiofit.ApiTests/
  Patients/RegisterPatientApiTests.cs
  Patients/GetPatientDetailsApiTests.cs
  Support/TestAuthenticationHandler.cs       # compilado/registrado somente pelo test host
tests/backend/Fisiofit.ArchitectureTests/
  RegistryContextIsolationTests.cs
  RegistryPersistenceBoundaryTests.cs
```

Os planos não criam projetos novos nem transformam folders em assemblies. IMP-001 consome o contract já entregue por IMP-000 e não modifica Infrastructure Organization. Não se presume que markers já sejam contratos funcionais.

## 30. Definition of Done

O predecessor IMP-000 estará DONE somente quando `OrganizationDbContext`, schema/history, Clinic, Unit, FK interna, lifecycle ACTIVE/INACTIVE, migration independente, public validation contract e integration/architecture tests PostgreSQL estiverem funcionais; sem CRUD/UI/Room/Calendar, sem seed de produção e sem acesso ao schema por Patients.

IMP-001 futuro estará DONE somente quando:

- `IMP-000 — ORGANIZATION / UNIT BASELINE` estiver DONE e seu contract validar uma Unit persistida real;
- Person/ContactPoint e PatientProfile implementarem as invariantes aprovadas;
- People/Patients Applications e public contracts mínimos existirem;
- `PeopleDbContext` e `PatientsDbContext` estiverem isolados;
- migrations independentes criarem somente a persistence scope aprovada;
- integration tests usarem PostgreSQL real/Testcontainers;
- POST e GET estiverem funcionais com RFC 9457 Problem Details;
- authorization deny-by-default, permissions, Unit scope e negative paths estiverem efetivamente enforced;
- test authentication estiver confinado aos projetos/test host, sem provider fake, header ou bypass no Host normal;
- idempotência obrigatória, mismatch, replay e recuperação de falha parcial estiverem provados;
- CPF, phone, Unit, adulto/gate de menor e unique profile estiverem testados;
- GET não fizer join cross-schema e minimizar PII;
- architecture tests provarem ausência de acesso cross-context e migration indevida;
- build e todas as suites backend passarem sem warnings/errors;
- o requisito de `SENSITIVE_AUDIT` estiver preservado como gate de ativação externa: se Audit ainda não estiver materializado, nenhum substituto improvisado será criado e logging operacional não será aceito como business audit;
- documentação/OpenAPI do endpoint implementado e PROJECT_OS/handoff estiverem atualizados;
- nenhum frontend, guardian/payer distinto, merge ou contexto adjacente tiver sido expandido.

## 31. Risks

| Risco | Mitigação |
|---|---|
| Person sem PatientProfile após falha | estado legítimo + receipt People + retry/reconciliation |
| duplicate Person no retry | workflow Patients + operation key People estável + CPF unique |
| CPF race | normalização + partial unique + tradução de constraint |
| duplicate PatientProfile | unique person_id + receipt Patients |
| Unit inativa/stale | contract owner antes de T1 e revalidação antes de T2 |
| baseline Unit inexistente | IMP-001 bloqueado até IMP-000 materializar Clinic/Unit e contract real |
| menor sem Guardian | branch gated/fail closed |
| transaction cross-DbContext acidental | duas transactions explícitas + architecture/integration tests |
| contracts crescerem demais | payload purpose-specific; nenhuma entity/collection genérica |
| CPF/telefone vazarem | Problem Details minimizado, log redaction, GET mask/no-store |
| auth test seam virar bypass | handler apenas em ApiTests/IntegrationTests; Host normal não o referencia; deny default |
| receipts sem retention | endpoint não ativa sem janela publicada; política jurídica/operacional posterior |
| audit virar logging | DoD exige superfície Audit apropriada; logs não satisfazem requisito |

## 32. Gated / Deferred

Gates que não bloqueiam o subfluxo adulto backend:

- guardian/minor e requisitos completos de representação/consentimento;
- payer diferente e seleção de Person existente;
- provider IAM de produção, default multi-unit grants e lifecycle de sessão/revogação;
- retenção definitiva de audit/idempotency receipts;
- UI, VS-01 turma, demais módulos.

Predecessor que bloqueia o início de IMP-001:

- `IMP-000 — ORGANIZATION / UNIT BASELINE`, com Clinic/Unit persistidas, OrganizationDbContext/migration e `ValidateUnitForPatientRegistration` testado em PostgreSQL.

Gates de ativação externa/produção, mesmo após IMP-000 e IMP-001:

- provider IAM/sessão/revogação/grants concretos;
- superfície pública/durável de Audit para a evidência requerida;
- existência de Unit real provisionada pelo owner Organization por mecanismo aprovado, sem seed/configuração ad hoc.

Nenhum gate permite relaxar invariantes. Enquanto não resolvido, o branch correspondente nega/falha explicitamente.

## 33. IMP-001 Readiness

**BLOCKED_BY_PREREQUISITE**.

O design está aprovado, mas IMP-001 não pode começar antes de `IMP-000 — ORGANIZATION / UNIT BASELINE` estar DONE. Depois do predecessor, o subfluxo adulto/self-payer poderá ser promovido para `READY_WITH_GATED_BRANCHES`; minors/payer diferente continuarão gated, e produção externa continuará gated por IAM/Audit e provisioning real.

As 15 respostas de decisão são:

1. POST pertence logicamente a Patients e é mapeado por Registry.
2. Patients Application (`RegisterPatient`) orquestra; Host apenas delega.
3. People cria Person/phone pelo public command e T1 local.
4. Patients cria PatientProfile com `personId` opaco e T2 local.
5. Falha T2 preserva Person e expõe falha correlacionada.
6. Retry usa o workflow receipt Patients e a operation key People derivada para reutilizar Person.
7. Idempotency-Key é obrigatória e pertence exclusivamente ao workflow Patients.
8. CPF ausente é válido; novo cria; mesmo receipt reusa; outro owner conflita; sem merge.
9. Unit é validada por contrato Organization real entregue por IMP-000 e revalidada antes de T2.
10. GET compõe contracts People/Organization em Patients Application, sem join.
11. Menores estão GATED; adultos estão READY mediante policy explícita.
12. IMP-000 materializa Organization clinic/unit; IMP-001 materializa People person/contact_point/command_receipt e Patients patient_profile/command_receipt.
13. Uma migration prerequisite Organization e, no IMP-001, duas migrations independentes People/Patients.
14. Unit, Application, Integration PostgreSQL, API e Architecture tests são obrigatórios.
15. O slice é backend-only e contém somente POST/GET, duas roots e persistence mínima.

### Blocker assessment

Há um único blocker técnico predecessor para iniciar IMP-001: ausência de Organization/Unit materializada. O próximo trabalho é IMP-000, não IMP-001. Isso não é falha do IMP-001-DESIGN. Ativação externa/produção permanece gated pelo IAM concreto, Audit adequado e provisioning real. Cadastro de menores e payer diferente não está READY.

## 34. Validation

- [x] slice limitado a POST/GET backend;
- [x] ownership People/Patients preservado;
- [x] sem transaction, FK, join ou DbContext cross-context;
- [x] orchestration owner definido;
- [x] partial failure e retry determinísticos;
- [x] idempotência obrigatória e mismatch definidos;
- [x] CPF, phone, birth date e Unit definidos;
- [x] menor/guardian gated sem relaxar regra;
- [x] POST/GET e Problem Details definidos;
- [x] lacuna de Unit convertida em prerequisite mínimo explícito;
- [x] persistence/migrations mínimas por owner;
- [x] eventos canônicos respeitados, sem evento inventado;
- [x] PostgreSQL/Testcontainers e suites obrigatórias planejados;
- [x] estrutura real do skeleton respeitada;
- [x] nenhum código foi criado ou alterado;
- [x] critérios de PASS de IMP-001-DESIGN atendidos.

**Resultado: IMP-001-DESIGN — PASS; IMP-001 — BLOCKED_BY_PREREQUISITE. Próxima tarefa: IMP-000 — ORGANIZATION / UNIT BASELINE.**
