# AUD-001-DESIGN — Audit durável mínimo para uso administrativo assistido

**Status:** PASS (design documental; nenhuma implementação, migration ou endpoint)  
**Data:** 2026-10-01  
**Owner:** Privacy & Audit (AuditDbContext / schema audit)  
**Próximo slice:** AUD-001A — Durable Administrative Audit Foundation

## 1. Decisão, ownership e logging

O mecanismo mínimo é uma evidência append-oriented persistida em PostgreSQL pelo contexto Privacy & Audit e recebida por contrato de aplicação semanticamente limitado. Ela prova ator técnico, instante, ação, alvo, contexto e resultado; não é log técnico, event bus, DTO copiado ou snapshot do recurso.

Privacy & Audit é o único owner de AuditRecord, persistência de evidência, retention metadata quando aplicável e consulta futura. Identity & Access, People, Patients e Organization podem produzir fatos mínimos, mas não possuem AuditRecord, não acessam AuditDbContext/EF/tabela Audit e não participam de FK cross-context, DbContext compartilhado ou transação distribuída.

| Technical log (ILogger, trace, métricas) | Durable audit evidence |
|---|---|
| diagnóstico, trace, erro técnico e métrica; pode expirar/amostrar | evidência persistida, consultável e append-only de ação relevante |
| pode conter stack trace redigido | não contém stack trace, body, headers ou log bruto |
| correlation liga investigação técnica | correlation liga Audit ao log sem copiá-lo |

ILogger, Activity, correlation/trace existentes e o TestAuthenticationHandler não fecham o gate Audit.

## 2. AuditRecord e catálogo

| Campo | Regra |
|---|---|
| auditRecordId / evidenceId | UUID; evidenceId único para entrega idempotente |
| occurredAt / recordedAt | instantes UTC da ação e da persistência; ambos necessários para atraso/retry |
| actorKind, actorUserAccountId? | USER_ACCOUNT, SYSTEM, BOOTSTRAP, BACKGROUND_PROCESS; conta é a identidade humana primária |
| action, sourceContext, schemaVersion | catálogo estável, contexto canônico, versão inicial 1 |
| resourceType, resourceId, unitId? | IDs opacos sem FK; Unit só em ação Unit-scoped |
| result, reasonCode? | SUCCESS, DENIED, FAILED; motivo por código permitido |
| correlationId, traceId? | correlation quando disponível; trace opcional, nunca requisito de negócio |
| metadata | JSON pequeno, versionado, allowlisted por action e redigido |

Não há actorPersonId inicial: UserAccountId é técnico e estável; Person é vínculo civil opcional de People. Test actor jamais representa ator produtivo. Sem trace distribuído, correlation/request ID gerado no Host é suficiente; ambos apenas relacionam logs.

Actions são minúsculas, com ponto e sem mensagem livre (identity.account.disabled); são aditivas, nunca reutilizadas. resourceType/resourceId apontam UserAccount, Session, PermissionGrant, UnitAccessGrant, PatientProfile, GuardianLink ou Person, sem snapshot.

| Fato IAM | Classificação | Action / metadata permitida |
|---|---|---|
| bootstrap consumido; conta criada; ativada/bloqueada/desabilitada/reativada | AUDIT REQUIRED | identity.bootstrap.consumed, identity.account.*; somente transição/código |
| login bem-sucedido | AUDIT REQUIRED | identity.login.succeeded; sessionId opaco opcional |
| login falho | SECURITY TELEMETRY ONLY | rate-limit/contador redigido; não Audit M1 |
| logout | OPTIONAL | identity.session.logged_out, se política futura exigir |
| revoke sessão / terminar todas | AUDIT REQUIRED | identity.session.revoked; sessionScope=single|all |
| credential reset iniciado/concluído | AUDIT REQUIRED | identity.credential.reset_initiated/reset_completed |
| permission allow concede/revoga; deny cria/remove | AUDIT REQUIRED | identity.permission.granted/revoked, identity.deny.created/removed; permissionCode, efeito |
| Unit access concede/revoga | AUDIT REQUIRED | identity.unit_access.granted/revoked; scope |

DENIED é recusa por policy/guard; FAILED é falha sanitizada após intenção aceita. Para tentativas IAM sensíveis, negar também pode ser evidência quando operacionalmente relevante; login continua separado para evitar enumeração. Action desconhecida, actor inválido e metadata inválida são rejeitados.

Metadata somente pode carregar permissionCode, previousStatus, newStatus, grantScopeType, sessionScope, reasonCode, versões e IDs opacos previstos. Serializar DTO/request automaticamente é proibido.

## 3. Patients, PII e privacy

| Fluxo | Classificação | Decisão |
|---|---|---|
| Person + PatientProfile criada (IMP-001) | AUDIT REQUIRED | futura patients.profile.created, alvo PatientProfile, personCreated/registrationKind=SELF |
| busca/lista (IMP-002) | consulta operacional comum | telemetria técnica agregada; sem AuditRecord por GET |
| detalhe (IMP-002) | OPTIONAL no corte | classificar antes do piloto; não permite bypass de Clinical |
| GuardianLink cria/encerra (IMP-003A) | AUDIT REQUIRED antes de assistido desse fluxo | patients.guardian.created/ended |
| alterações/inativação futuras | AUDIT REQUIRED | cada design define action/motivo/outcome |

Não se modifica Patients/People agora. O primeiro uso assistido exige AUD-001B para evidência de criação de PatientProfile; busca/lista não recebem escrita individual.

Audit nunca grava automaticamente CPF, nome, e-mail, telefone, endereço, nascimento, senha/hash, cookie, Authorization, token/session secret, activation/reset/bootstrap secret, payload bruto ou stack trace. IDs internos e códigos são preferidos. Produtores redigem e Audit valida/rejeita campos/chaves proibidos.

## 4. Durabilidade, consistência e falhas

Durável é commit confirmado em PostgreSQL no schema audit, append-only lógico, backup/restore de plataforma e consulta indexada. Não há update/delete operacional; erro é corrigido por registro compensatório com correctsEvidenceId.

Para IAM AUDIT REQUIRED, a escrita é síncrona purpose-specific após o resultado IAM ser conhecido e antes de concluir a resposta. É FAIL CLOSED para a conclusão externa: sem confirmação da evidência, não se declara sucesso. Não há 2PC; se o commit IAM já é incerto, a resposta é sanitizada/unknown e reconciliação por operationId é obrigatória — nunca alegar rollback. Logout OPTIONAL é BEST EFFORT; telemetria de login falho não bloqueia autenticação.

Não se adota Outbox automaticamente. AUD-001A cobre IAM; AUD-001B decidirá entrega/reconciliação de Patients sem transação distribuída. Audit evidence não é domain event/integration event; AuditLogCreated permanece rejeitado.

Timeout, Audit DB indisponível e erro de transporte são outcome desconhecido, sem retry cego da mutação. Reentrega usa o mesmo evidenceId e retorna recorded/alreadyRecorded; correlation não é idempotency key. Não há locking/ordem global; a ordenação relevante usa occurredAt e causalidade local.

## 5. Storage, consulta, authorization e security

Uma tabela audit.audit_record é suficiente. Índices: evidence_id único; ator+tempo; action+tempo; resourceType/resourceId+tempo; Unit+tempo parcial; correlation; occurredAt. Não há FK externa.

Não há dashboard/API de query no AUD-001A. AUD-001C pode definir a menor query filtrada por período/action/ator/recurso/Unit, paginação e privacy.audit.view; conteúdo sensível exige privacy.audit.view_sensitive, purpose e auditoria da leitura. Secretária, Developer/IT e Owner/Manager não recebem acesso por papel implícito.

Retention duration, legal hold e expurgo são OPEN / BLOCKING BEFORE GO-LIVE; não se inventa prazo. A foundation só exige metadata apta a futura policy. Segurança: TLS, DB least privilege, logs redigidos, migrations do owner, backup/restore e proteção contra tampering; sem criptografia customizada.

Step-up/delegação não são desenhados aqui. Quando existirem, mudanças de privilege registram somente resultado/código de policy, nunca segredo ou método; ações que AUTH-001 marca obrigatórias continuam bloqueadas até mecanismo concreto.

## 6. Contrato, slice e testes

AUD-001A exporá em ModuleContracts/PrivacyAudit uma escrita purpose-specific, por exemplo RecordAdministrativeEvidence: request fechado com os campos acima, action catalogada e metadata tipada/allowlisted; retorno verificável recorded/alreadyRecorded + ID. O nome final segue a convenção do contrato. Não expõe AuditDbContext, EF entity ou objeto arbitrário. Identity chama após decidir/aplicar o resultado local e antes de finalizar resposta; não escreve Audit diretamente.

**AUD-001A in scope:** schema/DbContext e migration owner-local; AuditRecord; catálogo, validação/redaction; contrato público; persistência append-only; idempotência/índices; integração IAM requerida; testes.

**Out of scope:** integração Patients/People/Organization; query/dashboard; retenção legal; outbox; Clinical/Finance; step-up/delegação; logs como Audit.

Testes futuros: unit/application (catálogo, actor, result, metadata/redaction); PostgreSQL (append, unique, índices, sem FK cross-context, imutabilidade); integração (IAM obrigatório, Audit indisponível, retry/unknown outcome); arquitetura (sem EF/DbContext Audit em produtores); segurança (sem secrets/PII, sem acesso implícito Developer).

| Classificação | Estado |
|---|---|
| AUD-001-DESIGN | PASS |
| AUD-001A implementation readiness | READY |
| IAM-001A implementation readiness | READY AFTER AUD-001A |
| Assisted-use readiness | BLOCKED — AUD-001A, IAM aplicável, AUD-001B Patients, WEB-001 e gates operacionais |
| Production readiness | BLOCKED — retenção/legal hold, query/segregação sensível, backup/restore e demais gates |

Open decisions não bloqueantes à foundation: duração/hold; query de piloto; classificação final de detalhe Patients; delivery de AUD-001B; política operacional de telemetria de login falho.

