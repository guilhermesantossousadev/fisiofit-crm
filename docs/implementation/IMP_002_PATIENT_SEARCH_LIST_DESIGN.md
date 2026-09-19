# IMP-002 — Patient Search / List Design

## 1. Status

`IMP-002-DESIGN — DONE / PASS` em 2026-09-19.

Este documento define o futuro slice backend `IMP-002 — PATIENT SEARCH / LIST`. Nenhum endpoint, contrato C#, índice, migration ou teste foi implementado nesta tarefa.

Readiness: **IMP-002 — READY**, limitado ao contrato e ao algoritmo abaixo. Os gates de menores, pagador diferente, IAM de produção, Audit durável e retenção de receipts permanecem inalterados.

## 2. Objective

Entregar o menor próximo vertical slice após IMP-000 e IMP-001: permitir que Secretária/Recepção e Proprietária/Gestora localizem pacientes existentes sem conhecer `patientId`, por listagem paginada ou busca administrativa simples.

O slice implementará `QRY-010 — SearchPatients` por `GET /api/v1/patients`, reutilizando a arquitetura comprovada de IMP-001 e sem transformar a busca em deduplicação, reporting ou busca global.

## 3. Inputs

Foram usados integralmente `PROJECT_OS.md` e seu último handoff, AI Profile, designs e resultados de IMP-000/IMP-001, Context Map, Ownership Map, ARC-003, DB-001, API-001, AUTH-001, MODEL-001, MODEL-005, Rules Index, Process Index e ADR-001 a ADR-007.

Também foi inspecionada a implementação real de Host, ModuleContracts, Organization, People, Patients e os quatro projetos de teste. O desenho não presume interfaces inexistentes: ele reutiliza `PatientRequestActor`, `patients.profile.read`, `people.person.read`, `PatientsDbContext`, `PeopleDbContext`, `IGetUnitForPatientRead`, normalização de CPF por dígitos, telefone `countryCode + areaCode + number` e CPF mascarado existentes. O único contrato novo necessário é o contrato People purpose-specific da seção 13.

## 4. Slice Boundary

### IN_SCOPE

- backend only;
- `GET /api/v1/patients` para listagem e busca administrativa;
- `search` com modos controlados de nome, CPF exato e telefone exato;
- `primaryUnitId`, `administrativeStatus`, `page`, `pageSize` e `sort` em whitelist;
- `UNIT_SCOPE`, permissions explícitas, PII minimizada e `Cache-Control: no-store`;
- composição síncrona Patients → People → Organization sem persistence cross-context;
- testes Unit/Application, PostgreSQL, API e Architecture.

### Classificação dos candidatos

| Candidato | Classificação | Decisão do primeiro slice |
|---|---|---|
| `search` textual | `IN_SCOPE` | um único critério purpose-specific; sem query language |
| nome | `IN_SCOPE` dentro de `search` | case-insensitive, accent-sensitive, contains, mínimo 3 caracteres; não há parâmetro `name` separado |
| CPF | `IN_SCOPE` dentro de `search` | somente CPF completo, válido e exact match após normalização; não há parâmetro `cpf` separado |
| telefone | `IN_SCOPE` dentro de `search` | somente número administrativo completo reconhecido, exact match normalizado; não há parâmetro `phone` separado |
| `primaryUnitId` | `IN_SCOPE` | filtro adicional, sempre intersectado com o scope autorizado |
| `administrativeStatus` | `IN_SCOPE` | somente `ACTIVE` ou `INACTIVE`, ambos já canônicos e materializados |
| `startDate` como filtro | `DEFERRED` | não necessário para localizar paciente no MVP |
| paginação | `IN_SCOPE` | `page`/`pageSize`, offset baseline |
| ordenação | `IN_SCOPE` | somente `name` e `-name` |
| `startDate` como sort | `DEFERRED` | amplia a composição entre owners sem benefício necessário no primeiro slice |
| parâmetros dedicados `name`, `cpf`, `phone` | `OUT_OF_SCOPE` | evitam duplicar semântica e combinações prematuras |

Também são `OUT_OF_SCOPE`: frontend, detalhe novo, update, deactivate/delete, guardian, payer diferente, merge, fuzzy duplicate detection, Clinical/Finance/CRM search, export, Reports, busca global, Redis, Elasticsearch, full text, trigram e cursor pagination.

## 5. Actors / Authorization

O endpoint administrativo exige, cumulativamente:

- principal autenticado e conta `ACTIVE`;
- ausência de explicit deny;
- `patients.profile.read`;
- `people.person.read`;
- pelo menos uma Unit grant válida em `UNIT_SCOPE`.

`OWNER_MANAGER` e `SECRETARY_RECEPTION` podem receber essas permissions dentro de seu scope explícito. Role isolada nunca autoriza a operação.

`PHYSIOTHERAPIST` não recebe esta listagem administrativa. Seu subset mínimo sob `ASSIGNED_PATIENT` pertence a uma query assistencial futura, não a um modo oculto deste endpoint. `DEVELOPER_IT` é `DENY`: autoridade técnica ou acesso a observabilidade não concede leitura administrativa.

Patients valida a permission e o scope do endpoint; People revalida `people.person.read` em seu próprio contrato. O Host apenas autentica, cria o actor context, correlaciona e mapeia HTTP.

## 6. HTTP Contract

```http
GET /api/v1/patients?search=ana&primaryUnitId={uuid}&administrativeStatus=ACTIVE&page=1&pageSize=25&sort=name
```

Sucesso:

```json
{
  "items": [
    {
      "patientId": "uuid",
      "fullName": "Ana Souza",
      "cpf": { "status": "PRESENT", "masked": "***.***.***-09" },
      "primaryPhone": {
        "countryCode": "55",
        "areaCode": "11",
        "maskedNumber": "*****6789"
      },
      "primaryUnit": {
        "unitId": "uuid",
        "name": "Unidade Centro",
        "status": "ACTIVE"
      },
      "administrativeStatus": "ACTIVE",
      "relationshipStartedOn": "2026-09-16"
    }
  ],
  "page": 1,
  "pageSize": 25,
  "totalCount": 1
}
```

O response usa `200 OK` inclusive quando `items=[]` e `totalCount=0`. Inclui `Cache-Control: no-store`. Não retorna `personId`, CPF cru, telefone completo, `birthDate`, dados clínicos, comerciais ou financeiros.

## 7. Query Parameters

| Parâmetro | Tipo/default | Regras |
|---|---|---|
| `search` | string opcional | trim/colapso de espaços; vazio equivale a omitido; máximo 100 caracteres; modos da seção 8 |
| `primaryUnitId` | UUID opcional | UUID não vazio; deve pertencer ao scope do ator |
| `administrativeStatus` | wire enum opcional | `ACTIVE` ou `INACTIVE`; omitido inclui ambos |
| `page` | inteiro, default `1` | mínimo `1` |
| `pageSize` | inteiro, default `25` | mínimo `1`, máximo `100` |
| `sort` | string, default `name` | somente `name` ou `-name`; uma chave pública, sem lista arbitrária |

Parâmetro desconhecido não ganha semântica de coluna. `name`, `cpf`, `phone`, `startDate`, `offset`, `limit` e qualquer sort não publicado retornam `400 VALIDATION_ERROR`, em vez de serem silenciosamente ignorados.

## 8. Search Semantics

`search` localiza pacientes para operação administrativa. Ele não sinaliza duplicata, não calcula similaridade, não seleciona Person para merge e não muda a decisão de People sobre identidade.

People é o owner da classificação e normalização do termo:

1. trim, colapso de whitespace e Unicode NFC;
2. se houver letras, o modo é **NAME**;
3. se o termo for uma representação de CPF completa e válida, inclui o predicado **CPF_EXACT**;
4. se o termo representar telefone internacional completo aceito pelo normalizador People, inclui **PHONE_EXACT**;
5. um termo numérico de 11 dígitos que seja simultaneamente CPF válido e telefone completo executa `CPF_EXACT OR PHONE_EXACT`; cada predicado continua exato;
6. se nenhum modo válido puder ser formado, retorna `400 VALIDATION_ERROR`.

### Nome

- mínimo 3 e máximo 100 caracteres após normalização;
- case-insensitive;
- accent-sensitive: `Jose` não corresponde a `José` neste slice;
- `contains` sobre `fullName`, sem fuzzy, phonetic, `unaccent`, full text ou trigram;
- resultado ordenado por nome case-insensitive e depois `personId` ascendente; direção inversa inverte ambos;
- o comportamento depende da collation PostgreSQL aprovada para comparação de case, mas nunca remove acentos implicitamente.

### CPF

- pontuação é removida; somente dígitos ASCII permanecem;
- exige exatamente 11 dígitos e check digits válidos;
- match é `cpf_normalized = valor`, nunca prefix/contains/last digits;
- CPF inválido não vira busca por nome nem consulta permissiva;
- a resposta mantém CPF mascarado exatamente como o detalhe de IMP-001;
- query string, valor normalizado e resultado não entram em logs, spans, métricas ou mensagens de erro.

### Telefone

- separadores visuais são removidos pelo owner People;
- aceita somente o número internacional completo, incluindo country code e area code, com 8 a 15 dígitos após remover separadores e um `+` inicial opcional; não presume nem acrescenta `55`;
- a forma canônica é a mesma de People: `+{countryCode}{areaCode}{number}`;
- a comparação é exact match contra `ContactPoint.NormalizedValue` do telefone primário `ACTIVE`;
- não há suffix match, “últimos quatro”, contains ou busca por número incompleto;
- a resposta devolve somente telefone mascarado.

## 9. Pagination

Usa a convenção canônica offset `page` 1-based + `pageSize`.

- default: `page=1`, `pageSize=25`;
- máximo: `pageSize=100`;
- `page < 1`, `pageSize < 1`, `pageSize > 100`, overflow do offset ou valor não inteiro: `400 VALIDATION_ERROR`;
- página além do fim: `200` com `items=[]` e o `totalCount` real;
- `totalCount` é obrigatório, conforme API-001, e conta a interseção final entre profiles autorizados/filtrados e Persons que atendem `search`;
- não há cursor neste slice.

## 10. Sorting

Whitelist do primeiro slice:

- `sort=name` — ascendente;
- `sort=-name` — descendente.

Default: `name`. O tie-breaker oculto e estável é `personId` na mesma direção. `startDate`, `createdAt`, CPF, telefone, status, Unit e nomes de coluna internos não são aceitos como sort.

Limitar o primeiro slice a nome deixa um único owner responsável por filtro, ordem, paginação e `totalCount`. `startDate` poderá entrar apenas com evidência de uso e desenho que preserve a mesma correção sem projection prematura.

## 11. Read Model

`PatientListItem` contém somente:

- `patientId`;
- `fullName`;
- `cpf: { status, masked? }`;
- `primaryPhone: { countryCode, areaCode, maskedNumber }`;
- `primaryUnit: { unitId, name, status }`;
- `administrativeStatus`;
- `relationshipStartedOn`.

`birthDate` não entra: não é necessária para a decisão de localizar o paciente nesta lista e ampliaria PII em todas as linhas. Continua disponível somente no detalhe autorizado já existente. `personId` também não entra no contrato HTTP de lista.

## 12. Ownership

1. **Query/HTTP owner:** Patients Application owns `GET /api/v1/patients` e `QRY-010`.
2. **Composition owner:** Patients Application.
3. **People owner:** filtra, normaliza, ordena e pagina os dados civis/contato somente sobre os candidatos fornecidos por Patients.
4. **Patients owner:** seleciona profiles por status, Unit e authorization scope e monta o `PatientListItem`.
5. **Organization owner:** resolve o resumo das Units da página pelo contrato existente.
6. **Host:** adapter/composition root; não acessa DbContext nem implementa algoritmo.

Não existe `RegistryDbContext`, query SQL cross-schema, navigation cross-context, shared repository ou exposição de `IQueryable`.

## 13. People Search Contract

Novo contrato mínimo em `Fisiofit.ModuleContracts.People`, com nome purpose-specific equivalente a:

```text
SearchPeopleForPatientList(
  eligiblePersonIds,
  search?,
  page,
  pageSize,
  sort=name|-name,
  actorContext,
  correlationId)

=> Found(
     items: [
       personId,
       fullName,
       cpfStatus,
       maskedCpf?,
       primaryPhoneMasked
     ],
     page,
     pageSize,
     totalCount)
 | ValidationFailure
 | Forbidden
 | DependencyFailure
```

`eligiblePersonIds` é uma coleção materializada, imutável e deduplicada de UUIDs opacos; não é `IQueryable`, expression tree, entity, repository ou callback. People rejeita coleção/termo inválido, revalida `people.person.read`, normaliza `search`, limita a query a Persons `CURRENT`, exige telefone primário `ACTIVE`, aplica search/order/page e produz `totalCount` no próprio owner.

O contrato não recebe Unit/status, não conhece `PatientProfile` e não vira API genérica de People/reporting. Seu output já minimiza CPF e telefone; Patients nunca recebe os valores crus para montar a lista.

## 14. Patients Query

Após autorização, `PatientsDbContext` executa uma única leitura owner-local `AsNoTracking`:

- limita `PrimaryUnitId` ao conjunto autorizado, independentemente do input do cliente;
- aplica `primaryUnitId` somente após validar que ela está no conjunto autorizado;
- aplica `administrativeStatus` quando informado;
- projeta somente `patientId`, `personId`, `primaryUnitId`, `administrativeStatus` e `relationshipStartedOn`;
- materializa esse conjunto candidato uma vez para a composição;
- não aplica offset antes de People, pois isso quebraria busca, ordenação e `totalCount` por nome/CPF/telefone.

Depois da resposta People, Patients correlaciona por `personId`, preserva exatamente a ordem retornada e nunca devolve seu persistence model.

## 15. Organization Dependency

IMP-002 reutiliza `IGetUnitForPatientRead`. Patients coleta os `primaryUnitId` distintos apenas da página final e resolve cada Unit pelo contrato owner, sem acessar `OrganizationDbContext`.

Não é necessário criar contrato batch no primeiro slice: `pageSize <= 100`, e o conjunto real de Units distintas é pequeno e limitado pelo resultado. A implementação deve deduplicar IDs e evitar chamadas repetidas. Uma Unit institucional `INACTIVE` pode continuar aparecendo como tal em paciente histórico; isso não retira o profile da lista.

Unit ausente após um profile referenciá-la é inconsistência de dados e retorna `500 INTERNAL_ERROR` sanitizado, como o detalhe atual.

## 16. Read Composition

Algoritmo normativo de duas fases controladas (opção D):

1. Patients autoriza permissions, explicit deny e Unit grants.
2. Patients valida parâmetros e calcula `effectiveUnitIds` a partir do scope, nunca apenas do filtro cliente.
3. Patients consulta e materializa os candidate profiles já filtrados por Units/status.
4. Patients envia somente os `personId` elegíveis e os parâmetros civis/paginação/sort ao contrato People.
5. People consulta somente seu schema, aplica search, `CURRENT`, order, offset, limit e count sobre a mesma relação filtrada.
6. People devolve a página ordenada, PII minimizada e `totalCount` exato.
7. Patients correlaciona os profiles da página em memória, mantendo a ordem People.
8. Patients resolve as Units distintas pelo contrato Organization e monta o envelope HTTP.

Essa ordem garante paginação correta: nenhuma página é cortada antes da interseção Patients × People. Também evita o problema da opção B em que uma página de Persons poderia conter não-pacientes. A opção A foi rejeitada porque não consegue filtrar/ordenar por People corretamente; projection (C) foi rejeitada por não ser necessária; read DB, Reports, Redis, Elasticsearch, materialized view e SQL cross-schema são proibidos.

Não há snapshot distribuído entre contexts. A relação candidata Patients é fixada em memória durante a request; concorrência entre requests tem a semântica normal de offset pagination. Se People encontrar referência ausente/inativa ou sem telefone primário para um profile do slice, retorna dependency inconsistency; o composer responde `500` em vez de reduzir silenciosamente a página ou falsear `totalCount`.

## 17. UNIT_SCOPE

- **Uma Unit grant:** sem filtro, busca somente essa Unit; com a mesma Unit, resultado equivalente.
- **Várias Unit grants:** sem filtro, busca a união permitida; com filtro permitido, restringe à Unit solicitada.
- **Unit solicitada sem grant:** `403 FORBIDDEN` antes de consultar profiles, mesmo que a Unit não tenha pacientes. Isso evita usar o filtro do cliente como autorização.
- **Nenhuma Unit grant:** `403 FORBIDDEN`; uma permission sem resource scope não autoriza enumeração.
- **Unit permitida sem resultados:** `200` vazio.

O endpoint nunca busca globalmente para depois remover rows. O predicado `PrimaryUnitId IN effectiveUnitIds` faz parte da primeira query Patients.

## 18. PII / Security

- `fullName` é necessário ao propósito administrativo e aparece somente a atores autorizados.
- CPF é aceito como critério completo, mas resposta e contratos de composição devolvem somente status + máscara.
- telefone é critério completo exato, porém a lista devolve somente máscara; o detalhe existente continua sendo a superfície autorizada para o número completo.
- `birthDate` é omitida da lista.
- query string inteira, `search`, CPF, telefone, lista de personIds e bodies/results não são registrados.
- telemetry registra somente `searchMode` (`NONE|NAME|CPF|PHONE|CPF_OR_PHONE`), presença dos filtros, page/pageSize, sort, contagens, duração, outcome, actor/resource references opacas e `traceId`; nunca o termo.
- access logs/proxy/APM devem redigir ou suprimir a query string desta rota; exceptions e Problem Details não repetem valores do cliente.
- resposta usa `Cache-Control: no-store`; nenhum distributed/output cache é introduzido.
- rate limiting concreto permanece configuração operacional; o máximo de página e a whitelist limitam custo já neste slice.

## 19. Persistence Impact

Não há nova entidade, coluna, constraint, schema, projection, outbox, inbox, cache ou read database.

**NO MIGRATION** para IMP-002. A implementação usa as tabelas e migrations existentes de IMP-000/IMP-001. Nenhum `DbContext` passa a mapear entity de outro owner.

## 20. Index Strategy

| Acesso | Classificação | Estado/decisão |
|---|---|---|
| `people.person.cpf_normalized` exact | `REQUIRED_FOR_LOOKUP` e já `REQUIRED_FOR_INVARIANT` | coberto por `ux_person__cpf_normalized` existente |
| `patients.patient_profile.person_id` correlação | `REQUIRED_FOR_LOOKUP` e já `REQUIRED_FOR_INVARIANT` | coberto por `ux_patient_profile__person_id` existente |
| `patients.patient_profile.primary_unit_id` scope/filtro | `REQUIRED_FOR_LOOKUP` | coberto por `ix_patient_profile__primary_unit_id` existente |
| `people.contact_point.normalized_value` exact | `PERFORMANCE_CANDIDATE` | sem índice novo; medir `EXPLAIN (ANALYZE, BUFFERS)` com cardinalidade representativa |
| `people.person.full_name` contains case-insensitive | `PERFORMANCE_CANDIDATE` | btree comum não atende contains; não adicionar trigram/unaccent/full-text neste slice |
| profile `(primary_unit_id, administrative_status, person_id)` | `PERFORMANCE_CANDIDATE` | avaliar somente se plano mostrar scan/custo relevante |

Os índices mínimos já existem. Como a query People é limitada ao conjunto de pacientes elegíveis e o volume inicial não foi demonstrado, criar índice/extension agora seria especulativo. Um índice parcial de telefone ou composto Patients exige evidência de query plan antes de migration futura.

## 21. Error Mapping

Todos os erros usam RFC 9457, `application/problem+json`, `code`, `traceId` e `errors` quando aplicável.

| HTTP/code | Situação |
|---|---|
| `400 VALIDATION_ERROR` | search, UUID, status, page/pageSize ou sort inválido/desconhecido |
| `401 UNAUTHORIZED` | principal ausente ou inválido |
| `403 FORBIDDEN` | conta inativa, explicit deny, permission ausente, nenhum Unit scope ou Unit solicitada fora do scope |
| `200` vazio | query válida/autorizada sem matches ou página além do fim |
| `500 INTERNAL_ERROR` | falha inesperada ou referência cross-context inconsistente, sempre sanitizada |

Busca vazia nunca retorna `404`. CPF/telefone inválido retorna `400` sem ecoar o valor. Não há `409`, `422` ou idempotency key para uma query read-only.

## 22. Test Plan

### Unit / Application

- whitespace/NFC e classificação dos modos de search;
- nome mínimo/máximo, case-insensitive e accent-sensitive;
- CPF completo válido/exato, formatado, inválido e ausência de partial match;
- telefone internacional completo com `+` opcional, exact match e rejeição de fragmento/prefixo nacional implícito;
- validação `page`, `pageSize`, overflow, enum e whitelist `name|-name`;
- filtro status e primary Unit;
- enforcement de permissions, explicit deny e scope antes de dados;
- composição preserva ordem People e `totalCount`;
- candidato Patients nunca é paginado antes de People;
- inconsistência de Person/phone/Unit falha sanitizada;
- resposta mascara CPF/telefone e omite birth date/personId.

### Integration — PostgreSQL real

- migrations existentes sobem sem migration IMP-002;
- listagem sem search;
- nome: contains, case-insensitive, accent-sensitive;
- CPF normalizado exact e sem partial;
- telefone normalizado exact e sem suffix/contains;
- `primaryUnitId`, `ACTIVE` e `INACTIVE`;
- múltiplas Units com interseção de scope;
- paginação e `totalCount` sobre a interseção final;
- sort asc/desc e desempate determinístico;
- isolamento dos contexts, schemas e histories;
- query plans dos acessos CPF/Unit usam índices existentes quando seletivos; candidatos de performance são documentados, não promovidos sem evidência.

### API

- list success e search success por cada modo;
- resposta vazia 200;
- defaults, página subsequente e página além do fim;
- invalid search/page/pageSize/status/sort 400;
- 401 e 403;
- Unit única, múltiplas, Unit solicitada sem grant e nenhum scope;
- `OWNER_MANAGER`/`SECRETARY_RECEPTION` com grants; Physiotherapist e Developer negados;
- CPF e telefone mascarados; ausência de birthDate/personId;
- `Cache-Control: no-store` e Problem Details sanitizado;
- access/diagnostic logs não contêm o termo pesquisado.

### Architecture

- Patients não referencia People/Organization Infrastructure nem seus DbContexts;
- People não referencia Patients internals;
- contratos não expõem entity, EF, `IQueryable`, repository ou delegate de query;
- nenhum DbContext/model/migration cross-schema;
- Host não contém composição ou SQL;
- nenhum Reports/search index/cache/projection foi introduzido.

## 23. Expected File Plan

Plano provável para o futuro IMP-002; nomes podem variar sem alterar ownership:

```text
src/backend/Fisiofit.ModuleContracts/People/
  PatientSearchContracts.cs
src/backend/Modules/Fisiofit.Modules.Registry/
  People/Application/SearchPeopleForPatientList.cs
  Patients/Application/SearchPatients.cs
  Patients/Application/PatientSearchModels.cs
  Patients/PatientsEndpoints.cs                 # adicionar somente GET collection
  RegistryModule.cs                             # DI dos handlers/contract
tests/backend/Fisiofit.UnitTests/Registry/
  People/PatientSearchTests.cs
  Patients/PatientSearchTests.cs
tests/backend/Fisiofit.IntegrationTests/Registry/PatientSearch/
  PatientSearchPostgreSqlTests.cs
tests/backend/Fisiofit.ApiTests/Patients/
  SearchPatientsApiTests.cs
tests/backend/Fisiofit.ArchitectureTests/
  RegistryPatientSearchBoundaryTests.cs
docs/implementation/
  IMP_002_PATIENT_SEARCH_LIST.md                 # resultado futuro
PROJECT_OS.md
```

Não são esperadas migrations nem arquivos em Reports, frontend ou infraestrutura externa. `GetPatientDetails` e seus contratos permanecem; helpers podem ser extraídos somente se houver duplicação real de máscara/actor/error mapping, sem refatoração estética ampla.

## 24. Definition of Done

O futuro IMP-002 estará DONE somente quando:

- `GET /api/v1/patients` implementar exatamente os filtros aprovados;
- `page/pageSize`, defaults 1/25, máximo 100, `totalCount` e `name|-name` funcionarem;
- search de nome, CPF e telefone obedecer às semânticas exatas deste design;
- authorization deny-by-default e `UNIT_SCOPE` forem aplicados antes da composição;
- Patients, People e Organization colaborarem somente por contratos públicos;
- paginação/count forem calculados após a interseção correta, sem cortar candidatos antes de People;
- o read model minimizar PII, mascarar CPF/telefone, omitir birthDate e usar `no-store`;
- PostgreSQL, API e Architecture tests cobrirem paths positivos/negativos e isolamento;
- nenhuma persistence cross-context, projection, cache ou infraestrutura pesada for criada;
- build e todas as suites backend passarem com zero warnings/errors;
- documentação do resultado/OpenAPI aplicável e PROJECT_OS/handoff forem atualizados;
- nenhum item OUT_OF_SCOPE/GATED tiver sido iniciado.

## 25. Risks

| Risco | Mitigação |
|---|---|
| coleção de `eligiblePersonIds` crescer e pressionar memória/parâmetros SQL | slice inicial simples; medir cardinalidade/latência; otimizar somente com evidência |
| `contains` em nome exigir scan | conjunto já limitado a patients/scope; page cap; query-plan test e candidato futuro explícito |
| paginação incorreta por filtrar depois do offset | People recebe todo o conjunto elegível e é o único a filtrar/ordenar/paginar |
| drift entre contexts durante a request | candidates Patients materializados uma vez; inconsistência falha fechada; sem falsa transação distribuída |
| ambiguidade CPF/telefone de 11 dígitos | OR entre dois matches exatos, nunca partial/fuzzy |
| vazamento pela URL/log do servidor | query-string redaction/suppression e telemetry apenas por modo |
| N+1 de Unit | deduplicar Units da página; conjunto institucional pequeno; batch somente com evidência |
| contrato People virar reporting | nome purpose-specific, input/output mínimos e ArchitectureTests |

Se cardinalidade real tornar a coleção de candidatos inaceitável, a próxima decisão deve ser baseada em métricas e query plans. Isso pode justificar contrato batch/algoritmo de merge ou projection operacional reconstruível; não autoriza join cross-schema.

## 26. Gated / Deferred

Permanecem `GATED`: menores/GuardianLink, payer diferente/ResponsiblePayerLink, ativação externa/produção sem IAM concreto, Audit durável e provisioning real, além da decisão definitiva de retenção dos receipts de IMP-001.

Permanecem `DEFERRED`: `startDate` filter/sort, filtros dedicados por campo, busca accent-insensitive, fuzzy/dedup/merge detection, projection, batch Organization, frontend, export e demais operações Patients.

IMP-002 não resolve nem relaxa nenhum desses gates.

## 27. IMP-002 Readiness

**READY.** Não há blocker novo para implementar este slice no ambiente backend/testes já comprovado por IMP-001.

Respostas obrigatórias:

1. Patients Application owns `GET /patients`.
2. Nome é buscado por contrato People sobre IDs elegíveis definidos por Patients, sem join.
3. CPF é completo, validado, normalizado para 11 dígitos e exact match; response mascarado.
4. Telefone é completo, normalizado como People e exact match; response mascarado.
5. `primaryUnitId` filtra `PatientProfile` em Patients após validação de scope.
6. `UNIT_SCOPE` produz o conjunto máximo de Units antes de qualquer query de profiles.
7. Paginação: `page/pageSize`, offset 1-based.
8. Defaults/máximo: `1`, `25`, máximo `100`.
9. Sort default: `name` ascendente + `personId` ascendente.
10. Sorts: `name` e `-name` somente.
11. O retorno possui `totalCount` exato.
12. Read model: patientId, nome, CPF/telefone mascarados, Unit, status e relationshipStartedOn.
13. `birthDate` não entra na lista.
14. CPF permanece masked.
15. Não há migration: `NO MIGRATION`.
16. Índices mínimos já existem; phone/name/composto status são apenas `PERFORMANCE_CANDIDATE` mediante query plan.
17. Não há necessidade legítima de projection agora.
18. A paginação permanece correta porque People filtra/ordena/pagina o conjunto completo já elegível de Patients.
19. O slice continua pequeno: um GET, um contrato People e handlers/tests, sem nova entidade/infra/UI.
20. Pode ser implementado sem novo blocker; gates existentes continuam fora do caminho aprovado.

## 28. Validation

- [x] endpoint, filtros, search, paginação, sort e read model definidos;
- [x] atores, permissions e todos os cenários de `UNIT_SCOPE` definidos;
- [x] ownership e algoritmo sem join/persistence cross-context definidos;
- [x] paginação e `totalCount` semanticamente corretos;
- [x] PII, logging, telemetry e cache definidos;
- [x] persistence `NO MIGRATION` e índices classificados;
- [x] reuse do detalhe/Organization avaliado sem refatoração agressiva;
- [x] test plan e file plan definidos;
- [x] nenhuma projection, read DB ou infraestrutura pesada introduzida;
- [x] gates anteriores preservados;
- [x] nenhum código foi implementado.

**Resultado: IMP-002-DESIGN — PASS; IMP-002 — READY. Próxima tarefa: IMP-002 — PATIENT SEARCH / LIST.**
