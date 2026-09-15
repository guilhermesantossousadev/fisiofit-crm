# Glossário de Domínio

## Status

APROVADO PARA MODELAGEM — baseline do Gate M1 em 2026-09-15.

Este glossário fixa o sentido dos termos usados na modelagem conceitual. Nomes técnicos podem ser refinados durante o Context Map, mas não podem mudar a responsabilidade de negócio sem nova decisão.

| Termo | Definição curta | Não confundir com | Domínio proprietário |
|---|---|---|---|
| Person | Identidade central de uma pessoa real, capaz de acumular vários papéis. | PatientProfile, UserAccount ou cadastro por unidade. | People |
| Patient | Papel assistencial de uma Person na clínica. | Usuário do sistema ou matrícula. | Patients |
| PatientProfile | Dados administrativos específicos do papel de paciente, sem duplicar a identidade civil. | Person ou prontuário clínico. | Patients |
| Professional | Papel ocupacional de uma Person que atua na clínica. | Conta de acesso. | Staff |
| ProfessionalProfile | Perfil profissional e vínculos de atuação da Person. | UserAccount ou autoria clínica apagável. | Staff |
| UserAccount | Conta usada para autenticação e acesso, vinculada a uma Person. | Person, ProfessionalProfile ou autoridade de negócio. | Identity & Access |
| Guardian | Person ligada a um paciente por responsabilidade legal. | Responsável administrativo ou pagador, embora uma mesma Person possa acumular papéis. | Patients |
| AdministrativeResponsibleLink | Vínculo vigente entre PatientProfile e uma Person autorizada a tratar assuntos administrativos definidos da clínica. | GuardianLink, ResponsiblePayerLink ou consentimento clínico; a mesma Person pode acumular papéis por vínculos distintos. | Patients |
| ResponsiblePayer | Person ligada ao paciente como pagador vigente; Contract e Receivable preservam snapshots históricos próprios. | Patient; podem ser pessoas diferentes. | Patients |
| Opportunity | Ciclo comercial concreto associado a uma Person. | Lead como cadastro separado ou PatientProfile. | CRM |
| Class | Identidade operacional de uma turma recorrente. | ClassSchedule ou aula de uma data específica. | Pilates |
| ClassSchedule | Regra recorrente vigente de dias, horário, profissional e capacidade de uma Class. | ClassOccurrence. | Pilates |
| ClassMembership | Vínculo com vigência entre paciente e turma fixa. | Enrollment ou presença em uma ocorrência. | Pilates |
| ClassOccurrence | Instância de uma turma em data e horário concretos. | Recorrência/ClassSchedule. | Pilates |
| Attendance | Resultado individual da participação esperada de um paciente em uma ocorrência. | Estado global da ocorrência ou evolução clínica. | Pilates |
| MakeupCredit | Direito controlado a uma reposição, originado por evento elegível. | Falta, reagendamento ou reserva. | Pilates |
| MakeupReservation | Reserva de um MakeupCredit em uma ocorrência com vaga. | Alteração do ClassMembership. | Pilates |
| Appointment | Compromisso ad-hoc, como avaliação, experimental, reposição, extraordinário ou encaixe. | Grade fixa ou ClassOccurrence. | Scheduling |
| CareEpisode | Agrupador longitudinal de registros de um período ou objetivo assistencial. | Contract ou atendimento isolado. | Clinical |
| Assessment | Avaliação ou reavaliação clínica estruturada. | ClinicalEntry de acompanhamento cotidiano. | Clinical |
| ClinicalTemplate | Conceito lógico de formulário/estrutura clínica que possui versões publicadas. | ClinicalTemplateVersion usada em um registro histórico. | Clinical |
| ClinicalTemplateVersion | Versão imutável da estrutura de um ClinicalTemplate, preservada no registro que a utilizou. | Template vigente hoje ou edição retroativa de registro. | Clinical |
| ClinicalEntry | Registro clínico longitudinal, inicialmente DRAFT e depois FINALIZED. | Nota administrativa ou Attendance. | Clinical |
| Rectification | Correção associada a registro clínico finalizado, preservando o original. | Addendum ou edição destrutiva do registro. | Clinical |
| Addendum | Complementação posterior associada a registro clínico finalizado, preservando o original. | Rectification ou edição destrutiva do registro. | Clinical |
| Plan | Oferta comercial do catálogo. | Contract ou preço histórico aceito. | Plans / Enrollment |
| PlanVersion | Versão imutável das condições e preços de um Plan em determinada vigência. | Alteração retroativa de contrato. | Plans / Enrollment |
| Contract | Snapshot das condições comerciais aceitas por paciente/pagador. | PlanVersion vigente hoje ou Enrollment. | Plans / Enrollment |
| Enrollment | Vínculo operacional do paciente com o serviço contratado. | Contract ou ClassMembership. | Plans / Enrollment |
| Receivable | Obrigação financeira de valor e vencimento definidos. | Payment ou entrada em caixa. | Billing |
| Payment | Recebimento efetivamente confirmado e não apagável. | Receivable ou receita prevista. | Billing |
| PaymentAllocation | Aplicação de valor de um Payment a um ou mais Receivables. | Payment ou ajuste de dívida. | Billing |
| PaymentReversal | Operação que corrige total ou parcialmente um Payment preservando o original. | Exclusão do pagamento ou Refund. | Billing |
| Refund | Devolução de valor elegível já pago ao pagador. | PaymentReversal ou cancelamento de Receivable. | Billing |
| BillingAdjustment | Ajuste autorizado sobre uma obrigação, com motivo e auditoria. | Perdão arbitrário de dívida. | Billing |
| FinancialRestriction | Restrição operacional independente decorrente de inadimplência. | Enrollment PAUSED ou cancelamento. | Billing |
| FinancialAccount | Conta onde dinheiro é mantido, do tipo BANK_ACCOUNT ou CASH. | Saldo digitado ou categoria de despesa. | Finance |
| FinancialTransaction | Movimento que compõe o saldo derivado de uma FinancialAccount. | Receivable ou Contract. | Finance |
| Expense | Obrigação/saída da clínica, classificada por categoria e escopo UNIT ou GLOBAL. | Receivable de cliente. | Finance |
| Transfer | Movimento pareado entre contas financeiras, sem ser receita ou despesa. | Pagamento ou Expense. | Finance |
| Closing | Snapshot/versionamento da conferência mensal financeira, reabrível sem apagar versões anteriores. | Saldo ou trava irreversível. | Finance |

## Convenções terminológicas

- “Lead” é uma visão operacional de Person com Opportunity; não é entidade de identidade.
- “Aluno” pode ser usado na operação, mas corresponde a Patient com Enrollment e, quando aplicável, ClassMembership.
- “Evolução” é o nome operacional de ClinicalEntry.
- “Reposição” envolve MakeupCredit e MakeupReservation; não altera automaticamente a turma fixa.
- Billing é dono de obrigações e liquidações de clientes; Finance é dono de contas, movimentos, despesas, conciliação e fechamento.

## Referências

- `PROJECT_OS.md`, seções 5 a 9.
- Caderno Mestre, capítulos 4, 6 e 11 a 18.
- Decisões oficiais do Gate M1, registradas em `docs/decisions/DECISIONS.md`.
