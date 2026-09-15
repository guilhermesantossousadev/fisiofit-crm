# Parâmetros de Negócio

## Status

APROVADO PARA MODELAGEM — baseline do Gate M1 em 2026-09-15.

## Como ler

A coluna **Regra** descreve o comportamento obrigatório. A coluna **Valor configurável** registra a política operacional inicial, que não deve ser espalhada como constante técnica. Alteração de valor deve ser autorizada, auditável e, quando afetar histórico ou contratos, possuir vigência.

| ID | Domínio | Regra | Valor configurável inicial | Status do valor | Fonte |
|---|---|---|---|---|---|
| PAR-AGD-001 | Scheduling | A grade usa slots cuja duração pode evoluir sem remodelagem. | 60 minutos | APROVADO | Decisão oficial Gate M1; Caderno 13.1 |
| PAR-AGD-002 | Scheduling / Pilates | Atraso deve ser registrado sem bloquear automaticamente o atendimento. | 10 minutos | APROVADO COMO VALOR OPERACIONAL INICIAL | Caderno 13.22 e 14.17 |
| PAR-CRM-001 | CRM | O primeiro contato deve ocorrer dentro do SLA configurado em horas úteis. | 2 horas úteis | APROVADO COMO VALOR OPERACIONAL INICIAL | Caderno 12.7 e 12.36 |
| PAR-CRM-002 | CRM | A cadência pode sugerir encerramento, mas não perder automaticamente a Opportunity. | 3 tentativas | APROVADO COMO VALOR OPERACIONAL INICIAL | Caderno 12.9 |
| PAR-CRM-003 | CRM | Após experimental realizada deve existir ação comercial no prazo configurado. | 1 dia útil | APROVADO COMO VALOR OPERACIONAL INICIAL | Caderno 12.30 |
| PAR-CRM-004 | CRM | Proposta sem resposta gera tarefa de follow-up no prazo configurado. | 2 dias úteis | APROVADO COMO VALOR OPERACIONAL INICIAL | Caderno 12.31 |
| PAR-PIL-001 | Pilates | Cada turma possui capacidade própria e não admite overbooking inicialmente. | Padrão de 4 pacientes por turma | APROVADO COMO VALOR OPERACIONAL INICIAL | Decisão oficial Gate M1; Caderno 14.4 |
| PAR-PIL-002 | Pilates | Cancelamento do paciente dentro da antecedência vigente pode gerar crédito. | 4 horas | APROVADO COMO VALOR OPERACIONAL INICIAL | Decisão oficial Gate M1; Caderno 14.19–14.20 |
| PAR-PIL-003 | Pilates | Crédito de reposição expira após período configurado. | 30 dias | APROVADO COMO VALOR OPERACIONAL INICIAL | Decisão oficial Gate M1; Caderno 14.23 |
| PAR-PIL-004 | Pilates | Créditos originados pelo paciente são limitados por período; cancelamentos da clínica não entram no limite. | 2 por mês | APROVADO COMO VALOR OPERACIONAL INICIAL | Decisão oficial Gate M1; Caderno 14.21 |
| PAR-PIL-005 | Pilates | Ocorrências futuras são materializadas em horizonte móvel configurável. | 90 dias | APROVADO COMO VALOR OPERACIONAL INICIAL | Decisão oficial Gate M1; Caderno 14.11 |
| PAR-ENR-001 | Plans / Enrollment | Pausa possui duração máxima e não prolonga o contrato. | 15 dias | APROVADO | Decisão oficial Gate M1 |
| PAR-BIL-001 | Billing | Vencimento deve ser escolhido entre dias permitidos. | 5, 10, 15, 20 ou 25 | APROVADO | Decisão oficial Gate M1 |
| PAR-BIL-002 | Billing | Tolerância financeira antecede a restrição operacional. | 5 dias | APROVADO | Decisão oficial Gate M1 |
| PAR-BIL-003 | Billing | A primeira ação de cobrança ocorre por atraso relativo ao vencimento. | D+2 | APROVADO | Decisão oficial Gate M1 |
| PAR-BIL-004 | Billing | Desconto por antecipação é opcional e configurável. | Tipo NONE, PERCENTAGE ou FIXED_AMOUNT; valor ainda não definido | PARCIALMENTE APROVADO / VALIDAR VALOR | Decisão oficial Gate M1 |
| PAR-CLI-001 | Clinical | Evolução deve ser concluída preferencialmente no mesmo dia; atraso não impede regularização. | Até 24 horas sem justificativa adicional | APROVADO COMO VALOR OPERACIONAL INICIAL | Caderno 15.15 |
| PAR-CLI-002 | Clinical | Upload clínico possui limite configurável. | 10 MB por arquivo | STATUS = VALIDAR OPERAÇÃO | Caderno 15.35; não confirmado no Gate M1 |

## Regras sem valor numérico configurável

| ID | Regra | Configuração autorizada |
|---|---|---|
| PAR-PIL-006 | Reposição pode ocorrer em outra turma/unidade compatível com vaga e sem conflito. | Habilitada inicialmente; alteração de política exige decisão e vigência. |
| PAR-PIL-007 | Falta sem aviso e falta em reposição não geram novo crédito automaticamente. | Desabilitado; exceção somente conforme alçada registrada. |
| PAR-PIL-008 | Cancelamento da clínica gera crédito conforme política vigente. | Habilitado inicialmente. |
| PAR-BIL-005 | Não há multa nem juros automáticos. | NONE; mudança exige decisão comercial formal. |
| PAR-BIL-006 | Métodos aceitos: PIX, dinheiro, crédito, débito, boleto e transferência. | Catálogo operacional, sem diferença automática de preço. |
| PAR-FIN-001 | Fechamento é mensal e pode ser reaberto com nova versão/snapshot. | Periodicidade aprovada: mensal. |

## Valores ainda a validar

- Valor ou faixa do desconto por antecipação para `PERCENTAGE` e `FIXED_AMOUNT`.
- Calendário comercial usado no SLA do CRM, além da informação de “horas úteis”.
- Lista formal de feriados e fonte de calendário para vencimentos e agenda.
- Limite de 10 MB de anexo clínico, que veio do Caderno como padrão inicial e não foi reconfirmado nas decisões do Gate.

## Referências

- `PROJECT_OS.md`.
- Caderno Mestre, capítulos 12 a 18 e 46.
- `docs/decisions/DECISIONS.md`.
