# Finance Cash e Closing

## Status

DOM-018 — DONE — APROVADO PARA MODELAGEM CONCEITUAL.

## Objetivo

Controlar contas, movimentações, despesas, contas a pagar, transferências, conciliação e fechamento mensal da clínica.

## Responsabilidades

FinancialAccount, FinancialTransaction, Expense, ExpenseCategory, Transfer, ReconciliationAdjustment e Closing; previsto x realizado, fluxo de caixa e saldo derivado.

## Fora de escopo

Receivable, regra de inadimplência, prontuário, comissão e saldo digitado como fonte principal.

## Conceitos

A clínica opera duas contas bancárias e um caixa físico compartilhado, sem caixa por unidade. Saldo é derivado das movimentações. Transferência entre contas não é receita nem despesa.

## Entidades candidatas

FinancialAccount, FinancialTransaction, Expense, ExpenseCategory, Transfer, ReconciliationAdjustment e Closing.

## Relacionamentos conhecidos

FinancialAccount é BANK_ACCOUNT ou CASH; Expense possui categoria e escopo UNIT ou GLOBAL; pagamentos confirmados de Billing geram/refletem movimentos; Closing versiona uma conferência mensal.

## Regras de negócio

- duas contas bancárias e um caixa físico compartilhado;
- saldo deriva de FinancialTransactions, não de campo digitado;
- controlar todas as despesas, contas a pagar, vencimentos, categorias, escopo por unidade/global, previsto x realizado, fluxo e saldos;
- comprovante/nota não é obrigatório no MVP;
- comissão não existe e fica fora do MVP;
- transferência não é receita nem despesa;
- fechamento é mensal, inclui devedores/pagadores, receitas, despesas, contas a pagar, saldos e previsto x realizado;
- pendências não bloqueiam o fechamento;
- fechamento pode ser reaberto; reabrir preserva snapshot/versão anterior;
- conciliação inicial pode ser manual.

## Estados conhecidos

Closing deve distinguir ao menos aberto/preparação, fechado e reaberto por nova versão, a formalizar em máquina de estados. Expense/conta a pagar deve distinguir previsto e realizado sem inferir estados adicionais nesta etapa.

## Processos

PROC-FIN-001 Registrar despesa; PROC-FIN-002 Pagar despesa; PROC-FIN-003 Transferir; PROC-FIN-004 Conciliar; PROC-FIN-005 Fechar mês; PROC-FIN-006 Reabrir fechamento.

## Eventos conhecidos

ExpenseRegistered/Paid, AccountTransferCompleted, AccountReconciled, MonthClosed e ClosingReopened.

## Permissões conhecidas

Secretária e Proprietária podem fechar/reabrir conforme permissão financeira; Desenvolvedor somente com permissão financeira explicitamente concedida. Autoridade técnica nunca basta.

## Dependências

Billing publica pagamentos/reversões/reembolsos; Organization fornece Unit; Identity/Authorization aplica alçadas; Audit registra fechamento/reabertura.

## Parâmetros configuráveis

Periodicidade mensal é regra aprovada. Categorias iniciais de despesas e contas financeiras são cadastros controlados, não código fixo.

## Questões não bloqueantes

Categorias iniciais, procedimento operacional de conciliação, granularidade de competência x caixa e matriz detalhada de permissão financeira antes da implementação.

## Referências

- Decisões oficiais do Gate M1, DOM-018.
- Caderno Mestre, capítulos 18 e 46, com conflitos registrados na auditoria.
