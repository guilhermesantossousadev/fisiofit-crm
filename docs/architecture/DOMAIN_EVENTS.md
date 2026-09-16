# EVT-001 — Domain & Integration Event Catalog

## 1. Status

- **Tarefa:** EVT-001
- **Status:** DONE
- **Data:** 2026-09-16
- **Resultado:** PASS
- **Fontes normativas:** `PROJECT_OS.md`, ARC-001/002, MODEL-001..005, STATE-001, glossário, parâmetros, regras, processos e decisões.
- **Próxima tarefa:** AUTH-001 — Matriz de Permissões e Policies.

## 2. Objetivo

Definir o catálogo canônico de fatos relevantes do Fisiofit CRM 2.0, seus owners, exposição entre contextos e contratos conceituais mínimos. Este catálogo fecha semântica; não define transporte.

## 3. Escopo

- eventos de domínio internos e eventos de integração dos 16 contextos;
- comandos, operações, mensagens e atualizações de read model distinguidos de eventos;
- owner, consumidores, payload mínimo, sensibilidade, ordenação, idempotência, auditoria, impacto de falha e evolução;
- cobertura de STATE-001, dos 37 processos e das cadeias cross-context prioritárias.

Contextos sem fato público sustentado permanecem sem evento de integração. Isso é decisão explícita, não lacuna.

## 4. Fora de Escopo

Broker, filas, tópicos, transporte, serialização final, schema físico, UUID/ULID, retry, dead-letter, worker, outbox/inbox físico e transações distribuídas. RabbitMQ, Kafka, Azure Service Bus, MassTransit e MediatR não são selecionados.

## 5. Terminologia

| Termo | Definição | Exemplo | Não é |
|---|---|---|---|
| Fato de domínio | algo de negócio que já aconteceu, independentemente de ser publicado | pagamento foi confirmado | pedido ou intenção |
| `COMMAND` | intenção dirigida a um owner, no imperativo | `ConfirmPayment` | evento |
| Operação | caso de uso que valida regras e pode produzir zero ou mais fatos | `ReversePayment` | contrato de evento |
| `DOMAIN_EVENT` | fato relevante dentro do boundary owner | `AttendanceCorrected` | necessariamente público |
| `INTEGRATION_EVENT` | fato estável deliberadamente exposto a outros contexts | `PersonMergeCompleted` | comando para consumer executar regra arbitrária |
| `BOTH` | o mesmo fato tem relevância interna e contrato público minimizado | `PaymentConfirmed` | compartilhamento do modelo interno |
| Mensagem de comunicação | artefato/tentativa de entrega de Communication | `MessageSent` | fato financeiro ou clínico originador |
| Intenção de comunicação | comando explícito para Communication quando o originador escolheu conteúdo/finalidade | `RequestCollectionCommunication` | evento de domínio |
| Atualização de read model | efeito reconstruível de projeção | atualizar `AgendaView` | evento de domínio |
| Audit trail | evidência de quem fez o quê, quando e por quê | `AuditRecord` | catálogo indiscriminado de eventos públicos |

`ConfirmPayment → PaymentConfirmed`; `PauseEnrollment → EnrollmentPaused`; `SendMessage` é comando; `AgendaViewUpdated` é atualização técnica. Evento nunca significa “faça qualquer coisa que o publisher desconhece”.

## 6. Event Principles

1. Evento descreve passado e usa linguagem de negócio.
2. Publisher é o único owner do fato.
3. Consumer altera somente seus próprios aggregates/read models.
4. Nem toda transição gera evento; nem todo domain event vira integration event.
5. Validação que precisa de resposta imediata usa contrato síncrono, não evento.
6. Payload é mínimo, orientado à finalidade conhecida e não replica objetos.
7. Evento não concede autorização nem substitui policy.
8. Auditabilidade e publicação são decisões distintas.
9. Reports somente consome; não publica fatos transacionais.
10. Cadeia de eventos não implica atomicidade cross-context.
11. Condição derivada só vira fato quando a mudança detectada possui valor operacional, sem transformar a condição em segundo source of truth.
12. Read models podem ser reconstruídos e suas atualizações não são eventos de domínio.

## 7. Naming Rules

- PascalCase, passado, vocabulário do negócio, sem nomes de tecnologia.
- Sufixos como `Created`, `Accepted`, `Confirmed`, `Cancelled`, `Completed`, `Reversed`, `Applied` e `Removed` precisam conservar significado estável.
- Proibidos: `OnPaymentUpdate`, `SendBillingEvent`, `DbRecordInserted`, `WebhookReceived` e nomes imperativos.
- Alteração ampla como `PersonUpdated` ou `UnitUpdated` não é contrato público; fatos específicos só surgem com utilidade comprovada.
- Nome lógico não embute versão física. A versão fica no envelope conceitual.

## 8. Event Envelope

Envelope conceitual comum aos eventos publicados:

| Campo | Regra |
|---|---|
| `eventId` | identifica unicamente a publicação para deduplicação; formato ainda não escolhido |
| `eventType` | nome canônico do fato |
| `eventVersion` | versão semântica positiva do contrato |
| `occurredAt` | instante em que o fato ocorreu, não o instante de consumo |
| `producerContext` | owner/publicador canônico |
| `aggregateId` ou `subjectId` | referência primária necessária ao fato; usar somente as aplicáveis |
| `correlationId` | liga fatos e comandos do mesmo fluxo de negócio |
| `causationId` | identifica comando/evento imediatamente causador, quando houver |
| `clinicId` / `unitId` | opcionais; somente quando escopo institucional for necessário ao consumer |

O envelope não define tipos físicos. `correlationId` conecta, por exemplo, `ContractAccepted → ReceivablesGenerated`; `causationId` de `ReceivablesGenerated` aponta conceitualmente para `ContractAccepted`.

## 9. Classification

| Valor | Uso |
|---|---|
| `DOMAIN_EVENT` | fato mantido no contexto owner, sem contrato público atual |
| `INTEGRATION_EVENT` | fato público cuja representação interna não é relevante/assumida pelo contrato |
| `BOTH` | fato de domínio também publicado em forma mínima |

Status do catálogo: `ADOPTED`, `INTERNAL_ONLY`, `DEFERRED`, `REJECTED` ou `DEPRECATED`. `ADOPTED` indica contrato canônico vigente; eventos `INTERNAL_ONLY` continuam válidos somente dentro do owner.

## 10. Sensitivity

| Classe | Aplicação |
|---|---|
| `NORMAL` | IDs e metadados operacionais sem dado pessoal desnecessário |
| `PERSONAL` | referência a pessoa/paciente/profissional ou vínculo pessoal |
| `FINANCIAL` | valores, contas e correlações financeiras estritamente necessárias |
| `CLINICAL_METADATA` | metadados mínimos de prontuário, sem conteúdo assistencial |
| `SECURITY_SENSITIVE` | acesso excepcional, exportação, identidade/autorização ou evidência de segurança |

## 11. Versioning

- Todo integration event possui `eventVersion`; a notação conceitual pode ser lida como `EventName.v1`, sem impor nome físico.
- Breaking change de estrutura obrigatória ou significado exige nova versão.
- Campo opcional aditivo pode ser compatível quando consumidores toleram ausência e desconhecidos.
- Significado, unidade, cardinalidade ou sensibilidade de campo não muda silenciosamente.
- Consumidor depende apenas dos campos necessários à reação declarada.
- Versões antigas têm janela de convivência definida na arquitetura física; remoção exige inventário de consumidores.

## 12. Idempotency

| Classe | Regra |
|---|---|
| `REQUIRED` | duplicação causaria fato externo, financeiro, vínculo, restrição ou projeção crítica duplicada |
| `RECOMMENDED` | deduplicação preserva qualidade, embora replay seja recuperável |
| `NOT_CRITICAL` | consumo duplicado é naturalmente idempotente/reconstruível |

Consumos `PaymentConfirmed`, `PaymentPartiallyReversed`, `PaymentReversed` e `RefundCompleted` por Finance são `REQUIRED`: um mesmo fato não pode criar duas `FinancialTransactions` equivalentes.

## 13. Ordering

| Classe | Regra |
|---|---|
| `NONE` | nenhuma ordem entre fatos é necessária |
| `PER_AGGREGATE` | preservar/validar sequência lógica do mesmo aggregate |
| `PER_SUBJECT` | importa a sequência relativa ao mesmo paciente/profissional/person |
| `WORKFLOW_DEPENDENT` | consumer usa causalidade/estado para coordenar workflow, sem ordem global |

Não existe ordenação global. Consumers devem reconhecer pré-condições e replay; `PaymentConfirmed` precede reversal do mesmo Payment e `EnrollmentActivated` precede pausa do mesmo Enrollment.

## 14. Organization Events

Owner/context: **Organization**.

| Event | Class / Status | Source; trigger; meaning | Consumers | Payload minimum | Sens. | Order / Idemp. | Audit / Failure | Versioning / Notes |
|---|---|---|---|---|---|---|---|---|
| InstitutionalCalendarChanged | BOTH / ADOPTED | InstitutionalCalendar; nova vigência/feriado aplicável; calendário institucional mudou | Scheduling, Pilates, Billing | calendarId, scope clinic/unit, effectiveFrom, changeKind | NORMAL | PER_AGGREGATE / REQUIRED | STANDARD / MEDIUM | não carrega calendário completo; consumer reavalia apenas dados próprios |
| UnitCreated | DOMAIN_EVENT / INTERNAL_ONLY | Unit; criação concluída; referência institucional passou a existir | projeções Organization | unitId, clinicId, createdAt | NORMAL | NONE / NOT_CRITICAL | STANDARD / LOW | existência imediata pode ser validada sincronamente |
| HolidayRegistered | DOMAIN_EVENT / INTERNAL_ONLY | Calendar; feriado incluído | projeções locais | calendarId, holidayId, date | NORMAL | PER_AGGREGATE / RECOMMENDED | STANDARD / LOW | integração consolidada por `InstitutionalCalendarChanged` |

`UnitUpdated` é rejeitado como contrato amplo. Room não publica evento: é informação não bloqueante.

## 15. People Events

Owner/context: **People**.

| Event | Class / Status | Source; trigger; meaning | Consumers | Payload minimum | Sens. | Order / Idemp. | Audit / Failure | Versioning / Notes |
|---|---|---|---|---|---|---|---|---|
| PersonCreated | DOMAIN_EVENT / INTERNAL_ONLY | Person; identidade criada; PersonId canônico existe | projeções internas | personId, createdAt | PERSONAL | NONE / RECOMMENDED | STANDARD / LOW | criação downstream usa contrato explícito, não reação implícita |
| PersonMergeCompleted | BOTH / ADOPTED | PersonMerge; merge concluído; source virou alias do target | Patients, Staff, Identity, CRM, Plans, Billing e demais detentores de PersonId | personMergeId, sourcePersonId, targetPersonId, completedAt | PERSONAL | PER_SUBJECT / REQUIRED | SENSITIVE / HIGH | sem nome, CPF, contato ou manifest completo; snapshots históricos não são reescritos |
| PersonMergeReversed | BOTH / ADOPTED | PersonMerge; reversão autorizada concluída; aliasing foi desfeito conforme manifest | mesmos consumers do merge | personMergeId, sourcePersonId, targetPersonId, reversedAt, reversalReference | PERSONAL | WORKFLOW_DEPENDENT / REQUIRED | SENSITIVE / HIGH | consumers revertem apenas referências correntes cuja segurança foi validada |
| PersonIdentityUpdated | DOMAIN_EVENT / INTERNAL_ONLY | Person; dado civil relevante mudou | projeções autorizadas internas | personId, changedFieldSet, occurredAt | PERSONAL | PER_SUBJECT / RECOMMENDED | SENSITIVE / LOW | sem before/after público; não substitui consulta autorizada |
| ContactPointChanged | DOMAIN_EVENT / INTERNAL_ONLY | Person; contato atual mudou | projeção autorizada de contato | personId, contactPointId, changeKind | PERSONAL | PER_SUBJECT / RECOMMENDED | SENSITIVE / LOW | não carrega telefone/e-mail em evento público |

`PersonUpdated`, `PersonNameChanged`, `PersonPhoneChanged` e `PersonAddressLineChanged` são rejeitados como event storm/contratos amplos.

## 16. Patients Events

Owner/context: **Patients**.

| Event | Class / Status | Source; trigger; meaning | Consumers | Payload minimum | Sens. | Order / Idemp. | Audit / Failure | Versioning / Notes |
|---|---|---|---|---|---|---|---|---|
| PatientProfileCreated | BOTH / ADOPTED | PatientProfile; papel criado; PatientId existe | CRM, Scheduling, Pilates, Plans | patientId, personId, createdAt | PERSONAL | PER_SUBJECT / REQUIRED | SENSITIVE / MEDIUM | não contém cadastro civil ou clínico |
| PatientActivated | BOTH / ADOPTED | PatientProfile; ativação/reativação concluída; novos usos são elegíveis | Scheduling, Pilates, Plans, CRM | patientId, activatedAt | PERSONAL | PER_AGGREGATE / REQUIRED | SENSITIVE / MEDIUM | não ativa Enrollment, membership ou Appointment |
| PatientDeactivated | BOTH / ADOPTED | PatientProfile; inativação concluída; novos usos ordinários devem cessar | Scheduling, Pilates, Plans, CRM | patientId, deactivatedAt, reasonCode | PERSONAL | PER_AGGREGATE / REQUIRED | SENSITIVE / HIGH | preserva histórico; consumers não apagam registros |
| ResponsiblePayerChanged | BOTH / ADOPTED | ResponsiblePayerLink; nova vigência iniciou | Plans, Billing | patientId, payerPersonId, payerLinkId, effectiveFrom | PERSONAL | PER_SUBJECT / REQUIRED | SENSITIVE / MEDIUM | vale para fatos futuros; Contract/Receivable existentes preservam snapshots |
| GuardianLinked | DOMAIN_EVENT / INTERNAL_ONLY | GuardianLink; responsabilidade vigente criada | projeções Patients/Clinical autorizadas | patientId, guardianPersonId, linkId, effectiveFrom | PERSONAL | PER_SUBJECT / RECOMMENDED | SENSITIVE / LOW | acesso é consultado por policy; não vira autorização implícita |

## 17. Staff Events

Owner/context: **Staff**.

| Event | Class / Status | Source; trigger; meaning | Consumers | Payload minimum | Sens. | Order / Idemp. | Audit / Failure | Versioning / Notes |
|---|---|---|---|---|---|---|---|---|
| ProfessionalProfileCreated | DOMAIN_EVENT / INTERNAL_ONLY | ProfessionalProfile; papel profissional criado | projeções Staff | professionalId, personId, createdAt | PERSONAL | NONE / RECOMMENDED | SENSITIVE / LOW | existência imediata usa contrato público |
| EmploymentStarted | BOTH / ADOPTED | EmploymentLink; vínculo começou | Identity, Scheduling, Pilates | professionalId, employmentLinkId, effectiveFrom | PERSONAL | PER_SUBJECT / REQUIRED | SENSITIVE / MEDIUM | não concede permissões por si só |
| EmploymentEnded | BOTH / ADOPTED | EmploymentLink; vínculo terminou | Identity, Scheduling, Pilates, Clinical | professionalId, employmentLinkId, effectiveTo, reasonCode | PERSONAL | PER_SUBJECT / REQUIRED | SENSITIVE / HIGH | remove elegibilidade futura/acesso conforme policy; preserva autoria |
| ProfessionalAssignedToUnit | DOMAIN_EVENT / INTERNAL_ONLY | ProfessionalUnitLink; atuação iniciou | projeções Staff | professionalId, unitId, linkId, effectiveFrom | PERSONAL | PER_SUBJECT / RECOMMENDED | SENSITIVE / LOW | validação atual pode usar contrato/read model |
| ProfessionalUnassignedFromUnit | DOMAIN_EVENT / INTERNAL_ONLY | ProfessionalUnitLink; atuação encerrou | projeções Staff | professionalId, unitId, linkId, effectiveTo | PERSONAL | PER_SUBJECT / RECOMMENDED | SENSITIVE / LOW | não cancela agenda automaticamente |
| ProfessionalLeaveRegistered | BOTH / ADOPTED | ProfessionalLeave; afastamento passou a vigorar/foi registrado | Scheduling, Pilates, Identity quando aplicável | leaveId, professionalId, effectivePeriod, unitScope? | PERSONAL | PER_SUBJECT / REQUIRED | SENSITIVE / HIGH | workflow humano avalia substituição/cancelamento |
| ProfessionalLeaveEnded | BOTH / ADOPTED | ProfessionalLeave; afastamento terminou | Scheduling, Pilates, Identity quando aplicável | leaveId, professionalId, endedAt | PERSONAL | PER_SUBJECT / REQUIRED | SENSITIVE / MEDIUM | não recria compromissos nem memberships |

`ProfessionalAvailabilityChanged` permanece DEFERRED até o contrato público de Availability ser detalhado; não se publica a disponibilidade inteira.

## 18. CRM Events

Owner/context: **CRM**. Todos abaixo são eventos de domínio internos; o pipeline não tem consumidor cross-context comprovado. Communication recebe intenção explícita quando CRM escolhe uma ação, não cada troca de estágio.

| Events | Class / Status | Source / meaning | Sens. | Order / Idemp. | Audit / Failure | Notes |
|---|---|---|---|---|---|---|
| OpportunityCreated, OpportunityQualified, ProposalPresented, OpportunityNegotiationStarted | DOMAIN_EVENT / INTERNAL_ONLY | Opportunity; marcos comerciais concluídos | PERSONAL | PER_AGGREGATE / RECOMMENDED | STANDARD / LOW | não publicar troca genérica de stage |
| OpportunityConverted, OpportunityLost, OpportunityDisqualified, OpportunityReactivated | DOMAIN_EVENT / INTERNAL_ONLY | Opportunity; resultado comercial decidido | PERSONAL | PER_AGGREGATE / REQUIRED | SENSITIVE / MEDIUM | conversão depende de Contract/Enrollment, nunca Payment |
| FirstContactAttempted, TaskStarted, TaskCompleted, TaskCancelled | DOMAIN_EVENT / INTERNAL_ONLY | Activity/Task; histórico operacional | PERSONAL | PER_AGGREGATE / RECOMMENDED | STANDARD / LOW | sem integração atual |

`ExperimentalScheduled` e `ExperimentalCompleted` como eventos do CRM são REJECTED: Scheduling publica os fatos canônicos de Appointment, com `appointmentPurpose=EXPERIMENTAL`; CRM apenas reage e decide seu próprio estado.

## 19. Scheduling Events

Owner/context: **Scheduling**.

| Event | Class / Status | Source; trigger; meaning | Consumers | Payload minimum | Sens. | Order / Idemp. | Audit / Failure | Versioning / Notes |
|---|---|---|---|---|---|---|---|---|
| AppointmentScheduled | BOTH / ADOPTED | Appointment; agendamento criado | CRM, Communication, Reports, projeções autorizadas | appointmentId, purpose, patient/person ref quando necessário, professionalId, unitId, interval, opportunityId? | PERSONAL | PER_AGGREGATE / REQUIRED | STANDARD / MEDIUM | purpose permite experimental sem evento duplicado |
| AppointmentRescheduled | BOTH / ADOPTED | Appointment; intervalo alterado com histórico | CRM, Communication, Reports | appointmentId, previousInterval, newInterval, reasonCode | PERSONAL | PER_AGGREGATE / REQUIRED | SENSITIVE / HIGH | não cria novo Appointment silenciosamente |
| AppointmentCancelled | BOTH / ADOPTED | Appointment; cancelamento concluído | CRM, Communication, Reports | appointmentId, cancelledAt, reasonCode, purpose | PERSONAL | PER_AGGREGATE / REQUIRED | SENSITIVE / HIGH | não autoriza CRM a perder Opportunity |
| AppointmentCompleted | BOTH / ADOPTED | Appointment; realização confirmada | CRM quando experimental, Clinical context, Reports | appointmentId, completedAt, purpose, opportunityId? | PERSONAL | PER_AGGREGATE / REQUIRED | STANDARD / MEDIUM | substitui `ExperimentalCompleted`; não cria ClinicalEntry |
| AppointmentNoShowRecorded | BOTH / ADOPTED | Appointment; ausência registrada | CRM quando experimental, Communication, Reports | appointmentId, recordedAt, purpose, opportunityId? | PERSONAL | PER_AGGREGATE / REQUIRED | SENSITIVE / MEDIUM | nome canônico evita confundir estado com comando |
| AppointmentConfirmed | DOMAIN_EVENT / INTERNAL_ONLY | Appointment; confirmação registrada | projeções Scheduling | appointmentId, confirmedAt | PERSONAL | PER_AGGREGATE / RECOMMENDED | STANDARD / LOW | sem consumidor público indispensável |
| ScheduleBlockCreated, ScheduleBlockCancelled | DOMAIN_EVENT / INTERNAL_ONLY | ScheduleBlock; indisponibilidade mudou | agenda local/projeções | blockId, professionalId, interval | PERSONAL | PER_SUBJECT / REQUIRED | SENSITIVE / MEDIUM | Pilates consulta conflito; não precisa copiar blocks |
| CalendarExceptionApplied | BOTH / ADOPTED | CalendarException; decisão operacional aplicada | Pilates, Communication, Reports | exceptionId, scope IDs, effectiveDate/interval, effectKind | NORMAL | PER_SUBJECT / REQUIRED | SENSITIVE / HIGH | Organization só informa calendário; Scheduling decide efeito operacional |

## 20. Pilates Events

Owner/context: **Pilates**.

| Event | Class / Status | Source; trigger; meaning | Consumers | Payload minimum | Sens. | Order / Idemp. | Audit / Failure | Versioning / Notes |
|---|---|---|---|---|---|---|---|---|
| ClassScheduleChanged | BOTH / ADOPTED | ClassSchedule; nova vigência recorrente iniciou | Scheduling, Reports | classId, classScheduleId, effectivePeriod, unitId, professionalId, recurrence summary | NORMAL | PER_AGGREGATE / REQUIRED | SENSITIVE / HIGH | não transporta occurrences/capacidade ocupada |
| ClassMembershipStarted | BOTH / ADOPTED | ClassMembership; vínculo recorrente iniciou | Scheduling, Plans, Reports | membershipId, classId, patientId, enrollmentId?, effectiveFrom | PERSONAL | PER_SUBJECT / REQUIRED | SENSITIVE / HIGH | consumer não altera Enrollment |
| ClassMembershipEnded | BOTH / ADOPTED | ClassMembership; vínculo encerrou | Scheduling, Plans, Reports | membershipId, classId, patientId, effectiveTo, reasonCode | PERSONAL | PER_SUBJECT / REQUIRED | SENSITIVE / HIGH | preserva histórico |
| ClassOccurrenceCreated | BOTH / ADOPTED | ClassOccurrence; ocorrência concreta materializada | Scheduling, Communication, Clinical context, Reports | occurrenceId, classId, unitId, plannedInterval, professionalId | PERSONAL | PER_AGGREGATE / REQUIRED | STANDARD / MEDIUM | sem lista de pacientes |
| ClassOccurrenceCancelled | BOTH / ADOPTED | ClassOccurrence; ocorrência cancelada | Scheduling, Communication, Reports | occurrenceId, cancelledAt, reasonCode | NORMAL | PER_AGGREGATE / REQUIRED | SENSITIVE / HIGH | efeitos de crédito são fatos separados do owner Pilates |
| SubstituteAssigned | BOTH / ADOPTED | ClassOccurrence; profissional real mudou | Scheduling, Communication, Reports | occurrenceId, professionalId, assignedAt, reasonCode | PERSONAL | PER_AGGREGATE / REQUIRED | SENSITIVE / MEDIUM | sem dados civis do profissional |
| ClassCreated, ClassOccurrenceStarted, ClassOccurrenceCompleted | DOMAIN_EVENT / INTERNAL_ONLY | Class/Occurrence; marcos locais | NORMAL | PER_AGGREGATE / RECOMMENDED | STANDARD / LOW | projeções externas usam contratos/eventos já suficientes |
| AttendanceRecorded, AttendanceCorrected | DOMAIN_EVENT / INTERNAL_ONLY | Attendance; resultado/correção append-only | projeções operacionais autorizadas | PERSONAL | PER_AGGREGATE / REQUIRED | STANDARD/SENSITIVE / MEDIUM | presença não é conteúdo clínico; Reports usa projeção minimizada |
| MakeupCreditGranted, MakeupReserved, MakeupReservationCancelled, MakeupConsumed, MakeupExpired, MakeupCreditCancelled | DOMAIN_EVENT / INTERNAL_ONLY | MakeupCredit; lifecycle local | PERSONAL | PER_AGGREGATE / REQUIRED | STANDARD/SENSITIVE / LOW | nenhuma cadeia automática de créditos |

`PatientTransferredClass` é REJECTED como fato duplicado: a transferência é workflow correlacionado de `ClassMembershipEnded` + `ClassMembershipStarted`.

## 21. Clinical Events

Owner/context: **Clinical**. Payload público nunca contém narrativa, diagnóstico textual, avaliação, respostas, evolução, observações, anexo ou arquivo.

| Event | Class / Status | Source; trigger; meaning | Consumers | Payload minimum | Sens. | Order / Idemp. | Audit / Failure | Versioning / Notes |
|---|---|---|---|---|---|---|---|---|
| AssessmentFinalized | BOTH / ADOPTED | Assessment; DRAFT tornou-se FINALIZED | Privacy & Audit; timeline clínica autorizada | assessmentId, patientId, professionalId, finalizedAt, templateVersionId? | CLINICAL_METADATA | PER_AGGREGATE / REQUIRED | CLINICAL / HIGH | sem conteúdo da avaliação |
| ClinicalEntryFinalized | BOTH / ADOPTED | ClinicalEntry; DRAFT tornou-se FINALIZED | Privacy & Audit; read models clínicos autorizados | clinicalEntryId, patientId, professionalId, finalizedAt, careEpisodeId, appointmentId?/occurrenceId? | CLINICAL_METADATA | PER_AGGREGATE / REQUIRED | CLINICAL / HIGH | sem corpo clínico |
| ClinicalEntryRectified | BOTH / ADOPTED | Rectification; correção append-only anexada | Privacy & Audit; timeline clínica autorizada | rectificationId, clinicalRecordId, patientId, authorProfessionalId, rectifiedAt, reasonCode | CLINICAL_METADATA | PER_AGGREGATE / REQUIRED | CLINICAL / HIGH | sem before/after clínico |
| ClinicalAddendumAdded | BOTH / ADOPTED | Addendum; complemento append-only anexado | Privacy & Audit; timeline clínica autorizada | addendumId, clinicalRecordId, patientId, authorProfessionalId, addedAt | CLINICAL_METADATA | PER_AGGREGATE / REQUIRED | CLINICAL / HIGH | sem texto do adendo |
| BreakGlassUsed | BOTH / ADOPTED | BreakGlassAccess; acesso excepcional efetivamente usado | Privacy & Audit, revisores autorizados | accessId, actorUserAccountId, patientId, resourceScope, usedAt, reasonCode, correlationId | SECURITY_SENSITIVE | PER_SUBJECT / REQUIRED | CLINICAL / CRITICAL | justificativa livre não trafega; somente código/referência |
| ClinicalRecordExported | BOTH / ADOPTED | ClinicalExportOperation; exportação terminou | Privacy & Audit; privacy workflow autorizado | exportOperationId, actorUserAccountId, patientId, scopeSummary, completedAt, outcome | SECURITY_SENSITIVE | PER_SUBJECT / REQUIRED | CLINICAL / CRITICAL | sem arquivo, URL pública ou conteúdo exportado |
| CareEpisodeOpened, CareEpisodePaused, CareEpisodeResumed, CareEpisodeClosed | DOMAIN_EVENT / INTERNAL_ONLY | CareEpisode; lifecycle clínico | projeções clínicas autorizadas | CLINICAL_METADATA | PER_AGGREGATE / REQUIRED | CLINICAL / MEDIUM | sem integração atual necessária |
| AssessmentCreated, ClinicalEntryCreated, ClinicalDocumentLinked | DOMAIN_EVENT / INTERNAL_ONLY | rascunho/vínculo clínico criado | read models clínicos restritos | CLINICAL_METADATA | PER_AGGREGATE / REQUIRED | CLINICAL / MEDIUM | DRAFT e documento não são publicados genericamente |

## 22. Plans & Enrollment Events

Owner/context: **Plans & Enrollment**.

| Event | Class / Status | Source; trigger; meaning | Consumers | Payload minimum | Sens. | Order / Idemp. | Audit / Failure | Versioning / Notes |
|---|---|---|---|---|---|---|---|---|
| ContractAccepted | BOTH / ADOPTED | Contract; DRAFT tornou-se ACTIVE com snapshot aceito | Billing, CRM, Reports | contractId, enrollmentId?, patientId, planVersionId, acceptedAt, billingTermsRef/minimum terms, renewedFromContractId? | PERSONAL | WORKFLOW_DEPENDENT / REQUIRED | SENSITIVE / CRITICAL | pagador/valores apenas no contrato Billing autorizado; renovação usa novo Contract |
| ContractCompleted | BOTH / ADOPTED | Contract; término regular concluído | Billing, CRM, Reports | contractId, patientId, completedAt | PERSONAL | PER_AGGREGATE / REQUIRED | SENSITIVE / MEDIUM | não equivale a cancelamento |
| ContractCancelled | BOTH / ADOPTED | Contract; acordo cancelado | Billing, CRM, Reports | contractId, patientId, cancelledAt, reasonCode | PERSONAL | PER_AGGREGATE / REQUIRED | SENSITIVE / HIGH | não perdoa Receivable vencido por inferência |
| EnrollmentActivated | BOTH / ADOPTED | Enrollment; direito operacional iniciou | Pilates, Billing, CRM | enrollmentId, contractId, patientId, effectiveFrom, frequencyRef | PERSONAL | PER_AGGREGATE / REQUIRED | SENSITIVE / CRITICAL | não cria ClassMembership |
| EnrollmentPaused | BOTH / ADOPTED | Enrollment; pausa efetiva iniciou | Billing, Pilates, Communication quando aplicável | enrollmentId, patientId, pausePeriod, reasonCode | PERSONAL | PER_AGGREGATE / REQUIRED | SENSITIVE / CRITICAL | consumer aplica somente efeito local; não prolonga Contract |
| EnrollmentResumed | BOTH / ADOPTED | Enrollment; pausa terminou após disponibilidade | Pilates, Billing | enrollmentId, patientId, resumedAt | PERSONAL | PER_AGGREGATE / REQUIRED | SENSITIVE / HIGH | não restaura vaga automaticamente |
| EnrollmentCancelled | BOTH / ADOPTED | Enrollment; vínculo terminou imediatamente | Billing, Pilates, Reports | enrollmentId, contractId, patientId, cancelledAt, reasonCode | PERSONAL | PER_AGGREGATE / REQUIRED | SENSITIVE / CRITICAL | Billing ajusta futuro; Pilates encerra vínculo próprio |
| EnrollmentCompleted | BOTH / ADOPTED | Enrollment; término regular ocorreu | Billing, Pilates, Reports | enrollmentId, contractId, patientId, completedAt | PERSONAL | PER_AGGREGATE / REQUIRED | SENSITIVE / HIGH | distinto de cancelamento |
| EnrollmentFrequencyChanged | BOTH / ADOPTED | Enrollment; nova frequência entrou em vigência | Billing, Pilates | enrollmentId, patientId, effectiveFrom, previousFrequencyRef, newFrequencyRef | PERSONAL | PER_AGGREGATE / REQUIRED | SENSITIVE / HIGH | nome canônico; `PlanFrequencyChanged` é deprecated |
| PlanVersionCreated, ContractCreated, EnrollmentCreated | DOMAIN_EVENT / INTERNAL_ONLY | catálogo/drafts criados | projeções internas | NORMAL/PERSONAL | PER_AGGREGATE / RECOMMENDED | STANDARD / LOW | não há ação externa antes de aceite/ativação |
| ContractRenewed | DOMAIN_EVENT / INTERNAL_ONLY | workflow ligou contrato anterior ao novo | projeção Plans | contractId, renewedFromContractId, occurredAt | PERSONAL | WORKFLOW_DEPENDENT / REQUIRED | SENSITIVE / LOW | integração usa `ContractAccepted` do novo Contract |

`PatientTransferredUnit` é REJECTED como evento de Plans: unidade/turma operacional resulta de vigências próprias e, quando turma muda, de membership em Pilates.

## 23. Billing Events

Owner/context: **Billing**.

| Event | Class / Status | Source; trigger; meaning | Consumers | Payload minimum | Sens. | Order / Idemp. | Audit / Failure | Versioning / Notes |
|---|---|---|---|---|---|---|---|---|
| ReceivablesGenerated | BOTH / ADOPTED | lote Billing; obrigações do Contract foram criadas uma vez | Plans workflow, Reports | generationId, contractId, receivableIds, generatedAt | FINANCIAL | WORKFLOW_DEPENDENT / REQUIRED | FINANCIAL / HIGH | valores ficam disponíveis por contrato/read model autorizado; não envia snapshots completos |
| PaymentConfirmed | BOTH / ADOPTED | Payment; recebimento foi confirmado | Finance, Communication quando aplicável, Reports | paymentId, financialAccountId, confirmedAt, amount, currency, methodCode?, payer/receivable refs somente se propósito exigir | FINANCIAL | PER_AGGREGATE / REQUIRED | FINANCIAL / CRITICAL | Finance cria uma única inflow correlacionada |
| PaymentPartiallyReversed | BOTH / ADOPTED | PaymentReversal; parte do valor confirmado foi revertida | Finance, Communication quando aplicável, Reports | reversalId, paymentId, financialAccountId, reversedAt, amount, currency, reasonCode | FINANCIAL | PER_AGGREGATE / REQUIRED | FINANCIAL / CRITICAL | Finance cria compensação; não edita movimento anterior |
| PaymentReversed | BOTH / ADOPTED | PaymentReversal; valor reversível restante foi integralmente revertido | Finance, Communication quando aplicável, Reports | reversalId, paymentId, financialAccountId, reversedAt, amount, currency, reasonCode | FINANCIAL | PER_AGGREGATE / REQUIRED | FINANCIAL / CRITICAL | distinto de Refund |
| RefundCompleted | BOTH / ADOPTED | Refund; saída real foi concluída | Finance, Communication quando aplicável, Reports | refundId, relatedPaymentId?, financialAccountId, completedAt, amount, currency | FINANCIAL | PER_AGGREGATE / REQUIRED | FINANCIAL / CRITICAL | único fato de Refund que cria outflow em Finance |
| FinancialRestrictionApplied | BOTH / ADOPTED | FinancialRestriction; restrição passou a vigorar | Pilates, Scheduling, Plans, Communication | restrictionId, patientId, effectiveFrom, reasonCode/policyRef | FINANCIAL | PER_SUBJECT / REQUIRED | FINANCIAL / CRITICAL | não muda Enrollment para PAUSED |
| FinancialRestrictionRemoved | BOTH / ADOPTED | FinancialRestriction; restrição deixou de vigorar | mesmos consumers | restrictionId, patientId, removedAt, reasonCode | FINANCIAL | PER_SUBJECT / REQUIRED | FINANCIAL / CRITICAL | nova restrição cria novo registro |
| ReceivableBecameOverdue | BOTH / ADOPTED | Receivable + relógio/policy; condição de atraso começou | Communication, Reports | receivableId, patientId/payerRef mínimo, dueDate, detectedAt, collectionPolicyRef | FINANCIAL | PER_AGGREGATE / REQUIRED | FINANCIAL / HIGH | não persiste OVERDUE como segundo estado; fato alimenta cobrança FACT_DRIVEN |
| ReceivableAdjusted, ReceivableCancelled, PaymentRegistered, PaymentAllocated, RefundIssued, RefundCancelled | DOMAIN_EVENT / INTERNAL_ONLY | fatos de obrigação/liquidação ainda sem necessidade pública direta | FINANCIAL | PER_AGGREGATE / REQUIRED | FINANCIAL / MEDIUM | `RefundIssued` reserva/autoriza, não representa saída real |
| NegotiationCreated | DOMAIN_EVENT / INTERNAL_ONLY | negociação registrada | projeção Billing | FINANCIAL | PER_AGGREGATE / REQUIRED | FINANCIAL / LOW | lifecycle detalhado deferred; `NegotiationChanged` DEFERRED |

`DelinquencyStarted/Resolved` são DEPRECATED: delinquency é condição derivada; usa-se `ReceivableBecameOverdue` para o marco temporal e `FinancialRestrictionApplied/Removed` para o efeito persistido.

## 24. Finance Events

Owner/context: **Finance**.

| Event | Class / Status | Source; trigger; meaning | Consumers | Payload minimum | Sens. | Order / Idemp. | Audit / Failure | Versioning / Notes |
|---|---|---|---|---|---|---|---|---|
| MonthClosed | BOTH / ADOPTED | Closing; nova versão mensal foi fechada | Reports, Privacy & Audit | closingId, period, snapshotVersion, closedAt, cutoff | FINANCIAL | PER_AGGREGATE / REQUIRED | FINANCIAL / HIGH | nome canônico; snapshot não trafega |
| ClosingReopened | BOTH / ADOPTED | Closing; período reabriu preservando snapshot | Reports, Privacy & Audit | closingId, period, previousSnapshotVersion, reopenedAt, reasonCode | FINANCIAL | PER_AGGREGATE / REQUIRED | FINANCIAL / HIGH | fechamento posterior gera novo `MonthClosed` |
| FinancialTransactionCreated | DOMAIN_EVENT / INTERNAL_ONLY | FinancialTransaction; movimento imutável criado | Closing/read models Finance | transactionId, accountId, direction, amount, currency, sourceRef, occurredAt | FINANCIAL | PER_SUBJECT / REQUIRED | FINANCIAL / HIGH | não republica `PaymentConfirmed` como se Finance fosse owner |
| ExpenseCreated, ExpensePaid, ExpenseCancelled, MoneyTransferred, ReconciliationAdjusted | DOMAIN_EVENT / INTERNAL_ONLY | Expense/Transfer/Reconciliation; fato financeiro local | Closing/read models Finance | FINANCIAL | PER_AGGREGATE / REQUIRED | FINANCIAL / MEDIUM | Reports usa projeções; Audit recebe evidência conforme policy |

`ClosingCompleted` é DEPRECATED em favor de `MonthClosed`. `AccountTransferCompleted` é alias DEPRECATED de `MoneyTransferred`. `ExpenseRegistered` é alias DEPRECATED de `ExpenseCreated` para consistência com o conceito aprovado.

## 25. Communication Events

Owner/context: **Communication**.

`MessageQueued`, `MessageSent`, `MessageDelivered` e `MessageFailed` são `DOMAIN_EVENT / INTERNAL_ONLY`: registram lifecycle técnico/operacional da mensagem sem se tornarem fatos financeiros, clínicos ou de agenda. Integração de status de entrega permanece DEFERRED até DOM-019 identificar consumers e SLA. Communication nunca publica `PaymentOverdue`, `AppointmentReminderNeeded` ou decisão de conversão.

Padrão conceitual por finalidade:

| Tipo | Padrão | Regra |
|---|---|---|
| lembrete/cancelamento de Appointment e ClassOccurrence | `FACT_DRIVEN` | Communication reage ao fato e a policy aprovada do originador |
| cobrança por atraso/restrição | `FACT_DRIVEN` | Billing publica `ReceivableBecameOverdue` ou restrição; Communication não recalcula dívida |
| confirmação/recibo de pagamento | `FACT_DRIVEN` | `PaymentConfirmed` pode disparar template aprovado |
| campanha, follow-up comercial escolhido, mensagem ad-hoc | `EXPLICIT_INTENT` | CRM/originador emite command com finalidade/template; não se disfarça de evento |
| comunicação clínica sensível | `EXPLICIT_INTENT` | exige comando autorizado e payload minimizado; nenhum conteúdo clínico em evento genérico |

## 26. Documents Events

Owner/context: **Documents**.

`DocumentStored`, `DocumentVersionCreated` e `DocumentRemoved` ficam `DEFERRED`: não há consumer cross-context comprovado nem política de versionamento/retenção final. O owner de negócio mantém `ClinicalDocumentLink` ou attachment semantics; Documents mantém bytes/metadata técnica e não publica conteúdo ou URL de acesso em evento.

## 27. Privacy & Audit Events

Owner/context: **Privacy & Audit**.

`AuditLogCreated` é REJECTED como integration event: audit trail não gera evento público para cada operação. `BreakGlassAudited` é REJECTED por duplicar `BreakGlassUsed`; Privacy & Audit registra sua evidência idempotente. `PrivacyRequestCreated/Completed` permanecem DEFERRED porque lifecycle, bases, retenção e legal hold ainda não foram aprovados. Identity & Access também permanece sem evento ADOPTED nesta etapa: IAM-001..007 devem definir ciclo da conta, sessão e segurança antes de publicar fatos `SECURITY_SENSITIVE`.

Reports não possui eventos transacionais. `AgendaViewUpdated`, `BillingDashboardUpdated` e `ReportRecalculated` são REJECTED como atualizações de read model/infraestrutura.

## 28. Master Event Catalog

| Evento/família | Owner | Classification | Aggregate/Subject | Consumers | Sensitivity | Ordering | Idempotency | Status |
|---|---|---|---|---|---|---|---|---|
| InstitutionalCalendarChanged | Organization | BOTH | InstitutionalCalendar | Scheduling, Pilates, Billing | NORMAL | PER_AGGREGATE | REQUIRED | ADOPTED |
| UnitCreated; HolidayRegistered | Organization | DOMAIN_EVENT | Unit/Calendar | internos | NORMAL | NONE/PER_AGGREGATE | NOT_CRITICAL/RECOMMENDED | INTERNAL_ONLY |
| UnitUpdated | Organization | — | Unit | — | NORMAL | — | — | REJECTED |
| PersonMergeCompleted; PersonMergeReversed | People | BOTH | PersonMerge/Person | ID holders | PERSONAL | PER_SUBJECT/WORKFLOW_DEPENDENT | REQUIRED | ADOPTED |
| PersonCreated; PersonIdentityUpdated; ContactPointChanged | People | DOMAIN_EVENT | Person | internos | PERSONAL | NONE/PER_SUBJECT | RECOMMENDED | INTERNAL_ONLY |
| PersonUpdated e field-level setters | People | — | Person | — | PERSONAL | — | — | REJECTED |
| PatientProfileCreated; PatientActivated; PatientDeactivated; ResponsiblePayerChanged | Patients | BOTH | PatientProfile/PayerLink | CRM/Scheduling/Pilates/Plans/Billing conforme evento | PERSONAL | PER_SUBJECT/PER_AGGREGATE | REQUIRED | ADOPTED |
| GuardianLinked | Patients | DOMAIN_EVENT | GuardianLink | internos | PERSONAL | PER_SUBJECT | RECOMMENDED | INTERNAL_ONLY |
| EmploymentStarted; EmploymentEnded; ProfessionalLeaveRegistered; ProfessionalLeaveEnded | Staff | BOTH | Employment/Leave | Identity, Scheduling, Pilates, Clinical conforme evento | PERSONAL | PER_SUBJECT | REQUIRED | ADOPTED |
| ProfessionalProfileCreated; assigned/unassigned unit | Staff | DOMAIN_EVENT | Professional/Profile/UnitLink | internos | PERSONAL | PER_SUBJECT | RECOMMENDED | INTERNAL_ONLY |
| ProfessionalAvailabilityChanged | Staff | — | Availability | — | PERSONAL | — | — | DEFERRED |
| Opportunity lifecycle; Task lifecycle | CRM | DOMAIN_EVENT | Opportunity/Task | internos | PERSONAL | PER_AGGREGATE | REQUIRED/RECOMMENDED | INTERNAL_ONLY |
| ExperimentalScheduled/Completed por CRM | CRM | — | Opportunity | — | PERSONAL | — | — | REJECTED |
| AppointmentScheduled/Rescheduled/Cancelled/Completed/NoShowRecorded; CalendarExceptionApplied | Scheduling | BOTH | Appointment/Exception | CRM, Communication, Reports, Pilates/Clinical conforme evento | PERSONAL/NORMAL | PER_AGGREGATE/PER_SUBJECT | REQUIRED | ADOPTED |
| AppointmentConfirmed; ScheduleBlockCreated/Cancelled | Scheduling | DOMAIN_EVENT | Appointment/Block | internos | PERSONAL | PER_AGGREGATE/PER_SUBJECT | RECOMMENDED/REQUIRED | INTERNAL_ONLY |
| ClassScheduleChanged; MembershipStarted/Ended; OccurrenceCreated/Cancelled; SubstituteAssigned | Pilates | BOTH | ClassSchedule/Membership/Occurrence | Scheduling, Plans, Communication, Reports | NORMAL/PERSONAL | PER_AGGREGATE/PER_SUBJECT | REQUIRED | ADOPTED |
| Class/Occurrence/Attendance/Makeup demais fatos | Pilates | DOMAIN_EVENT | respectivos aggregates | internos | NORMAL/PERSONAL | PER_AGGREGATE | REQUIRED/RECOMMENDED | INTERNAL_ONLY |
| PatientTransferredClass | Pilates | — | workflow | — | PERSONAL | — | — | REJECTED |
| AssessmentFinalized; ClinicalEntryFinalized/Rectified; ClinicalAddendumAdded | Clinical | BOTH | registro clínico | Audit/read models autorizados | CLINICAL_METADATA | PER_AGGREGATE | REQUIRED | ADOPTED |
| BreakGlassUsed; ClinicalRecordExported | Clinical | BOTH | acesso/exportação | Privacy & Audit | SECURITY_SENSITIVE | PER_SUBJECT | REQUIRED | ADOPTED |
| CareEpisode e drafts/document link | Clinical | DOMAIN_EVENT | conceitos clínicos | internos | CLINICAL_METADATA | PER_AGGREGATE | REQUIRED | INTERNAL_ONLY |
| ContractAccepted/Completed/Cancelled; Enrollment lifecycle/frequency | Plans & Enrollment | BOTH | Contract/Enrollment | Billing, CRM, Pilates, Reports, Communication conforme evento | PERSONAL | PER_AGGREGATE/WORKFLOW_DEPENDENT | REQUIRED | ADOPTED |
| PlanVersionCreated; drafts; ContractRenewed | Plans & Enrollment | DOMAIN_EVENT | PlanVersion/Contract/Enrollment | internos | NORMAL/PERSONAL | PER_AGGREGATE | RECOMMENDED/REQUIRED | INTERNAL_ONLY |
| PlanFrequencyChanged | Plans & Enrollment | — | Enrollment | — | PERSONAL | — | — | DEPRECATED |
| ReceivablesGenerated; PaymentConfirmed/PartiallyReversed/Reversed; RefundCompleted; restrictions; ReceivableBecameOverdue | Billing | BOTH | Billing aggregates | Plans, Finance, Communication, Reports, operational contexts | FINANCIAL | PER_AGGREGATE/PER_SUBJECT/WORKFLOW_DEPENDENT | REQUIRED | ADOPTED |
| adjustments/cancel/allocation/RefundIssued/NegotiationCreated | Billing | DOMAIN_EVENT | Billing aggregates | internos | FINANCIAL | PER_AGGREGATE | REQUIRED | INTERNAL_ONLY |
| DelinquencyStarted/Resolved | Billing | — | derived condition | — | FINANCIAL | — | — | DEPRECATED |
| MonthClosed; ClosingReopened | Finance | BOTH | Closing | Reports, Privacy & Audit | FINANCIAL | PER_AGGREGATE | REQUIRED | ADOPTED |
| transaction/expense/transfer/reconciliation | Finance | DOMAIN_EVENT | Finance aggregates | internos | FINANCIAL | PER_AGGREGATE/PER_SUBJECT | REQUIRED | INTERNAL_ONLY |
| ClosingCompleted; AccountTransferCompleted; ExpenseRegistered | Finance | — | aliases | — | FINANCIAL | — | — | DEPRECATED |
| Message lifecycle | Communication | DOMAIN_EVENT | Message | internos | PERSONAL | PER_AGGREGATE | REQUIRED | INTERNAL_ONLY |
| Delivery status integration | Communication | — | Message | originators TBD | PERSONAL | — | — | DEFERRED |
| Document lifecycle | Documents | — | Document/Version | TBD | SECURITY_SENSITIVE | — | — | DEFERRED |
| AuditLogCreated; BreakGlassAudited | Privacy & Audit | — | AuditRecord | — | SECURITY_SENSITIVE | — | — | REJECTED |
| PrivacyRequest lifecycle; Identity events | Privacy & Audit / Identity | — | TBD | TBD | SECURITY_SENSITIVE | — | — | DEFERRED |
| read-model update events | Reports/qualquer | — | projections | — | NORMAL | — | — | REJECTED |

## 29. Integration Event Payload Catalog

Envelope da seção 8 é obrigatório em todos. A tabela lista somente payload de negócio adicional.

| Event | Required Fields | Optional Fields | Explicitly Forbidden Data |
|---|---|---|---|
| InstitutionalCalendarChanged | calendarId, scope, effectiveFrom, changeKind | unitId | calendário inteiro, dados pessoais |
| PersonMergeCompleted / Reversed | personMergeId, sourcePersonId, targetPersonId, fact timestamp | reversalReference | CPF, nome, contatos, MergeManifest completo |
| PatientProfileCreated | patientId, personId, createdAt | primaryUnitId | dados clínicos, cadastro civil completo |
| PatientActivated / Deactivated | patientId, fact timestamp | reasonCode na inativação | dados civis/clínicos |
| ResponsiblePayerChanged | patientId, payerPersonId, payerLinkId, effectiveFrom | — | CPF, dados bancários, alteração de snapshots |
| EmploymentStarted / Ended | professionalId, employmentLinkId, effective timestamp | reasonCode no término | salário, documentos, credenciais |
| ProfessionalLeaveRegistered / Ended | leaveId, professionalId, effective period/fact timestamp | unitScope | motivo médico, diagnóstico, anexo |
| AppointmentScheduled | appointmentId, purpose, professionalId, unitId, interval | patientId/personId, opportunityId | notas livres, dados clínicos |
| AppointmentRescheduled | appointmentId, previousInterval, newInterval, reasonCode | — | observações sensíveis |
| AppointmentCancelled | appointmentId, cancelledAt, reasonCode, purpose | opportunityId | dados clínicos |
| AppointmentCompleted / NoShowRecorded | appointmentId, fact timestamp, purpose | opportunityId | evolução, diagnóstico, observações clínicas |
| CalendarExceptionApplied | exceptionId, scope refs, effective interval/date, effectKind | sourceCalendarId | lista de pacientes, dados clínicos |
| ClassScheduleChanged | classId, classScheduleId, effectivePeriod, unitId, professionalId, recurrence summary | roomId informativo | lista de membros, agenda completa |
| ClassMembershipStarted / Ended | membershipId, classId, patientId, fact effective date | enrollmentId, reasonCode no fim | cadastro civil, prontuário |
| ClassOccurrenceCreated | occurrenceId, classId, unitId, plannedInterval, professionalId | roomId | roster/lista de pacientes |
| ClassOccurrenceCancelled | occurrenceId, cancelledAt, reasonCode | — | lista de pacientes |
| SubstituteAssigned | occurrenceId, professionalId, assignedAt, reasonCode | priorProfessionalId | dados civis/profissionais completos |
| AssessmentFinalized | assessmentId, patientId, professionalId, finalizedAt | templateVersionId | respostas, diagnóstico, conteúdo, anexo |
| ClinicalEntryFinalized | clinicalEntryId, patientId, professionalId, careEpisodeId, finalizedAt | appointmentId, occurrenceId | narrativa, avaliação, diagnóstico, anexo |
| ClinicalEntryRectified | rectificationId, clinicalRecordId, patientId, authorProfessionalId, rectifiedAt, reasonCode | — | conteúdo original/corrigido |
| ClinicalAddendumAdded | addendumId, clinicalRecordId, patientId, authorProfessionalId, addedAt | — | texto do adendo |
| BreakGlassUsed | accessId, actorUserAccountId, patientId, resourceScope, usedAt, reasonCode | reviewReference | prontuário, justificativa livre, credencial/token |
| ClinicalRecordExported | exportOperationId, actorUserAccountId, patientId, scopeSummary, completedAt, outcome | privacyRequestId | arquivo, link público, conteúdo clínico |
| ContractAccepted | contractId, patientId, planVersionId, acceptedAt, billingTermsRef | enrollmentId, renewedFromContractId | Contract completo, CPF, credencial, dados bancários |
| ContractCompleted / Cancelled | contractId, patientId, fact timestamp | reasonCode no cancelamento | snapshot completo |
| EnrollmentActivated | enrollmentId, contractId, patientId, effectiveFrom, frequencyRef | — | plano/contrato completo |
| EnrollmentPaused | enrollmentId, patientId, pausePeriod, reasonCode | — | conteúdo clínico/financeiro detalhado |
| EnrollmentResumed | enrollmentId, patientId, resumedAt | — | disponibilidade inteira |
| EnrollmentCancelled / Completed | enrollmentId, contractId, patientId, fact timestamp | reasonCode no cancelamento | histórico completo |
| EnrollmentFrequencyChanged | enrollmentId, patientId, effectiveFrom, previousFrequencyRef, newFrequencyRef | — | PlanVersion completo |
| ReceivablesGenerated | generationId, contractId, receivableIds, generatedAt | — | objetos Receivable completos, CPF |
| PaymentConfirmed | paymentId, financialAccountId, confirmedAt, amount, currency | methodCode, minimal receivable refs | CPF, cartão, credencial bancária, token, Person completo |
| PaymentPartiallyReversed / Reversed | reversalId, paymentId, financialAccountId, reversedAt, amount, currency, reasonCode | — | dados bancários/CPF, Payment completo |
| RefundCompleted | refundId, financialAccountId, completedAt, amount, currency | relatedPaymentId | dados bancários, CPF, comprovante/arquivo |
| FinancialRestrictionApplied / Removed | restrictionId, patientId, fact timestamp, reasonCode | policyRef | dívida detalhada, CPF, histórico financeiro |
| ReceivableBecameOverdue | receivableId, dueDate, detectedAt, collectionPolicyRef | patientId ou payerRef mínimo | CPF, receivable completo, dados bancários |
| MonthClosed | closingId, period, snapshotVersion, closedAt, cutoff | — | snapshot, lista de pacientes/pagadores |
| ClosingReopened | closingId, period, previousSnapshotVersion, reopenedAt, reasonCode | — | snapshot, saldos detalhados desnecessários |

## 30. Producer / Consumer Matrix

| Producer | Event | Consumer | Reaction | Consistency |
|---|---|---|---|---|
| Organization | InstitutionalCalendarChanged | Scheduling/Pilates/Billing | reavaliar regras futuras próprias | EVENTUAL |
| People | PersonMergeCompleted/Reversed | detentores de PersonId | atualizar alias/referência corrente idempotentemente | WORKFLOW |
| Patients | PatientProfileCreated/Activated/Deactivated | CRM/Scheduling/Pilates/Plans | atualizar elegibilidade/projeção local; preservar histórico | EVENTUAL |
| Patients | ResponsiblePayerChanged | Plans/Billing | usar novo vínculo apenas em fatos futuros | EVENTUAL |
| Staff | EmploymentStarted/Ended | Identity/Scheduling/Pilates | rever acesso/elegibilidade futura | WORKFLOW |
| Staff | ProfessionalLeaveRegistered/Ended | Scheduling/Pilates | detectar impacto e abrir decisão humana | WORKFLOW |
| Scheduling | Appointment* | CRM | mover pipeline somente segundo regras CRM e purpose | WORKFLOW |
| Scheduling | Appointment*/CalendarExceptionApplied | Communication | avaliar comunicação prevista pelo owner/policy | EVENTUAL |
| Scheduling | AppointmentCompleted | Clinical | disponibilizar contexto; não criar prontuário automaticamente | EVENTUAL |
| Pilates | ClassScheduleChanged/Occurrence* | Scheduling | reconstruir agenda projetada | EVENTUAL |
| Pilates | MembershipStarted/Ended | Plans/Reports | refletir vínculo operacional, sem editar Enrollment | EVENTUAL |
| Clinical | finalization/correction/addendum events | Privacy & Audit | registrar evidência mínima | EVENTUAL |
| Clinical | BreakGlassUsed/ClinicalRecordExported | Privacy & Audit | trilha reforçada e revisão | EVENTUAL |
| Plans & Enrollment | ContractAccepted | Billing | gerar Receivables uma vez | WORKFLOW |
| Plans & Enrollment | ContractAccepted / EnrollmentActivated | CRM | concluir conversão segundo regras CRM | WORKFLOW |
| Plans & Enrollment | EnrollmentPaused/Cancelled/Completed | Billing | ajustar/cancelar apenas obrigações elegíveis | WORKFLOW |
| Plans & Enrollment | EnrollmentPaused/Resumed/Cancelled/Completed/FrequencyChanged | Pilates | alterar apenas memberships/direitos operacionais próprios | WORKFLOW |
| Billing | ReceivablesGenerated | Plans workflow | registrar conclusão/correlação sem possuir Receivables | EVENTUAL |
| Billing | PaymentConfirmed | Finance | criar uma inflow idempotente | EVENTUAL |
| Billing | PaymentPartiallyReversed/Reversed | Finance | criar compensação idempotente | EVENTUAL |
| Billing | RefundCompleted | Finance | criar outflow idempotente | EVENTUAL |
| Billing | restriction events | Pilates/Scheduling/Plans | aplicar/retirar efeito operacional local | EVENTUAL |
| Billing | overdue/restriction/payment events | Communication | enviar somente comunicação prevista | EVENTUAL |
| Finance | MonthClosed/ClosingReopened | Reports | atualizar projeção de fechamento | EVENTUAL |
| Finance | MonthClosed/ClosingReopened | Privacy & Audit | registrar evidência | EVENTUAL |

`SYNC_NOT_APPLICABLE` não aparece como reação: eventos não simulam validação síncrona. Consultas de conflito, existência, conta e autorização continuam contratos síncronos separados.

## 31. State Transition / Event Matrix

| State Transition | Event | Classification |
|---|---|---|
| Opportunity criação→NEW; CONTACT_INITIATED→QUALIFIED; pipeline/resultados | eventos CRM correspondentes | DOMAIN_EVENT / INTERNAL_ONLY |
| Opportunity QUALIFIED→EXPERIMENTAL_SCHEDULED | nenhum evento CRM público; AppointmentScheduled é do Scheduling | REJECTED alias |
| Appointment criação→SCHEDULED | AppointmentScheduled | BOTH / ADOPTED |
| Appointment SCHEDULED→CONFIRMED | AppointmentConfirmed | DOMAIN_EVENT / INTERNAL_ONLY |
| Appointment →COMPLETED/CANCELLED/NO_SHOW; reschedule self-transition | AppointmentCompleted/Cancelled/NoShowRecorded/Rescheduled | BOTH / ADOPTED |
| ClassOccurrence criação/cancelamento | ClassOccurrenceCreated/Cancelled | BOTH / ADOPTED |
| ClassOccurrence PLANNED→IN_PROGRESS→COMPLETED | ClassOccurrenceStarted/Completed | DOMAIN_EVENT / INTERNAL_ONLY |
| Attendance PENDING→resultado; resultado→resultado corrigido | AttendanceRecorded/Corrected | DOMAIN_EVENT / INTERNAL_ONLY |
| MakeupCredit lifecycle | MakeupCreditGranted/MakeupReserved/MakeupReservationCancelled/MakeupConsumed/MakeupExpired/Cancelled | DOMAIN_EVENT / INTERNAL_ONLY |
| CareEpisode lifecycle | CareEpisodeOpened/Paused/Resumed/Closed | DOMAIN_EVENT / INTERNAL_ONLY |
| Assessment DRAFT→FINALIZED | AssessmentFinalized | BOTH / ADOPTED |
| ClinicalEntry DRAFT→FINALIZED | ClinicalEntryFinalized | BOTH / ADOPTED |
| correção/adendo append-only | ClinicalEntryRectified/ClinicalAddendumAdded | BOTH / ADOPTED |
| Contract DRAFT→ACTIVE/→COMPLETED/→CANCELLED | ContractAccepted/Completed/Cancelled | BOTH / ADOPTED |
| Enrollment DRAFT→ACTIVE→PAUSED→ACTIVE; →COMPLETED/CANCELLED | EnrollmentActivated/Paused/Resumed/Completed/Cancelled | BOTH / ADOPTED |
| mudança temporal de frequência | EnrollmentFrequencyChanged | BOTH / ADOPTED |
| Receivable criação/alocação/reversal/cancel | ReceivableCreated/PaymentApplied/ReopenedByReversal/Cancelled | DOMAIN_EVENT / INTERNAL_ONLY |
| Receivable cruza condição derivada de vencimento | ReceivableBecameOverdue | BOTH / ADOPTED; não é state enum |
| Payment PENDING→CONFIRMED | PaymentConfirmed | BOTH / ADOPTED |
| Payment →PARTIALLY_REVERSED/REVERSED | PaymentPartiallyReversed/PaymentReversed | BOTH / ADOPTED |
| Refund criação→ISSUED→COMPLETED/CANCELLED | RefundIssued internal; RefundCompleted public; RefundCancelled internal | mixed |
| FinancialRestriction criação→ACTIVE→REMOVED | FinancialRestrictionApplied/Removed | BOTH / ADOPTED |
| Expense lifecycle | ExpenseCreated/Paid/Cancelled | DOMAIN_EVENT / INTERNAL_ONLY |
| Closing OPEN/REOPENED→CLOSED; CLOSED→REOPENED | MonthClosed/ClosingReopened | BOTH / ADOPTED |
| PatientProfile activate/inactivate | PatientActivated/PatientDeactivated | BOTH / ADOPTED |
| Professional employment/leave temporal lifecycle | Employment*/ProfessionalLeave* | BOTH / ADOPTED |

Não há evento para toda transição. DRAFT creation clínico, confirmação simples de Appointment, Attendance/Makeup e fatos financeiros locais permanecem internos.

## 32. Process / Event Matrix

| Processo | Comando inicial | Fatos relevantes | Consumers / resultado |
|---|---|---|---|
| PROC-PPL-001 | CreatePerson | PersonCreated (internal) | nenhum consumer obrigatório |
| PROC-PAC-001 | CreatePatientProfile | PatientProfileCreated, PatientActivated | contextos que usam PatientId |
| PROC-AGD-001 | Create/Change/EndScheduleRule | eventos internos de vigência | agenda local/read model |
| PROC-AGD-002 | ScheduleAppointment | AppointmentScheduled e fatos posteriores | CRM/Communication/Reports |
| PROC-PIL-001 | CreateClass | ClassCreated internal, ClassScheduleChanged | Scheduling/Reports |
| PROC-PIL-002 | AddPatientToClass | ClassMembershipStarted | Scheduling/Plans/Reports |
| PROC-PIL-003 | TransferPatient | MembershipEnded + MembershipStarted correlacionados | Scheduling/Plans/Reports |
| PROC-PIL-004 | StartOccurrence/RecordAttendance/CompleteOccurrence | Occurrence e Attendance internal; cancel public quando aplicável | projeções locais; Communication no cancelamento |
| PROC-PIL-005 | Grant/Reserve/ConsumeMakeup | Makeup lifecycle internal | projeções Pilates |
| PROC-CLI-001 | OpenCareEpisode | CareEpisodeOpened internal | projeções clínicas |
| PROC-CLI-002 | Create/FinalizeAssessment | AssessmentFinalized | Audit/timeline autorizada |
| PROC-CLI-003 | CreateClinicalEntry | ClinicalEntryCreated internal | PendingClinicalEntries |
| PROC-CLI-004 | FinalizeClinicalEntry | ClinicalEntryFinalized | Audit/read model clínico autorizado |
| PROC-CLI-005 | RectifyClinicalRecord | ClinicalEntryRectified | Audit/timeline autorizada |
| PROC-PLN-001 | Create/AcceptContract | ContractAccepted | Billing/CRM/Reports |
| PROC-ENR-001 | ActivateEnrollment | EnrollmentActivated | Pilates/Billing/CRM |
| PROC-ENR-002 | PauseEnrollment | EnrollmentPaused | Billing/Pilates/Communication |
| PROC-ENR-003 | ResumeEnrollment | EnrollmentResumed | Pilates/Billing |
| PROC-ENR-004 | CancelEnrollment/Contract | EnrollmentCancelled, ContractCancelled | Billing/Pilates/CRM/Reports |
| PROC-ENR-005 | RenewContract | novo ContractAccepted; ContractRenewed internal | Billing/CRM |
| PROC-ENR-006 | ChangeFrequency | EnrollmentFrequencyChanged | Billing/Pilates |
| PROC-ENR-007 | ChangeUnit/Class | MembershipEnded + MembershipStarted; frequência se mudou | Scheduling/Plans/Reports |
| PROC-BIL-001 | GenerateReceivables | ReceivablesGenerated | Plans workflow/Reports |
| PROC-BIL-002 | Register/ConfirmPayment | PaymentConfirmed | Finance/Communication/Reports |
| PROC-BIL-003 | AllocatePayment | PaymentAllocated internal | Billing projections |
| PROC-BIL-004 | AllocateAdvance/ApplyAuthorizedDiscount | adjustments/allocation internal | Billing projections |
| PROC-BIL-005 | CreateNegotiation | NegotiationCreated internal | Communication somente por intent explícita/fato posterior |
| PROC-BIL-006 | ReversePayment | PaymentPartiallyReversed ou PaymentReversed | Finance/Communication/Reports |
| PROC-BIL-007 | Issue/CompleteRefund | RefundIssued internal → RefundCompleted | Finance/Communication/Reports |
| PROC-BIL-008 | ApplyFinancialRestriction | FinancialRestrictionApplied | operational contexts/Communication |
| PROC-BIL-009 | RemoveFinancialRestriction | FinancialRestrictionRemoved | operational contexts/Communication |
| PROC-FIN-001 | RegisterExpense | ExpenseCreated internal | Finance read models |
| PROC-FIN-002 | PayExpense | ExpensePaid internal + FinancialTransactionCreated internal | Closing/read models |
| PROC-FIN-003 | TransferMoney | MoneyTransferred internal | Closing/read models |
| PROC-FIN-004 | ReconcileAccount | ReconciliationAdjusted internal | Closing/Audit policy |
| PROC-FIN-005 | CloseMonth | MonthClosed | Reports/Privacy & Audit |
| PROC-FIN-006 | ReopenClosing | ClosingReopened | Reports/Privacy & Audit |

Cobertura: **37/37 processos**. Processos single-context não foram artificialmente transformados em integração.

## 33. Event Chains

### Contratação

`AcceptContract` → `ContractAccepted` → Billing executa `GenerateReceivables` → `ReceivablesGenerated`.

`EnrollmentActivated` pode ocorrer no mesmo workflow, com a mesma correlação, mas não se presume atomicidade com Billing ou Pilates.

### Pagamento, reversal e refund

- `PaymentConfirmed` → Finance cria `FinancialTransaction(INFLOW)` → `FinancialTransactionCreated` interno.
- `PaymentPartiallyReversed`/`PaymentReversed` → Finance cria compensação; movimento anterior não é editado.
- `RefundIssued` é interno e não movimenta caixa; `RefundCompleted` → Finance cria `FinancialTransaction(OUTFLOW)`.

### Pausa e cancelamento

- `EnrollmentPaused` → Billing ajusta obrigações próprias → Pilates encerra/libera efeitos próprios → Communication atua quando policy exigir.
- `EnrollmentCancelled`/`ContractCancelled` → Billing ajusta somente futuro elegível → Pilates encerra vínculo operacional quando aplicável.
- Receivables vencidos não são perdoados automaticamente.

### Clínico

`ClinicalEntryFinalized` → Audit/read models clínicos autorizados recebem metadados mínimos. Nenhum conteúdo clínico trafega.

### Professional leave

`ProfessionalLeaveRegistered` → Scheduling/Pilates detectam compromissos/occurrences afetados → workflow humano decide substituição, reagendamento ou cancelamento. O evento sozinho não cancela nada.

### Merge

`PersonMergeCompleted` → cada detentor de PersonId aplica source→target idempotentemente nas próprias referências correntes; snapshots históricos permanecem.

## 34. Duplicate Event Review

| Candidatos | Decisão canônica |
|---|---|
| ContractAccepted vs ContractActivated | `ContractAccepted`; estado resultante é ACTIVE |
| ContractRenewed vs novo ContractAccepted | novo `ContractAccepted` com `renewedFromContractId?`; `ContractRenewed` internal-only |
| PlanFrequencyChanged vs EnrollmentFrequencyChanged | `EnrollmentFrequencyChanged`; primeiro deprecated |
| ExperimentalScheduled/Completed (CRM) vs Appointment facts | Scheduling publica `AppointmentScheduled/Completed` com purpose EXPERIMENTAL |
| PatientTransferredClass vs MembershipEnded+Started | dois fatos correlacionados; evento composto rejeitado |
| ClassMembershipEnded vs PatientRemovedFromClass | `ClassMembershipEnded`; alias rejeitado |
| RefundIssued vs RefundCompleted | ambos distintos internamente; somente `RefundCompleted` é fato de caixa público |
| MonthClosed vs ClosingCompleted | `MonthClosed`; segundo deprecated |
| ExpenseRegistered vs ExpenseCreated | `ExpenseCreated`; primeiro deprecated |
| AccountTransferCompleted vs MoneyTransferred | `MoneyTransferred`; primeiro deprecated |
| ReceivableOverdue vs DelinquencyStarted | `ReceivableBecameOverdue`; delinquency continua condição derivada |
| BreakGlassUsed vs BreakGlassAudited | `BreakGlassUsed`; Audit registra consequência, sem segundo evento redundante |

## 35. Rejected Events

- comandos no imperativo: `ConfirmPayment`, `PauseEnrollment`, `SendMessage`, `CreateContract`;
- atualizações de projeção: `AgendaViewUpdated`, `BillingDashboardUpdated`, `ReportRecalculated`;
- setters de campo e eventos amplos: `PersonUpdated`, `UnitUpdated`, `PersonNameChanged`, `PersonPhoneChanged`, `PersonAddressLineChanged`;
- aliases/duplicações: `ExperimentalCompleted` pelo CRM, `PatientTransferredClass`, `PatientRemovedFromClass`, `BreakGlassAudited`;
- `AuditLogCreated` como publicação pública;
- qualquer evento de Reports que alegue fato transacional.

## 36. Deferred Events

- lifecycle de UserAccount/session/access até IAM-001..007;
- `ProfessionalAvailabilityChanged` até contrato de Availability;
- `NegotiationChanged` até lifecycle/alçadas;
- status de entrega público de Communication até DOM-019;
- `DocumentStored/VersionCreated/Removed` até consumidores/retenção/versionamento;
- PrivacyRequest lifecycle até modelo LGPD/legal hold;
- comunicação de campanha e clínica: usar intents explícitas após policies, não inventar evento agora.

## 37. Security / Privacy Rules

1. Least data e finalidade explícita por consumer.
2. Nunca transportar segredo, senha, credential, token, session, dado completo de cartão/conta ou URL pública de documento.
3. CPF, nome, telefone, e-mail e endereço não entram sem finalidade específica aprovada; nenhum evento atual exige CPF.
4. Evento clínico não leva corpo, diagnóstico textual, resposta, observação, retificação textual, addendum, anexo ou export.
5. Eventos financeiros levam valor/moeda/conta somente quando Finance precisa registrar o movimento; não levam pagador completo.
6. `SECURITY_SENSITIVE`, `CLINICAL_METADATA` e `FINANCIAL` exigem acesso minimizado, logs redigidos e auditoria proporcional na arquitetura física.
7. `eventId`, correlation e causation não podem carregar dado pessoal embutido.
8. Evento recebido não autoriza a ação: consumer revalida sua policy/guard local.

## 38. Integration Risks

| Risco | Requisito arquitetural futuro |
|---|---|
| publicação/entrega duplicada | identificação do evento e consumo idempotente |
| fora de ordem | ordem somente por aggregate/subject e guards de versão/causalidade |
| atraso/consumer indisponível | processamento recuperável, observabilidade e reconciliação |
| replay | consumers seguros para replay e retenção compatível com sensibilidade |
| schema evolution | versionamento e compatibilidade explícitos |
| poison event | isolamento, diagnóstico e recuperação sem vazar payload |
| stale projection | indicar freshness e reconstruir a partir da fonte autorizada |
| vazamento sensível | minimização, controles de acesso, redaction e propósito |
| perda entre commit e publicação | reliable publication; outbox é candidato futuro, não decisão |
| reprocessamento financeiro | inbox/deduplicação é candidato futuro; correlação obrigatória |

## 39. Cross-Context Consistency

Eventos produzem consistência `EVENTUAL` ou coordenam `WORKFLOW`. Nenhuma cadeia é transação distribuída e nenhum consumer edita aggregate do publisher. Falha parcial deixa fatos já concluídos válidos e exige retomada/reconciliação futura; não há rollback silencioso entre contexts.

Contratos síncronos continuam necessários para validações como conflito, existência de Person/Patient/Professional, elegibilidade atual, conta financeira e autorização. Evento não finge resposta síncrona.

## 40. Architecture Consequences

- A arquitetura física deverá garantir publicação confiável e consumo idempotente onde marcado.
- Outbox/inbox são candidatos de pattern, não escolhas desta tarefa.
- Deverá existir correlação/causação observável, evolução versionada, replay controlado e segregação de eventos sensíveis.
- Dependências devem preservar publisher owner, impedir ciclos de escrita e separar contrato síncrono, evento e read model.
- Estratégia de broker, serialização, schema registry, retry e armazenamento fica para ARC-003/007/008.

## 41. Ambiguities Resolved

1. `RefundCompleted`, não `RefundIssued`, representa saída real.
2. Reversal parcial tem nome próprio; reversal integral usa `PaymentReversed`.
3. `ReceivableBecameOverdue` registra a passagem temporal sem transformar OVERDUE em estado persistido.
4. Experimental usa fatos de Appointment de Scheduling; CRM não duplica ownership.
5. Renovação cria novo Contract; `ContractAccepted` é o contrato público e `ContractRenewed` é resumo interno.
6. Transferência de turma é dois fatos de membership correlacionados.
7. `MonthClosed` substitui `ClosingCompleted`.
8. Audit trail não é integration event para toda alteração.
9. Communication usa FACT_DRIVEN para fatos objetivos aprovados e EXPLICIT_INTENT para campanha/ad-hoc/clínico.
10. Eventos de Clinical publicados são apenas metadata; drafts e documentos ficam internos.

## 42. Remaining Ambiguities

| Tema | Classificação | Efeito no catálogo |
|---|---|---|
| ciclo público de Identity | BLOCKING BEFORE IMPLEMENTATION | eventos IAM deferred; não bloqueia AUTH-001 |
| efeito da pausa em MakeupCredit | BLOCKING BEFORE IMPLEMENTATION | nenhum evento/efeito inventado |
| Receivables vencidos no cancelamento | BLOCKING BEFORE IMPLEMENTATION | Contract/Enrollment events não ordenam perdão |
| precedência financeira concorrente e reallocation | BLOCKING BEFORE IMPLEMENTATION | fatos append-only preservados; guards futuros |
| Negotiation e descontos | BLOCKING BEFORE IMPLEMENTATION | mudanças públicas deferred |
| status público de Communication | BLOCKING BEFORE ARCHITECTURE | identificar consumers/SLA em DOM-019/ARC |
| Documents version/retention | BLOCKING BEFORE ARCHITECTURE | eventos Documents deferred |
| PrivacyRequest/LGPD/legal hold | BLOCKING BEFORE GO-LIVE | lifecycle deferred |

## 43. Open Questions

### BLOCKING FOR AUTH-001

Nenhuma.

### BLOCKING BEFORE ARCHITECTURE

- Qual contrato físico representará versionamento, correlation/causation e segregação de payloads sensíveis?
- Quais consumers de status de entrega de Communication justificam publicação?
- Quais categorias de Document exigem versionamento/evento e sob qual retenção?

### BLOCKING BEFORE IMPLEMENTATION

- Como operar reallocation/reversal concorrente, vencidos no cancelamento, MakeupCredit durante pausa e alçadas de negociação/desconto?
- Quais detalhes de Availability e IAM serão públicos?

### BLOCKING BEFORE GO-LIVE

- Retenção/legal hold/LGPD, requisitos clínicos/regulatórios, suporte privilegiado, backup/restore e migração.

### NON_BLOCKING

- Provider de canal/storage/observabilidade, KPIs, categorias/motivos e comprovantes opcionais.

## 44. Consequences for AUTH-001

**READY.** O catálogo identifica comandos e transições sensíveis, owners e classes de dados. AUTH-001 deve mapear permissions/policies para:

- merge/reversal de Person e inativação de Patient;
- Employment/Leave e impacto de acesso;
- reschedule/cancel/no-show e exceções de calendário;
- correção de Attendance e exceções de Makeup;
- finalização, retificação, addendum, break-glass e exportação clínica;
- aceite/cancelamento/pausa/retomada/frequência;
- confirmação/reversal/refund/restrição/negociação;
- fechamento/reabertura e operações financeiras.

Step-up deve ser avaliado em AUTH-001 para merge sensível, break-glass, exportação clínica, reversal/refund, alteração de restrição, Closing reopen e ações financeiras/privilegiadas. Audit reason é obrigatório nos pontos já marcados por STATE-001.

## 45. Consequences for Physical Architecture

ARC-003 e etapas físicas recebem um catálogo sem dependência de tecnologia. Devem preservar boundaries, escolher contratos síncronos versus eventos, definir reliable publication/idempotent consumption, versionamento, observabilidade, segurança, replay e freshness de projeções. Não podem converter `INTERNAL_ONLY`/`DEFERRED` em publicação por conveniência técnica.

## 46. Validation Criteria

- [x] terminologia e diferença command/event estão explícitas;
- [x] domain/integration events estão separados;
- [x] todo integration event possui owner e publisher correto;
- [x] todos os nomes adotados representam passado;
- [x] nenhum evento autoriza escrita externa;
- [x] payload mínimo e forbidden data estão definidos;
- [x] Clinical não expõe conteúdo do prontuário;
- [x] dados pessoais/financeiros são minimizados e credenciais/tokens são proibidos;
- [x] consumidores, idempotência, ordering, auditoria e failure impact estão classificados;
- [x] não existe ordering global;
- [x] duplicidades/aliases foram resolvidos;
- [x] read model e audit não foram confundidos com domain event;
- [x] Reports não publica fatos transacionais;
- [x] STATE-001, MODEL-005, 37/37 processos e regras cross-context permanecem coerentes;
- [x] nenhuma tecnologia de mensageria foi escolhida;
- [x] não existe blocker para AUTH-001.

**Resultado:** EVT-001 — PASS. **AUTH-001 — READY.**
