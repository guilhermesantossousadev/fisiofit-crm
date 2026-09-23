# IMP-002 — Patient Search / List

## 1. Status

`DONE — PASS` em 2026-09-23. O status cobre somente a busca/listagem administrativa backend aprovada em `IMP_002_PATIENT_SEARCH_LIST_DESIGN.md`.

## 2. Objective

Implementar `QRY-010 — SearchPatients` por `GET /api/v1/patients`, permitindo localizar pacientes existentes por listagem, nome, CPF completo ou telefone internacional completo, com filtros administrativos, paginação e ordenação controlados.

## 3. Scope entregue

- `search` opcional com nome case-insensitive/accent-sensitive contains, CPF exato e telefone exato;
- `primaryUnitId`, `administrativeStatus=ACTIVE|INACTIVE`, `page`, `pageSize` e `sort=name|-name`;
- defaults `page=1`, `pageSize=25`, máximo 100 e `totalCount` exato;
- `UNIT_SCOPE`, permissions cumulativas e explicit deny antes da leitura;
- composição Patients → People → Organization exclusivamente por contratos públicos;
- resposta administrativa mínima, mascarada e `Cache-Control: no-store`.

## 4. HTTP API

`GET /api/v1/patients` devolve `200 OK` com `items`, `page`, `pageSize` e `totalCount`, inclusive sem matches ou em página além do fim. Parâmetros desconhecidos, UUID/status/paginação/sort inválidos e termos incompletos retornam `400 VALIDATION_ERROR`. Principal inválido retorna 401; permission, estado, explicit deny ou Unit scope insuficiente retornam 403.

## 5. Search semantics

People normaliza whitespace e Unicode NFC. Nomes exigem 3 a 100 caracteres, usam `ILIKE` com `%`, `_` e escape tratados como literais e preservam sensibilidade a acentos. CPF é validado e comparado pelos 11 dígitos. Telefone aceita 8 a 15 dígitos internacionais, com `+` e separadores opcionais, sem acrescentar `55`; a comparação é contra o telefone primário ACTIVE normalizado. Um termo numérico válido nos dois modos usa CPF OR telefone, ambos exatos.

## 6. Ownership e composição

Patients Application autoriza, calcula as Units efetivas, filtra profiles por Unit/status e materializa uma vez apenas patientId, personId, Unit, status e início do relacionamento. O contrato purpose-specific `ISearchPeopleForPatientList` recebe somente os personIds elegíveis e faz filtro civil, ordenação estável, offset, limit e count em People. Patients preserva a ordem devolvida, correlaciona profiles em memória e resolve somente as Units distintas da página via `IGetUnitForPatientRead`.

Nenhum offset é aplicado antes de People. Não existe join, navigation, repository, `IQueryable`, DbContext compartilhado ou SQL cross-schema.

## 7. Authorization

A operação exige principal autenticado, conta ACTIVE, ausência de explicit deny, `patients.profile.read`, `people.person.read` e ao menos um grant de Unit. A Unit solicitada é validada contra o scope antes da query Patients. People revalida sua permission no contrato owner. Role isolada não autoriza a rota; os atores administrativos recebem capacidade por grants explícitos, enquanto Physiotherapist/Developer sem esses grants permanecem negados.

## 8. PII e segurança

O item retorna apenas patientId, nome, CPF status/máscara, telefone separado e mascarado, Unit, status administrativo e relationshipStartedOn. Não retorna personId, CPF/telefone crus ou birthDate. Problem Details não ecoa valores. O endpoint e seus logs de falha não registram query string ou termo; respostas usam `no-store`.

## 9. Consistência

People valida que todo candidato aponta para Person CURRENT com telefone primário ACTIVE; inconsistência falha fechada com 500 sanitizado, sem reduzir página nem falsear count. Unit ausente também retorna 500. Units são deduplicadas para impedir chamadas repetidas.

## 10. Persistence e índices

`NO MIGRATION`. Nenhuma entidade, coluna, projection, cache, read database, extension ou índice foi criado. PostgreSQL confirmou uso possível de `ux_person__cpf_normalized` e `ix_patient_profile__primary_unit_id` em planos seletivos. Nome, telefone e índice composto continuam apenas candidatos dependentes de evidência de cardinalidade/custo real.

## 11. Testing

- UnitTests: normalização NFC/whitespace, modos NONE/NAME/CPF/PHONE/CPF_OR_PHONE, limites, collection IDs, paginação e sort;
- IntegrationTests/PostgreSQL: nome case/acento, CPF/telefone exatos, rejeição de fragmento, paginação/count/ordem, Unit/status/scope, migrations existentes e planos de índice;
- ApiTests/PostgreSQL/TestServer: busca por modos, defaults, página além do fim, PII mínima/mascarada, no-store, validações, 401 e 403;
- ArchitectureTests: contratos sem persistence/query delegates, boundaries entre owners, Host adapter-only e ausência de migration/projection/cache/Reports.

## 12. Validation results

- prerequisite Docker Desktop/context `desktop-linux`: PASS;
- baseline anterior: PASS, 62/62;
- build final: PASS, zero warnings/errors;
- UnitTests: PASS, 34/34;
- IntegrationTests PostgreSQL: PASS, 27/27;
- ApiTests PostgreSQL/HTTP: PASS, 12/12;
- ArchitectureTests: PASS, 14/14;
- full suite: PASS, 86/86;
- `git diff --check`: PASS;
- migrations novas: zero.

## 13. Out of scope preservado

Não foram iniciados frontend, detalhe novo, update/deactivate/delete, guardian, payer diferente, merge/dedup/fuzzy, filtros dedicados, startDate filter/sort, export, Reports, Redis, Elasticsearch, full text, trigram, cursor, projection ou batch Organization.

## 14. Riscos remanescentes

A coleção materializada de personIds e os scans de nome/telefone precisam ser medidos com cardinalidade de produção. Offset pagination pode variar entre requests concorrentes. Redaction de query string deve permanecer requisito de configuração para qualquer access logging/proxy/APM futuramente habilitado. IAM/Audit/provisioning continuam gates para ativação externa/produção.
