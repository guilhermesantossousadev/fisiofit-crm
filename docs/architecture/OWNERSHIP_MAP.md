# Fisiofit CRM 2.0 — Ownership Map

## 1. Status

- **Tarefa:** ARC-002
- **Status:** DONE
- **Data:** 2026-09-15
- **Baseline de origem:** `discovery-v1` / Gate M1
- **Fonte arquitetural:** `docs/architecture/CONTEXT_MAP.md` (ARC-001)
- **Direção arquitetural:** monólito modular

Este documento define ownership conceitual e contratos entre contextos. Não define tabelas, chaves físicas, DTOs, APIs, transações distribuídas, mensageria ou arquitetura de deploy.

## 2. Objetivo

Fixar, para cada conceito relevante, um único contexto com autoridade de escrita; distinguir referências, projeções, consumidores de eventos e snapshots; proibir escrita cross-context implícita; e preparar o terreno para `MODEL-001` sem redefinir os boundaries do ARC-001.

## 3. Definição de ownership

- **TRANSACTIONAL_OWNER:** único contexto que cria e governa o estado original, suas transições, correções, inativação e histórico.
- **REFERENCE_ONLY:** contexto consumidor guarda apenas um identificador conceitual opaco. A referência não concede escrita nem navegação ao storage do owner.
- **READ_MODEL_CONSUMER:** contexto lê projeção específica, minimizada e reconstruível. A projeção não substitui a fonte transacional.
- **EVENT_CONSUMER:** contexto reage a fato já decidido pelo publisher. A reação cria ou altera apenas dados que o consumidor possui.
- **SNAPSHOT_OWNER:** contexto possui cópia histórica deliberada de atributos necessários para provar um fato passado. O snapshot não acompanha mudanças posteriores do dado original.

Um dado transacional possui exatamente um owner. Contrato público, referência por ID, projeção, evento e snapshot não criam ownership compartilhado.

## 4. Regras de ownership

1. Somente o owner escreve seu estado transacional. Outro contexto solicita a operação por contrato público ou reage a evento.
2. O owner do fato também é o publisher autoritativo do fato público; um consumidor não republica o fato como se fosse seu.
3. IDs externos são referências conceituais; formato físico de ID será decidido depois.
4. Snapshots preservam o acordo ou fato histórico e normalmente não são atualizados quando a fonte muda.
5. Inativação, cancelamento, vigência, reversão, retificação e versionamento preservam histórico; hard delete operacional não é o mecanismo normal.
6. Reports não escreve fatos transacionais. Communication não decide a regra que originou uma mensagem.
7. Documents possui arquivo e metadados técnicos; o domínio originador possui vínculo, significado e autorização de negócio.
8. Privacy & Audit possui a evidência de auditoria e o workflow de privacidade, não o fato auditado nem a mutação clandestina de outro contexto.
9. Identity & Access possui conta, credencial, sessão, role e permission; não possui Person nem recursos protegidos.
10. Clinical é segregado, deny-by-default, e único owner de registros clínicos.
11. Billing possui obrigação/liquidação do cliente. Finance possui conta e movimento financeiro. `Payment != FinancialTransaction`.
12. Scheduling possui Appointment e conflito temporal geral; Pilates possui turma, recorrência, ocorrência, capacidade, presença e reposição.
13. Frontend, n8n, providers e storage externo nunca são owners de domínio.
14. Schemas públicos de eventos são contratos do contexto publisher; envelope técnico transversal será definido em ARC-007.

## 5. Ownership por contexto

### Identity & Access

- **Owns:** UserAccount, Role, Permission, RoleAssignment, Session, AuthenticationFactor e RecoveryToken.
- **May reference:** PersonId; ProfessionalId e UnitId somente quando necessários a políticas contextuais.
- **May consume:** fatos mínimos de vínculo/afastamento/desligamento de Staff e audit workflow.
- **Must not own:** Person, PatientProfile, ProfessionalProfile, ClinicalEntry, Contract, Payment ou regras de elegibilidade.
- **Must not write:** cadastros, recursos protegidos ou fatos de negócio dos demais contextos.

### Organization

- **Owns:** Clinic, Unit, Room, InstitutionalCalendar e Holiday.
- **May reference:** ator autorizado por UserAccountId.
- **May consume:** autorização e auditoria.
- **Must not own:** CalendarException operacional, conflito, capacidade, vínculo profissional ou caixa por unidade.
- **Must not write:** Staff, Scheduling, Pilates ou Finance.

### People

- **Owns:** Person, ContactPoint, Address, PersonRelationship, PersonMerge e MergeManifest.
- **May reference:** UnitId quando houver unidade principal.
- **May consume:** autorização e auditoria.
- **Must not own:** PatientProfile, ProfessionalProfile, UserAccount, Opportunity, Contract ou Payment.
- **Must not write:** perfis, conta, prontuário, contratação ou cobrança.

### Patients

- **Owns:** PatientProfile, GuardianLink, AdministrativeResponsibleLink, ResponsiblePayerLink vigente e EmergencyContact.
- **May reference:** PersonId para paciente/responsável/pagador e UnitId.
- **May consume:** fatos de merge/inativação de People.
- **Must not own:** Person, prontuário, Enrollment, Contract, ClassMembership, Receivable ou Payment.
- **Must not write:** Person ou dados clínicos/contratuais/financeiros.

### Staff

- **Owns:** ProfessionalProfile, EmploymentLink, ProfessionalUnitLink, Availability e ProfessionalLeave.
- **May reference:** PersonId e UnitId.
- **May consume:** fatos de People/Organization e autorização/auditoria.
- **Must not own:** Person, UserAccount, Appointment, Class, ClinicalEntry ou comissão.
- **Must not write:** identidade civil, conta, agenda, turma ou prontuário.

### CRM

- **Owns:** Opportunity, PipelineStage, Activity, Task, LossReason e CommercialProposal/ProposalSnapshot. Lead é read model operacional.
- **May reference:** PersonId, AppointmentId, ContractId e EnrollmentId.
- **May consume:** resultados de Appointment e aceite/ativação de Contract/Enrollment.
- **Must not own:** Person/Lead duplicado, Appointment, PatientProfile, Contract, Payment ou regra de entrega.
- **Must not write:** People, Scheduling, Plans & Enrollment, Billing ou Communication internals.

### Scheduling

- **Owns:** Appointment, ScheduleBlock, CalendarException operacional e o contrato/projeção de ConflictCheck/AvailabilityProjection. ScheduleRule/FixedSchedule somente para agenda geral não vinculada a turma.
- **May reference:** PatientId, ProfessionalId, UnitId, RoomId e OpportunityId.
- **May consume:** InstitutionalCalendar, Availability de Staff, restrição financeira pública e read model de turmas/ocorrências.
- **Must not own:** Class, ClassSchedule, ClassMembership, ClassOccurrence, Attendance ou capacidade.
- **Must not write:** Pilates, Staff, Patients, Billing ou Clinical.

### Pilates

- **Owns:** Class, ClassSchedule, ClassMembership, ClassOccurrence, Attendance, MakeupCredit, MakeupReservation e configuração de capacidade.
- **May reference:** PatientId, ProfessionalId, UnitId e EnrollmentId.
- **May consume:** validação de conflito, elegibilidade de Enrollment e FinancialRestriction pública.
- **Must not own:** Appointment geral, ScheduleRule geral, Enrollment, Contract, FinancialRestriction ou ClinicalEntry.
- **Must not write:** Scheduling, Staff, Patients, Plans & Enrollment, Billing ou Clinical.

### Clinical

- **Owns:** CareEpisode, Assessment, ClinicalTemplate, ClinicalTemplateVersion, ClinicalEntry, Rectification, Addendum, ClinicalDocumentLink e decisão/registro operacional de BreakGlassAccess.
- **May reference:** PatientId, ProfessionalId, AppointmentId, ClassOccurrenceId e DocumentId/DocumentVersionId.
- **May consume:** contexto assistencial mínimo e handles privados de Documents.
- **Must not own:** Person, PatientProfile, Appointment, ClassOccurrence, arquivo binário, Contract, Payment ou AuditLog.
- **Must not write:** cadastros administrativos, agenda/turma, contratos, Billing, Finance ou metadados internos de Documents.

### Plans & Enrollment

- **Owns:** Plan, PlanVersion, Contract, Enrollment, Benefit/Entitlement quando confirmado e ContractAmendment quando necessário à mudança com vigência.
- **May reference:** PatientId, PersonId do pagador, ResponsiblePayerLinkId, UnitId e ClassMembershipId apenas quando necessário à correlação.
- **May consume:** vínculo vigente de pagador e disponibilidade pública de Pilates quando necessária.
- **Must not own:** Receivable, Payment, FinancialTransaction, ClassMembership, capacidade ou Attendance.
- **Must not write:** Billing, Finance ou Pilates.

### Billing

- **Owns:** Receivable, Payment, PaymentAllocation, PaymentReversal, Refund, BillingAdjustment, Negotiation e FinancialRestriction.
- **May reference:** ContractId, EnrollmentId, PatientId, PersonId do pagador e FinancialAccountId.
- **May consume:** snapshots/fatos contratuais e catálogo público de FinancialAccount.
- **Must not own:** Plan, Contract, FinancialAccount, FinancialTransaction, Expense, Closing ou Enrollment.PAUSED.
- **Must not write:** Plans & Enrollment ou Finance; não pode transformar Payment em FinancialTransaction.

### Finance

- **Owns:** FinancialAccount, FinancialTransaction, Expense, ExpenseCategory, Transfer, ReconciliationAdjustment, Closing e ClosingSnapshot/versionamento.
- **May reference:** UnitId e PaymentId/PaymentReversalId/RefundId como correlação de origem.
- **May consume:** PaymentConfirmed, PaymentReversed, RefundIssued e read models de Billing para fechamento.
- **Must not own:** Receivable, Payment, Contract, FinancialRestriction ou prontuário.
- **Must not write:** Billing, Plans & Enrollment ou Clinical.

### Communication

- **Owns:** Message, MessageTemplate, DeliveryAttempt, OutboundRequest/dispatch, provider reference e status técnico de entrega. CommunicationPreference de canal, se adotada, permanece aqui; consentimento jurídico não é inferido.
- **May reference:** PersonId/ContactPointId e ID/correlação opaca do fato originador.
- **May consume:** intenções de CRM, Scheduling e Billing e contato público autorizado de People.
- **Must not own:** política de cobrança, cadência comercial, regra de agenda, fato clínico ou decisão de audiência.
- **Must not write:** Opportunity, Appointment, Receivable, FinancialRestriction ou Person.

### Documents

- **Owns:** Document como handle técnico, StoredFile, DocumentMetadata e DocumentVersion quando versionamento for necessário.
- **May reference:** owner reference opaca do vínculo de negócio e ator autorizado.
- **May consume:** comandos autorizados de armazenamento/retenção e políticas de Privacy & Audit.
- **Must not own:** significado clínico/comercial/financeiro, ClinicalDocumentLink, Contract, Payment ou Expense.
- **Must not write:** fatos do domínio originador; remoção física deve respeitar retenção/legal hold ainda a detalhar.

### Privacy & Audit

- **Owns:** AuditLog/AuditRecord, PrivacyRequest, BreakGlassAudit, evidência de acesso/ação, legal hold e registro de execução autorizada.
- **May reference:** ator e recurso por IDs opacos.
- **May consume:** audit facts mínimos de todos os contextos, inclusive BreakGlassUsed.
- **Must not own:** fato auditado, permissão primária, Person, prontuário ou ConsentRecord sem decisão jurídica futura.
- **Must not write:** agregados alheios; solicita mudanças ao owner por comando explícito e autorizado.

### Reports

- **Owns:** ReadModel, Projection, definição de KPI e ReportDefinition, além de metadados de atualização/rebuild.
- **May reference:** IDs e versões de projeção autorizados.
- **May consume:** eventos públicos e read models minimizados dos owners.
- **Must not own:** qualquer entidade transacional original.
- **Must not write:** nenhum dado transacional; correção ocorre no owner ou por rebuild da projeção.

## 6. Matriz mestre de ownership

`REFERENCE_BY_ID`, `READ_MODEL_CONSUMER`, `EVENT_CONSUMER` e `SNAPSHOT_OWNER` descrevem acesso de consumidores. “Preserva” significa inativação/cancelamento/versionamento, nunca hard delete normal.

| Conceito | Owner | Cria | Altera | Inativa/Cancela | Leitores relevantes | Referência externa | Tipo de acesso | Histórico/Snapshot |
|---|---|---|---|---|---|---|---|---|
| UserAccount | Identity & Access | Identity & Access | Identity & Access | Identity & Access | Privacy & Audit; contextos via identidade autenticada | UserAccountId | REFERENCE_BY_ID | sessões/auditoria preservadas |
| Role | Identity & Access | Identity & Access | Identity & Access | Identity & Access | contextos autorizadores; Privacy & Audit | RoleId | READ_MODEL_CONSUMER | vigência/auditoria |
| Permission | Identity & Access | Identity & Access | Identity & Access | Identity & Access | contextos autorizadores; Privacy & Audit | PermissionId/chave conceitual | READ_MODEL_CONSUMER | vigência/auditoria |
| RoleAssignment | Identity & Access | Identity & Access | Identity & Access | Identity & Access | Privacy & Audit | UserAccountId + RoleId | REFERENCE_BY_ID | concessão/revogação preservadas |
| Session | Identity & Access | Identity & Access | Identity & Access | Identity & Access | Privacy & Audit | SessionId opaco | EVENT_CONSUMER | revogação/auditoria |
| AuthenticationFactor | Identity & Access | Identity & Access | Identity & Access | Identity & Access | Identity & Access; Audit mínimo | UserAccountId | REFERENCE_BY_ID | segredo não é projetado |
| RecoveryToken | Identity & Access | Identity & Access | Identity & Access | Identity & Access | Identity & Access; Audit mínimo | UserAccountId | REFERENCE_BY_ID | uso/expiração auditados |
| Clinic / Unit / Room | Organization | Organization | Organization | Organization | People, Patients, Staff, Scheduling, Pilates, Plans, Finance | ClinicId/UnitId/RoomId | REFERENCE_BY_ID | vigência; Room é informativa |
| InstitutionalCalendar / Holiday | Organization | Organization | Organization | Organization | Scheduling, Pilates, Billing | CalendarId/HolidayId | READ_MODEL_CONSUMER | vigência do calendário |
| CalendarException | Scheduling | Scheduling | Scheduling | Scheduling | Pilates, Communication, Reports | CalendarExceptionId | EVENT_CONSUMER | exceção operacional preservada |
| Person | People | People | People | People | Identity, Patients, Staff, CRM, Plans, Billing, Communication | PersonId | REFERENCE_BY_ID | inativação/merge preservam aliases |
| ContactPoint / Address | People | People | People | People | Patients, CRM, Plans, Billing, Communication | PersonId/ContactPointId | REFERENCE_BY_ID | vigência quando relevante |
| PersonRelationship | People | People | People | People | Patients e consumidores autorizados | RelationshipId/PersonIds | REFERENCE_BY_ID | encerramento preservado |
| PersonMerge / MergeManifest | People | People | People | People | consumidores de Person; Privacy & Audit | MergeId + aliases | EVENT_CONSUMER | imutável/auditável |
| PatientProfile | Patients | Patients | Patients | Patients | Clinical, CRM, Scheduling, Pilates, Plans, Billing | PatientId | REFERENCE_BY_ID | não duplica Person; sem hard delete |
| GuardianLink | Patients | Patients | Patients | Patients | Clinical autorizado; Plans | GuardianLinkId/PersonIds | REFERENCE_BY_ID | vigência do vínculo |
| AdministrativeResponsibleLink | Patients | Patients | Patients | Patients | Scheduling, Communication e consumidores autorizados | AdministrativeResponsibleLinkId/PersonIds | REFERENCE_BY_ID | vigência e autorizações administrativas preservadas |
| ResponsiblePayerLink | Patients | Patients | Patients | Patients | Plans, Billing | PayerLinkId/PersonId | REFERENCE_BY_ID | fonte vigente; snapshots downstream |
| EmergencyContact | Patients | Patients | Patients | Patients | Scheduling/Clinical quando necessário e autorizado | EmergencyContactId/PersonId opcional | REFERENCE_BY_ID | vigência |
| ProfessionalProfile | Staff | Staff | Staff | Staff | Identity, Scheduling, Pilates, Clinical | ProfessionalId | REFERENCE_BY_ID | autoria passada preservada |
| EmploymentLink / ProfessionalUnitLink | Staff | Staff | Staff | Staff | Identity, Scheduling, Pilates, Clinical | EmploymentLinkId/UnitId | EVENT_CONSUMER | vigência obrigatória |
| Availability / ProfessionalLeave | Staff | Staff | Staff | Staff | Scheduling, Pilates | ProfessionalId + vigência | READ_MODEL_CONSUMER | granularidade em MODEL-001 |
| Opportunity / PipelineStage | CRM | CRM | CRM | CRM | Scheduling, Plans, Communication, Reports | OpportunityId | REFERENCE_BY_ID | transições preservadas |
| Activity / Task / LossReason | CRM | CRM | CRM | CRM | Communication e Reports quando necessário | IDs do CRM | REFERENCE_BY_ID | histórico comercial preservado |
| CommercialProposal / ProposalSnapshot | CRM | CRM | CRM | CRM | Plans na contratação; Reports | ProposalId | SNAPSHOT_OWNER | condição apresentada é imutável/versionada |
| Lead | CRM | CRM (projeção) | rebuild por CRM | CRM | operação comercial | PersonId + OpportunityId | READ_MODEL_CONSUMER | não é entidade transacional duplicada |
| Appointment | Scheduling | Scheduling | Scheduling | Scheduling | CRM, Clinical, Communication, Reports | AppointmentId | REFERENCE_BY_ID | cancelamento/reagendamento preservados |
| ScheduleBlock | Scheduling | Scheduling | Scheduling | Scheduling | Staff, Pilates, Reports | ScheduleBlockId | READ_MODEL_CONSUMER | vigência |
| ScheduleRule / FixedSchedule geral | Scheduling | Scheduling | Scheduling | Scheduling | Staff, Reports | ScheduleRuleId | REFERENCE_BY_ID | somente agenda não-turma; vigência |
| ConflictCheck / AvailabilityProjection | Scheduling | Scheduling (deriva) | rebuild por Scheduling | Scheduling | Pilates, CRM | IDs + intervalo | READ_MODEL_CONSUMER | projeção, não fato autônomo |
| Class / ClassSchedule | Pilates | Pilates | Pilates | Pilates | Scheduling, Clinical, Plans, Reports | ClassId/ClassScheduleId | REFERENCE_BY_ID | schedule versionado por vigência |
| Capacity configuration | Pilates | Pilates | Pilates | Pilates | Scheduling (informativo), Plans, Reports | ClassId/ClassScheduleId | READ_MODEL_CONSUMER | valor com vigência |
| ClassMembership | Pilates | Pilates | Pilates | Pilates | Scheduling, Plans, Clinical, Reports | ClassMembershipId | REFERENCE_BY_ID | encerramento, nunca exclusão |
| ClassOccurrence | Pilates | Pilates | Pilates | Pilates | Scheduling, Clinical, Communication, Reports | ClassOccurrenceId | REFERENCE_BY_ID | mudança pontual não altera recorrência |
| Attendance | Pilates | Pilates | Pilates | Pilates | Clinical somente contexto; Reports | AttendanceId | EVENT_CONSUMER | não é ClinicalEntry |
| MakeupCredit / MakeupReservation | Pilates | Pilates | Pilates | Pilates | Scheduling por conflito; Reports | MakeupCreditId/ReservationId | REFERENCE_BY_ID | ciclo e expiração preservados |
| CareEpisode / Assessment | Clinical | Clinical | Clinical | Clinical | Clinical; Reports clínicos autorizados; Audit mínimo | CareEpisodeId/AssessmentId | REFERENCE_BY_ID | finalização/fechamento preservados |
| ClinicalTemplate / ClinicalTemplateVersion | Clinical | Clinical | Clinical | Clinical | Clinical | TemplateId/VersionId | REFERENCE_BY_ID | versões imutáveis após uso |
| ClinicalEntry | Clinical | Clinical | Clinical somente enquanto DRAFT | Clinical não apaga | Clinical autorizado; Audit mínimo | ClinicalEntryId | REFERENCE_BY_ID | FINALIZED imutável |
| Rectification / Addendum | Clinical | Clinical | Clinical por novo registro | Clinical não apaga | Clinical autorizado; Audit mínimo | ClinicalEntryId | REFERENCE_BY_ID | original e correção imutáveis |
| ClinicalDocumentLink | Clinical | Clinical | Clinical | Clinical | Clinical autorizado | DocumentId/VersionId | REFERENCE_BY_ID | vínculo/semântica clínicos |
| BreakGlassAccess | Clinical | Clinical | Clinical encerra | Clinical | Identity; Privacy & Audit | ActorId + resource IDs | EVENT_CONSUMER | justificativa e escopo preservados |
| Plan / PlanVersion | Plans & Enrollment | Plans & Enrollment | Plan: Plans; versão publicada não muda | Plans & Enrollment | CRM, Billing, Reports | PlanId/PlanVersionId | REFERENCE_BY_ID | PlanVersion imutável |
| Contract / Enrollment | Plans & Enrollment | Plans & Enrollment | Plans & Enrollment | Plans & Enrollment | CRM, Pilates, Billing, Reports | ContractId/EnrollmentId | REFERENCE_BY_ID | contrato snapshot; estados com vigência |
| Benefit / Entitlement | Plans & Enrollment | Plans & Enrollment | Plans & Enrollment | Plans & Enrollment | Pilates, Billing | EntitlementId/EnrollmentId | READ_MODEL_CONSUMER | candidato, sem detalhe interno ainda |
| ContractAmendment | Plans & Enrollment | Plans & Enrollment | novo amendment | Plans & Enrollment | Billing, Reports | ContractId/AmendmentId | EVENT_CONSUMER | aditivo imutável se adotado |
| Receivable | Billing | Billing | Billing | Billing | Plans (resultado), Finance, Communication, Reports | ReceivableId/ContractId | REFERENCE_BY_ID | obrigação original/snapshot preservados |
| Payment / PaymentAllocation | Billing | Billing | Billing via operações próprias | Billing não apaga | Finance, Communication, Reports | PaymentId/ReceivableId | EVENT_CONSUMER | confirmado preservado |
| PaymentReversal / Refund | Billing | Billing | Billing por novo fato | Billing não apaga | Finance, Communication, Reports | IDs Billing | EVENT_CONSUMER | separados e imutáveis como fatos |
| BillingAdjustment / Negotiation | Billing | Billing | Billing | Billing | Communication, Finance read model, Reports | IDs Billing | REFERENCE_BY_ID | motivo/alçada/auditoria |
| FinancialRestriction | Billing | Billing | Billing | Billing | Pilates, Scheduling, Plans, Communication | PatientId/EnrollmentId + restrição | EVENT_CONSUMER | independente de Enrollment.PAUSED |
| FinancialAccount | Finance | Finance | Finance | Finance | Billing, Reports | FinancialAccountId | REFERENCE_BY_ID | saldo derivado, não digitado |
| FinancialTransaction | Finance | Finance | Finance por contrapartida/ajuste | Finance não apaga | Reports, Closing | FinancialTransactionId + source ref | REFERENCE_BY_ID | movimento preservado |
| Expense / ExpenseCategory | Finance | Finance | Finance | Finance | Reports, Closing | ExpenseId/CategoryId/UnitId | REFERENCE_BY_ID | previsto/realizado preservados |
| Transfer / ReconciliationAdjustment | Finance | Finance | Finance | Finance | Reports, Closing | IDs Finance | REFERENCE_BY_ID | movimentos/ajustes auditáveis |
| Closing / ClosingSnapshot | Finance | Finance | nova versão por Finance | Finance não apaga | Reports, Privacy & Audit | ClosingId/VersionId | SNAPSHOT_OWNER | snapshot mensal imutável/versionado |
| Message / OutboundRequest | Communication | Communication | Communication | Communication | domínio originador, Reports | MessageId + correlation ID | EVENT_CONSUMER | fato originador não muda |
| MessageTemplate / DeliveryAttempt | Communication | Communication | Communication | Communication | originadores, Privacy & Audit | TemplateId/AttemptId | REFERENCE_BY_ID | tentativas preservadas |
| CommunicationPreference (se adotada) | Communication | Communication | Communication | Communication | People e originadores autorizados | PersonId/ContactPointId | READ_MODEL_CONSUMER | consentimento jurídico fora do escopo atual |
| Document / StoredFile / Metadata / Version | Documents | Documents | Documents | Documents conforme retenção | Clinical, Plans, Billing, Finance autorizados | DocumentId/VersionId | REFERENCE_BY_ID | arquivo/metadados, não significado |
| AuditLog / AuditRecord | Privacy & Audit | Privacy & Audit | append/correção não destrutiva | Privacy & Audit não apaga normalmente | auditores autorizados | actor/resource IDs opacos | EVENT_CONSUMER | imutável |
| PrivacyRequest / legal hold | Privacy & Audit | Privacy & Audit | Privacy & Audit | Privacy & Audit | owners envolvidos, solicitante autorizado | PrivacyRequestId | REFERENCE_BY_ID | workflow/evidência preservados |
| BreakGlassAudit | Privacy & Audit | Privacy & Audit | append | Privacy & Audit não apaga | revisores autorizados | BreakGlassAccessId/correlação | EVENT_CONSUMER | evidência imutável |
| ConsentRecord | não adotado | — | — | — | — | — | OWNERSHIP_CONFLICT NON_BLOCKING | depende de inventário LGPD/jurídico |
| ReadModel / Projection | Reports | Reports (deriva) | rebuild por Reports | Reports | consumidores autorizados | source IDs/versions | READ_MODEL_CONSUMER | descartável/reconstruível |
| KPI definition / ReportDefinition | Reports | Reports | Reports | Reports | gestão autorizada | DefinitionId | READ_MODEL_CONSUMER | definição versionada quando aplicável |

## 7. Matriz de acesso cross-context

### 7.1 Escrita entre contextos

| Contexto consumidor | Conceito externo | Pode escrever diretamente? | Como solicita alteração? |
|---|---|---:|---|
| Identity & Access | Person / ProfessionalProfile | NÃO | contrato público de People/Staff; Identity altera somente conta/permissão |
| Patients | Person | NÃO | busca/criação/alteração por contrato público de People |
| Staff | Person | NÃO | busca/criação/alteração por contrato público de People |
| CRM | Person / Appointment / Contract | NÃO | People; comando público de Scheduling; comando público de Plans & Enrollment |
| Scheduling | ProfessionalProfile / PatientProfile | NÃO | validação/consulta pública a Staff/Patients |
| Scheduling | ClassOccurrence / capacidade | NÃO | lê projeção de Pilates e solicita validação ao owner |
| Pilates | Appointment / conflito | NÃO | solicita ConflictCheck a Scheduling; Appointment geral continua em Scheduling |
| Pilates | Enrollment / FinancialRestriction | NÃO | consulta contrato público e reage a eventos de Plans/Billing |
| Clinical | PatientProfile / ProfessionalProfile | NÃO | referencia/valida por contratos públicos; não corrige cadastro administrativo |
| Clinical | Appointment / ClassOccurrence | NÃO | valida contexto e guarda IDs opcionais |
| Clinical | StoredFile/DocumentMetadata | NÃO | solicita armazenamento/recuperação a Documents |
| Plans & Enrollment | ResponsiblePayerLink | NÃO | consulta vínculo vigente em Patients e grava snapshot próprio no Contract |
| Plans & Enrollment | ClassMembership | NÃO | publica fatos/solicita operação a Pilates; não mantém composição de turma |
| Billing | Contract / Enrollment | NÃO | recebe comando/fatos de Plans & Enrollment e aplica efeitos somente em Billing |
| Billing | FinancialAccount / FinancialTransaction | NÃO | valida FinancialAccount por contrato público; publica fatos para Finance |
| Finance | Payment / Receivable | NÃO | reage a eventos e lê projeções de Billing; correção é solicitada a Billing |
| Communication | Opportunity / Appointment / Receivable | NÃO | retorna status técnico; domínio originador decide qualquer mudança |
| Documents | ClinicalDocumentLink / significado de negócio | NÃO | retorna handle; owner do negócio cria/altera o vínculo |
| Privacy & Audit | qualquer fato auditado | NÃO | envia comando autorizado ao owner no workflow da PrivacyRequest |
| Reports | qualquer dado transacional | NÃO | nenhuma escrita; owner corrige o fato e Reports reconstrói projeção |
| Frontend / n8n | qualquer agregado | NÃO | somente caso de uso/API/evento público do owner |

### 7.2 Referências importantes

| Consumidor/conceito | Referência conceitual | Semântica | Valida existência na operação? | Snapshot? | Mudança futura do owner |
|---|---|---|---:|---:|---|
| PatientProfile | PersonId | identidade civil do paciente | SIM ao criar/ativar | NÃO | dados correntes vêm de People |
| ProfessionalProfile | PersonId | identidade civil do profissional | SIM ao criar/ativar | NÃO | dados correntes vêm de People |
| Opportunity | PersonId | interessado do ciclo comercial | SIM ao criar | proposta pode snapshotar nome/condição | merge atualiza alias/referência, não história |
| Appointment | PatientId/PersonId, ProfessionalId, UnitId | participantes/local do compromisso | SIM ao agendar | dados mínimos no histórico se necessário | não reescreve Appointment passado |
| ClassMembership/ClassOccurrence | PatientId, ProfessionalId, UnitId, EnrollmentId | participação, condução, local e elegibilidade | SIM ao iniciar/materializar quando aplicável | ocorrência preserva contexto mínimo | mudanças futuras não reescrevem passado |
| ClinicalEntry | PatientId, ProfessionalId, AppointmentId opcional, ClassOccurrenceId opcional | paciente, autoria e contexto assistencial | SIM na criação/finalização | ClinicalTemplateVersion obrigatória quando template usado | cadastros/agenda futuros não alteram registro finalizado |
| ClinicalDocumentLink | DocumentId/DocumentVersionId | arquivo privado ligado ao registro | SIM ao vincular/acessar | versão quando integridade exigir | nova versão não troca vínculo passado automaticamente |
| Contract | PatientId, PersonId do pagador, PlanVersionId | partes e oferta aceita | SIM ao aceitar | SIM: plano, valores, frequência, pagador, condições | não altera Contract aceito |
| Receivable | ContractId, EnrollmentId, payer PersonId | origem contratual e devedor histórico | SIM ao criar lote | SIM: pagador, valor, vencimento, origem | não altera obrigação original; ajustes são fatos próprios |
| Payment | ReceivableIds, FinancialAccountId, UserAccountId | alocação, conta declarada e registrador | SIM ao confirmar | preserva valores/conta/método | conta renomeada não reescreve Payment |
| FinancialTransaction | PaymentId/ReversalId/RefundId opcional | correlação com fato externo que originou movimento | SIM pelo evento/idempotência, sem escrita em Billing | payload financeiro mínimo | mudança em Billing gera novo evento, não edição direta |
| Expense | UnitId opcional | escopo UNIT ou GLOBAL | SIM quando UNIT | contexto mínimo no lançamento/fechamento | mudança da Unit não reescreve despesa passada |
| AuditLog | ActorId + resource reference opaca | quem fez o quê em qual recurso | conforme evento/contrato | payload mínimo imutável | não acompanha renomes salvo projeção autorizada |

Nenhuma linha escolhe UUID, ULID ou chave física.

## 8. Snapshots históricos

| Snapshot | Owner do dado original | Owner do snapshot | Conteúdo mínimo | Motivo | Imutabilidade / atualização |
|---|---|---|---|---|---|
| Contract | Plans & Enrollment (PlanVersion); People/Patients (partes/vínculo vigente) | Plans & Enrollment | PlanVersion, valores/H-A, frequência, vigência, pagador e condições aceitas | provar o acordo | imutável após aceite; mudança usa amendment ou novo Contract |
| Receivable | Plans & Enrollment (origem contratual); Patients (pagador vigente na origem) | Billing | payer, valor original, vencimento, Contract/Enrollment e origem | preservar obrigação gerada | não acompanha mudanças; ajuste/cancelamento é fato rastreável |
| ClinicalTemplate usado | Clinical | Clinical | ClinicalTemplateVersion e estrutura aplicável | interpretar registro no contexto original | versão usada não muda; nova versão vale para usos futuros |
| ClosingSnapshot | Finance e read models autorizados de Billing | Finance | movimentos, saldos, previsto/realizado, pendências e versão | provar conferência mensal | imutável; reabertura gera nova versão |
| CommercialProposal | CRM; catálogo pode vir de Plans | CRM | oferta, valores/condições apresentados, data e destinatário | provar o que foi apresentado | imutável/versionada; nova proposta substitui comercialmente, não historicamente |
| DocumentVersion | Documents | Documents | bytes/checksum/metadados técnicos da versão | integridade do arquivo | nova versão não sobrescreve a anterior quando versionamento for exigido |

## 9. Eventos e ownership

| Evento | Publicado por | Consumido por | Owner do fato | Motivo |
|---|---|---|---|---|
| PersonMergeCompleted | People | Patients, Staff, CRM e outros detentores de PersonId | People | atualizar aliases/referências sem duplicar Person |
| PatientProfileActivated | Patients | CRM, Scheduling, Pilates, Plans quando necessário | Patients | informar existência do papel de paciente |
| AppointmentScheduled/Rescheduled/Cancelled | Scheduling | CRM, Communication, Reports | Scheduling | propagar fato de agenda sem transferir ownership |
| ExperimentalCompleted | Scheduling | CRM | Scheduling (Appointment); CRM decide pipeline | separar resultado operacional de decisão comercial |
| ClassScheduleChanged | Pilates | Scheduling, Reports | Pilates | atualizar projeção da agenda sem Scheduling possuir recorrência |
| ClassMembershipStarted/Ended | Pilates | Scheduling, Plans, Reports | Pilates | refletir composição da turma preservando histórico |
| AttendanceRecorded | Pilates | Reports e contexto clínico mínimo quando autorizado | Pilates | presença não é evolução clínica |
| ClinicalEntryFinalized/Rectified | Clinical | Privacy & Audit; projeção clínica autorizada | Clinical | registrar integridade sem expor conteúdo indevido |
| ContractAccepted | Plans & Enrollment | CRM, Billing | Plans & Enrollment | iniciar efeitos comerciais/cobrança a partir do acordo |
| EnrollmentActivated | Plans & Enrollment | CRM, Pilates, Billing | Plans & Enrollment | informar elegibilidade/vínculo operacional |
| EnrollmentPaused/Resumed/Cancelled | Plans & Enrollment | Pilates, Billing, Communication quando solicitado | Plans & Enrollment | cada consumidor aplica apenas seu próprio efeito |
| EnrollmentFrequencyChanged | Plans & Enrollment | Pilates, Billing | Plans & Enrollment | aplicar vigência sem sobrescrever história |
| PaymentConfirmed | Billing | Finance, Communication, Reports | Billing | Finance cria FinancialTransaction próprio |
| PaymentReversed | Billing | Finance, Communication, Reports | Billing | Finance registra efeito inverso, sem editar Payment |
| RefundIssued | Billing | Finance, Communication, Reports | Billing | Finance reflete saída correlacionada |
| FinancialRestrictionApplied/Removed | Billing | Pilates, Scheduling, Plans, Communication | Billing | restrição não vira Enrollment.PAUSED |
| ExpenseRegistered/Paid | Finance | Reports, Privacy & Audit | Finance | fato de despesa permanece financeiro |
| MonthClosed/ClosingReopened | Finance | Reports, Privacy & Audit | Finance | preservar versões do fechamento |
| BreakGlassUsed | Clinical | Privacy & Audit | Clinical (uso/acesso); Privacy & Audit possui evidência | separar decisão/acesso clínico da trilha imutável |

Eventos não concedem ao consumidor escrita no agregado do publisher. Envelope, entrega, retries, idempotência e outbox pertencem a ARC-007/ARC-008.

## 10. Conceitos compartilhados sem ownership compartilhado

- **Person:** People possui a identidade; Patients, Staff, Identity, CRM, Plans e Billing guardam papéis ou referências.
- **Patient:** Patients possui PatientProfile; Clinical possui prontuário, Pilates possui participação e Plans possui Enrollment.
- **Professional:** Staff possui perfil/vínculo; Identity possui conta, Clinical preserva autoria e Scheduling/Pilates referenciam disponibilidade/contexto.
- **Pagador:** Patients possui o vínculo administrativo vigente; Contract e Receivable possuem snapshots históricos próprios.
- **Agenda:** Scheduling possui Appointment/conflito; Pilates possui recorrência/ocorrência de turma; Scheduling apenas projeta turmas.
- **Atendimento:** Scheduling/Pilates possuem contexto operacional; Clinical possui o registro assistencial.
- **Documento:** Documents possui arquivo/metadados; Clinical, Plans, Billing ou Finance possuem vínculo e significado no próprio domínio.
- **Pagamento/movimento:** Billing possui Payment; Finance possui FinancialTransaction correlacionada.
- **Auditoria:** Privacy & Audit possui AuditLog; cada domínio continua owner do fato auditado.
- **Relatório:** Reports possui projeção/definição; o owner original continua sendo a fonte de verdade.

## 11. Dependências proibidas

- acesso direto a tabela, repositório ou modelo interno de outro contexto;
- Patients/Staff/CRM duplicarem Person;
- Identity centralizar Patient, Professional, ClinicalEntry, Contract ou Payment;
- Scheduling possuir ClassSchedule, ClassOccurrence ou capacidade;
- Pilates possuir Appointment geral ou regra geral de conflito;
- Clinical escrever PatientProfile ou expor prontuário a CRM/Billing/Finance;
- Billing editar Contract, Enrollment, FinancialAccount ou FinancialTransaction;
- Finance editar Receivable, Payment, PaymentAllocation, PaymentReversal ou Refund;
- Communication decidir cobrança, perda/conversão, cancelamento de agenda ou audiência clínica;
- Documents decidir significado/autorização de negócio;
- Privacy & Audit alterar fatos alheios diretamente;
- Reports reparar/transacionar a fonte original;
- frontend/n8n/provider acessar persistência interna ou decidir regra central;
- qualquer consumer republicar fato externo atribuindo a si o ownership.

## 12. Ambiguidades resolvidas

1. **Calendário:** Organization possui InstitutionalCalendar/Holiday; Scheduling possui CalendarException operacional. Pilates e Billing consomem calendário por contrato/projeção.
2. **ScheduleRule/FixedSchedule:** pertence a Scheduling somente para agenda geral não-turma. Recorrência de turma é exclusivamente ClassSchedule em Pilates.
3. **Pagador:** Patients possui ResponsiblePayerLink vigente; Plans possui snapshot aceito no Contract; Billing possui snapshot da obrigação no Receivable.
4. **Refund/Reversal:** PaymentReversal e Refund são conceitos distintos de Billing.
5. **CashSession/CashTransaction:** CashSession não é recriado sem necessidade aprovada; o conceito canônico é FinancialAccount do tipo CASH e FinancialTransaction em Finance.
6. **Documentos:** Documents possui storage/metadados; o contexto de negócio possui o link e o significado.
7. **Break-glass:** Clinical possui a decisão/uso operacional; Privacy & Audit possui a evidência imutável `BreakGlassAudit`.
8. **Lead:** read model de Person + Opportunity, não entidade transacional duplicada.
9. **Eventos:** o publisher/owner do fato possui seu schema semântico; envelope técnico transversal fica para ARC-007.

## 13. Ambiguidades restantes

| ID | OWNERSHIP_CONFLICT / tema | Classificação | Decisão preservada / próximo estágio |
|---|---|---|---|
| OWC-001 | granularidade interna de ProfessionalProfile, EmploymentLink, ProfessionalUnitLink, Availability e Leave | NON_BLOCKING | ownership permanece em Staff; detalhar em MODEL-001/DOM-020 |
| OWC-002 | efeito da pausa sobre disponibilidade/expiração de MakeupCredits | NON_BLOCKING para ownership; blocker antes da implementação da regra | Plans possui pausa; Pilates possui créditos; definir reação sem prolongar Contract |
| OWC-003 | Entitlement/Benefit e ContractAmendment necessários/detalhe | NON_BLOCKING | se adotados, owner único é Plans & Enrollment; fechar em modelagem |
| OWC-004 | CommunicationPreference e modelos internos de Message/OutboundRequest | NON_BLOCKING | boundary em Communication; consentimento jurídico não foi inferido |
| OWC-005 | retenção, legal hold e possível ConsentRecord | NON_BLOCKING para MODEL-001; blocker antes do go-live/implementação aplicável | inventário LGPD/jurídico deve decidir; não há ConsentRecord adotado |
| OWC-006 | detalhe de DocumentVersion/retenção e modelos de Reports | NON_BLOCKING | boundaries já fixados; detalhar em ARC-009/DOM-021 |
| OWC-007 | granularidade de ClosingSnapshot e competência x caixa | NON_BLOCKING | owner permanece Finance; detalhar em MODEL financeiro |
| OWC-008 | tratamento de Receivables vencidos no cancelamento e alçadas de ajuste/negociação | NON_BLOCKING para ownership; blocker antes da implementação | owner permanece Billing; não presumir perdão |

Nenhuma ambiguidade acima cria ownership transacional compartilhado nem bloqueia `MODEL-001`.

## 14. Ownership Violations

| ID | Origem | Conceito | Owner correto | Problema | Severidade | Recomendação |
|---|---|---|---|---|---|---|
| OVR-001 | `docs/domain/GLOSSARY.md` | ResponsiblePayer | Patients (vínculo vigente); Contract/Receivable apenas snapshots | “Patients / Billing” sugeria ownership compartilhado | LOW | CORRIGIDA: glossário alinhado ao ARC-001/ARC-002 |
| OVR-002 | `docs/domain/03-scheduling.md`, objetivo/responsabilidades | grade fixa recorrente | Pilates quando for turma; Scheduling somente regra geral não-turma | linguagem ampla sugeria ScheduleRule duplicando ClassSchedule | MEDIUM | CORRIGIDA: escopo de Scheduling explicitamente restringido |
| OVR-003 | `PROJECT_OS.md`, Fase 2 | CashSession / CashTransaction | Finance: FinancialAccount(CASH) / FinancialTransaction | nomes residuais podiam recriar conceitos não aprovados | LOW | CORRIGIDA: roadmap usa os conceitos canônicos |

Não foi encontrada documentação canônica que autorize escrita direta de um módulo em agregados de outro. As violações acima eram terminológicas/residuais, não blockers, e foram corrigidas porque a decisão oficial atual é suficiente.

### Revisão contra processos

- `PROC-PAC-001`: People cria/localiza Person; Patients cria PatientProfile. Patients não escreve Person.
- `PROC-AGD-001`: Scheduling gerencia apenas regra fixa geral não-turma; `PROC-PIL-001/002` gerenciam Class/ClassSchedule/ClassMembership em Pilates.
- `PROC-PIL-004`: Pilates cria Attendance; Clinical não recebe escrita e não cria presença.
- `PROC-CLI-003/004/005`: Clinical cria/finaliza/retifica ClinicalEntry; Pilates e Scheduling apenas fornecem contexto por ID.
- `PROC-PLN-001` e `PROC-ENR-*`: Plans & Enrollment altera Contract/Enrollment; Billing e Pilates reagem nos próprios agregados.
- `PROC-BIL-002/003/006/007`: Billing cria Payment/Reversal/Refund; Finance somente cria movimentos próprios após eventos.
- `PROC-FIN-005/006`: Finance cria/versiona Closing e lê projeções de Billing sem escrever Receivable/Payment.

Não foi encontrada inconsistência de processo que bloqueie a modelagem. `PROC-AGD-001` requer a interpretação restrita registrada em OVR-002.

### Revisão contra regras

- Histórico/vigência: ownership preserva `RB-AGD-004/005/008`, `RB-PIL-001/002/003`, `RB-CLI-003/004`, `RB-PLN-001/002/003`, `RB-ENR-005/006`, `RB-BIL-008/009` e `RB-FIN-006`.
- Segurança: Identity decide autenticação/permissão; o owner do recurso decide elegibilidade contextual, conforme `RB-SEC-001/002`.
- Clinical: somente Clinical escreve conteúdo, conforme `RB-CLI-001` a `RB-CLI-008`.
- Billing/Finance: owners distintos preservam `RB-BIL-001` e `RB-FIN-001/002`, além de DEC-026.
- Pilates/Scheduling: capacidade e recorrência de turma permanecem em Pilates conforme `RB-PIL-001/004`; Scheduling mantém conflito conforme `RB-AGD-002/003`.

## 15. Consequências para MODEL-001

1. Modelar Person como raiz de identidade em People, sem campos duplicados em PatientProfile ou ProfessionalProfile.
2. PatientProfile e ProfessionalProfile referenciam Person por ID conceitual e têm ciclos próprios.
3. GuardianLink, AdministrativeResponsibleLink e ResponsiblePayerLink pertencem a Patients; snapshots de pagador não entram nesses agregados.
4. ProfessionalUnitLink, Availability e ProfessionalLeave permanecem em Staff, ainda que a granularidade seja refinada.
5. Clinic, Unit, Room, InstitutionalCalendar e Holiday pertencem a Organization; CalendarException não entra em MODEL-001 de Organization.
6. Definir vigência/inativação conceitual e eventos públicos necessários, sem escolher PK, FK, UUID/ULID, tabela ou repository.
7. Preservar contratos públicos para criação/consulta de Person e validação de Unit, evitando dependência de storage.
8. Não há blocker de ownership conhecido para iniciar MODEL-001.

## 16. Open Questions

### BLOCKING

Nenhuma para `MODEL-001 — People / Patients / Staff / Organization`.

### NON-BLOCKING

- Qual granularidade exata separará ProfessionalProfile, EmploymentLink, ProfessionalUnitLink, Availability e ProfessionalLeave?
- Availability será regra recorrente, intervalos efetivos ou ambos? O owner continua Staff.
- Qual fonte externa alimentará Holiday/InstitutionalCalendar e qual precedência terá frente a CalendarException?
- Quais atributos mínimos de pagador devem compor snapshots de Contract e Receivable?
- Entitlement/Benefit e ContractAmendment serão conceitos explícitos ou comportamento dos agregados existentes?
- CommunicationPreference será apenas preferência de canal ou haverá modelo jurídico separado após inventário LGPD?
- Quais documentos exigem DocumentVersion e quais políticas de retenção/legal hold se aplicam?
- Como MakeupCredits reagem à pausa sem alterar o término do Contract?
- Qual tratamento será dado a Receivables já vencidos no cancelamento?

### Checklist de validação do ARC-002

- [x] Todo conceito transacional adotado possui exatamente um owner.
- [x] Nenhum conceito possui dois owners concorrentes.
- [x] Nenhum conceito principal ficou sem owner; `ConsentRecord` foi explicitamente não adotado.
- [x] Reports não possui escrita transacional.
- [x] Clinical está isolado.
- [x] Billing e Finance estão separados; Payment não é FinancialTransaction.
- [x] Patients e Staff não duplicam Person.
- [x] Scheduling não possui ClassOccurrence; Pilates não possui Appointment geral.
- [x] Documents não se tornou owner do conteúdo/significado de negócio.
- [x] Identity não virou God Module.
- [x] Snapshots estão explícitos.
- [x] Cross-context writes estão proibidos ou formalizados por contrato público.
- [x] Eventos preservam o owner do fato.
- [x] Não existe ciclo de ownership conhecido.
- [x] Revisões contra `PROCESS_INDEX` e `RULES_INDEX` foram registradas.
- [x] Não existe blocker conhecido para MODEL-001.
