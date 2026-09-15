# Índice de Processos

## Status

Catálogo inicial aprovado para a modelagem conceitual no Gate M1. Os processos não estão todos detalhados; `arquivo detalhado` vazio indica trabalho posterior, não ausência do processo.

| ID | Nome | Ator principal | Domínio | Prioridade | Status | Arquivo detalhado |
|---|---|---|---|---|---|---|
| PROC-PPL-001 | Criar pessoa | Secretária | People | P0 | CATALOGADO | — |
| PROC-PAC-001 | Cadastrar paciente | Secretária | Patients | P0 | CATALOGADO | — |
| PROC-AGD-001 | Gerenciar horário fixo | Secretária | Scheduling | P0 | CATALOGADO | — |
| PROC-AGD-002 | Criar compromisso ad-hoc | Secretária | Scheduling | P0 | CATALOGADO | — |
| PROC-PIL-001 | Criar turma | Secretária | Pilates | P0 | CATALOGADO | — |
| PROC-PIL-002 | Adicionar paciente à turma | Secretária / Fisioterapeuta autorizada | Pilates | P0 | CATALOGADO | — |
| PROC-PIL-003 | Transferir paciente | Secretária | Pilates | P0 | CATALOGADO | — |
| PROC-PIL-004 | Realizar chamada | Fisioterapeuta | Pilates | P0 | CATALOGADO | — |
| PROC-PIL-005 | Solicitar e usar reposição | Secretária | Pilates | P0 | CATALOGADO | — |
| PROC-CLI-001 | Abrir episódio | Fisioterapeuta | Clinical | P0 | CATALOGADO | — |
| PROC-CLI-002 | Realizar avaliação | Fisioterapeuta | Clinical | P0 | CATALOGADO | — |
| PROC-CLI-003 | Registrar evolução | Fisioterapeuta | Clinical | P0 | CATALOGADO | — |
| PROC-CLI-004 | Finalizar evolução | Fisioterapeuta autora | Clinical | P0 | CATALOGADO | — |
| PROC-CLI-005 | Retificar registro | Fisioterapeuta autora / clínico autorizado | Clinical | P0 | CATALOGADO | — |
| PROC-PLN-001 | Contratar plano | Secretária | Plans | P0 | CATALOGADO | — |
| PROC-ENR-001 | Ativar matrícula | Secretária | Enrollment | P0 | CATALOGADO | — |
| PROC-ENR-002 | Pausar matrícula | Secretária | Enrollment | P0 | CATALOGADO | — |
| PROC-ENR-003 | Retomar matrícula | Secretária | Enrollment | P0 | CATALOGADO | — |
| PROC-ENR-004 | Cancelar matrícula | Secretária | Enrollment | P0 | CATALOGADO | — |
| PROC-ENR-005 | Renovar contrato | Secretária | Plans / Enrollment | P0 | CATALOGADO | — |
| PROC-ENR-006 | Alterar frequência | Secretária | Enrollment | P0 | CATALOGADO | — |
| PROC-ENR-007 | Alterar unidade ou turma | Secretária | Enrollment / Pilates | P0 | CATALOGADO | — |
| PROC-BIL-001 | Gerar recebíveis | Sistema no fechamento da contratação | Billing | P0 | CATALOGADO | — |
| PROC-BIL-002 | Registrar pagamento | Secretária / Proprietária | Billing | P0 | CATALOGADO | — |
| PROC-BIL-003 | Registrar pagamento parcial | Secretária / Proprietária | Billing | P0 | CATALOGADO | — |
| PROC-BIL-004 | Antecipar parcelas | Secretária / Proprietária | Billing | P1 | CATALOGADO | — |
| PROC-BIL-005 | Negociar dívida | Secretária / Proprietária | Billing | P0 | CATALOGADO | — |
| PROC-BIL-006 | Estornar pagamento | Usuário financeiro autorizado | Billing | P0 | CATALOGADO | — |
| PROC-BIL-007 | Realizar reembolso | Usuário financeiro autorizado | Billing | P0 | CATALOGADO | — |
| PROC-BIL-008 | Aplicar restrição financeira | Sistema / usuário financeiro autorizado | Billing | P0 | CATALOGADO | — |
| PROC-BIL-009 | Remover restrição financeira | Sistema / usuário financeiro autorizado | Billing | P0 | CATALOGADO | — |
| PROC-FIN-001 | Registrar despesa | Secretária / Proprietária | Finance | P0 | CATALOGADO | — |
| PROC-FIN-002 | Pagar despesa | Secretária / Proprietária | Finance | P0 | CATALOGADO | — |
| PROC-FIN-003 | Transferir entre contas | Usuário financeiro autorizado | Finance | P0 | CATALOGADO | — |
| PROC-FIN-004 | Conciliar conta | Usuário financeiro autorizado | Finance | P1 | CATALOGADO | — |
| PROC-FIN-005 | Fechar mês | Secretária / Proprietária autorizada | Finance | P0 | CATALOGADO | — |
| PROC-FIN-006 | Reabrir fechamento | Secretária / Proprietária autorizada | Finance | P0 | CATALOGADO | — |

## Ordem obrigatória para detalhamento

Para cada processo: objetivo → trigger → atores/permissões → pré-condições → fluxo e exceções → regras → estados → entidades → eventos → auditoria/concorrência → pós-condições. A definição de API, banco ou tela vem depois.

## Observações de ownership

- `PROC-BIL-008/009` altera FinancialRestriction em Billing e publica o efeito; não muda Enrollment para PAUSED.
- `PROC-FIN-005/006` pertence a Finance e consulta projeções/referências de Billing, sem escrever em Receivable/Payment.
- `PROC-PIL-005` usa conflitos de Scheduling por contrato e nunca altera ClassMembership apenas para reservar uma reposição.

## Referências

- Caderno Mestre, capítulos 7, 8 e 11 a 18.
- Documentos em `docs/domain/`.
- `docs/business-rules/RULES_INDEX.md`.
