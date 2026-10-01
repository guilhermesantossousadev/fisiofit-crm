# IAM-001-DECISIONS — Fechamento de Identity & Access

**Status:** APPROVED — decisões de especificação; nenhuma implementação, migration ou contrato HTTP é alterado por este documento.
**Data:** 2026-10-01
**Fontes verificadas:** `PROJECT_OS.md`, ROADMAP-MVP-001, IAM-001-DESIGN, AUTH-001, API-001, DB-001, STATE-001, RULES_INDEX, MODEL-001, People, IMP-000, GATE_M1_AUDIT e ADR-005.

## 1. Decisões e limites

Estas decisões concretizam somente o IAM mínimo do M1: login web administrativo,
provisionamento assistido e Patients adulto/self-payer em Units explicitamente
concedidas. Access continua owner de conta, credential, sessão e grants; `Person`
e contatos civis continuam em People; `Unit` continua em Organization. Referências
cross-context permanecem opacas, sem FK. `TestAuthenticationHandler` continua
exclusivo de API tests e não é um mecanismo de produção.

| Decision | Status | Selected option | Reason | Implementation impact | Canonical docs affected |
|---|---|---|---|---|---|
| IAM-OD-001 | APPROVED | username independente, normalizado | desacopla login de contato civil e do recovery | unique index e comando de alteração privilegiado | IAM-001-DESIGN, DB-001, API-001 |
| IAM-OD-002 | APPROVED | credential local com `PasswordHasher<TUser>` do ASP.NET Core Identity | mecanismo maduro com formato/versionamento e rehash; IdP não é necessário ao piloto | credential local, rate limit, lockout e reset | IAM-001-DESIGN, DB-001, API-001 |
| IAM-OD-003 | APPROVED | sessão opaca server-side em cookie HttpOnly | logout e revogação/grants atuais têm efeito na próxima request | tabela de sessão, autenticação por lookup e CSRF | IAM-001-DESIGN, DB-001, API-001, AUTH-001 |
| IAM-OD-004 | APPROVED | comando one-shot, protegido por segredo de deployment de uso único | cria a primeira governante sem seed, senha padrão ou backdoor | estado de consumo, artefato de ativação e audit | IAM-001-DESIGN, DB-001, API-001, AUTH-001 |
| IAM-OD-005 | APPROVED | Secretária recebe quatro permissions administrativas mínimas, por Unit explícita | least privilege para o primeiro fluxo; roles não autorizam por si | grants de permission e Unit separados | IAM-001-DESIGN, DB-001, API-001, AUTH-001 |
| IAM-OD-006 | APPROVED | `PENDING`, `ACTIVE`, `LOCKED`, `DISABLED` | cada estado tem finalidade operacional de ativação, defesa ou retirada de acesso | lifecycle, transitions e API administrativa | IAM-001-DESIGN, DB-001, API-001, STATE-001 |
| IAM-OD-007 | APPROVED | reset administrativo controlado, sem self-service no piloto | é suficiente ao uso assistido sem tratar e-mail como canal confiável | activation/reset token de uso único e revogação total | IAM-001-DESIGN, DB-001, API-001, AUTH-001 |

## 2. IAM-OD-001 — Login identifier

**Problema.** `Person.Email` é contato civil de People, pode mudar e não é um
identificador de autenticação aprovado. O login precisa ser único, recuperável no
piloto e não deve revelar se uma Person existe.

| Alternativa | Segurança e revogação | Operação/MVP e futuro |
|---|---|---|
| E-mail como login | mistura ou duplica dado civil; mudanças/reuso exigem governança | familiar, mas recovery e alteração de contato ficam acoplados |
| Username independente | mantém Access separado, não exige e-mail e não muda por edição civil | exige entrega assistida do username; permanece compatível com e-mail/IdP futuros |
| Identificador de Person/CPF | expõe PII e cria acoplamento de domínio | inadequado como credential/login |

**Decisão.** `loginIdentifier` será um username próprio de Access, obrigatório,
único após `trim` e normalização case-insensitive Unicode (forma canônica e
comparação ordinal). O valor não é CPF, e-mail nem ID de Person. Mudança só ocorre
por comando administrativo privilegiado, com confirmação/audit; editar e-mail ou
outro contato civil não o altera. Login e recovery retornam resposta externa
indistinguível para username ausente, inválido, credencial inválida ou conta sem
uso permitido. A UI de login poderá chamá-lo de “usuário”.

**Aprovação agora.** Sim: atende a separação Person/UserAccount de DB-001 e
RB-PPL-003, não pressupõe contato civil e é suficiente para o piloto assistido.

## 3. IAM-OD-002 — Credential/provider

**Problema.** O MVP precisa de autenticação real, sem provider externo já
contratado/configurado e sem tornar senha um dado de negócio ou de log.

| Alternativa | Segurança/revogação | Simplicidade/operação/futuro |
|---|---|---|
| Credential local | Access controla reset, lockout e disable; requer defesa de senha | adequada ao piloto; pode coexistir com provider futuro |
| IdP externo | boa centralização se já operado, mas adiciona disponibilidade, configuração e sincronização de disable | não há IdP canônico ou necessidade demonstrada no MVP |
| senha própria com algoritmo fixado pela aplicação | alto risco de parâmetros/upgrade inadequados | rejeitada em favor de componente maduro |

**Decisão.** Usar credential local no owner Access e o
`PasswordHasher<TUser>` do ASP.NET Core Identity, sem adotar o framework inteiro
ASP.NET Identity nem alterar o owner `identity`. O componente usa formato
versionado, salt, derivação configurada pelo framework e o resultado
`SuccessRehashNeeded`, permitindo upgrade transparente após verificação bem
sucedida. Logo, este documento não fixa manualmente algoritmo, iterações ou
formato de hash: a versão instalada e sua configuração aprovada no momento da
implementação serão a fonte técnica, com teste de rehash.

Senha nunca é persistida ou logada em texto. O implementation slice deverá
aplicar TLS, limites por origem e identifier, slowdown/lockout temporário,
comparação delegada ao componente, rotação de sessão no login, reset por
artefato de uso único e falha fechada se Access indisponível. `LOCKED` é a resposta
de proteção a tentativas repetidas; não é um substituto de rate limiting.

**Aprovação agora.** Sim: a dependência externa não foi justificada e o componente
maduro do ecossistema .NET fornece evolução verificável sem inventar criptografia.

## 4. IAM-OD-003 — Session/revocation

**Problema.** AUTH-001 exige logout real, disable e alterações críticas de grants
na próxima request; claims stale não podem preservar acesso.

| Alternativa | Segurança/revogação | Operação/MVP e futuro |
|---|---|---|
| Sessão opaca server-side | lookup atual permite revoke/logout/disable imediato na próxima request | simples para web; pode emitir outros canais depois |
| Access/refresh tokens | requer rotação, storage e revocation de refresh/access para o mesmo requisito | maior complexidade sem caso mobile atual |
| JWT autoportante | não prova revogação atual até expirar | rejeitado: incompatível com AUTH-DENY-018 |

**Decisão.** O browser usará identificador opaco de sessão em cookie `Secure`,
`HttpOnly` e `SameSite` adequado, com proteção CSRF definida no contrato web. O
servidor persiste somente o derivado do segredo de sessão, associa conta,
`accessVersion` no momento da emissão, expiração, revogação e metadados mínimos.
Em toda request autenticada, Access encontra a sessão não revogada, exige conta
`ACTIVE`, compara `accessVersion` e resolve permission, deny e Unit grants atuais;
claims são somente hints não autoritativos. Logout revoga a sessão corrente;
revoke/disable/reset invalidam todas as sessões da conta; mudança de permission,
deny ou Unit grant incrementa `accessVersion` e afeta a próxima request.

**Aprovação agora.** Sim: é a única opção comparada que satisfaz todos os efeitos
rápidos obrigatórios sem antecipar mobile ou aceitar JWT stale.

## 5. IAM-OD-004 — Administrative bootstrap

**Problema.** A primeira conta governante não pode depender de uma conta prévia,
seed com senha, administrador hardcoded ou porta de entrada reutilizável.

| Alternativa | Segurança/revogação | Operação/MVP e futuro |
|---|---|---|
| comando one-shot + segredo de deployment | audita consumo e pode ser definitivamente desabilitado | verificável e apto a ambiente assistido |
| convite inicial sem bootstrap | ainda requer um emissor inicial confiável | desloca, mas não resolve, a raiz de confiança |
| seed/senha padrão/backdoor | segredo previsível ou persistente | rejeitado |

**Decisão.** Um comando administrativo one-shot poderá criar somente a primeira
conta `OWNER_MANAGER`, quando não existir conta governante ativa. Ele exige segredo
de bootstrap fornecido exclusivamente pelo deployment, compara-o sem registrá-lo,
e consome atomicamente o marcador de bootstrap. A segunda execução falha de modo
determinístico, inclusive sob concorrência. O comando cria conta `PENDING` e um
artefato de ativação aleatório de uso único, com expiração curta, cuja representação
persistida é somente derivada. O artefato é entregue ao operador pelo canal
operacional aprovado; a pessoa define a própria senha na ativação. Depois do uso,
o segredo é removido/rotacionado no deployment e o comando não volta a ser uma
rota HTTP ou bypass permanente. AUD-001 deve registrar bootstrap/resultado, sem
segredo ou artefato.

**Aprovação agora.** Sim: atende bootstrap seguro e verificável, mantendo convite
para provisionamento posterior por governante autenticada.

## 6. IAM-OD-005 — Default grants / Unit scope da Secretária

**Problema.** AUTH-001 deixa a abrangência padrão `UNIT`/`MULTI_UNIT` da
Secretária aberta; o primeiro fluxo só precisa de Patients adulto/self-payer.

| Alternativa | Segurança/revogação | Operação/MVP e futuro |
|---|---|---|
| acesso global/CLINIC | excede least privilege e amplia impacto de erro | rejeitado |
| uma Unit implícita pelo role | dificulta remoção e contradiz grants explícitos | rejeitado |
| grants independentes por Unit | revogação e vigência precisas; deny prevalece | suporta uma ou várias Units sem grant global |

**Decisão.** A Secretária não recebe acesso global. No provisionamento do M1, o
perfil operacional `SECRETARY_RECEPTION` recebe somente
`patients.profile.read`, `patients.profile.create`, `people.person.read` e
`people.person.create`, cada uma sujeita à Unit acessada. `patients.guardian.manage`
não é default. Ela pode ter uma ou várias Units, cada uma em
`unit_access_grant` separado, vigente e revogável. A governante com permission
Identity explícita concede/remove permissions, denies e Units de outra conta;
Secretária não governa acesso nem a própria conta. Permission grants e Unit grants
são independentes e cumulativos; ausência de qualquer um nega. Explicit deny
aplicável precede allow, role e quaisquer hints de sessão.

**Aprovação agora.** Sim: entrega o fluxo administrativo mínimo e preserva a
regra AUTH-001 de que role não é autorização implícita.

## 7. IAM-OD-006 — Account lifecycle

**Problema.** STATE-001 deixou `UserAccount` deferred, mas ativação, proteção e
disable exigem estados distintos e transições verificáveis.

**Decisão.** Estados mínimos: `PENDING` (criada ou em reset, sem credential ativa),
`ACTIVE` (pode autenticar), `LOCKED` (defesa temporária/controle de segurança;
não autentica) e `DISABLED` (retirada administrativa de acesso; não autentica).
Transições: criação/bootstrap/provisionamento → `PENDING`; ativação válida
`PENDING → ACTIVE`; falhas conforme política `ACTIVE → LOCKED`; unlock/reset
controlado `LOCKED → PENDING`; disable de qualquer estado não terminal →
`DISABLED`, com revogação total; reativação administrativa `DISABLED → PENDING`,
nunca diretamente para `ACTIVE`; nova ativação volta a `ACTIVE`. Não há delete
operacional nem estado `DELETED`. Reset de conta ACTIVE faz `ACTIVE → PENDING` e
revoga credencial/sessões anteriores.

**Alternativas rejeitadas.** Só `ACTIVE/DISABLED` não expressa ativação/reset ou
defesa; estados adicionais como suspended/archived não possuem finalidade M1.
Os estados são mínimos, auditáveis e compatíveis com deny-by-default.

**Aprovação agora.** Sim: define comportamento operacional sem criar lifecycle
artificial; STATE-001 deverá ser reconciliado na tarefa de contratos.

## 8. IAM-OD-007 — Recovery mínimo

**Problema.** O piloto precisa recuperar acesso sem e-mail self-service,
transmitir senha existente ou deixar sessões comprometidas vivas.

**Decisão.** Reset administrativo controlado é suficiente ao piloto. Uma
governante autorizada inicia reset da conta alvo; a operação invalida credential e
todas as sessões, muda a conta para `PENDING` e cria artefato de ativação aleatório
de uso único, curto e persistido somente em forma derivada. A pessoa define uma
nova senha no endpoint de ativação; sucesso consome o artefato e ativa a conta.
Canal de entrega é procedimento assistido aprovado, nunca resposta HTTP/log.
Reset, tentativa e resultado são evidência de segurança para AUD-001. Self-service
por e-mail, SMS, MFA e recuperação federada ficam explicitamente pós-MVP.

**Alternativas rejeitadas.** Enviar senha/revelar senha é proibido; e-mail
self-service não é necessário nem tem canal/ownership operacional aprovados;
preservar sessões após reset contraria o requisito de revogação.

**Aprovação agora.** Sim: é proporcional ao piloto e mantém o caminho de evolução
para recovery self-service sem assumir contato civil como credential.

## 9. Reconciliação obrigatória antes de IAM-001-IMP

> Registro histórico dos requisitos de 2026-10-01, agora atendidos por
> IAM-001-CONTRACTS em API-001 §24.1, DB-001 §14.1, AUTH-001 §§47/49 e
> STATE-001 (UserAccount/Sessão M1). IAM-OD-001..007 não foram reabertas.
> Estado atual: contratos DONE / PASS; IAM-001-IMP BLOCKED pelos gates
> residuais de IAM-001-DESIGN §29; uso assistido/produção BLOCKED.
> Próxima tarefa única: AUD-001-DESIGN. A redação abaixo preserva o requisito
> original, não uma pendência atual de reconciliação.

Esta tarefa não modifica contratos HTTP ou modelo lógico. `IAM-001-CONTRACTS` deve
reconciliar, em conjunto, os seguintes pontos:

- **API-001:** login anônimo; ativação; logout da sessão atual; current
  session/current actor; criação administrativa de conta `PENDING`; disable e
  reativação; reset administrativo; grant/revoke de permission e explicit deny;
  grant/revoke de Unit; revoke de sessão individual e término total de sessões;
  remover a ambiguidade de `CMD-001..005`/`CMD-119`, seus payloads, permissions,
  idempotência, ETag, `401/403/429` e Problem Details.
- **DB-001:** completar `identity.user_account` com username normalizado,
  lifecycle/access version e metadados mínimos; definir `credential`, `session`,
  activation/recovery artifact e consumo único de bootstrap; decidir
  `unit_access_grant` explícita e `permission_grant.effect` (`ALLOW`/`DENY`),
  todos apenas com FKs internas ao schema Identity. `person_id` e `unit_id`
  continuam referências externas sem FK.
- **AUTH-001 e STATE-001:** substituir AUTH-GAP-003/006 para este M1, formalizar
  matriz mínima da Secretária, sessão atual e lifecycle; manter roles como bundles
  não autoritativos e explicit deny prevalente.

Portanto, **IAM-001-IMP readiness: NEEDS_CONTRACT_RECONCILIATION**. Uso
assistido continua **BLOCKED** por IAM-001-IMP, AUD-001-DESIGN/IMP, WEB-001 e
gates operacionais. A única próxima tarefa é `IAM-001-CONTRACTS`.
