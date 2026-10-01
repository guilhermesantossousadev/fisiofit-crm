# IAM-001-DESIGN — Identity & Access mínimo para uso administrativo assistido

**Status:** PASS (design documental)  
**Implementation readiness:** NEEDS_CONTRACT_RECONCILIATION — IAM-OD-001..007 estão aprovadas; reconciliar API-001/DB-001/AUTH-001/STATE-001 antes de IAM-001-IMP.
**Assisted-use readiness:** BLOCKED — IAM implementado/validado, Audit durável e WEB-001 ainda são necessários.  
**Data:** 2026-09-30  
**Fontes:** `PROJECT_OS.md`, `ROADMAP_MVP_OPERACIONAL.md`, AUTH-001, API-001, DB-001, ARC-003, Context/Ownership Map, MODEL-001/005, STATE-001, `docs/decisions/IAM_001_DECISIONS.md` e código atual.

## 1. Objetivo

Definir o menor modelo persistido de Identity & Access (Access) que substitui a autenticação exclusiva de testes para o fluxo administrativo assistido: autenticar uma conta real, identificar seu ator estável, avaliar permission + `UNIT_SCOPE` + explicit deny no estado atual e revogar o acesso. O primeiro uso é exclusivamente o fluxo administrativo já existente para paciente adulto `SELF`.

## 2. Estado atual

Access é um shell (`AccessModule`) sem schema, `DbContext`, conta, credential, sessão, endpoint ou storage. O Host chama `AddAuthentication`/`AddAuthorization`, mas não registra scheme de produção. Os endpoints de Patients convertem claims (`NameIdentifier`, `permission`, `unit_id`, `account_status`, `explicit_deny`) em `PatientRequestActor` e aplicam os guards no caso de uso. Em `Fisiofit.ApiTests`, somente o `TestAuthenticationHandler` registra o scheme e alimenta essas claims por headers `X-Test-*`.

Isso valida o comportamento de Patients, mas não autenticação, persistência, revogação ou autorização de produção. Organization, People e Patients possuem persistência owner-local; Staff não está implementado. Audit é shell, sem evidência durável.

## 3. Escopo

- conta administrativa real e lifecycle mínimo;
- identificador de login e credential somente depois de decisão formal;
- login, current session, logout e invalidação/revogação;
- grants diretos persistidos de permissions, explicit denies e `UNIT_SCOPE`;
- bootstrap único e recovery administrativo mínimo;
- avaliação atual por request para os endpoints administrativos de Patients;
- contratos purpose-specific, testes e evidências consumíveis por Audit futuro.

## 4. Out of scope

MFA/step-up completo, provider externo específico, service identities, impersonation, contas profissionais automáticas, Clinical, Finance, grants globais, role management completo, recovery self-service por e-mail, UI, migration e implementação. Roles podem existir conforme DB-001, mas não são necessários para autorizar o primeiro fluxo e não substituem permission.

## 5. Ownership

Access possui `UserAccount`, credential ou sua referência, sessão/revogação, permission grants/denies, unit-access grants, lifecycle e autenticação. Pode guardar `PersonId`, `ProfessionalProfileId` futuro e `UnitId` como IDs opacos. Não cria FK cross-context, `DbContext` compartilhado nem escreve People, Staff ou Organization.

People continua owner de `Person`; Staff de `ProfessionalProfile`; Organization de `Unit`; Patients decide a policy do recurso paciente. Access fornece a identidade e a decisão de acesso atual; o owner do recurso continua validando estado e policy. Uma Unit é validada por contrato Organization purpose-specific no provisionamento e quando o recurso é usado; Access não consulta tabelas Organization nem presume que `UnitId` enviado pelo cliente seja autorizado.

## 6. UserAccount

`UserAccount` é a conta de autenticação, não a identidade civil nem um papel profissional. DB-001 já prevê `identity.user_account`, referência externa `person_id` e unicidade lógica de no máximo uma conta interna ativa por Person. Para o primeiro slice, o vínculo com `PersonId` é **opcional**, porque o bootstrap administrativo não pode depender de Staff nem de uma Person previamente criada; quando informado, é validado pelo contrato People e respeita aquela unicidade. `ProfessionalProfile` não é campo necessário neste slice; a futura ligação ocorre por referências independentes `Person → ProfessionalProfile` (Staff) e `Person → UserAccount` (Access).

Campos mínimos: `userAccountId`, `status`, `personId?`, `loginIdentifier` normalizado, `createdAt`, `activatedAt?`, `disabledAt?`, `credentialState`, `securityStamp`/`accessVersion`, `version` de concorrência e metadados de última autenticação/fracasso somente se a estratégia escolhida os exigir. Nunca retornar credential, hash, token ou identificador de recovery.

O lifecycle aprovado em IAM-OD-006 é `PENDING`, `ACTIVE`, `LOCKED` e `DISABLED`: criação, reativação e reset entram em `PENDING`; ativação válida leva a `ACTIVE`; a defesa pode levar a `LOCKED`; disable vence qualquer estado; reativação/unlock exigem novo fluxo controlado de ativação. A matriz completa está em `IAM_001_DECISIONS.md`; `DISABLED` nunca autentica ou autoriza e não há delete operacional.

## 7. Credential

IAM-OD-002 aprova credential local owned por Access via `PasswordHasher<TUser>` do ASP.NET Core Identity, sem adotar o framework inteiro como owner. Seu formato versionado e `SuccessRehashNeeded` sustentam rehash futuro sem algoritmo manual fixado aqui. Senha nunca é texto persistido/logado; rate limiting, lockout, reset, TLS, segredos externos e efeitos atuais de disable/revogação continuam obrigatórios. IdP externo não foi selecionado para o piloto.

## 8. Login identifier

IAM-OD-001 aprova username independente, próprio de Access, único após trim/normalização case-insensitive Unicode e sem derivação de e-mail, CPF ou PersonId. Mudança só ocorre por comando privilegiado/auditável; editar contato civil não o muda. Respostas externas não enumeram conta/identifier.

## 9. Authentication

Contrato conceitual de login: receber username e prova de credential; normalizar, aplicar limite de abuso antes/depois da verificação, localizar a conta, verificar status e credential, criar/rotacionar a sessão opaca aprovada e retornar apenas a representação pública da sessão. Conta ausente, identifier inválido, credential inválida, `PENDING`, `DISABLED` e `LOCKED` retornam resposta externa indistinguível, sem revelar a existência da conta. PII, senha, hash, token e header de autenticação não vão para logs.

API-001 não publica login/logout/current-session: somente lifecycle administrativo de `UserAccount` e `TerminateUserSessions`. A reconciliação de API-001 é obrigatória antes do código; nenhuma rota definitiva é inventada aqui.

## 10. Session/token

IAM-OD-003 aprova sessão opaca server-side para web, identificada por cookie `Secure`/`HttpOnly`/`SameSite` apropriado e proteção CSRF a definir no contrato web. O servidor guarda somente derivado do segredo, expiry, revogação e `accessVersion`; cada request resolve sessão, conta, grants, denies e Units atuais. JWT autoportante sem verificação atual de revogação é rejeitado. Expiração, logout, múltiplas sessões, revogação por sessão/conta e proteção contra fixation/theft são obrigatórios.

## 11. Logout/revocation

Logout revoga somente a sessão atual. Um administrador autorizado pode revogar sessão alvo ou terminar todas as sessões da conta; disable termina todas e impede novas autenticações. Compromise força reset de credential e termina sessões. Alterar permission, deny ou Unit grant incrementa o `accessVersion` ou revoga as sessões afetadas: o efeito precisa ser imediato na próxima request, não apenas no próximo login. O mesmo vale para explicit deny. Expiração é falha autenticada/reautenticação necessária, não uma permissão implícita.

## 12. Permission grants

AUTH-001 permanece canônico: deny-by-default; permission é ação de negócio; role é apenas agregação e não autoriza fora da policy. DB-001 já prevê `permission`, `permission_grant`, role assignments, scope, vigência, grantor/revoker e revogação.

O MVP administrativo exige, conforme os endpoints já implementados: `patients.profile.read`, `patients.profile.create`, `people.person.read`, `people.person.create`; `patients.guardian.manage` somente se o piloto incluir o endpoint de GuardianLink já implementado. Nenhuma permission clínica, financeira, developer ou superuser é concedida por persona. `UNIT_SCOPE` é requisito cumulativo e a policy do paciente continua no owner Patients.

## 13. Explicit deny

Persistir deny como grant de efeito `DENY`, com alvo `UserAccount`, permission (ou deny total somente se formalmente aprovado), `unitId?`/escopo, vigência, concedente, revokedAt/revokedBy e versão. A resolução é: conta/sessão válida → deny ativo aplicável? negar → grant ativo aplicável? continuar → Unit válida/concedida? continuar → policy do owner. Um deny aplicável sempre precede grant, role e claims. Claims de login podem carregar somente hints não autoritativos; a decisão atual consulta Access.

## 14. UNIT_SCOPE

`unit_access_grant` é a estrutura mínima explícita: `grantId`, `userAccountId`, `unitId` opaco, `effectiveFrom`, `effectiveTo?`, `revokedAt?`, `grantedBy`, `version`. Múltiplas Units são grants separados; sem grant vigente, nega. Unit inactive/inexistente não cria autorização: Organization responde via contrato que a Unit é utilizável para a operação; Patients também revalida a Unit do paciente. O request somente informa o contexto/recurso desejado e nunca amplia scopes. Grants globais ficam fora do slice.

## 15. Administrative personas

Os nomes abaixo são perfis operacionais, não autorização implícita.

| Perfil | Grants candidatos do primeiro fluxo | Unit scope | Proibido por padrão |
|---|---|---|---|
| `OWNER_MANAGER` | quatro permissions de Patients/People acima; `patients.guardian.manage` apenas se necessário | lista explícita, não inferida de propriedade | Clinical, financeiro, developer, superuser, grants próprios |
| `SECRETARY_RECEPTION` | `patients.profile.read/create` e `people.person.read/create`; guardian não é default | uma ou mais Units por grants explícitos separados | Clinical completo, financeiro, developer, superuser, grants próprios |

IAM-OD-005 fecha o padrão mínimo: nenhuma permissão global/CLINIC, exatamente as quatro permissions acima e uma ou mais Units explicitamente concedidas. Permission e Unit grant são independentes; explicit deny aplicável prevalece.

## 16. Developer/IT

Developer/IT é custodiante técnico: observabilidade sanitizada e operação explicitamente autorizada não concedem dados de negócio. Não há backdoor, conta admin hardcoded, bypass por ambiente de produção, impersonation nem "login as"; AUTH-001 rejeita impersonation no MVP. Qualquer suporte privilegiado é posterior, purpose-bound, auditável e fora deste slice.

## 17. Staff future integration

O primeiro MVP administrativo funciona antes de `ProfessionalProfile`. No futuro, People continua owner de Person, Staff cria/gerencia `ProfessionalProfile → PersonId`, e Access associa `UserAccount → PersonId?`; um contrato Staff purpose-specific pode validar elegibilidade profissional para políticas clínicas. Criar ProfessionalProfile não cria UserAccount, e desligamento Staff remove acesso operacional via fato/contrato, sem apagar autoria histórica.

## 18. Bootstrap

IAM-OD-004 aprova comando administrativo one-shot com segredo de deployment de uso único. Quando não há governante ativa, o comando consome atomicamente o marcador, cria a conta `PENDING` e artefato de ativação aleatório derivado/expirável; segunda execução falha. O segredo não é registrado e deve ser removido/rotacionado após uso. Não há seed, senha padrão, admin hardcoded, endpoint de bootstrap ou bypass permanente.

## 19. Recovery

IAM-OD-007 aprova reset administrativo controlado: governante autorizada invalida credential/sessões, muda a conta para `PENDING` e entrega por canal operacional aprovado um artefato de ativação aleatório, curto, de uso único e persistido apenas derivado. A pessoa define nova senha na ativação. Senha em texto é proibida; self-service por e-mail, SMS, MFA e recovery federada são pós-MVP.

## 20. Security controls

TLS é obrigatório fora de Development. Exigir proteção contra enumeração, rate limit e proteção de brute force; rotação de sessão após login/elevação; expiração e revogação; segredos fora do Git; logs redigidos; e proteção CSRF/cookie ou bearer conforme a estratégia decidida. Persistência indisponível falha fechada para login/autorização administrativa. Não transformar este slice em plataforma de segurança: MFA, step-up e suporte privilegiado seguem gated.

## 21. Test authentication boundary

`TestAuthenticationHandler` permanece em `tests/backend/Fisiofit.ApiTests`, registrado somente por `WebApplicationFactory.ConfigureTestServices`. O Host normal não referencia o assembly de testes, não registra o scheme, não reconhece `X-Test-*`, nem possui fallback baseado em environment/configuração comum. Architecture tests futuros devem falhar se o handler/header aparecer em produção, se `AddAuthentication` do Host apontar para o scheme de teste ou se Access aceitar principal forjado.

## 22. Audit boundary

AUD-001 é owner de `AuditRecord`; Access não cria um substituto local. IAM deve emitir/fornecer fatos mínimos consumíveis: login sucesso/falha sanitizado, logout, bootstrap, activate/disable/lock, credential reset, sessão revogada, permission/unit grant ou deny criado/revogado e alteração de lifecycle. Campos mínimos: actor ou tentativa anônima, target opaco, action, result/code, timestamp, correlation e Unit/escopo; nunca segredo/token/senha ou payload PII. IAM pode ser implementável localmente após decisões, mas não é liberável para uso assistido enquanto AUD-001 não definir persistência/atomicidade e for implementado.

## 23. Persistence model

No schema owner `identity`, preservar as tabelas canônicas DB-001 e reconciliá-las com as decisões: `user_account`, `credential`, `session`, artefato de activation/recovery, `permission`, `permission_grant`, `unit_access_grant`, marker de bootstrap e `command_receipt`. `permission_grant` deve representar ALLOW/DENY, permission, scope/Unit opaca, vigência, concedente/revogador e concurrency. Os nomes/campos físicos finais pertencem a IAM-001-CONTRACTS, sem alterar a semântica aprovada.

Índices mínimos futuros: login identifier normalizado único; uma conta interna ativa por `person_id` quando não nulo; grants ativos por account/permission/Unit/vigência; sessões válidas por account/expiry/revocation; e constraint/idempotency para bootstrap. FKs somente internas ao schema Identity; referências `person_id`/`unit_id` são sem FK. Versionamento otimista protege UserAccount e grants; sessões/revogação devem ter atualização atômica.

## 24. Public contracts

Access publica contratos mínimos, sem `DbContext`, EF entity, repository ou `IQueryable`: (1) current actor/access decision para Application, com `UserAccountId`, authentication/session validity e método purpose-specific para permission + Unit; (2) validação People ao vincular `PersonId`; (3) validação Organization de Unit utilizável, chamada pelo caso de uso que precisa dela; (4) futuro Staff fornece elegibilidade profissional sem expor seus internals; (5) factos mínimos para Audit. Não há serviço genérico de leitura cross-context.

Patients deve receber um actor/decision atual e continuar a validar seu recurso/Unit. Access não escreve PatientProfile nem decide regra de paciente.

## 25. HTTP contracts

API-001 já prevê `CreateUserAccount`, disable, role/permission grants, revogação e busca de contas, mas não login, logout, current-session, revoke-session individual, activate/reset credential nem `UNIT_SCOPE` como contrato explícito. Antes de IAM-001-IMP, reconciliar API-001 para publicar apenas o conjunto mínimo escolhido: login anônimo, logout autenticado, current session, criação/bootstrap de conta, disable, grants/revokes e Unit grant/revoke; `TerminateUserSessions` pode cobrir revogação por conta. Rotas, payloads, permissions e respostas somente entram após as decisões abertas; não foram alterados nesta tarefa.

## 26. Frontend contract

Após login, frontend precisa de `userAccountId`, display name seguro quando existir, estado da sessão, permissions efetivas e Units acessíveis, mais expiry/metadado necessário para renovar ou encerrar. Não recebe hashes, denies internos, grants administrativos ou token em payload indevido. `401` leva ao login/reautenticação; `403` mostra acesso negado sem enumerar grants; sessão expirada pede nova autenticação; conta disabled limpa estado e bloqueia o shell; permission/Unit revogada remove a ação e a próxima chamada falha fechada.

## 27. Concurrency

Use version/ETag nos comandos administrativos de account e grants. Duas alterações concorrentes de grant falham com precondição/conflito e exigem releitura; não aplicar last-write-wins. Disable, credential compromise e terminate-sessions serializam com criação/validação de sessão e incrementam `accessVersion`. Request em voo revalida antes de autorizar a operação: uma revogação vencida vence o próximo request, sem promessa impossível de cancelar trabalho já commitado. Bootstrap tem unique/idempotency de consumo único e falha determinística na segunda execução.

## 28. Failure handling

Login retorna problema genérico equivalente para identifier/credential inválido, inexistência, disabled e locked; rate limit pode retornar resposta genérica/retry conforme decisão. Endpoint autenticado sem sessão válida retorna `401`; autenticado sem permission, Unit ou com deny retorna `403`; sessão revogada/expirada retorna `401`; persistência/validação owner indisponível retorna `503` fail-closed. APIs usam RFC 9457/Problem Details, `code` estável e `traceId`, sem segredo ou detalhe de enumeração.

## 29. Implementation slice

**Candidato: IAM-001-IMP — Local Administrative Access Foundation.** Só pode iniciar após IAM-001-CONTRACTS e AUD-001-DESIGN.

**In scope:** Access schema/DbContext owner-local; uma estratégia de credential/session aprovada; login/logout/current-session; lifecycle; grants/denies/Unit; bootstrap/recovery mínimo; adaptação do actor de Patients para decisão atual; contratos/API reconciliados; migrations e testes. **Out:** MFA, Staff, UI, roles como fonte implícita, Clinical/Finance, provider não decidido, impersonation e AuditRecord.

**PASS:** login de conta ACTIVE; request de paciente só passa com permission + Unit vigente e sem deny; logout/disable/revoke/grant change têm efeito no próximo request; test auth continua isolado; PostgreSQL confirma constraints/concurrency; Audit boundary está integrado conforme AUD-001 aprovado. A implementação não declara uso assistido pronto sem AUD-001-IMP e WEB-001.

## 30. Test plan

- **Unit/Application:** credential e lifecycle; permission resolution; deny precedence; Unit/vigência; no self-escalation; reset/revoke; actor current-state.
- **PostgreSQL:** migrations Identity; unicidade de identifier/Person; grant/deny temporal; concurrency de grants; disable versus sessão; bootstrap único; revogação persistida.
- **API:** login válido/inválido sem enumeração; logout; expiry/revoke; disabled; permission/Unit/deny em Patients; Problem Details; rate/lock behavior decidido.
- **Architecture:** test auth ausente do Host normal; sem headers mágicos; Access não referencia persistence People/Organization/Staff; sem FK cross-context; ModuleContracts sem EF entities.
- **Security:** nenhum segredo/hash/token em resposta, evento ou log; cookie/header e CSRF conforme a decisão; TLS/configuração/secret validation; session fixation/theft boundaries.

## 31. Closed decisions

`docs/decisions/IAM_001_DECISIONS.md` fecha IAM-OD-001..007 como `APPROVED`:
username independente; credential local via `PasswordHasher<TUser>` do ASP.NET
Core Identity; sessão opaca server-side para web; bootstrap one-shot com segredo
de deployment; grants administrativos mínimos da Secretária por Unit explícita;
lifecycle `PENDING/ACTIVE/LOCKED/DISABLED`; e reset administrativo controlado.
O documento registra alternativas, critérios de segurança/operação/revogação e
o impacto preciso. Ele não altera API-001, DB-001, AUTH-001 ou STATE-001.

## 32. Gates

Design: PASS porque ownership, lifecycle, auth boundary, credential/session/bootstrap, grants/deny/Unit, test boundary, Audit boundary, persistence, contracts, security, slice e testes estão explícitos. Implementação: NEEDS_CONTRACT_RECONCILIATION por API-001/DB-001/AUTH-001/STATE-001; AUD-001-DESIGN continua pré-requisito do uso assistido. Uso assistido: BLOCKED adicionalmente por IAM-001-IMP, AUD-001-IMP, WEB-001, HTTPS/operação, migrations, backup/restore e validação de piloto. Produção pública/regulatória não é declarada.

## 33. Next action

Executar exclusivamente `IAM-001-CONTRACTS` para reconciliar API-001, DB-001, AUTH-001 e STATE-001 com as decisões aprovadas. Não iniciar código, migration, frontend ou AUD-001 nesta tarefa.
