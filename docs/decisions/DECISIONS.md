# Log de Decisões

As datas abaixo indicam o registro nesta consolidação quando a fonte não traz data individual da decisão. Todas as decisões estão `APPROVED`; conflitos históricos são descritos na consequência ou na auditoria.

## DEC-001 — Person como identidade central

Status: APPROVED
Data: 2026-09-15 — registrada nesta consolidação
Contexto: uma pessoa pode ser paciente, profissional, usuária, responsável e pagadora.
Decisão: usar uma Person central, permitir ausência de CPF e exigir unicidade quando informado.
Consequências: perfis e vínculos não duplicam identidade; merge preserva histórico e auditoria.
Impacta: People, Patients, Staff, Identity, CRM e Billing.
Fonte: decisões oficiais do Gate M1; Caderno 11.

## DEC-002 — Monólito modular como direção arquitetural

Status: APPROVED
Data: 2026-09-15 — registrada nesta consolidação
Contexto: o produto precisa de boundaries claros sem complexidade distribuída prematura.
Decisão: seguir monólito modular; módulos não acessam tabelas internas uns dos outros por atalho.
Consequências: Context Map e Ownership Map antecedem arquitetura física.
Impacta: todos os domínios.
Fonte: `PROJECT_OS.md`; Caderno 6 e 26–27.

## DEC-003 — Sala é informação não bloqueante

Status: APPROVED
Data: 2026-09-15 — registrada nesta consolidação
Contexto: sala ajuda a operação, mas não define disponibilidade rígida.
Decisão: sala não participa de conflito nem capacidade.
Consequências: conflitos consideram profissional, paciente e capacidade da turma.
Impacta: Scheduling, Organization e Pilates.
Fonte: decisões oficiais do Gate M1; Caderno 13.

## DEC-004 — Equipamento fora do Scheduling

Status: APPROVED
Data: 2026-09-15 — registrada nesta consolidação
Contexto: equipamentos não são reserváveis por horário.
Decisão: excluir equipamentos da disponibilidade e conflito de agenda.
Consequências: eventual patrimônio/manutenção será contexto separado.
Impacta: Scheduling.
Fonte: decisões oficiais do Gate M1; Caderno 13.

## DEC-005 — Recorrência e ocorrência são conceitos distintos

Status: APPROVED
Data: 2026-09-15 — registrada nesta consolidação
Contexto: grade fixa não pode ser confundida com o que ocorreu em uma data.
Decisão: separar Class, ClassSchedule, ClassOccurrence e Appointment ad-hoc.
Consequências: mudança pontual não altera recorrência; mudança permanente usa vigência.
Impacta: Scheduling e Pilates.
Fonte: decisões oficiais do Gate M1; Caderno 13–14.

## DEC-006 — Histórico operacional é preservado

Status: APPROVED
Data: 2026-09-15 — registrada nesta consolidação
Contexto: o presente não pode reescrever o passado.
Decisão: encerramentos, transferências, cancelamentos e correções preservam registros e autoria.
Consequências: não há hard delete operacional de pacientes, memberships, pagamentos ou fechamentos.
Impacta: todos os domínios transacionais.
Fonte: `PROJECT_OS.md`; decisões oficiais do Gate M1.

## DEC-007 — ClinicalEntry finalizado é imutável

Status: APPROVED
Data: 2026-09-15 — registrada nesta consolidação
Contexto: prontuário exige integridade e autoria.
Decisão: ClinicalEntry usa DRAFT e FINALIZED; correções usam Rectification/Addendum.
Consequências: FINALIZED não retorna a rascunho nem é sobrescrito.
Impacta: Clinical, Security e Audit.
Fonte: decisões oficiais do Gate M1; Caderno 15.

## DEC-008 — PlanVersion preserva condições históricas

Status: APPROVED
Data: 2026-09-15 — registrada nesta consolidação
Contexto: mudanças futuras de preço não podem alterar acordos existentes.
Decisão: mudança de preço/condição cria PlanVersion e Contract mantém snapshot aceito.
Consequências: renovação cria novo Contract com versão vigente, salvo exceção registrada.
Impacta: Plans, Enrollment e Billing.
Fonte: decisões oficiais do Gate M1.

## DEC-009 — Receivables são criados na contratação

Status: APPROVED
Data: 2026-09-15 — registrada nesta consolidação
Contexto: obrigações futuras precisam estar visíveis desde o fechamento/ativação.
Decisão: criar todos os Receivables do Contract nesse momento.
Consequências: cancelamento e pausa ajustam obrigações futuras de modo rastreável, sem geração mensal implícita.
Impacta: Plans/Enrollment e Billing.
Fonte: decisões oficiais do Gate M1.

## DEC-010 — Pró-rata por aulas restantes e H A

Status: APPROVED
Data: 2026-09-15 — registrada nesta consolidação
Contexto: início pode ocorrer em qualquer data do mês.
Decisão: pró-rata = quantidade de aulas restantes × H/A da PlanVersion.
Consequências: substitui a fórmula mensal equivalente do capítulo 46 do Caderno.
Impacta: Plans/Enrollment e Billing.
Fonte: decisões oficiais do Gate M1.

## DEC-011 — Pausa máxima de 15 dias

Status: APPROVED
Data: 2026-09-15 — registrada nesta consolidação
Contexto: a pausa precisa de limite operacional.
Decisão: pausa é permitida por no máximo 15 dias.
Consequências: supera a pausa sem limite do capítulo 46 do Caderno.
Impacta: Enrollment, Pilates e Billing.
Fonte: decisões oficiais do Gate M1.

## DEC-012 — Pausa não mantém vaga nem prolonga contrato

Status: APPROVED
Data: 2026-09-15 — registrada nesta consolidação
Contexto: vaga e duração contratual devem ter comportamento claro.
Decisão: pausa libera a vaga, suspende cobrança correspondente e não move a data final; retorno depende de disponibilidade.
Consequências: supera a prorrogação de contrato/créditos descrita no capítulo 46.
Impacta: Enrollment, Pilates e Billing.
Fonte: decisões oficiais do Gate M1.

## DEC-013 — Cancelamento imediato sem multa

Status: APPROVED
Data: 2026-09-15 — registrada nesta consolidação
Contexto: encerramento antecipado foi autorizado.
Decisão: cancelamento tem efeito imediato, sem multa; futuras cobranças deixam de ser devidas e o histórico permanece.
Consequências: valores pagos elegíveis podem gerar reembolso proporcional.
Impacta: Enrollment e Billing.
Fonte: decisões oficiais do Gate M1.

## DEC-014 — Dias permitidos de vencimento

Status: APPROVED
Data: 2026-09-15 — registrada nesta consolidação
Contexto: vencimentos precisam ser previsíveis.
Decisão: permitir dias 5, 10, 15, 20 ou 25; fim de semana/feriado passa ao próximo útil.
Consequências: supera o “mesmo dia da contratação” do capítulo 46.
Impacta: Billing.
Fonte: decisões oficiais do Gate M1.

## DEC-015 — Tolerância financeira e primeira cobrança

Status: APPROVED
Data: 2026-09-15 — registrada nesta consolidação
Contexto: cobrança e restrição precisam de marcos distintos.
Decisão: tolerância financeira de 5 dias e primeira ação de cobrança em D+2.
Consequências: substitui a suspensão após 15 dias do capítulo 46; efeito é FinancialRestriction independente.
Impacta: Billing, Communication e Pilates.
Fonte: decisões oficiais do Gate M1.

## DEC-016 — Não cobrar juros nem multa

Status: APPROVED
Data: 2026-09-15 — registrada nesta consolidação
Contexto: política de inadimplência não aplica encargos automáticos.
Decisão: juros e multa são NONE.
Consequências: eventual mudança exige nova decisão, não configuração informal.
Impacta: Billing.
Fonte: decisões oficiais do Gate M1; Caderno 46.

## DEC-017 — Pagamento é preservado e corrigido por reversão

Status: APPROVED
Data: 2026-09-15 — registrada nesta consolidação
Contexto: eventos financeiros confirmados não podem desaparecer.
Decisão: Payment confirmado não é apagado; correção usa PaymentReversal total ou parcial; Refund é separado.
Consequências: conta de recebimento e usuário registrador são obrigatórios.
Impacta: Billing, Finance e Audit.
Fonte: decisões oficiais do Gate M1.

## DEC-018 — Fechamento financeiro mensal e versionado

Status: APPROVED
Data: 2026-09-15 — registrada nesta consolidação
Contexto: a clínica precisa conferir previsto, realizado e pendências.
Decisão: fechamento é mensal, pode ocorrer com pendências e pode ser reaberto sem apagar snapshot anterior.
Consequências: reabertura cria continuidade versionada, não exclusão.
Impacta: Finance e Audit.
Fonte: decisões oficiais do Gate M1.

## DEC-019 — Caixa físico compartilhado

Status: APPROVED
Data: 2026-09-15 — registrada nesta consolidação
Contexto: a clínica não opera caixas separados por unidade.
Decisão: usar uma FinancialAccount CASH compartilhada.
Consequências: unidade da despesa não determina caixa separado.
Impacta: Finance.
Fonte: decisões oficiais do Gate M1.

## DEC-020 — Duas contas bancárias

Status: APPROVED
Data: 2026-09-15 — registrada nesta consolidação
Contexto: estrutura financeira atual conhecida.
Decisão: modelagem deve suportar duas BANK_ACCOUNT e uma CASH, sem fixar quantidade no código.
Consequências: saldos são derivados das movimentações por conta.
Impacta: Finance e Billing.
Fonte: decisões oficiais do Gate M1.

## DEC-021 — Comissão fora do MVP

Status: APPROVED
Data: 2026-09-15 — registrada nesta consolidação
Contexto: não existe comissão para fisioterapeutas.
Decisão: não implementar Commission no MVP.
Consequências: supera a funcionalidade opcional descrita no capítulo 46 do Caderno.
Impacta: Finance, Staff e roadmap.
Fonte: decisões oficiais do Gate M1.

## DEC-022 — FinancialRestriction não é Enrollment PAUSED

Status: APPROVED
Data: 2026-09-15 — registrada nesta consolidação
Contexto: inadimplência e pausa voluntária têm causas e efeitos diferentes.
Decisão: representar restrição por conceito independente em Billing.
Consequências: matrícula não é falsamente pausada; efeito operacional é consumido por contratos públicos.
Impacta: Billing, Enrollment, Scheduling e Pilates.
Fonte: decisões oficiais do Gate M1.

## DEC-023 — Capacidade e reposição são parâmetros configuráveis

Status: APPROVED
Data: 2026-09-15 — registrada nesta consolidação
Contexto: valores operacionais podem mudar.
Decisão: capacidade 4, validade 30 dias, limite 2/mês, horizonte 90 dias e antecedência 4h são valores iniciais configuráveis.
Consequências: invariantes estruturais permanecem; valores não viram constantes técnicas.
Impacta: Pilates e Scheduling.
Fonte: decisões oficiais do Gate M1; Caderno 14.

## DEC-024 — Acesso clínico exige permissão apropriada

Status: APPROVED
Data: 2026-09-15 — registrada nesta consolidação
Contexto: cargo administrativo ou propriedade não justifica acesso ao prontuário.
Decisão: Secretária não lê prontuário integral por padrão; Proprietária e Desenvolvedor precisam de permissão clínica explícita; break-glass é excepcional.
Consequências: `admin=true` e papel técnico não bastam.
Impacta: Clinical, Identity, Security e Audit.
Fonte: decisões oficiais do Gate M1; Caderno 15 e 31.

## DEC-025 — Autoridade financeira é explícita

Status: APPROVED
Data: 2026-09-15 — registrada nesta consolidação
Contexto: acesso técnico não concede poder financeiro.
Decisão: Secretária e Proprietária podem fechar/reabrir dentro da permissão; Desenvolvedor somente com permissão financeira concedida.
Consequências: supera a exclusividade da Proprietária descrita no capítulo 46, mantendo deny-by-default.
Impacta: Finance, Identity e Audit.
Fonte: decisões oficiais do Gate M1.

## DEC-026 — Billing e Finance têm ownership distinto

Status: APPROVED
Data: 2026-09-15 — registrada nesta consolidação
Contexto: obrigação do cliente não é o mesmo que conta/caixa da clínica.
Decisão: Billing possui Receivable/Payment; Finance possui FinancialAccount/Transaction/Expense/Closing.
Consequências: integração por contratos/eventos; nenhum módulo escreve nas tabelas internas do outro.
Impacta: Billing, Finance e arquitetura.
Fonte: `PROJECT_OS.md`; Caderno 6 e 27.
