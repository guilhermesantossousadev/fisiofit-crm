# Billing Payments e Delinquency

## Status

DOM-017 — DONE — APROVADO PARA MODELAGEM CONCEITUAL.

## Objetivo

Controlar obrigações de clientes, pagamentos, alocações, reversões, reembolsos, ajustes, negociações e restrições por inadimplência.

## Responsabilidades

Receivable, Payment, PaymentAllocation, PaymentReversal, Refund, BillingAdjustment, Negotiation e FinancialRestriction.

## Fora de escopo

Despesas e contas a pagar da clínica, saldo bancário/caixa, fechamento mensal e lógica de Enrollment PAUSED.

## Conceitos

Receivable é obrigação; Payment é dinheiro confirmado; Allocation liga recebimento a obrigação; Reversal corrige sem apagar; Refund devolve; FinancialRestriction representa efeito operacional da inadimplência independentemente do estado de matrícula.

## Entidades candidatas

Receivable, Payment, PaymentAllocation, PaymentReversal, Refund, BillingAdjustment, Negotiation e FinancialRestriction.

## Relacionamentos conhecidos

Receivables derivam do Contract; Payment pode alocar parcialmente/antecipadamente; pagador pode diferir do paciente; Payment deve indicar conta de recebimento e usuário registrador.

## Regras de negócio

- todos os Receivables do contrato são criados no fechamento/ativação da contratação;
- vencimentos permitidos: 5, 10, 15, 20 e 25; sábado/domingo/feriado migra ao próximo dia útil;
- primeiro período parcial cobra somente pró-rata;
- métodos: PIX, dinheiro, crédito, débito, boleto e transferência, sem preço automático diferente;
- cartão pode parcelar até a quantidade correspondente ao plano;
- pagamentos parcial e antecipado são permitidos; desconto por antecipação é opcional/configurável;
- tolerância financeira 5 dias; primeira cobrança D+2; sem multa e sem juros;
- após inadimplência, paciente não frequenta normalmente; usar FinancialRestriction, nunca PAUSED como sinônimo;
- Secretária e Proprietária podem negociar; perdão arbitrário não é operação normal;
- Payment confirmado não é apagado; correção usa PaymentReversal; estorno parcial e reembolso são permitidos;
- cancelamento pago: reembolso = aulas não utilizadas × H/A, limitado ao valor pago elegível;
- pausa: devido = aulas efetivamente disponíveis × H/A; se já pago, gera reembolso correspondente;
- conta de recebimento e usuário registrador são obrigatórios; comprovante formal/anexo não são obrigatórios no MVP.

## Estados conhecidos

Receivable: OPEN, PARTIALLY_PAID, PAID, CANCELLED; OVERDUE é derivado. Payment: ao menos CONFIRMED e REVERSED/PARTIALLY_REVERSED conforme modelagem; não inventar estados de provider sem integração escolhida. FinancialRestriction possui ciclo independente a detalhar.

## Processos

PROC-BIL-001 a PROC-BIL-009: gerar recebíveis, registrar pagamento/parte/antecipação, negociar, estornar, reembolsar, aplicar e remover restrição.

## Eventos conhecidos

ReceivablesCreated, PaymentConfirmed, PaymentAllocated, PaymentReversed, RefundIssued, ReceivableOverdue, FinancialRestrictionApplied/Removed e NegotiationAgreed.

## Permissões conhecidas

Secretária e Proprietária registram pagamentos e negociações na alçada; operações sensíveis são auditadas; Desenvolvedor não possui autoridade financeira sem permissão explicitamente concedida.

## Dependências

Plans/Enrollment e People/Payer; Scheduling/Pilates consomem restrição por contrato público; Finance recebe movimentações confirmadas e exige FinancialAccount.

## Parâmetros configuráveis

Dias 5/10/15/20/25, tolerância 5 dias, cobrança D+2 e política opcional NONE/PERCENTAGE/FIXED_AMOUNT de antecipação.

## Questões não bloqueantes

Alçadas/limites de negociação, fórmula de elegibilidade do desconto por antecipação e catálogo de motivos de ajustes/reversões.

## Referências

- Decisões oficiais do Gate M1, DOM-017.
- Caderno Mestre, capítulos 17 e 46, com conflitos registrados em `docs/GATE_M1_AUDIT.md`.

