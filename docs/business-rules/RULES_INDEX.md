# Índice Canônico de Regras de Negócio

## Status

Baseline aprovada no Gate M1 em 2026-09-15. Este índice evita IDs concorrentes; detalhes e parâmetros estão nos documentos de domínio.

| ID | Nome | Domínio | Status | Fonte canônica |
|---|---|---|---|---|
| RB-PPL-001 | Person é a identidade central para múltiplos papéis | People | APPROVED | `docs/domain/01-people-patients.md` |
| RB-PPL-002 | Person pode existir sem CPF e CPF informado é único | People | APPROVED | `docs/domain/01-people-patients.md` |
| RB-PPL-003 | ProfessionalProfile e UserAccount são distintos | People / Staff / Identity | APPROVED | `docs/domain/01-people-patients.md` |
| RB-PPL-004 | Merge exige revisão, histórico e auditoria | People | APPROVED | `docs/domain/01-people-patients.md` |
| RB-PAC-001 | PatientProfile não duplica identidade civil | Patients | APPROVED | `docs/domain/01-people-patients.md` |
| RB-PAC-002 | Paciente e responsável são Persons distintas | Patients | APPROVED | `docs/domain/01-people-patients.md` |
| RB-PAC-003 | Pagador pode diferir do paciente | Patients / Billing | APPROVED | `docs/domain/01-people-patients.md` |
| RB-PAC-004 | Paciente não sofre hard delete operacional | Patients | APPROVED | `docs/domain/01-people-patients.md` |
| RB-CRM-001 | Opportunity é a entidade do ciclo comercial | CRM | APPROVED | `docs/domain/02-crm.md` |
| RB-CRM-002 | Uma Person pode ter múltiplas Opportunities | CRM | APPROVED | `docs/domain/02-crm.md` |
| RB-CRM-003 | LossReason é obrigatório em perda | CRM | APPROVED | `docs/domain/02-crm.md` |
| RB-CRM-004 | Perda por silêncio não é automática | CRM | APPROVED | `docs/domain/02-crm.md` |
| RB-CRM-005 | Próxima ação é exigida após o primeiro contato | CRM | APPROVED | `docs/domain/02-crm.md` |
| RB-CRM-006 | Conversão exige contrato/matrícula aceitos, não pagamento | CRM | APPROVED | `docs/domain/02-crm.md` |
| RB-CRM-007 | Experimental é Appointment ad-hoc | CRM / Scheduling | APPROVED | `docs/domain/02-crm.md` |
| RB-AGD-001 | Grade regular é fixa e recorrente | Scheduling | APPROVED | `docs/domain/03-scheduling.md` |
| RB-AGD-002 | Profissional não pode ter sobreposição | Scheduling | APPROVED | `docs/domain/03-scheduling.md` |
| RB-AGD-003 | Paciente não pode ter sobreposição | Scheduling | APPROVED | `docs/domain/03-scheduling.md` |
| RB-AGD-004 | Mudança permanente usa vigência | Scheduling | APPROVED | `docs/domain/03-scheduling.md` |
| RB-AGD-005 | Mudança pontual afeta somente ocorrência | Scheduling | APPROVED | `docs/domain/03-scheduling.md` |
| RB-AGD-006 | Sala é informativa e não bloqueia conflito | Scheduling | APPROVED | `docs/domain/03-scheduling.md` |
| RB-AGD-007 | Equipamentos não participam do Scheduling | Scheduling | APPROVED | `docs/domain/03-scheduling.md` |
| RB-AGD-008 | Cancelamento/reagendamento preserva histórico | Scheduling | APPROVED | `docs/domain/03-scheduling.md` |
| RB-AGD-009 | Ad-hoc não cria vínculo recorrente | Scheduling | APPROVED | `docs/domain/03-scheduling.md` |
| RB-PIL-001 | ClassSchedule recorrente difere de ClassOccurrence | Pilates | APPROVED | `docs/domain/04-pilates.md` |
| RB-PIL-002 | ClassMembership possui vigência e histórico | Pilates | APPROVED | `docs/domain/04-pilates.md` |
| RB-PIL-003 | Remover paciente não apaga histórico | Pilates | APPROVED | `docs/domain/04-pilates.md` |
| RB-PIL-004 | Capacidade pertence à turma e deve ser respeitada | Pilates | APPROVED | `docs/domain/04-pilates.md` |
| RB-PIL-005 | Overbooking não é permitido inicialmente | Pilates | APPROVED | `docs/domain/04-pilates.md` |
| RB-PIL-006 | Reposição não altera turma fixa | Pilates | APPROVED | `docs/domain/04-pilates.md` |
| RB-PIL-007 | Ausência elegível gera no máximo um crédito | Pilates | APPROVED | `docs/domain/04-pilates.md` |
| RB-PIL-008 | Falta sem aviso não gera crédito automaticamente | Pilates | APPROVED | `docs/domain/04-pilates.md` |
| RB-PIL-009 | Falta em reposição não gera outro crédito automaticamente | Pilates | APPROVED | `docs/domain/04-pilates.md` |
| RB-PIL-010 | Reposição ocupa vaga real e respeita conflitos | Pilates | APPROVED | `docs/domain/04-pilates.md` |
| RB-CLI-001 | Conteúdo clínico é segregado | Clinical | APPROVED | `docs/domain/05-clinical.md` |
| RB-CLI-002 | Registro clínico preserva autoria | Clinical | APPROVED | `docs/domain/05-clinical.md` |
| RB-CLI-003 | ClinicalEntry FINALIZED é imutável | Clinical | APPROVED | `docs/domain/05-clinical.md` |
| RB-CLI-004 | Correção usa Rectification/Addendum | Clinical | APPROVED | `docs/domain/05-clinical.md` |
| RB-CLI-005 | Acesso administrativo não concede acesso clínico | Clinical / Security | APPROVED | `docs/domain/05-clinical.md` |
| RB-CLI-006 | Break-glass é excepcional e auditado | Clinical / Security | APPROVED | `docs/domain/05-clinical.md` |
| RB-CLI-007 | Anexo clínico usa storage privado | Clinical | APPROVED | `docs/domain/05-clinical.md` |
| RB-CLI-008 | Exportação clínica é sensível e auditada | Clinical | APPROVED | `docs/domain/05-clinical.md` |
| RB-PLN-001 | Mudança de preço cria PlanVersion | Plans | APPROVED | `docs/domain/06-plans-enrollment.md` |
| RB-PLN-002 | Contract é snapshot das condições aceitas | Plans | APPROVED | `docs/domain/06-plans-enrollment.md` |
| RB-PLN-003 | Renovação cria novo Contract | Plans | APPROVED | `docs/domain/06-plans-enrollment.md` |
| RB-ENR-001 | Pró-rata usa aulas restantes × H/A da PlanVersion | Enrollment | APPROVED | `docs/domain/06-plans-enrollment.md` |
| RB-ENR-002 | Pausa máxima é 15 dias | Enrollment | APPROVED | `docs/domain/06-plans-enrollment.md` |
| RB-ENR-003 | Pausa libera vaga, não cobra período e não prolonga contrato | Enrollment | APPROVED | `docs/domain/06-plans-enrollment.md` |
| RB-ENR-004 | Retorno de pausa depende de disponibilidade | Enrollment | APPROVED | `docs/domain/06-plans-enrollment.md` |
| RB-ENR-005 | Cancelamento é imediato, sem multa e preserva histórico | Enrollment | APPROVED | `docs/domain/06-plans-enrollment.md` |
| RB-ENR-006 | Mudança de frequência possui vigência | Enrollment | APPROVED | `docs/domain/06-plans-enrollment.md` |
| RB-ENR-007 | Mudança de unidade não recria paciente | Enrollment / Pilates | APPROVED | `docs/domain/06-plans-enrollment.md` |
| RB-BIL-001 | Receivable é obrigação e Payment é liquidação | Billing | APPROVED | `docs/domain/07-billing.md` |
| RB-BIL-002 | Todos os Receivables nascem na contratação/ativação | Billing | APPROVED | `docs/domain/07-billing.md` |
| RB-BIL-003 | Vencimento usa 5/10/15/20/25 e próximo útil | Billing | APPROVED | `docs/domain/07-billing.md` |
| RB-BIL-004 | Primeiro período parcial cobra apenas pró-rata | Billing | APPROVED | `docs/domain/07-billing.md` |
| RB-BIL-005 | Pagamento parcial e antecipado são permitidos | Billing | APPROVED | `docs/domain/07-billing.md` |
| RB-BIL-006 | Não há multa nem juros | Billing | APPROVED | `docs/domain/07-billing.md` |
| RB-BIL-007 | Inadimplência gera FinancialRestriction, não PAUSED | Billing | APPROVED | `docs/domain/07-billing.md` |
| RB-BIL-008 | Payment confirmado não é apagado | Billing | APPROVED | `docs/domain/07-billing.md` |
| RB-BIL-009 | Correção de Payment usa PaymentReversal | Billing | APPROVED | `docs/domain/07-billing.md` |
| RB-BIL-010 | Estorno parcial e reembolso são permitidos | Billing | APPROVED | `docs/domain/07-billing.md` |
| RB-BIL-011 | Conta de recebimento e registrador são obrigatórios | Billing | APPROVED | `docs/domain/07-billing.md` |
| RB-BIL-012 | Perdão arbitrário não é operação normal | Billing | APPROVED | `docs/domain/07-billing.md` |
| RB-FIN-001 | FinancialAccount suporta BANK_ACCOUNT e CASH | Finance | APPROVED | `docs/domain/08-finance.md` |
| RB-FIN-002 | Saldo é derivado das movimentações | Finance | APPROVED | `docs/domain/08-finance.md` |
| RB-FIN-003 | Expense tem escopo UNIT ou GLOBAL | Finance | APPROVED | `docs/domain/08-finance.md` |
| RB-FIN-004 | Transferência não é receita nem despesa | Finance | APPROVED | `docs/domain/08-finance.md` |
| RB-FIN-005 | Fechamento é mensal e não exige zerar pendências | Finance | APPROVED | `docs/domain/08-finance.md` |
| RB-FIN-006 | Reabertura preserva snapshot anterior | Finance | APPROVED | `docs/domain/08-finance.md` |
| RB-FIN-007 | Comissão fica fora do MVP | Finance | APPROVED | `docs/domain/08-finance.md` |
| RB-SEC-001 | Autorização é deny-by-default e contextual | Security | APPROVED | `PROJECT_OS.md` |
| RB-SEC-002 | Autoridade técnica não implica autoridade de negócio | Security | APPROVED | `PROJECT_OS.md` |
| RB-AUD-001 | Operações sensíveis têm trilha de auditoria | Audit | APPROVED | `PROJECT_OS.md`; Caderno 33 |
| RB-COM-001 | n8n/canal externo não é dono da regra | Communication | APPROVED | `PROJECT_OS.md`; Caderno 19 |

## Equivalências consolidadas

- A antiga regra “RB-AGD-005 Capacidade” do capítulo 13 do Caderno foi consolidada em `RB-PIL-004`, pois capacidade pertence à turma/Pilates; Scheduling apenas consulta a validação.
- As formulações “alteração futura não modifica ocorrência passada”, “remoção encerra vínculo” e “histórico preservado” foram mantidas como regras específicas por agregado, não duplicadas como IDs genéricos.
- A antiga `RB-BIL-007` do capítulo 46, que usava suspensão após 15 dias, foi superada pela combinação `RB-BIL-007` atual + parâmetros de 5 dias/D+2; o ID atual representa a regra oficial deste Gate.
- A antiga `RB-FIN-002` do capítulo 46 limitava fechamento/reabertura à Proprietária; a regra atual de permissão está nos documentos de domínio/decisões e exige autorização explícita, inclusive para Desenvolvedor.

## Governança

Novas regras recebem novo ID somente quando não forem equivalentes a uma regra existente. Alterações incompatíveis exigem decisão registrada e preservação da versão anterior na história do Git.
