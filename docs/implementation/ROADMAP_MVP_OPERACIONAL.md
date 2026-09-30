# ROADMAP-MVP-001 — Roadmap para MVP operacional

## 1. Contexto

Esta revisão parte do fechamento de `IMP-003A — DONE / PASS` e substitui a
ordenação puramente horizontal por cortes verticais demonstráveis. Não altera
owners, contratos ou decisões canônicas. Código, migrations e testes existentes
continuam sendo a evidência do que existe; design aprovado não é funcionalidade
disponível.

O objetivo não é declarar produção pública. É preparar uso interno assistido por
proprietária e secretária, com dados administrativos e operação de Pilates
deliberadamente limitada e rastreável.

## 2. Estado atual

### Evidência implementada e testada

- **Organization (owner: Registry/Organization):** `Clinic` e `Unit`, migration
  própria e validação PostgreSQL; Unit ativa é validada no cadastro/leitura de
  paciente. Não há administração de Unit, sala, calendário ou feriados.
- **People/Patients (owners: Registry/People e Registry/Patients):** persistência
  separada, `Person`, contatos do cadastro inicial, `PatientProfile` e receipts.
  Há POST/GET de cadastro de adulto `SELF`, GET de busca por nome/CPF/telefone e
  GET de detalhe, com paginação, PII minimizada, `no-store`, idempotência somente
  no cadastro e `UNIT_SCOPE` aplicado pelo handler.
- **Guardian Links (owner: Registry/Patients):** criar/listar/encerrar vínculo
  temporal com `Person` existente/CURRENT; versionamento/ETag, `If-Match`, lock
  local, histórico, autorização e testes de concorrência/retry PostgreSQL.
- **Testes já validados no fechamento de IMP-003A:** restore e build sem warnings;
  39 unitários, 43 integração PostgreSQL, 22 API e 14 arquitetura (118/118).
  Esta tarefa não os reexecuta, pois não altera código.

### Parcial ou somente infraestrutura de testes

- O host registra `AddAuthentication`, `AddAuthorization` e extrai claims
  (`NameIdentifier`, `permission`, `unit_id`, `account_status`, `explicit_deny`),
  mas não possui scheme/provedor, conta, credencial, sessão nem endpoint real.
  A API é exercida por `TestAuthenticationHandler` e headers `X-Test-*` nos testes.
- A lógica de Patients já impõe conta ativa, permission, explicit deny e Unit no
  request; isso prova o comportamento do slice, não IAM de produção.
- `AccessModule` e `AuditModule` são shells; Audit não persiste evidência.
- Os demais módulos (CRM, Operations/Scheduling/Pilates, Clinical, Revenue,
  Communication, Documents e Reports) são markers/DI/rotas vazias, sem domínio,
  persistência, endpoints ou testes funcionais.
- O frontend React/Vite contém somente `App` estático de fundação e `apiBaseUrl`.
  Não há router, estado, cliente API, sessão, shell administrativo ou tela de negócio.

### Modelado/documentado, não implementado

Organization ampliada, Staff, Scheduling, Pilates, Clinical, Plans & Enrollment,
Billing, Finance, CRM, Communication, Documents, Privacy & Audit e Reports têm
owners, modelos, processos, estados e/ou contratos canônicos. Nenhum deles deve
ser comunicado como pronto para uso. People/Patients também não estão completos:
edição, endereço, inativação, deduplicação operacional, criação/resolução de
Pessoa para relação, menores e pagador diferente continuam fora do código.

### Gated/blocked

IAM físico e Audit durável bloqueiam ativação externa/uso real. Menor depende de
design atômico de GuardianLink; payer diferente depende de lifecycle contratual;
responsável administrativo de `OQ-M001-005`; contato de emergência de
`OQ-M001-001`; retroatividade/correção histórica e retenção definitiva de receipts
seguem gated. MFA/step-up, RT e definições jurídicas/regulatórias associadas não
são pré-requisito do corte administrativo inicial, salvo operação que os requeira.

## 3. Objetivo e definição do MVP operacional

O menor corte coerente é **uso administrativo assistido, restrito a adultos
self-payer em Unit explicitamente concedida**, sem Clinical, financeiro nem
automação: usuário provisionado entra, a aplicação identifica a conta, aplica
permission + Unit scope, localiza/cadastra/consulta paciente permitido e deixa
evidência durável. Agenda e turmas são a etapa seguinte: dependem de Staff e de
um design/implementação Operations ainda inexistentes.

Assim, login → busca → detalhe → cadastro de adulto/self-payer é o primeiro fluxo
operacional. Não se promete no mesmo release criar turma ou associar paciente a
ela. O MVP ampliado do piloto só começa após os marcos de Staff, Scheduling e
Pilates abaixo.

Usuários iniciais: Proprietária/Gestora e Secretária/Recepção, com grants mínimos
e Units explícitas. Profissional é consultado/cadastrado no marco próprio; não é
sinônimo de `UserAccount`.

## 4. Inventário funcional por contexto

| Capacidade | Owner | Backend / persistência / autorização / testes | Frontend | Status real e dependência |
|---|---|---|---|---|
| Organization | Registry/Organization | Clinic/Unit, EF/migrations, contratos internos e integração testada; sem IAM real | inexistente | Parcial; Unit baseline suporta Patients |
| People | Registry/People | Person e consulta purpose-specific; persistido/testado no slice de Patients | inexistente | Parcial; sem edição/endereço operacional |
| Patients | Registry/Patients | cadastro adulto SELF, busca, detalhe e GuardianLink; migrations e 118 testes de fechamento; autorização simulada | inexistente | Parcial; bloqueado para uso real por IAM/Audit |
| Guardian Links | Registry/Patients | Create/List/End, temporalidade/ETag/concurrency testados | inexistente | Implementado limitado a Person existente |
| Identity & Access | Access | apenas abstração de claims e auth de testes; sem storage | inexistente | Sem implementação operacional; gate |
| Staff | Registry/Staff | somente modelagem | inexistente | NEEDS_DESIGN para slice físico/API |
| Scheduling | Operations/Scheduling | somente modelagem | inexistente | NEEDS_DESIGN |
| Pilates | Operations/Pilates | somente modelagem | inexistente | NEEDS_DESIGN |
| Clinical | Clinical | skeleton; modelado | inexistente | Fora do MVP inicial |
| Plans & Enrollment | Revenue/Plans | skeleton; modelado | inexistente | Fora do primeiro corte; decisão se membership piloto exige Enrollment |
| Billing | Revenue/Billing | skeleton; modelado | inexistente | Fora do MVP |
| Finance | Revenue/Finance | skeleton; modelado | inexistente | Fora do MVP |
| CRM | CRM | skeleton; modelado | inexistente | Fora do MVP |
| Communication | Communication | skeleton; domínio ainda incompleto | inexistente | Fora do MVP |
| Documents | Documents | skeleton; modelado | inexistente | Fora do MVP |
| Privacy & Audit | Audit | shell; sem `AuditRecord`/retenção | inexistente | Gate para uso assistido |
| Reports | Reports | skeleton/read models modelados | inexistente | Fora do MVP |

## 5. Security prerequisites — IAM mínimo

AUTH-001 é linguagem de autorização, não implementação de IAM. O mínimo para
trocar a autenticação exclusiva de testes é um slice desenhado e aprovado que
materialize, sem escolher silenciosamente provider ou protocolo externo:

1. `UserAccount` com identificador, estado ativo/inativo, vínculo opcional e
   explícito a `Person`/`ProfessionalProfile` (conta não é profissional), grants
   e Units com vigência; bootstrap administrativo seguro, de uso único/auditado;
2. mecanismo aprovado para credencial, login, logout, sessão/token, expiração,
   revogação e desativação imediata; erros de login sem enumeração de conta;
3. decisão por request: authenticated + conta ACTIVE + grant de permission +
   Unit scope + policy do recurso + ausência de explicit deny; revogação não pode
   depender de claim stale;
4. proteção de credencial, rate limiting/lockout e recuperação de acesso
   suficientes para o piloto. MFA/step-up completo é futuro, exceto se a decisão
   de risco do piloto o tornar obrigatório.

IAM completo futuro inclui MFA/step-up, recovery detalhado, service identities,
grants delegados/temporários e todos os scopes clínicos/financeiros. Não é válido
emitir claims confiando em header de cliente nem derivar permissão de role, cargo,
propriedade ou vínculo profissional.

## 6. Privacy & Audit mínimo

`ILogger` e trace de request não são Audit. Antes do piloto, deve existir
persistência append-only/durável de evidência, com falha da operação sensível se a
política exigir audit e ele não puder ser gravado. Para o corte administrativo,
auditar ao menos: bootstrap/ativação/desativação/revogação de conta/grant; login
com resultado (sem segredo); criação de paciente; consulta de lista/detalhe quando
classificada sensível; e futura alteração/inativação/transferência de Unit.

Cada registro mínimo deve guardar ator/conta (ou tentativa anônima sanitizada),
timestamp, ação semântica, resource type/id, Unit/escopo, resultado/código,
correlation/trace e motivo quando a operação o exigir. Não registrar senha,
token, cabeçalhos de autorização, CPF completo, telefone/endereço, nascimento,
conteúdo clínico ou payload PII inteiro. Retenção, legal hold e expurgo definitivo
permanecem decisão pendente; o slice define apenas que evidência não é substituída
por log efêmero.

## 7. Frontend

Estado atual: React 19/Vite/TypeScript, CSS global mínimo, uma tela estática e
configuração de URL; sem design system reutilizável, router, armazenamento de
sessão, guard, queries, mutation handling ou formulários.

Primeiras telas, entregues junto aos fluxos que as sustentam: Login; shell
administrativo com identidade/Unit ativa/logout; dashboard mínimo (atalhos e
estado seguro); Pacientes lista, cadastro adulto/self-payer, detalhes e edição
administrativa apenas após API; Profissionais lista; Agenda somente leitura; e
Turmas lista/detalhe. Não criar telas de prontuário, financeiro, CRM ou chamadas
antes dos respectivos slices.

## 8. Patients — gap operacional

Obrigatório para o primeiro corte: UI sobre POST/GET existentes, tratamento de
duplicidade/erros já expostos, idempotência do cadastro, busca por campos
permitidos, detalhe administrativo, Unit scope real e Audit. Somente adultos,
`SELF`, Unit ativa; não se inferem responsabilidades de GuardianLink.

Pode ser postergado: edição cadastral/contatos/endereço, mudança permitida de
Unit, ativação/inativação, histórico e deduplicação operacional — cada mutação
precisa de contrato, concorrência, autorização, audit e testes. Menor continua
dependente de `IMP-003C-DESIGN`; payer diferente depende de `IMP-003D-DESIGN` e
contratos de Plans/Billing. O GuardianLink já implementado não autoriza menor,
payer, acesso, consentimento ou cadastro de nova Person.

## 9. Staff

ProfessionalProfile, EmploymentLink, Unit de atuação, Availability e Leave estão
modelados, sem schema/API/UI/testes. O menor slice posterior deve definir e
implementar: consultar/cadastrar profissional, vínculo ativo com Unit e vigência,
papel profissional, disponibilidade mínima consultável e inativação que preserva
autoria. `UserAccount ↔ Person` e `ProfessionalProfile ↔ Person` são vínculos
separados; não criar conta automaticamente ao criar profissional.

## 10. Scheduling

Scheduling modela `ScheduleRule` para grade recorrente geral, Appointment,
exceções e conflitos de paciente/profissional; sala é informativa e equipamento
não é recurso agendável. Nada está implementado. Para primeira agenda utilizável,
escolher por design um único fluxo: visão por Unit/profissional de grade fixa
recorrente, horário da clínica, profissional e paciente, capacidade/conflitos
impeditivos e exceção pontual. Mudança permanente deve gerar nova vigência, não
reescrever passado. Feriados, férias e cancelamentos podem entrar somente quando
o design definir sua representação mínima; agenda ad-hoc completa e todas as
transições de Appointment ficam posteriores.

## 11. Pilates

`Class`, `ClassSchedule`, `ClassOccurrence`, `ClassMembership`, capacidade,
attendance e reposição estão modelados, mas não físicos. Para operação de turmas,
o menor corte é: criar/consultar turma com Unit, profissional, capacidade e
vigência; definir horário recorrente; visualizar ocorrências derivadas; associar
adulto elegível à turma com vigência e bloquear capacidade/conflito. Chamada,
falta, correção de presença, transferência e reposição são extensões posteriores.
Se a regra de negócio mantiver `Enrollment` como pré-condição obrigatória de
membership (MODEL-005), Plans/Enrollment precisa ser desenhado antes; não se deve
relaxar esse contrato para acelerar o piloto.

## 12. Fluxos verticais e marcos

| Marco | Objetivo e valor demonstrável | Saída / gates |
|---|---|---|
| M0 — Foundation | baseline já entregue | IMP-000..003A; não equivale a uso real |
| M1 — Administrative Access | login real → shell → logout; permission + Unit + deny/revogação reais | IAM mínimo, Audit mínimo, bootstrap, UI; bloqueia uso assistido até validação |
| M2 — Patient Administration | localizar → detalhar → cadastrar adulto/self-payer com evidência | UI dos endpoints existentes; depois projetar edição, se necessária |
| M3 — Staff Foundation | consultar/cadastrar profissional e vínculo Unit vigente | design + API/persistência/UI/audit próprios |
| M4 — Scheduling Core | visualizar agenda por Unit/profissional, regras fixas e conflitos | design do corte e integração com Staff/Patients |
| M5 — Pilates Operational Flow | turma → horário → paciente adulto elegível associado | decidir dependência de Enrollment; capacidade/conflito/audit |
| M6 — Assisted Clinic Pilot | operar supervisionadamente, medir e corrigir processo | backup/restore, HTTPS, migrations, dados separados, treinamento e rollback |

## 13. Backlog sequencial

| ID sugerido | Tipo / status | Objetivo, escopo e dependências | Aceite, testes e demonstração |
|---|---|---|---|
| IAM-001-DESIGN | DESIGN / NEEDS_DESIGN | IAM mínimo do M1: conta, vínculo Person, credencial/sessão aprovada, grants/Unit/deny/revogação, bootstrap, enumeração e recovery mínimo. Fontes: AUTH-001, API-001, DB-001, ARC-003. Fora: provider externo/MFA completo. | contrato e modelo aprovados, ameaças e testes definidos; demonstração é specification revisável |
| AUD-001-DESIGN | DESIGN / NEEDS_DESIGN | Audit durável mínimo e atomicidade com ações M1/M2. Fontes: AUTH-001, DB-001, ARC-003, STATE-001. Fora: retenção jurídica definitiva. Depende de decidir fronteira IAM. | schema/eventos, campos proibidos, falha e testes de integridade aprovados |
| IAM-001-IMP | IMPLEMENTATION / BLOCKED | materializar somente IAM aprovado, login/logout, sessão/revogação e bootstrap seguro. Depende de IAM-001-DESIGN e AUD-001-DESIGN. | testes unit/integration/API de enumeração, deny, Unit, desativação e revogação; login real demonstrável |
| AUD-001-IMP | IMPLEMENTATION / BLOCKED | persistir AuditRecord mínimo e integrá-lo às ações selecionadas. Depende de AUD-001-DESIGN. | testes de persistência/atomicidade, sanitização PII e correlação; evidência consultável restrita |
| WEB-001-ADMIN | IMPLEMENTATION / BLOCKED | Login, shell, logout, guard, Unit visível e estados de erro. Depende de IAM-001-IMP. Fora: produto completo. | testes de UI/E2E críticos; usuário entra e só vê o escopo autorizado |
| WEB-002-PATIENTS | IMPLEMENTATION / BLOCKED | lista, detalhe e cadastro adulto/self-payer sobre APIs existentes. Depende de WEB-001, IAM/Audit. | E2E busca→detalhe→cadastro e erros/duplicidade; secretária demonstra fluxo permitido |
| STF-006-DESIGN | DESIGN / READY | corte Staff para profissional, EmploymentLink/Unit/vigência, Availability e inativação. Fontes: MODEL-001, AUTH-001, API-001, STATE-001. Fora: conta automática. | contratos, invariantes, audit e testes aprovados |
| STF-006-IMP | IMPLEMENTATION / BLOCKED | API/persistência/UI do design Staff. Depende de M1 e STF-006-DESIGN. | testes de Unit scope/vigência/inativação; profissionais consultáveis |
| AGD-008-DESIGN | DESIGN / NEEDS_DESIGN | corte de agenda fixa de M4 e política de conflito/feriado/exception. Fontes: MODEL-002, STATE-001, AUTH-001, API-001. | decisão de ocorrência/read model, vigência, audit e testes aprovada |
| AGD-008-IMP | IMPLEMENTATION / BLOCKED | agenda de leitura e grade mínima aprovada. Depende de Staff e AGD-008-DESIGN. | conflitos e visão Unit/profissional testados; agenda demonstrável |
| PIL-010-DESIGN | DESIGN / BLOCKED | turma/membership MVP. Depende da decisão de elegibilidade via Enrollment e AGD-008. | capacidade, conflito, vigência, autorização e audit aprovados |
| PIL-010-IMP | IMPLEMENTATION / BLOCKED | turma, horário e membership conforme design. | testes de capacidade/conflito e demonstração turma→paciente |
| PILOT-001 | VALIDATION / BLOCKED | readiness e operação assistida. Depende de M1–M5 aplicáveis. | checklist de segurança, backup/restore, treinamento e métricas aprovados |

## 14. READY / NEEDS_DESIGN / BLOCKED

**READY:** `STF-006-DESIGN` é o primeiro design de domínio posterior que já tem
modelos/estados/policies suficientes, mas só deve iniciar após M1/M2 conforme a
priorização. A recomendação imediata abaixo é DESIGN, não READY de implementação.

**NEEDS_DESIGN:** IAM-001-DESIGN, AUD-001-DESIGN e AGD-008-DESIGN. AUTH-001
explicitamente exclui IAM físico; não há decisão de tecnologia, lifecycle ou
schema para implementá-los.

**BLOCKED:** todas as implementações M1+ dependem de seus designs; UI depende de
IAM real; turma depende de Staff/Scheduling e da decisão de Enrollment. Menores,
payer diferente, responsável administrativo, contato de emergência, correção
histórica e retenção definitiva mantêm os blockers canônicos.

## 15. Decisões pendentes

| Assunto | Opções/documentação existente | Consequência e tarefa afetada | Blocker? |
|---|---|---|---|
| IAM físico | AUTH-001 exclui ASP.NET Identity/JWT/OIDC/provider; exige sessão/revogação conceitual | escolher mecanismo, storage e lifecycle antes de IAM-001-IMP | Sim |
| Bootstrap e recovery | AUTH-001 prevê grants auditados, IAM backlog prevê recovery; detalhes ausentes | impede conta administrativa segura e recuperação | Sim para M1 |
| Audit/retention | AuditRecord é modelado; retenção/legal hold não definidos | AUD-001 pode fixar evidência mínima, não retenção definitiva | Retenção não bloqueia design mínimo; definição de falha/persistência bloqueia impl. |
| Scope padrão de Secretária/MULTI_UNIT | AUTH-001 §7 mantém configurável | grants e cenário piloto não podem assumir todas as Units | Sim para provisioning piloto |
| Membership e Enrollment | MODEL-005 descreve consulta de elegibilidade/frequência de Plans | impede PIL-010 se Enrollment for guard obrigatório | Sim para Pilates |
| Menor/payer/responsáveis | IMP-003B..F e OQs canônicas | não incluir em cadastro do piloto | Sim para esses fluxos, não para adulto SELF |

## 16. Fora do primeiro MVP

Financeiro e Billing completos; Clinical/prontuário; CRM; comunicação automatizada;
documentos avançados; Reports avançados; integrações externas, n8n e automações;
MFA/step-up completo; reposições/chamada/transferência de Pilates; agenda ad-hoc;
menor, non-self payer e os relacionamentos ainda gated. Esses módulos seguem no
roadmap geral, apenas não são condição do primeiro corte assistido.

## 17. Estratégia de piloto e critérios de uso assistido

Ambientes: **Development** usa dados sintéticos e auth de testes; **Homologation**
valida migrations e fluxos em ambiente separado; **Assisted Internal Use** atende
somente equipe autorizada, supervisionada e com rollback; **Production** exige
go-live público/regulatório e não é declarado por este documento.

Entrada no piloto: IAM real e least privilege validados; Audit mínimo aprovado;
HTTPS; migrations controladas; backup e restore testados; dados de teste separados;
grants/Units revisados; treinamento de proprietária/secretária; janela de suporte;
plano de rollback operacional (suspender acesso e retornar ao processo anterior
sem apagar fatos). Não iniciar com dados clínicos/financeiros nem concluir que
suporte técnico concede autoridade de negócio.

Métricas não invasivas: mediana para localizar e cadastrar paciente; erros de
cadastro/duplicidade; operações que exigem correção manual; tentativas/conflitos
de agenda bloqueados quando M4 existir; e feedback estruturado de secretária e
proprietária por fluxo. Não registrar analytics que amplie exposição de PII.

## 18. Riscos

Maior risco é transformar claims de teste em autorização real ou publicar UI sobre
endpoints sem IAM/Audit. Outros riscos: confundir GuardianLink com autorização de
menor/payer; fazer Scheduling/Pilates sem Staff/Enrollment; relaxar conflitos ou
vigência; e iniciar piloto sem recuperação de backup. Mitigação é manter os gates,
executar design antes da implementação e validar cada marco com um fluxo completo.

## 19. Próxima tarefa recomendada

**IAM-001-DESIGN — Identity & Access mínimo para uso administrativo assistido.**
Ela vem antes porque todo endpoint e toda tela útil dependem de identidade
confiável, grants, Unit scope, deny e revogação reais; o backend atual só recebe
esses fatos de um handler de teste. Dependências: AUTH-001, DB-001, API-001,
ARC-003, decisões de bootstrap/recovery e alinhamento com AUD-001-DESIGN.
Readiness: **NEEDS_DESIGN**. Resultado concreto: especificação aprovada, pequena
e testável que torna IAM-001-IMP e WEB-001 estimáveis sem escolher provider ou
tecnologia silenciosamente.

## 20. Evidências consultadas

`PROJECT_OS.md`; `FISIOFIT_AI_PROFILE`; Context/Ownership/ARC-003; API-001;
AUTH-001; DB-001; STATE-001; RULES/PROCESS indexes; DECISIONS; domínio de
People/Patients, Scheduling e Pilates; MODEL-001..005; `GATE_M1_AUDIT`; todos os
documentos existentes IMP-000, IMP-001, IMP-002, IMP-003-DESIGN e IMP-003A;
`Program.cs`, módulos, frontend e testes API/integração existentes.
