# AUTH-001 — Permissions & Policies

## 1. Status

- **Tarefa:** AUTH-001
- **Status:** DONE — PASS
- **Data:** 2026-09-16
- **Natureza:** autorização conceitual; RBAC + policies/resource-based authorization
- **Baseline:** Gate M1, ARC-001/002, MODEL-001..005, STATE-001 e EVT-001
- **Próxima tarefa:** ARC-003 — Physical Architecture / Modular Monolith Design
- **ARC-003 readiness:** READY

## 2. Objetivo

Definir quem pode solicitar ou executar cada operação de negócio, sobre qual recurso e escopo, sob quais condições, com qual auditoria, motivo, aprovação e step-up. O resultado é uma linguagem canônica de autorização, não uma implementação de IAM.

## 3. Escopo

- quatro papéis operacionais conhecidos e combinação segura de papéis;
- scopes, permissions e policies contextuais;
- operações dos 16 contexts, transições de STATE-001 e 37 processos;
- segregação Clinical, Billing/Finance e administração técnica;
- autorização cross-context, Documents, Reports, eventos, auditoria e negações;
- requisitos conceituais de sessão/revogação, field-level security, service identity e step-up.

## 4. Fora de Escopo

IAM físico, ASP.NET Identity, JWT, OAuth, OpenID Connect, provider de MFA, tokens, secrets, claims físicas, middleware, handlers, endpoints, banco, tabelas, migrations, código e UI. Também ficam fora valores de alçada ainda não aprovados, lifecycle detalhado de PrivacyRequest, retenção jurídica, algoritmo de conflito e arquitetura física.

## 5. Authorization Principles

1. **Authentication != authorization:** identidade autenticada é entrada, nunca decisão suficiente.
2. **DENY_BY_DEFAULT:** ausência de grant explícito, policy satisfeita e escopo válido resulta em `DENY`.
3. **RBAC + resource policy:** role fornece capacidade geral; o owner do recurso valida contexto, vínculo, estado e escopo no momento da operação.
4. **Least privilege:** cada papel recebe somente capacidades necessárias à sua função conhecida.
5. **No implicit privilege:** propriedade da clínica, acesso técnico, vínculo profissional, autoria histórica, consumo de evento ou leitura de projeção não concedem autoridade adicional.
6. **Owner decides:** somente o contexto proprietário autoriza mutação do próprio recurso; consumers não contornam sua policy.
7. **Current decision:** autorização é reavaliada no request/use; concessão antiga ou sessão stale não prova acesso atual.
8. **Business action, not CRUD:** permissions representam operações com risco próprio; fatos históricos usam cancel, reverse, rectify, end ou reopen, não delete genérico.
9. **Approval != authorization:** pedir, aprovar e executar são capacidades separadas quando há workflow aprovado.
10. **Explicit deny wins:** uma negação de boundary, estado, elegibilidade ou recurso prevalece sobre a união de grants dos papéis.

### 5.1 Decision model

`ALLOW` e `DENY` são decisões finais. `REQUIRES_APPROVAL` e `REQUIRES_STEP_UP` são resultados intermediários explícitos: a operação ainda não está autorizada até a condição ser satisfeita. `DEFERRED` impede implementação permissiva.

Conceitualmente:

`ALLOW = authenticated actor AND explicit permission AND scope match AND resource policy allows AND state guard allows AND no explicit deny AND required approval/step-up satisfied`.

### 5.2 Combination of roles

Uma conta pode ter vários papéis. Grants são combinados por ação e escopo, nunca por “super-role”. `OWNER_MANAGER + PHYSIOTHERAPIST` permite capacidades administrativas do primeiro e clínicas do segundo somente quando as policies clínicas também passam. Escopos não se ampliam mutuamente; o mais restritivo aplicável ao recurso prevalece. Nenhum papel pode conceder ou ampliar os próprios privilégios.

## 6. Roles

| Role | Business purpose | Default capabilities | Explicit boundary |
|---|---|---|---|
| `OWNER_MANAGER` | gestão administrativa, operacional, comercial e financeira | operação ampla, catálogo, usuários/permissões sob policy, exceções e indicadores | não concede Clinical |
| `SECRETARY_RECEPTION` | recepção e operação administrativa | People/Patients, CRM, agenda, turmas, contratação, Billing/Finance dentro da alçada | não lê prontuário integral nem escreve Clinical |
| `PHYSIOTHERAPIST` | atendimento e operação assistencial | agenda/turmas próprias, chamada, PatientProfile mínimo e registros clínicos sob contexto assistencial | não concede Finance, gestão comercial ou IAM global |
| `DEVELOPER_IT` | custódia e suporte técnico | configuração técnica, observabilidade e logs sanitizados no suporte autorizado | não concede operação de negócio, Clinical, Billing ou Finance |

`RESPONSIBLE_TECHNICAL / RT` não é adotado como quinto role global. As fontes sustentam possíveis alçadas adicionais, mas não definem requisitos, lifecycle ou alcance. Fica como **professional attribute ou permission assignment contextual — DEFERRED**, `BLOCKING BEFORE IMPLEMENTATION` das operações que dependam de RT e `BLOCKING BEFORE GO-LIVE` para validação regulatória.

Identidades de serviço ficam fora do role model humano: `SERVICE_IDENTITY — DEFERRED FOR ARCHITECTURE`, sempre least privilege, finalidade limitada e sem grants globais para n8n.

## 7. Scopes

| Scope | Semântica adotada | Usos principais |
|---|---|---|
| `SELF` | a própria conta/pessoa, sem poder elevar privilégios | dados próprios, vínculo/sessões próprias quando permitido |
| `OWN_RESOURCE` | recurso criado/atribuído ao ator, condicionado a estado e policy | drafts clínicos próprios, tarefas próprias |
| `ASSIGNED_PATIENT` | paciente coberto por relação assistencial atual | Clinical e PatientProfile mínimo |
| `ASSIGNED_CLASS` | Class/ClassOccurrence atribuída ao profissional, incluindo substituição válida | Pilates, Attendance e contexto clínico |
| `ASSIGNED_APPOINTMENT` | Appointment atribuído ao profissional | agenda própria e contexto clínico |
| `UNIT` | uma ou mais Units explicitamente concedidas | recepção, agenda, turmas, despesas UNIT |
| `MULTI_UNIT` | conjunto explícito de Units; não significa toda a clínica | operação compartilhada; configuração `DEFERRED` |
| `CLINIC` | toda a clínica para capacidade administrativa específica | Owner/Manager quando grantado |
| `FINANCIAL_SCOPE` | contas, categorias, operações e/ou Units financeiras explicitamente concedidas | Billing e Finance |
| `CLINICAL_SCOPE` | pacientes/recursos autorizados pela policy clínica; nunca “todos” por role | prontuário, export e break-glass |
| `TECHNICAL_SUPPORT_SCOPE` | sistema/ambiente/incidente autorizado, temporário quando sensível | suporte, logs sanitizados, manutenção |

`OWN_CLASS` e `OWN_APPOINTMENT` são nomes de policies, não scopes globais paralelos. `MULTI_UNIT` e a abrangência padrão da Secretária são configuráveis e permanecem `BLOCKING BEFORE IMPLEMENTATION`; Owner/Manager pode receber `CLINIC` explicitamente, não por inferência do nome do papel.

## 8. Permission Model

Formato: `context.resource.action`, estável e semanticamente ligado a uma operação. Permission não contém ID de Unit/paciente nem substitui policy. Grants podem ser atribuídos por role e, excepcionalmente, diretamente, sempre com escopo, vigência, concedente e auditoria.

Não existem `*.manage_all`, `reports.view_all`, `clinical.manage` ou `delete` genérico para fatos históricos. Sensibilidade: `STANDARD`, `SENSITIVE`, `CLINICAL`, `FINANCIAL`, `SECURITY_SENSITIVE`.

### 8.1 Resource matrix

| Resource | Read | Create | Modify | Sensitive operations | Scope rule |
|---|---|---|---|---|---|
| Person/PatientProfile | identificação administrativa necessária | Person/Profile | dados correntes e links | merge, payer change, deactivate | UNIT/CLINIC; Clinical recebe view mínima |
| ProfessionalProfile | perfil/vínculos autorizados | profile/link | availability/leave/vigência | end employment, inactivate | SELF ou UNIT/CLINIC administrativo |
| Opportunity/Task | pipeline autorizado | opportunity/task | transições válidas | lost/disqualify/reactivate | UNIT/assigned commercial owner |
| Appointment | agenda autorizada | schedule | confirm/reschedule/result | cancel/no-show/exceptions | UNIT ou OWN_APPOINTMENT |
| Class/Occurrence | turma/roster necessário | class/occurrence | schedule/membership/call | cancel/substitute/correct attendance | UNIT ou OWN_CLASS |
| Clinical record | summary/record sob policy | draft | draft próprio | finalize/rectify/addendum/export/break-glass | CLINICAL_SCOPE contextual |
| Contract/Enrollment | posição comercial autorizada | plan/contract/enrollment | transitions/vigência | price version/cancel/pause | UNIT/CLINIC comercial |
| Billing | posição do paciente autorizada | receivable/payment | allocation/restriction | adjustment/reversal/refund/negotiation | FINANCIAL_SCOPE |
| Finance | contas e posição autorizadas | account/expense | pay/reconcile/transfer | closing/reopen | FINANCIAL_SCOPE |
| Document | metadata somente após owner allow | upload | draft unlink | download/export | herda contexto do owner |
| Audit/Report | view minimizada | request/report definition quando aprovado | sem editar fatos | sensitive audit/report export | sensibilidade da fonte |

## 9. Permission Catalog

| Permission | Context | Business Meaning | Sensitivity | Scope |
|---|---|---|---|---|
| `identity.account.create` | Identity | criar conta vinculada a Person | SECURITY_SENSITIVE | CLINIC |
| `identity.account.disable` | Identity | desabilitar conta e iniciar revogação | SECURITY_SENSITIVE | CLINIC |
| `identity.role.assign`, `identity.role.revoke` | Identity | conceder/revogar papel de outra conta | SECURITY_SENSITIVE | CLINIC |
| `identity.permission.assign`, `identity.permission.revoke` | Identity | concessão excepcional explícita | SECURITY_SENSITIVE | CLINIC |
| `identity.session.terminate` | Identity | encerrar sessões de conta autorizada | SECURITY_SENSITIVE | SELF/CLINIC |
| `organization.structure.manage` | Organization | criar/alterar/inativar Clinic, Unit e Room | SENSITIVE | CLINIC |
| `organization.calendar.read`, `organization.calendar.manage` | Organization | consultar ou alterar calendário/feriados | STANDARD/SENSITIVE | UNIT/CLINIC |
| `people.person.create`, `people.person.read`, `people.person.update` | People | identidade administrativa | SENSITIVE | UNIT/CLINIC |
| `people.contact.manage` | People | gerir contatos/endereço | SENSITIVE | UNIT/CLINIC |
| `people.duplicate.flag` | People | sinalizar possível duplicidade | STANDARD | UNIT/ASSIGNED_PATIENT |
| `people.person.merge.simple` | People | concluir merge seguro | SENSITIVE | UNIT/CLINIC |
| `people.person.merge.request_sensitive`, `people.person.merge.approve_sensitive` | People | solicitar/aprovar merge sensível | SECURITY_SENSITIVE | CLINIC |
| `patients.profile.create`, `patients.profile.update`, `patients.profile.deactivate` | Patients | gerir papel administrativo | SENSITIVE | UNIT/CLINIC |
| `patients.guardian.manage`, `patients.admin_responsible.manage`, `patients.payer.change`, `patients.emergency_contact.manage` | Patients | gerir vínculos específicos | SENSITIVE | UNIT/CLINIC |
| `staff.profile.create`, `staff.profile.update` | Staff | gerir perfil profissional | SENSITIVE | CLINIC |
| `staff.employment.start`, `staff.employment.end`, `staff.unit.assign` | Staff | gerir vínculo e atuação | SENSITIVE | CLINIC |
| `staff.availability.manage`, `staff.leave.manage` | Staff | disponibilidade e afastamento | SENSITIVE | SELF autorizado/CLINIC |
| `crm.opportunity.create`, `crm.opportunity.assign`, `crm.opportunity.advance`, `crm.opportunity.qualify` | CRM | operar início do pipeline | STANDARD | UNIT/CLINIC |
| `crm.proposal.present`, `crm.negotiation.start`, `crm.opportunity.convert` | CRM | operar proposta/conversão | SENSITIVE | UNIT/CLINIC |
| `crm.opportunity.lose`, `crm.opportunity.disqualify`, `crm.opportunity.reactivate` | CRM | encerrar/retomar ciclo com histórico | SENSITIVE | UNIT/CLINIC |
| `crm.task.manage` | CRM | criar/iniciar/concluir/cancelar tarefas | STANDARD | OWN_RESOURCE/UNIT |
| `scheduling.agenda.view` | Scheduling | consultar agenda autorizada | STANDARD | UNIT/OWN_APPOINTMENT |
| `scheduling.appointment.schedule`, `scheduling.appointment.confirm` | Scheduling | agendar/confirmar Appointment | STANDARD | UNIT/OWN_APPOINTMENT |
| `scheduling.appointment.reschedule`, `scheduling.appointment.cancel` | Scheduling | reagendar/cancelar preservando histórico | SENSITIVE | UNIT/OWN_APPOINTMENT |
| `scheduling.appointment.no_show`, `scheduling.appointment.complete` | Scheduling | registrar resultado | STANDARD | UNIT/OWN_APPOINTMENT |
| `scheduling.block.create`, `scheduling.calendar_exception.manage` | Scheduling | bloquear agenda/aplicar exceção | SENSITIVE | SELF/UNIT/CLINIC |
| `pilates.class.create`, `pilates.class.schedule_change` | Pilates | criar turma/mudar vigência | SENSITIVE | UNIT/CLINIC |
| `pilates.occurrence.cancel`, `pilates.substitute.assign` | Pilates | cancelar ocorrência/atribuir substituto | SENSITIVE | UNIT/OWN_CLASS |
| `pilates.membership.add`, `pilates.membership.end`, `pilates.membership.transfer` | Pilates | gerir vínculo recorrente | SENSITIVE | UNIT/OWN_CLASS |
| `pilates.attendance.record`, `pilates.attendance.correct` | Pilates | registrar/corrigir chamada | STANDARD/SENSITIVE | OWN_CLASS/UNIT |
| `pilates.makeup.grant`, `pilates.makeup.reserve`, `pilates.makeup.cancel_reservation` | Pilates | gerir direito/reserva de reposição | STANDARD/SENSITIVE | UNIT/OWN_CLASS |
| `clinical.summary.read`, `clinical.record.read` | Clinical | ler resumo ou prontuário autorizado | CLINICAL | ASSIGNED_PATIENT/CLINICAL_SCOPE |
| `clinical.episode.open` | Clinical | abrir CareEpisode | CLINICAL | ASSIGNED_PATIENT |
| `clinical.assessment.create`, `clinical.assessment.edit_draft`, `clinical.assessment.finalize` | Clinical | criar/editar/finalizar Assessment | CLINICAL | ASSIGNED_PATIENT/OWN_RESOURCE |
| `clinical.entry.create`, `clinical.entry.edit_draft`, `clinical.entry.finalize` | Clinical | criar/editar/finalizar evolução | CLINICAL | ASSIGNED_PATIENT/OWN_RESOURCE |
| `clinical.entry.rectify`, `clinical.entry.addendum` | Clinical | corrigir/complementar registro finalizado | CLINICAL | ASSIGNED_PATIENT/OWN_RESOURCE ou alçada clínica |
| `clinical.document.attach` | Clinical | vincular arquivo privado ao registro | CLINICAL | mesmo scope do alvo |
| `clinical.record.export` | Clinical | exportar prontuário minimizado | SECURITY_SENSITIVE | patient-specific |
| `clinical.break_glass.use` | Clinical | acesso excepcional limitado | SECURITY_SENSITIVE | patient/resource-specific |
| `plans.plan.create`, `plans.plan_version.create` | Plans | gerir catálogo e nova condição | SENSITIVE | CLINIC |
| `plans.contract.create`, `plans.contract.accept`, `plans.contract.cancel`, `plans.contract.renew` | Plans | ciclo contratual | SENSITIVE | UNIT/CLINIC |
| `plans.enrollment.activate`, `plans.enrollment.pause`, `plans.enrollment.resume`, `plans.enrollment.cancel`, `plans.enrollment.frequency_change` | Plans | ciclo operacional da matrícula | SENSITIVE | UNIT/CLINIC |
| `billing.receivables.generate`, `billing.receivable.view` | Billing | gerar/consultar obrigações | FINANCIAL | FINANCIAL_SCOPE |
| `billing.receivable.adjust` | Billing | ajuste manual rastreável | FINANCIAL | FINANCIAL_SCOPE |
| `billing.payment.register`, `billing.payment.allocate`, `billing.payment.reverse` | Billing | registrar/alocar/reverter pagamento | FINANCIAL | FINANCIAL_SCOPE |
| `billing.refund.issue`, `billing.refund.complete` | Billing | emitir/concluir devolução | FINANCIAL | FINANCIAL_SCOPE |
| `billing.negotiation.create` | Billing | registrar negociação autorizada | FINANCIAL | FINANCIAL_SCOPE |
| `billing.restriction.apply`, `billing.restriction.remove` | Billing | gerir restrição financeira | FINANCIAL | FINANCIAL_SCOPE |
| `finance.view` | Finance | consultar posição, contas e closing | FINANCIAL | FINANCIAL_SCOPE |
| `finance.account.create` | Finance | criar conta financeira | FINANCIAL | CLINIC |
| `finance.expense.register`, `finance.expense.pay`, `finance.expense.cancel` | Finance | ciclo de despesa | FINANCIAL | FINANCIAL_SCOPE |
| `finance.money.transfer`, `finance.account.reconcile` | Finance | transferir/conciliar | FINANCIAL | FINANCIAL_SCOPE |
| `finance.closing.close`, `finance.closing.reopen` | Finance | fechar/reabrir período | FINANCIAL | FINANCIAL_SCOPE |
| `communication.message.send_manual`, `communication.template.manage`, `communication.delivery.view` | Communication | enviar, configurar e consultar entrega | SENSITIVE | source purpose/UNIT |
| `communication.approved.trigger` | Communication | executar intenção aprovada do owner | SENSITIVE | source context |
| `documents.upload`, `documents.read`, `documents.draft.remove`, `documents.download` | Documents | operar arquivo técnico | INHERITED | owner resource policy |
| `privacy.audit.view`, `privacy.audit.view_sensitive` | Privacy & Audit | consultar trilha comum/sensível | SENSITIVE/SECURITY_SENSITIVE | assigned audit scope |
| `privacy.request.create`, `privacy.request.process` | Privacy & Audit | solicitar/processar workflow | SECURITY_SENSITIVE | SELF/assigned privacy scope |
| `reports.operational.read`, `reports.financial.read`, `reports.clinical.read` | Reports | ler relatório por conteúdo | STANDARD/FINANCIAL/CLINICAL | source-derived scope |

## 10. Policy Catalog

| Policy | Inputs | Decision Rule | Used By | Audit |
|---|---|---|---|---|
| `ACTIVE_ACCOUNT` | account status, revocation state | allow somente conta ativa e não revogada | todas | STANDARD em deny sensível |
| `NO_SELF_PRIVILEGE_ESCALATION` | actor, target account, permission delta | deny quando ator beneficia a própria autoridade direta ou indiretamente | role/permission assignment | SENSITIVE |
| `UNIT_SCOPE` | actor Unit grants, resource Unit/effective date | resource deve pertencer ao conjunto explícito vigente | operação administrativa | STANDARD/SENSITIVE pela ação |
| `FINANCIAL_SCOPE` | grants de conta/Unit/operação, recurso | permission financeira e escopo explícito devem coincidir | Billing/Finance | FINANCIAL |
| `ACTIVE_PROFESSIONAL_RELATIONSHIP` | ProfessionalProfile, EmploymentLink, UnitLink, leave, instant | perfil/vínculo aplicável ativo; leave pode negar operação assistencial | Clinical/Scheduling/Pilates | CLINICAL/SENSITIVE |
| `OWN_APPOINTMENT` | assigned professional, substitute, time, actor | ator é profissional atribuído ou substituto autorizado e vínculo está ativo | agenda própria/Clinical | STANDARD/CLINICAL |
| `OWN_CLASS` | planned/actual professional, substitute, effective schedule, actor | ator conduz a Class/Occurrence no instante ou recebeu delegação explícita | Pilates/Clinical | STANDARD/SENSITIVE |
| `ASSIGNED_PATIENT` | Appointment/Class/CareEpisode relationship, dates | existe contexto assistencial atual e necessário ao purpose | Clinical read/write | CLINICAL |
| `CLINICAL_CARE_RELATIONSHIP` | clinical role, active professional, patient, Appointment/Class/CareEpisode, continuity | todos os componentes relevantes são válidos; mera autoria passada não basta | Clinical | CLINICAL |
| `CLINICAL_DRAFT_AUTHOR` | record status, author ProfessionalId, actor | somente DRAFT e autor atual autorizado; alçada excepcional deve ser explícita | edit draft | CLINICAL |
| `OWNER_APPROVAL_REQUIRED` | action, classification, requester, approver | aprovador distinto e autorizado quando merge sensível; não se estende sem regra | sensitive merge | SENSITIVE |
| `BREAK_GLASS_ELIGIBLE` | clinical permission, active relationship, patient/scope, reason, justification, confirmation, duration | allow excepcional somente a ator clínico elegível, escopo/duração mínimos e step-up satisfeito | break-glass | CLINICAL reforçado |
| `DOCUMENT_CONTEXT_ACCESS` | document owner reference, requested action, owner decision | Documents só permite se o contexto dono autorizar a mesma ação/recurso | documents read/download/remove | nível do owner |
| `REPORT_SOURCE_SENSITIVITY` | report fields, sources, actor grants/scopes | interseção das policies das fontes; projeção nunca amplia acesso | Reports | conforme fonte |
| `TECHNICAL_SUPPORT_AUTHORIZATION` | ticket/incidente, environment, time window, approved scope | acesso técnico temporário/minimizado; dados de negócio continuam negados | Developer/IT | SENSITIVE |
| `CURRENT_RESOURCE_STATE` | current state/version, requested action | ação precisa ser válida em STATE-001; grant não contorna estado terminal | todas as transições | nível da ação |

## 11. Organization Authorization

- Owner/Manager: `organization.structure.manage` e calendário em `CLINIC`, condicionais a recurso/estado.
- Secretary: leitura de Units/Rooms/calendário em `UNIT/MULTI_UNIT`; `organization.calendar.manage` somente se grantado. Alteração estrutural é `DENY` por padrão.
- Physiotherapist: leitura das Units/calendário necessários às próprias atividades; sem mutação estrutural.
- Developer/IT: `DENY` para operação de Organization; configuração técnica não equivale a cadastro institucional.

## 12. People Authorization

Owner/Manager e Secretary podem criar, ler, atualizar Person e contatos em escopo operacional. Physiotherapist recebe somente identificação administrativa mínima de `ASSIGNED_PATIENT` e pode `people.duplicate.flag`; não recebe edição civil ampla. Developer/IT não opera cadastro.

Merge simples é `CONDITIONAL` para Secretary e Owner/Manager: classificação `SIMPLE`, sem conflitos clínicos/contratuais/financeiros/operacionais, motivo e audit. Merge sensível separa request de approval: Secretary pode solicitar; Owner/Manager pode aprovar/executar sob `OWNER_APPROVAL_REQUIRED`. Physiotherapist somente sinaliza. Reversal de merge continua condicionado à policy ainda incompleta e não é grantado nesta baseline.

## 13. Patients Authorization

Owner/Manager e Secretary operam profile, guardian, administrative responsible, payer e emergency contact em `UNIT/CLINIC`. Deactivate e payer change exigem motivo e `SENSITIVE_AUDIT`. Physiotherapist lê somente o subset administrativo necessário ao cuidado sob `ASSIGNED_PATIENT`; não altera PatientProfile nem vínculos administrativos. Developer/IT: `DENY`.

Field-level future need: separar identificação/contato necessário ao atendimento, dados civis sensíveis, vínculos legais/administrativos e conteúdo clínico. Não são criadas permissions por campo nesta etapa.

## 14. Staff Authorization

Owner/Manager administra ProfessionalProfile, EmploymentLink e UnitLink. Secretary pode receber create/update, availability e leave em escopo operacional; start/end employment e assignment estrutural exigem grant explícito e ficam `DENY` por padrão no papel base. Physiotherapist lê `SELF` e pode gerir disponibilidade própria apenas quando policy organizacional conceder; não altera vínculo empregatício. Developer/IT não recebe autoridade de Staff.

EndEmployment/InactivateProfessional dispara remoção de acesso operacional; não remove autoria. RT permanece atributo/alçada deferred.

## 15. CRM Authorization

Owner/Manager e Secretary operam pipeline em `UNIT/CLINIC`, respeitando estado, owner comercial e motivo. `MarkLost`, `Disqualify` e `Reactivate` exigem reason e `SENSITIVE_AUDIT`; conversão exige Contract/Enrollment, nunca Payment. Physiotherapist e Developer/IT: `DENY` por padrão. Um Physiotherapist pode participar do atendimento experimental sem se tornar pipeline manager.

## 16. Scheduling Authorization

- Owner/Manager e Secretary: agenda ampla dentro de `UNIT/CLINIC`, incluindo schedule, confirm, reschedule, cancel, no-show, complete, blocks e CalendarException conforme grant.
- Physiotherapist: `scheduling.agenda.view` sob `OWN_APPOINTMENT`/`OWN_CLASS`; confirm/complete/no-show do Appointment próprio podem ser grantados. Encaixar, reagendar ou cancelar continua **CONDITIONAL/DEFERRED** por ausência de regra de alçada ampla; não se presume.
- Developer/IT: `DENY`.

Reschedule/cancel exigem reason e audit sensível. ConflictPolicy e estado corrente continuam guards mesmo para Owner/Manager.

## 17. Pilates Authorization

Owner/Manager e Secretary podem criar turma, mudar schedule, gerir memberships, ocorrências e reposições em `UNIT/CLINIC`. Physiotherapist pode gerir pacientes, chamada, substituição e reposições apenas sob `OWN_CLASS`, capacidade, elegibilidade, conflito e regras vigentes. Transferência cross-class/unit requer acesso a origem e destino.

`CorrectAttendance`, cancelamento de ocorrência durante execução, exceções de chamada e concessão/cancelamento excepcional de crédito exigem reason e audit sensível. **Não existe `OverrideCapacity`: overbooking é proibido.** Developer/IT: `DENY`.

## 18. Clinical Authorization

### 18.1 Normal clinical access

`NORMAL_CLINICAL_ACCESS` exige cumulativamente:

1. role `PHYSIOTHERAPIST` ou permission clínica explicitamente atribuída;
2. `ACTIVE_PROFESSIONAL_RELATIONSHIP` na data da ação;
3. `CLINICAL_CARE_RELATIONSHIP` por Appointment, Class/Occurrence, CareEpisode/continuidade ou substituição autorizada;
4. permission específica para summary, record, draft, finalize, rectify, addendum, document ou export;
5. estado atual compatível.

ReadClinicalSummary pode ser mais minimizado que ReadClinicalRecord, mas ambos exigem policy clínica. Secretária, Owner/Manager sem capacidade clínica e Developer/IT recebem `DENY`, inclusive quando conseguem ver PatientProfile administrativo.

### 18.2 Own record

O autor pode editar somente seu DRAFT sob `CLINICAL_DRAFT_AUTHOR`. FINALIZED nunca é editável, nem pelo autor, Owner/Manager ou RT. Rectification/Addendum usam permissions próprias, motivo, audit clínico e policy de autoria/alçada. Autoria histórica não concede acesso após término do vínculo.

### 18.3 Break-glass

`BREAK_GLASS_ACCESS` é policy separada: somente ator clínico elegível, razão selecionada, justificativa, confirmação, escopo por paciente/recurso, efeito temporal limitado, `REQUIRES_STEP_UP` e auditoria reforçada/revisável. Secretary, Developer/IT e Owner/Manager sem papel clínico: `DENY`.

### 18.4 Clinical export

`clinical.record.export` não decorre de read. Pode ser solicitada/executada somente por autoridade clínica ou privacy workflow explicitamente autorizados, por paciente e escopo minimizado, com motivo, resultado, audit reforçado e step-up. Regras de destinatário, menores, disclosure, RT/jurídico e comprovação de entrega são `BLOCKING BEFORE IMPLEMENTATION / GO-LIVE`; não se concede export genérico enquanto pendentes.

## 19. Plans Authorization

Owner/Manager gere Plan e PlanVersion; Secretary: `DENY` para criar/alterar condições de preço por padrão, podendo operar Contract/Enrollment dentro das condições publicadas. Owner/Manager e Secretary podem criar/aceitar/cancelar/renovar Contract e ativar/pausar/retomar/cancelar/mudar frequência conforme grants, scope, estados e regras aprovadas. Mudança de preço sempre cria PlanVersion e requer Owner/Manager; alçadas comerciais excepcionais permanecem deferred.

## 20. Billing Authorization

Owner/Manager possui grants financeiros amplos quando `FINANCIAL_SCOPE` for atribuído. Secretary pode view, registrar/alocar Payment, gerar Receivables e gerir restrição dentro da alçada. AdjustReceivable, ReversePayment, Refund e Negotiation são `CONDITIONAL`: permission específica, scope, motivo, audit financeiro e limites/alçadas aprovados. Como os valores ainda não existem, descontos, negociação e refund relevantes ficam `BLOCKING BEFORE IMPLEMENTATION`.

Physiotherapist e Developer/IT: `DENY` por padrão. A permissão técnica do Developer não valida FinancialAccount, não confirma Payment e não processa evento em nome de Billing.

## 21. Finance Authorization

Owner/Manager pode receber todas as permissions financeiras. Secretary pode registrar/pagar/cancelar Expense, transferir, reconciliar, fechar/reabrir somente quando cada grant e `FINANCIAL_SCOPE` forem explícitos; DEC-025 autoriza a possibilidade, não uma concessão universal. Physiotherapist: `DENY`. Developer/IT: `DENY` salvo atribuição financeira excepcional, explícita, não derivada do papel técnico e auditada.

Reconcile, TransferMoney e CloseMonth são sensíveis. ReopenClosing exige permission própria, motivo, `FINANCIAL_AUDIT` e step-up candidato requerido antes da implementação.

## 22. Communication Authorization

Owner/Manager e Secretary podem `communication.message.send_manual`, templates e delivery status dentro de finalidade/escopo. Physiotherapist pode enviar comunicação assistencial somente por fluxo explicitamente aprovado e minimizado; não recebe campanhas ou cobrança. Developer/IT consulta somente telemetria sanitizada, não conteúdo.

`communication.approved.trigger` executa intenção já decidida pelo owner. Communication não decide audiência, inadimplência, cancelamento, perda ou conteúdo clínico.

## 23. Documents Authorization

Toda operação exige `DOCUMENT_CONTEXT_ACCESS`. `documents.read/download` jamais permite busca universal; o owner reference é resolvido e o contexto de negócio autoriza o ator. Para anexo Clinical, exige a mesma policy clínica do alvo. `documents.draft.remove` só vale para draft e retenção aplicável; documento consolidado não é apagado silenciosamente. Developer/IT pode manter storage sem ler conteúdo; suporte excepcional usa `TECHNICAL_SUPPORT_AUTHORIZATION` e audit.

## 24. Privacy & Audit Authorization

`privacy.audit.view` não inclui automaticamente trilhas clínicas, financeiras ou de segurança. `privacy.audit.view_sensitive` exige escopo específico, purpose e audit da consulta. AuditLog é append-only e não possui permission de edição/exclusão de negócio.

Qualquer usuário autorizado pode criar PrivacyRequest para `SELF` ou sujeito representado quando validado; `privacy.request.process` fica `DEFERRED` até lifecycle, papéis de privacidade, bases, retenção e legal hold serem aprovados. Não se inventa role DPO/privacy officer.

## 25. Reports Authorization

Relatórios são separados por conteúdo: operational, financial e clinical. Cada read model aplica `REPORT_SOURCE_SENSITIVITY`, scope e freshness; `reports.operational.read` não concede Finance nem Clinical, `reports.financial.read` exige `FINANCIAL_SCOPE`, e `reports.clinical.read` exige policy clínica e finalidade. Exportar relatório não pode ser usado como atalho para exportar prontuário ou dados financeiros além do grant.

## 26. Role / Permission Matrix

Legenda: `ALLOW` = grant base ainda sujeito a scope/estado; `CONDITIONAL` = policies citadas; `DENY` = negação base; `DEFERRED` = regra insuficiente para conceder.

| Action | Context | Owner/Manager | Secretary | Physiotherapist | Developer/IT | Resource Policy | Audit | Step-up |
|---|---|---|---|---|---|---|---|---|
| Create/DisableUserAccount | Identity | CONDITIONAL | DENY | DENY | CONDITIONAL | NO_SELF_PRIVILEGE_ESCALATION | SENSITIVE | RECOMMENDED |
| Assign/RevokeRole/Permission | Identity | CONDITIONAL | DENY | DENY | DENY por role técnico | NO_SELF_PRIVILEGE_ESCALATION | SENSITIVE | REQUIRED_BEFORE_IMPLEMENTATION |
| TerminateSessions | Identity | CONDITIONAL | DENY | SELF somente | CONDITIONAL suporte | ACTIVE_ACCOUNT/target scope | SENSITIVE | RECOMMENDED |
| ManageClinic/Unit/Room | Organization | ALLOW | DENY | DENY | DENY | CLINIC | SENSITIVE | NOT_REQUIRED |
| ReadCalendar | Organization | ALLOW | ALLOW | CONDITIONAL | DENY | UNIT_SCOPE/own activity | STANDARD | NOT_REQUIRED |
| ManageCalendar/Holiday | Organization | ALLOW | CONDITIONAL | DENY | DENY | UNIT_SCOPE | SENSITIVE | NOT_REQUIRED |
| Create/Read/UpdatePerson, ManageContacts | People | ALLOW | ALLOW | CONDITIONAL read mínimo | DENY | UNIT_SCOPE/ASSIGNED_PATIENT | SENSITIVE | NOT_REQUIRED |
| SimplePersonMerge | People | ALLOW | CONDITIONAL | DENY | DENY | safe SIMPLE classification | SENSITIVE | RECOMMENDED |
| SensitivePersonMerge | People | CONDITIONAL approve | request only | flag only | DENY | OWNER_APPROVAL_REQUIRED | SENSITIVE | REQUIRED_BEFORE_IMPLEMENTATION |
| PatientProfile and responsible links | Patients | ALLOW | ALLOW | CONDITIONAL read mínimo | DENY | UNIT_SCOPE/ASSIGNED_PATIENT | SENSITIVE | NOT_REQUIRED |
| ChangePayer/DeactivatePatient | Patients | ALLOW | CONDITIONAL | DENY | DENY | UNIT_SCOPE + reason | SENSITIVE | NOT_REQUIRED |
| ProfessionalProfile/Employment/Unit | Staff | ALLOW | CONDITIONAL | SELF read | DENY | UNIT_SCOPE/current state | SENSITIVE | NOT_REQUIRED |
| Availability/Leave | Staff | ALLOW | CONDITIONAL | CONDITIONAL SELF | DENY | SELF or UNIT_SCOPE | SENSITIVE | NOT_REQUIRED |
| Opportunity pipeline | CRM | ALLOW | ALLOW | DENY | DENY | UNIT_SCOPE/state | STANDARD/SENSITIVE | NOT_REQUIRED |
| Lose/Disqualify/Reactivate | CRM | ALLOW | ALLOW | DENY | DENY | reason + current state | SENSITIVE | NOT_REQUIRED |
| ViewAgenda | Scheduling | ALLOW | ALLOW | CONDITIONAL | DENY | UNIT_SCOPE/OWN_APPOINTMENT/OWN_CLASS | STANDARD | NOT_REQUIRED |
| Schedule/Confirm/Complete/NoShow | Scheduling | ALLOW | ALLOW | CONDITIONAL own | DENY | conflict + OWN_APPOINTMENT | STANDARD | NOT_REQUIRED |
| Reschedule/CancelAppointment | Scheduling | ALLOW | ALLOW | DEFERRED | DENY | UNIT_SCOPE/OWN_APPOINTMENT + reason | SENSITIVE | NOT_REQUIRED |
| CreateBlock/CalendarException | Scheduling | ALLOW | ALLOW | CONDITIONAL SELF block | DENY | UNIT_SCOPE/SELF | SENSITIVE | NOT_REQUIRED |
| CreateClass/ChangeSchedule | Pilates | ALLOW | ALLOW | CONDITIONAL OWN_CLASS; permanent change deferred | DENY | UNIT_SCOPE/OWN_CLASS | SENSITIVE | NOT_REQUIRED |
| Add/EndMembership | Pilates | ALLOW | ALLOW | CONDITIONAL | DENY | OWN_CLASS + eligibility/conflict/capacity | SENSITIVE | NOT_REQUIRED |
| TransferPatient | Pilates | ALLOW | ALLOW | CONDITIONAL both classes | DENY | origin+destination access | SENSITIVE | NOT_REQUIRED |
| RecordAttendance | Pilates | ALLOW | CONDITIONAL | CONDITIONAL | DENY | UNIT_SCOPE/OWN_CLASS | STANDARD | NOT_REQUIRED |
| CorrectAttendance | Pilates | ALLOW | CONDITIONAL | CONDITIONAL | DENY | OWN_CLASS + reason | SENSITIVE | NOT_REQUIRED |
| Grant/Reserve/CancelMakeup | Pilates | ALLOW | ALLOW | CONDITIONAL OWN_CLASS | DENY | eligibility/capacity/conflict | STANDARD/SENSITIVE | NOT_REQUIRED |
| OverrideCapacity | Pilates | DENY | DENY | DENY | DENY | prohibited: no overbooking | SENSITIVE on attempt | N/A |
| ReadClinicalSummary/Record | Clinical | CONDITIONAL only if clinical | DENY | CONDITIONAL | DENY | NORMAL_CLINICAL_ACCESS | CLINICAL | NOT_REQUIRED ordinary read |
| Create/FinalizeAssessment/Entry | Clinical | CONDITIONAL only if clinical | DENY | CONDITIONAL | DENY | care relationship + active professional | CLINICAL | NOT_REQUIRED |
| EditDraft | Clinical | CONDITIONAL only if author | DENY | CONDITIONAL | DENY | CLINICAL_DRAFT_AUTHOR | CLINICAL | NOT_REQUIRED |
| Rectify/Addendum | Clinical | CONDITIONAL | DENY | CONDITIONAL | DENY | author/alçada + reason | CLINICAL | RECOMMENDED |
| AttachClinicalDocument | Clinical | CONDITIONAL | DENY | CONDITIONAL | DENY | target access + DOCUMENT_CONTEXT_ACCESS | CLINICAL | NOT_REQUIRED |
| ExportClinicalRecord | Clinical | CONDITIONAL/DEFERRED | DENY | CONDITIONAL/DEFERRED | DENY | patient-specific + legal/RT policy | CLINICAL reforçado | REQUIRED_BEFORE_IMPLEMENTATION |
| UseBreakGlass | Clinical | CONDITIONAL only if clinical | DENY | CONDITIONAL | DENY | BREAK_GLASS_ELIGIBLE | CLINICAL reforçado | REQUIRED_BEFORE_IMPLEMENTATION |
| CreatePlan/PlanVersion | Plans | ALLOW | DENY | DENY | DENY | CLINIC | SENSITIVE | RECOMMENDED for price change |
| Contract/Enrollment lifecycle | Plans | ALLOW | ALLOW | DENY | DENY | UNIT_SCOPE/current state | SENSITIVE | NOT_REQUIRED |
| ViewBilling/Register/AllocatePayment | Billing | ALLOW | CONDITIONAL | DENY | DENY | FINANCIAL_SCOPE | FINANCIAL | NOT_REQUIRED |
| Adjustment/Negotiation/Restriction | Billing | CONDITIONAL | CONDITIONAL | DENY | DENY | FINANCIAL_SCOPE + alçada/reason | FINANCIAL | RECOMMENDED |
| ReversePayment/Refund | Billing | CONDITIONAL | CONDITIONAL | DENY | DENY | FINANCIAL_SCOPE + alçada/reason | FINANCIAL | REQUIRED_BEFORE_IMPLEMENTATION |
| ViewFinance/Register/PayExpense | Finance | ALLOW | CONDITIONAL | DENY | DENY | FINANCIAL_SCOPE | FINANCIAL | NOT_REQUIRED |
| Transfer/Reconcile | Finance | CONDITIONAL | CONDITIONAL | DENY | DENY | FINANCIAL_SCOPE + reason where adjustment | FINANCIAL | RECOMMENDED |
| CloseMonth | Finance | CONDITIONAL | CONDITIONAL | DENY | DENY | FINANCIAL_SCOPE | FINANCIAL | RECOMMENDED |
| ReopenClosing | Finance | CONDITIONAL | CONDITIONAL | DENY | DENY | FINANCIAL_SCOPE + reason | FINANCIAL | REQUIRED_BEFORE_IMPLEMENTATION |
| ManualMessage/Template/Delivery | Communication | ALLOW | ALLOW | CONDITIONAL approved clinical purpose | technical telemetry only | source purpose/scope | SENSITIVE | NOT_REQUIRED |
| Upload/Read/DownloadDocument | Documents | CONDITIONAL | CONDITIONAL | CONDITIONAL | DENY content | DOCUMENT_CONTEXT_ACCESS | inherited | inherited |
| ViewAudit/ViewSensitiveAudit | Privacy & Audit | CONDITIONAL | DENY | DENY | technical sanitized only | assigned audit scope | SENSITIVE | RECOMMENDED sensitive |
| Operational/Financial/ClinicalReport | Reports | CONDITIONAL | operational + conditional financial | clinical conditional | DENY business data | REPORT_SOURCE_SENSITIVITY | source level | inherited |

### 26.1 Default role grants summary

| Role | Permission families | Default | Conditions |
|---|---|---|---|
| OWNER_MANAGER | Organization, People, Patients, CRM, Scheduling, Pilates, Plans, Billing, Finance, IAM governance | ALLOW/CONDITIONAL | explicit scope; no Clinical inheritance |
| SECRETARY_RECEPTION | People, Patients, CRM, Scheduling, Pilates, Plans, routine Billing/Finance | ALLOW/CONDITIONAL | Unit/financial scope and alçadas; Clinical denied |
| PHYSIOTHERAPIST | own agenda/class, Attendance, patient minimum, Clinical actions | CONDITIONAL | active professional + own/assigned care context |
| DEVELOPER_IT | technical configuration/observability/support | CONDITIONAL | TECHNICAL_SUPPORT_SCOPE; business permissions denied |

## 27. State Transition Authorization

| Concept | Transition / operation | Allowed Actors | Policy | Audit |
|---|---|---|---|---|
| Opportunity | NEW→…→NEGOTIATION | Owner/Manager, Secretary | UNIT_SCOPE + CURRENT_RESOURCE_STATE | STANDARD/SENSITIVE by action |
| Opportunity | →LOST/DISQUALIFIED; LOST→prior | Owner/Manager, Secretary | reason + CURRENT_RESOURCE_STATE | SENSITIVE |
| Appointment | create/confirm/complete/no-show | Owner/Manager, Secretary; Physiotherapist own when grantado | conflict + OWN_APPOINTMENT/UNIT_SCOPE | STANDARD |
| Appointment | reschedule/cancel | Owner/Manager, Secretary; Physiotherapist deferred | conflict + reason + scope | SENSITIVE |
| ClassOccurrence | PLANNED→IN_PROGRESS→COMPLETED | assigned Physiotherapist; Owner/Secretary when operationally authorized | OWN_CLASS or UNIT_SCOPE; resolved call | STANDARD |
| ClassOccurrence | →CANCELLED / AssignSubstitute | Owner/Manager, Secretary, assigned professional when grantado | OWN_CLASS/UNIT_SCOPE + reason | SENSITIVE |
| Attendance | PENDING→result | assigned Physiotherapist; Secretary/Owner grantado | OWN_CLASS/UNIT_SCOPE | STANDARD |
| Attendance | result→corrected result | same actors with correction permission | reason + before/after | SENSITIVE |
| MakeupCredit | grant/reserve/consume/cancel | Owner/Manager, Secretary; Physiotherapist OWN_CLASS | eligibility/capacity/conflict/current state | STANDARD/SENSITIVE |
| CareEpisode | OPEN↔PAUSED→CLOSED | authorized Physiotherapist | CLINICAL_CARE_RELATIONSHIP + reason where required | CLINICAL |
| Assessment | DRAFT→FINALIZED | author Physiotherapist or explicit clinical authority | active professional + author/care policy | CLINICAL |
| ClinicalEntry | DRAFT→FINALIZED | author Physiotherapist or explicit clinical authority | active professional + author/care policy | CLINICAL |
| Rectification/Addendum | append to FINALIZED | author or explicit clinical authority | NORMAL_CLINICAL_ACCESS + reason/alçada | CLINICAL |
| Contract | DRAFT→ACTIVE | Owner/Manager, Secretary | UNIT_SCOPE + complete snapshot | SENSITIVE |
| Contract | ACTIVE→CANCELLED/COMPLETED | Owner/Manager, Secretary | state + reason on cancel | SENSITIVE/STANDARD |
| Enrollment | DRAFT→ACTIVE↔PAUSED→terminal | Owner/Manager, Secretary | state, duration, availability, reason | SENSITIVE |
| Receivable | OPEN↔payment states/CANCELLED | financial actor | FINANCIAL_SCOPE + guards; cancel alçada | FINANCIAL |
| Payment | PENDING→CONFIRMED; →reversed | financial actor | FINANCIAL_SCOPE; reversal reason/alçada | FINANCIAL |
| Refund | ISSUED→COMPLETED/CANCELLED | financial actor | FINANCIAL_SCOPE + eligibility/alçada/reason | FINANCIAL |
| FinancialRestriction | create ACTIVE→REMOVED | financial actor/system policy | base + FINANCIAL_SCOPE + reason | FINANCIAL |
| Expense | OPEN→PAID/CANCELLED | Owner/Manager, Secretary grantado | FINANCIAL_SCOPE + reason on cancel | FINANCIAL |
| Closing | OPEN/REOPENED→CLOSED | Owner/Manager, Secretary grantado | FINANCIAL_SCOPE + cutoff | FINANCIAL |
| Closing | CLOSED→REOPENED | Owner/Manager, Secretary grantado | permission específica + reason + step-up | FINANCIAL |
| PatientProfile | ACTIVE↔INACTIVE | Owner/Manager, Secretary | UNIT_SCOPE + reason on deactivate | SENSITIVE |
| ProfessionalProfile | ACTIVE↔INACTIVE | Owner/Manager | CLINIC + reason; Secretary only explicit grant | SENSITIVE |

## 28. Resource-Based Policies

Cada policy abaixo é avaliada pelo owner do recurso no instante da ação.

### `OWN_CLASS`

- **Purpose:** limitar a operação do fisioterapeuta às turmas/occurrences que conduz.
- **Inputs:** actor ProfessionalId, ClassSchedule vigente, planned/actual professional, SubstituteAssigned, instante, Unit.
- **Allow:** atribuição vigente ou substituição explícita e vínculo profissional ativo.
- **Deny:** autoria passada, vínculo encerrado, leave impeditivo, outra Class ou scope expirado.
- **Owner:** Pilates; consulta Staff.
- **Audit:** STANDARD; SENSITIVE para membership, correction, cancel/substitute.

### `OWN_APPOINTMENT`

- **Purpose:** limitar agenda própria e contexto clínico.
- **Inputs:** actor, assigned ProfessionalId, status, intervalo, substituição/delegação.
- **Allow:** atribuição atual e ação permitida pelo estado/grant.
- **Deny:** outro profissional, terminal incompatível, vínculo inativo.
- **Owner:** Scheduling.
- **Audit:** conforme ação.

### `ASSIGNED_PATIENT` / `CLINICAL_CARE_RELATIONSHIP`

- **Purpose:** provar necessidade assistencial atual.
- **Inputs:** role/permission clínica, ProfessionalProfile/Employment, patient, Appointment, ClassOccurrence, CareEpisode, continuidade, substituição e tempo.
- **Allow:** pelo menos uma relação legítima vigente e finalidade compatível; continuidade deve ser documentável.
- **Deny:** mera curiosidade, autoria histórica, cadastro administrativo, propriedade da clínica, evento recebido ou vínculo encerrado.
- **Owner:** Clinical, com fatos públicos de Staff/Scheduling/Pilates.
- **Audit:** CLINICAL_AUDIT em read sensível e mutações.

### `ACTIVE_PROFESSIONAL_RELATIONSHIP`

- **Purpose:** impedir atuação após inativação/desligamento.
- **Inputs:** ProfessionalProfile, EmploymentLink, UnitLink, Leave e instante.
- **Allow:** perfil/vínculo aplicável ativos e sem impedimento vigente.
- **Deny:** vínculo terminado/inativo; autoria histórica não excepciona.
- **Owner:** Staff para fatos; consumidor decide a ação.
- **Audit:** deny/mudança sensível correlacionada ao lifecycle.

### `UNIT_SCOPE`

- **Purpose:** restringir operação administrativa ao conjunto institucional concedido.
- **Inputs:** Unit grants vigentes, resource Unit, multi-unit configuration.
- **Allow:** Unit está no conjunto explícito ou grant `CLINIC` cobre a ação.
- **Deny:** Unit ausente, inativa para novo uso ou não concedida.
- **Owner:** Identity mantém grant; resource owner valida Unit.
- **Audit:** ação subjacente.

### `FINANCIAL_SCOPE`

- **Purpose:** segregar Billing/Finance por ação, conta e escopo institucional.
- **Inputs:** permission, contas/categorias/Units concedidas, resource owner, alçada.
- **Allow:** todos coincidem e operação está dentro da alçada aprovada.
- **Deny:** papel técnico, role sem grant, conta/Unit fora do escopo ou alçada desconhecida.
- **Owner:** Billing/Finance para recurso; Identity para grant.
- **Audit:** FINANCIAL_AUDIT.

### `OWNER_APPROVAL_REQUIRED`

- **Purpose:** separar solicitação e aprovação de merge sensível.
- **Inputs:** classificação, requester, approver, conflitos e motivo.
- **Allow:** requester autorizado, Owner/Manager aprovador autorizado, e quando exigido atores distintos.
- **Deny:** self-approval quando segregação exigida, ausência de motivo ou classificação segura não comprovada.
- **Owner:** People.
- **Audit:** SENSITIVE_AUDIT.

### `BREAK_GLASS_ELIGIBLE`

- **Purpose:** permitir acesso clínico excepcional e limitado.
- **Inputs:** permission própria, clinical eligibility, patient/resource scope, reason, justification, confirmation, step-up, expiry.
- **Allow:** todos presentes e revalidados no uso.
- **Deny:** papel administrativo/técnico, justificativa ausente, escopo amplo ou grant expirado.
- **Owner:** Clinical; Privacy & Audit registra evidência.
- **Audit:** CLINICAL_AUDIT reforçado e revisão.

### `DOCUMENT_CONTEXT_ACCESS`

- **Purpose:** impedir storage universal.
- **Inputs:** owner context/reference, classification, requested action, owner authorization result.
- **Allow:** owner permite a mesma ação sobre o recurso e retenção não bloqueia.
- **Deny:** reference ausente, owner deny, Clinical sem policy ou tentativa de bypass por Documents role.
- **Owner:** business context para semântica; Documents para arquivo.
- **Audit:** herdado do owner.

### `NO_SELF_PRIVILEGE_ESCALATION`

- **Purpose:** impedir elevação própria e circular.
- **Inputs:** actor, target, before/after roles/permissions/scopes, delegação.
- **Allow:** ator autorizado, target distinto quando necessário e delta dentro da autoridade delegada.
- **Deny:** benefício próprio, concessão acima da própria delegação ou cadeia que resulte em autoelevação.
- **Owner:** Identity & Access.
- **Audit:** SENSITIVE_AUDIT; step-up requerido antes da implementação.

## 29. Sensitive Action Catalog

| Action | Sensitivity | Reason Required | Approval | Step-up | Audit |
|---|---|---:|---|---|---|
| Assign/RevokeRole/Permission | SECURITY_SENSITIVE | sim | authorized IAM governance | REQUIRED_BEFORE_IMPLEMENTATION | SENSITIVE |
| DisableAccount/TerminateOtherSessions | SECURITY_SENSITIVE | sim | não inventada | RECOMMENDED | SENSITIVE |
| SensitivePersonMerge | SECURITY_SENSITIVE | sim | Owner/Manager | REQUIRED_BEFORE_IMPLEMENTATION | SENSITIVE |
| PatientDeactivate/PayerChange | SENSITIVE | sim | não inventada | NOT_REQUIRED | SENSITIVE |
| EmploymentEnd/ProfessionalInactivate | SENSITIVE | sim | não inventada | RECOMMENDED | SENSITIVE |
| OpportunityLost/Disqualified/Reactivated | SENSITIVE | sim | não inventada | NOT_REQUIRED | SENSITIVE |
| AppointmentReschedule/Cancel | SENSITIVE | sim | não inventada | NOT_REQUIRED | SENSITIVE |
| OccurrenceCancel/Substitute | SENSITIVE | sim | não inventada | NOT_REQUIRED | SENSITIVE |
| AttendanceCorrection | SENSITIVE | sim | alçada futura para exceções | NOT_REQUIRED | SENSITIVE |
| ClinicalFinalize | CLINICAL | condicional se tardia | não inventada | NOT_REQUIRED | CLINICAL |
| ClinicalRectification | CLINICAL | sim | clinical authority policy | RECOMMENDED | CLINICAL |
| BreakGlass | SECURITY_SENSITIVE | sim + justification | policy eligibility | REQUIRED_BEFORE_IMPLEMENTATION | CLINICAL reforçado |
| ClinicalExport | SECURITY_SENSITIVE | sim | legal/RT workflow deferred | REQUIRED_BEFORE_IMPLEMENTATION | CLINICAL reforçado |
| Contract/EnrollmentCancel/Pause | SENSITIVE | sim | não inventada | NOT_REQUIRED | SENSITIVE |
| ManualBillingAdjustment | FINANCIAL | sim | alçada deferred | RECOMMENDED | FINANCIAL |
| PaymentReversal | FINANCIAL | sim | alçada deferred | REQUIRED_BEFORE_IMPLEMENTATION | FINANCIAL |
| RefundIssue/Complete/Cancel | FINANCIAL | sim em issue/cancel | alçada deferred | REQUIRED_BEFORE_IMPLEMENTATION | FINANCIAL |
| Negotiation/Discount | FINANCIAL | sim | alçada deferred | RECOMMENDED | FINANCIAL |
| RestrictionApply/Remove | FINANCIAL | sim | não inventada | RECOMMENDED | FINANCIAL |
| ExpenseCancel/TransferMoney | FINANCIAL | sim | alçada deferred | RECOMMENDED | FINANCIAL |
| ReconciliationAdjustment | FINANCIAL | sim | não inventada | RECOMMENDED | FINANCIAL |
| CloseMonth | FINANCIAL | conforme pendência/policy | não inventada | RECOMMENDED | FINANCIAL |
| ReopenClosing | FINANCIAL | sim | permission própria | REQUIRED_BEFORE_IMPLEMENTATION | FINANCIAL |
| ViewSensitiveAudit | SECURITY_SENSITIVE | finalidade | assigned audit scope | RECOMMENDED | SENSITIVE |

## 30. Step-Up Candidates

| Classification | Actions |
|---|---|
| `REQUIRED_BEFORE_IMPLEMENTATION` | break-glass, clinical export, role/permission change, sensitive merge, payment reversal, refund, Closing reopen |
| `RECOMMENDED` | disable account/terminate others, rectify clinical record, price/PlanVersion publication, manual billing adjustment, negotiation/discount, restriction changes, transfer/reconciliation, CloseMonth, sensitive audit |
| `NOT_REQUIRED` | rotina administrativa/agenda/chamada/drafts dentro de scope, sem excluir auditoria normal |

“Required before implementation” significa que a arquitetura deve definir e testar o mecanismo concreto antes de liberar a ação; não seleciona MFA/provider nesta tarefa.

## 31. Reason Requirements

Reason é obrigatório para merge, inativação relevante, loss/disqualify/reactivate, reschedule/cancel, correction, clinical pause/close quando aplicável, rectification, break-glass, export, cancel/pause de Contract/Enrollment, BillingAdjustment, reversal, refund issue/cancel, restriction apply/remove, Expense cancel, reconciliation, transfer quando policy exigir, Closing reopen e mudanças de role/permission. Texto livre sensível não deve ser copiado para evento/log genérico; use code/reference + armazenamento protegido quando necessário.

## 32. Audit Requirements

| Action family | Audit Level | Minimum evidence |
|---|---|---|
| routine reads/writes | STANDARD_AUDIT | actor/process, action, resource, result, timestamp, correlation, scope |
| merge, identity, state exception, admin changes | SENSITIVE_AUDIT | STANDARD + reason, relevant before/after, approval/step-up outcome |
| clinical read/write/finalize/correct/export/break-glass | CLINICAL_AUDIT | minimized patient/resource, professional/actor, purpose, record version, reason/result; never clinical payload in generic log |
| Billing/Finance mutations | FINANCIAL_AUDIT | amount/account/source/cutoff, before/after derived balance where relevant, actor, reason, idempotency correlation |

Audit records are not business-editable. Denied sensitive attempts, self-escalation, cross-scope access and document/report bypass attempts must be captured proportionally without logging secrets or sensitive payloads.

## 33. Negative Authorization Rules

- `AUTH-DENY-001`: Secretary cannot read a full clinical record or create/correct/finalize clinical content by default.
- `AUTH-DENY-002`: Developer/IT role grants no Clinical access.
- `AUTH-DENY-003`: Developer/IT role grants no Billing or Finance authority.
- `AUTH-DENY-004`: Owner/Manager role alone grants no Clinical access.
- `AUTH-DENY-005`: Physiotherapist has no Billing, Finance, IAM governance or commercial pipeline authority by default.
- `AUTH-DENY-006`: no actor can grant, approve or indirectly obtain their own privilege escalation.
- `AUTH-DENY-007`: Reports cannot expose data beyond source permissions/scopes.
- `AUTH-DENY-008`: Documents permission cannot bypass the owner context, especially Clinical.
- `AUTH-DENY-009`: historical authorship does not grant current Clinical access after professional termination.
- `AUTH-DENY-010`: FINALIZED Clinical records cannot be edited or returned to DRAFT by any role.
- `AUTH-DENY-011`: confirmed Payment, FinancialTransaction, Attendance history, Contract and ClosingSnapshot cannot be deleted as ordinary authorization.
- `AUTH-DENY-012`: event consumption, read-model possession and reference by ID grant no command authority.
- `AUTH-DENY-013`: service identities and n8n receive no global permissions.
- `AUTH-DENY-014`: overbooking/OverrideCapacity is prohibited in the initial model.
- `AUTH-DENY-015`: administrative impersonation/masquerading is `REJECTED FOR MVP`; silent “login as user” is forbidden.
- `AUTH-DENY-016`: a cross-context consumer cannot apply its own role to override the resource owner's policy.
- `AUTH-DENY-017`: disabled/inactive accounts and ended professional relationships cannot authorize new operational actions.
- `AUTH-DENY-018`: stale sessions/claims cannot indefinitely preserve a critically revoked grant.
- `AUTH-DENY-019`: unrestricted “any authenticated user” access is forbidden for business-sensitive actions.
- `AUTH-DENY-020`: unknown/deferred alçada never defaults to approval.

## 34. Authorization Invariants

- `AUTH-INV-001`: every action is denied unless an explicit permission and all applicable policies allow it.
- `AUTH-INV-002`: roles are capability bundles, not sufficient authorization for a resource.
- `AUTH-INV-003`: explicit deny, boundary and invalid state override combined role grants.
- `AUTH-INV-004`: role combination never transfers Clinical/Financial/Technical scopes between roles.
- `AUTH-INV-005`: authorization is evaluated at request/use time against current account, grant, resource and relationship state.
- `AUTH-INV-006`: only the owner context authorizes mutation of its resource.
- `AUTH-INV-007`: Clinical requires clinical permission, active professional eligibility and care context.
- `AUTH-INV-008`: professional termination removes operational access but preserves authorship.
- `AUTH-INV-009`: historical authorship alone is never current authorization.
- `AUTH-INV-010`: technical administration is distinct from business administration.
- `AUTH-INV-011`: resource sensitivity follows the source into Documents, Reports, events and projections.
- `AUTH-INV-012`: sensitive actions preserve actor/process, scope, result, time, correlation and reason/approval/step-up when required.
- `AUTH-INV-013`: request permission and approval permission are distinct.
- `AUTH-INV-014`: no self-privilege escalation, including indirect role/scope chains.
- `AUTH-INV-015`: disabling an account removes access; critical revocation cannot depend indefinitely on stale session state.
- `AUTH-INV-016`: read permission does not imply export, mutation, rectification, reversal or approval.
- `AUTH-INV-017`: event consumption does not grant command authority.
- `AUTH-INV-018`: service identities use explicit purpose-bound least privilege.
- `AUTH-INV-019`: a permission is valid only in the granted Unit/Clinic/financial/clinical/technical scope and vigência.
- `AUTH-INV-020`: deferred policy produces deny for implementation, never permissive fallback.

## 35. Cross-Context Authorization

The protected resource owner returns or enforces the authoritative decision. Identity supplies actor/grants; it does not absorb eligibility rules. A caller passes actor, permission intent and resource/context references; the owner evaluates state, relationship and scope. Cross-context contracts expose only the minimal decision/facts needed.

Examples:

- Documents receives a download request for a Clinical attachment and requires a Clinical authorization result for that patient/link/version.
- Reports composes Billing and Finance data only for the intersection of both permitted scopes.
- Clinical validates Appointment/ClassOccurrence and Professional status, but Scheduling/Pilates/Staff do not grant Clinical access themselves.
- Finance consuming `PaymentConfirmed` may create its own idempotent movement as a system reaction, but no human/technical role thereby gains `billing.payment.register`.

## 36. Event Consumption Rules

`EVENT_CONSUMPTION_DOES_NOT_GRANT_COMMAND_AUTHORITY` is normative. A consumer identity is authorized only for the declared reaction, on its own resources, with least data and idempotent correlation. It cannot reuse event payload or producer trust to execute a user command, change the publisher aggregate or broaden human access. Replays and failures preserve the same policy.

## 37. Document Access Rules

1. `DOCUMENT_ACCESS_INHERITS_BUSINESS_CONTEXT_AUTHORIZATION`.
2. DocumentId/storage handle is not a capability token.
3. Upload requires an authorized owner target and classification.
4. Read/download revalidates owner access; cached URLs or projections cannot outlive authorization.
5. Removal is logical and limited to allowed draft/retention state; finalized Clinical links and legal hold cannot be bypassed.
6. Logs expose no file content, public URL, credential or unnecessary metadata.

## 38. Read Model Access Rules

Read models preserve source sensitivity, field minimization, scope and purpose. Projection ownership grants rebuild/maintenance, not business-data access. Freshness must not be treated as authorization; sensitive decisions revalidate current source policy. Operational, financial and clinical projections remain separately authorized, and export of a projection inherits all source restrictions.

## 39. Process Coverage

| Process | Actor(s) | Required Permissions | Policies | Coverage |
|---|---|---|---|---|
| PROC-PPL-001 Create person | Secretary, Owner | people.person.create | UNIT_SCOPE | SUPPORTED |
| PROC-PAC-001 Register patient | Secretary, Owner | patients.profile.create + People read/create | UNIT_SCOPE | SUPPORTED |
| PROC-AGD-001 Manage fixed schedule | Secretary, Owner | scheduling block/rule operation | UNIT_SCOPE + conflict | SUPPORTED |
| PROC-AGD-002 Ad-hoc appointment | Secretary, Owner; Physiotherapist conditional | scheduling.appointment.schedule | UNIT_SCOPE/OWN_APPOINTMENT + conflict | SUPPORTED |
| PROC-PIL-001 Create class | Secretary, Owner | pilates.class.create + schedule_change | UNIT_SCOPE + conflict/capacity | SUPPORTED |
| PROC-PIL-002 Add patient | Secretary, Owner; Physiotherapist | pilates.membership.add | UNIT_SCOPE/OWN_CLASS + eligibility/conflict/capacity | SUPPORTED |
| PROC-PIL-003 Transfer patient | Secretary, Owner; Physiotherapist conditional | membership.end + add/transfer | access to both classes + guards | SUPPORTED |
| PROC-PIL-004 Attendance | Physiotherapist; Secretary/Owner grantado | attendance.record/correct | OWN_CLASS/UNIT_SCOPE | SUPPORTED |
| PROC-PIL-005 Makeup | Secretary, Owner; Physiotherapist conditional | makeup.grant/reserve/cancel | eligibility/capacity/conflict | PARTIAL — pause/ad-hoc policy open |
| PROC-CLI-001 Open episode | Physiotherapist | clinical.episode.open | NORMAL_CLINICAL_ACCESS | SUPPORTED |
| PROC-CLI-002 Assessment | Physiotherapist | assessment create/edit/finalize | care relationship + author | SUPPORTED |
| PROC-CLI-003 Clinical entry | Physiotherapist | entry.create/edit_draft | care relationship + author | SUPPORTED |
| PROC-CLI-004 Finalize entry | Physiotherapist author | entry.finalize | author + state + clinical policy | SUPPORTED |
| PROC-CLI-005 Rectify record | author/clinical authority | entry.rectify | care relationship/alçada + reason | PARTIAL — RT/alçada open |
| PROC-PLN-001 Contract plan | Secretary, Owner | contract.create/accept | UNIT_SCOPE + published PlanVersion | SUPPORTED |
| PROC-ENR-001 Activate enrollment | Secretary, Owner | enrollment.activate | state/current Contract | SUPPORTED |
| PROC-ENR-002 Pause enrollment | Secretary, Owner | enrollment.pause | state + ≤15d + reason | SUPPORTED |
| PROC-ENR-003 Resume enrollment | Secretary, Owner | enrollment.resume | availability + state | SUPPORTED |
| PROC-ENR-004 Cancel enrollment | Secretary, Owner | enrollment.cancel | state + reason | PARTIAL — overdue treatment open |
| PROC-ENR-005 Renew contract | Secretary, Owner | contract.renew/create/accept | PlanVersion/current state | SUPPORTED |
| PROC-ENR-006 Change frequency | Secretary, Owner | enrollment.frequency_change | vigência + scope | SUPPORTED |
| PROC-ENR-007 Change unit/class | Secretary, Owner | enrollment update + Pilates transfer | both owner policies | SUPPORTED |
| PROC-BIL-001 Generate receivables | authorized workflow/system identity | billing.receivables.generate | Contract correlation/least privilege | SUPPORTED |
| PROC-BIL-002 Register payment | Secretary, Owner | billing.payment.register | FINANCIAL_SCOPE | SUPPORTED |
| PROC-BIL-003 Partial payment | Secretary, Owner | payment.register + allocate | FINANCIAL_SCOPE/balance | SUPPORTED |
| PROC-BIL-004 Advance installments | Secretary, Owner | payment.allocate + possible adjust | FINANCIAL_SCOPE + discount alçada | PARTIAL |
| PROC-BIL-005 Negotiate debt | Secretary, Owner | billing.negotiation.create | FINANCIAL_SCOPE + deferred alçada | AUTH_GAP |
| PROC-BIL-006 Reverse payment | authorized financial actor | billing.payment.reverse | FINANCIAL_SCOPE + reason/step-up/alçada | PARTIAL |
| PROC-BIL-007 Refund | authorized financial actor | billing.refund.issue/complete | eligibility + reason/step-up/alçada | PARTIAL |
| PROC-BIL-008 Apply restriction | system/financial actor | billing.restriction.apply | policy/base + FINANCIAL_SCOPE | SUPPORTED |
| PROC-BIL-009 Remove restriction | system/financial actor | billing.restriction.remove | regularization/rule + reason | SUPPORTED |
| PROC-FIN-001 Register expense | Secretary, Owner | finance.expense.register | FINANCIAL_SCOPE | SUPPORTED |
| PROC-FIN-002 Pay expense | Secretary, Owner | finance.expense.pay | FINANCIAL_SCOPE/account | SUPPORTED |
| PROC-FIN-003 Transfer money | authorized financial actor | finance.money.transfer | FINANCIAL_SCOPE + alçada | PARTIAL |
| PROC-FIN-004 Reconcile account | authorized financial actor | finance.account.reconcile | FINANCIAL_SCOPE + reason | SUPPORTED |
| PROC-FIN-005 Close month | Secretary/Owner authorized | finance.closing.close | FINANCIAL_SCOPE + cutoff | SUPPORTED |
| PROC-FIN-006 Reopen closing | Secretary/Owner authorized | finance.closing.reopen | FINANCIAL_SCOPE + reason/step-up | SUPPORTED conceptually |

Coverage: 29 `SUPPORTED`, 7 `PARTIAL`, 1 `AUTH_GAP`. `PARTIAL/AUTH_GAP` do not block ARC-003 because boundaries and required policy points are explicit; they block the affected implementation.

## 40. Rule Coverage

| Rules crossed | Count | Authorization representation | Coverage |
|---|---:|---|---|
| RB-PPL-001..004 | 4 | central Person, role separation, merge request/approval/audit | SUPPORTED |
| RB-PAC-001..004 | 4 | administrative-only profile, distinct persons/roles, no hard delete | SUPPORTED |
| RB-CRM-001..007 | 7 | pipeline permissions, human loss, reason, next action, conversion guards | SUPPORTED |
| RB-AGD-001..009 | 9 | scoped schedule actions, conflict/state policy and preserved history | SUPPORTED |
| RB-PIL-001..010 | 10 | OWN_CLASS, capacity deny, temporal membership, attendance/makeup policies | SUPPORTED; exception alçadas deferred |
| RB-CLI-001..008 | 8 | clinical segregation, care relationship, immutable finalization, break-glass/export | SUPPORTED conceptually |
| RB-PLN-001..003 | 3 | Owner PlanVersion authority and contract snapshot/renewal actions | SUPPORTED |
| RB-ENR-001..007 | 7 | scoped transitions, reason, availability and cross-owner policies | SUPPORTED |
| RB-BIL-001..012 | 12 | separate payment actions, financial scope, no delete, reason/audit/alçada | SUPPORTED; monetary alçadas deferred |
| RB-FIN-001..007 | 7 | separate financial permissions, derived balance, closing/reopen and no Commission | SUPPORTED |
| RB-SEC-001..002 | 2 | deny-by-default/contextual; technical authority segregated | SUPPORTED |
| RB-AUD-001 | 1 | four audit levels and sensitive catalog | SUPPORTED |
| RB-COM-001 | 1 | communication executes only approved owner intent | SUPPORTED |
| **Total** | **75** | all rules reviewed | **75 SUPPORTED / 0 AUTH_RULE_CONFLICT** |

Special checks: actor/access rules do not contradict process actors; historical modifications use correction/reversal/end/reopen; merge simple versus sensitive is preserved; Clinical and Finance remain segregated. Open alçadas are represented as conditional/deferred, never inferred.

## 41. Privacy / Least Privilege Review

| Check | Result | Evidence |
|---|---|---|
| minimum necessary access | PASS | explicit permission + scope + resource policy |
| Clinical segregation | PASS | NORMAL_CLINICAL_ACCESS and explicit denies |
| Finance segregation | PASS | FINANCIAL_SCOPE; Physiotherapist/Developer denied |
| technical role segregation | PASS | TECHNICAL_SUPPORT_SCOPE; no business inheritance |
| read models preserve privilege | PASS | REPORT_SOURCE_SENSITIVITY |
| export cannot bypass read | PASS | separate clinical export permission/policy |
| storage cannot bypass owner | PASS | DOCUMENT_CONTEXT_ACCESS |
| logs avoid sensitive payloads | PASS conceptually | audit minimization rules |
| role combination safety | PASS | explicit deny/resource policy override union |
| field-level future need | IDENTIFIED | clinical, financial, civil and credentials; no permission explosion |

## 42. Auth Gaps

| ID | Gap | Classification | Safe default |
|---|---|---|---|
| AUTH-GAP-001 | discount/negotiation limits and approval bands | BLOCKING BEFORE IMPLEMENTATION | deny action beyond routine payment/allocation |
| AUTH-GAP-002 | refund/reversal thresholds and approvers | BLOCKING BEFORE IMPLEMENTATION | permission alone is insufficient |
| AUTH-GAP-003 | Secretary/roles default Unit versus multi-unit assignments | BLOCKING BEFORE IMPLEMENTATION | explicit Unit list only |
| AUTH-GAP-004 | RT qualification and clinical alçadas | BLOCKING BEFORE IMPLEMENTATION; GO-LIVE regulatory | no RT-derived privilege |
| AUTH-GAP-005 | clinical export requester/executor/disclosure/minors | BLOCKING BEFORE IMPLEMENTATION / GO-LIVE | deny export |
| AUTH-GAP-006 | concrete step-up/session/revocation mechanism | BLOCKING BEFORE IMPLEMENTATION | sensitive candidate disabled |
| AUTH-GAP-007 | privacy processing roles and lifecycle | BLOCKING BEFORE GO-LIVE | create request only; processing denied |
| AUTH-GAP-008 | service identity grants | BLOCKING BEFORE ARCHITECTURE/IMPLEMENTATION | no global automation account |
| AUTH-GAP-009 | professional reschedule/cancel/fit-in alçada | BLOCKING BEFORE IMPLEMENTATION | deny beyond explicitly assigned routine result actions |
| AUTH-GAP-010 | Attendance/Makeup exception alçadas | BLOCKING BEFORE IMPLEMENTATION | ordinary rules only; no override |
| AUTH-GAP-011 | support access to production/business data | BLOCKING BEFORE GO-LIVE | sanitized observability only |
| AUTH-GAP-012 | correction of terminal Appointment result | BLOCKING BEFORE IMPLEMENTATION | no overwrite/reopen |

## 43. Auth Rule Conflicts

No `AUTH_RULE_CONFLICT` was found. DEC-025 allows Secretary and Owner/Manager to close/reopen **when granted**; it does not mandate a default grant. DEC-024 and RB-CLI-005 keep Owner/Manager and Developer/IT outside Clinical without clinical authorization. Process actors remain compatible with conditional permissions and do not create entitlement merely by being named.

## 44. Ambiguities Resolved

1. Four operational roles are sufficient; RT is not invented as a global role.
2. Multiple roles combine grants without transferring scopes or defeating explicit denies.
3. `OWN_CLASS` and `OWN_APPOINTMENT` are resource policies, evaluated by current assignments.
4. Clinical access requires active professional relationship plus care relationship; role alone is insufficient.
5. Historical authorship survives but access does not.
6. Owner/Manager financial breadth still requires explicit financial permissions/scopes; Developer/IT never inherits it.
7. Secretary may complete a safe simple merge; sensitive merge separates request and approval.
8. Closing/reopen permissions may be assigned to Secretary/Owner per DEC-025; reopen remains separately sensitive.
9. No capacity override permission exists.
10. Documents and Reports inherit source authorization rather than broad local read grants.
11. Event consumers act only on declared local reactions and gain no human command authority.
12. Impersonation is rejected for MVP; support uses scoped technical access instead.

## 45. Remaining Ambiguities

- exact direct permissions versus role bundle composition;
- default Unit assignments and multi-unit operation;
- clinical continuity window, substitution reach and RT alçadas;
- whether non-author clinical authority can finalize another professional's DRAFT (default deny);
- concrete step-up strength, validity and recovery behavior;
- thresholds/approvers for discount, negotiation, reversal, refund, transfer and closing;
- privacy operator/auditor segregation;
- exact field minimization for PatientClinicalSummary and administrative patient view;
- lifecycle/vigência of temporary exceptional grants and production support.

All remain deny/deferred at the affected edge and do not alter boundaries required by ARC-003.

## 46. Open Questions

### BLOCKING BEFORE ARC-003

None.

### BLOCKING BEFORE IMPLEMENTATION

- define Unit/multi-unit grants and default assignments;
- define RT/professional attributes and clinical alçadas;
- define discount, negotiation, reversal, refund and transfer thresholds/approvals;
- finalize clinical export workflow and step-up/session/revocation architecture;
- define professional self-service limits for agenda, Attendance/Makeup exceptions and draft delegation;
- define service identities and event-consumer permissions by purpose;
- close remaining domain precedence issues already identified in STATE-001.

### BLOCKING BEFORE GO-LIVE

- validate clinical export, minors, RT, retention and regulatory access;
- complete LGPD inventory, privacy roles, legal hold and sensitive audit access;
- define and test privileged support, incident response and critical revocation.

### NON_BLOCKING

- final catalogs of reason codes, report definitions and technical observability views;
- provider-specific step-up and authentication UX, once architecture requirements are approved.

## 47. Consequences for Physical Architecture

ARC-003 must provide a policy enforcement boundary per owner context, a current authorization decision contract, explicit permission/scope inputs, resource facts without cross-table bypass, separate sensitive audit channels, revocation propagation, and purpose-bound service identities. It must preserve resource policies for Clinical, Financial, Documents and Reports; keep Identity from becoming a business-rule god module; and keep `REQUIRES_APPROVAL/STEP_UP` as explicit workflow outcomes.

No IAM technology is selected. Session lifetime, claim shape, policy handler, storage model, MFA provider and token strategy remain future decisions.

## 48. Validation Criteria

- [x] deny by default is normative;
- [x] roles remain limited to four operational roles; RT is safely deferred;
- [x] permissions represent business actions rather than artificial CRUD;
- [x] scopes and resource policies are explicit;
- [x] Owner/Manager and Developer/IT receive no implicit Clinical;
- [x] Developer/IT receives no implicit Billing/Finance;
- [x] Secretary receives no full clinical record;
- [x] Physiotherapist receives no Finance;
- [x] own/assigned resources and current care context are represented;
- [x] self-privilege escalation is prohibited;
- [x] Documents and Reports preserve source sensitivity;
- [x] event consumption grants no command authority;
- [x] sensitive actions have reason/audit/step-up classifications;
- [x] break-glass and clinical export are separate and protected;
- [x] role/permission assignment is protected;
- [x] historical authorship differs from current authorization;
- [x] state transitions, 37 processes and 75 rules were crossed;
- [x] no AUTH_RULE_CONFLICT was found;
- [x] least privilege review passed;
- [x] no IAM technology or implementation was selected;
- [x] no blocker prevents ARC-003.

**Result:** AUTH-001 — PASS. **ARC-003 — READY.**
