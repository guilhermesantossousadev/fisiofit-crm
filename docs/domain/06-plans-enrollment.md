# Plans Contracts e Enrollment

## Status

DOM-016 — DONE — APROVADO PARA MODELAGEM CONCEITUAL.

## Objetivo

Representar catálogo versionado, acordo aceito e vínculo operacional, preservando condições históricas e mudanças com vigência.

## Responsabilidades

Plan, PlanVersion, Contract, Enrollment, contratação, pró-rata, pausa, retomada, cancelamento, renovação, mudança de frequência e unidade/turma.

## Fora de escopo

Confirmação de Payment, caixa, despesas, composição de turma e prontuário.

## Conceitos

Plan é catálogo; PlanVersion preserva preço e condições; Contract é snapshot aceito; Enrollment é o vínculo operacional; ClassMembership é a participação em turma e pertence a Pilates.

## Entidades candidatas

Plan, PlanVersion, Contract, Enrollment e, se necessário após modelagem, Entitlement/Benefit.

## Relacionamentos conhecidos

Plan possui versões; Contract referencia uma PlanVersion e pagador; Enrollment decorre do Contract e pode sustentar ClassMemberships sem possuí-los.

## Regras de negócio

- planos atuais: mensal, trimestral e semestral; frequências 1x, 2x e 3x por semana;
- mudança de preço cria PlanVersion; não sobrescreve Contract;
- início pode ocorrer em qualquer data;
- pró-rata = aulas restantes × H/A da PlanVersion;
- pausa: máximo 15 dias, não mantém vaga, não cobra período correspondente e não prolonga término;
- retorno depende de disponibilidade;
- cancelamento: imediato, sem multa, futuras cobranças deixam de ser devidas e histórico permanece;
- renovação cria novo Contract e usa PlanVersion vigente, salvo exceção registrada;
- mudança de frequência tem vigência e preserva histórico;
- mudança de unidade não recria Person/PatientProfile e normalmente altera ClassMembership.

## Estados conhecidos

Enrollment precisa distinguir ao menos estados operacionais ativos, pausados, cancelados e encerrados, sem representar inadimplência como PAUSED. A máquina formal será criada depois do Context/Ownership Map.

## Processos

PROC-PLN-001 Contratar plano; PROC-ENR-001 Ativar matrícula; PROC-ENR-002 Pausar; PROC-ENR-003 Retomar; PROC-ENR-004 Cancelar; PROC-ENR-005 Renovar; PROC-ENR-006 Alterar frequência; PROC-ENR-007 Alterar unidade/turma.

## Eventos conhecidos

ContractAccepted, EnrollmentActivated, EnrollmentPaused/Resumed/Cancelled, ContractRenewed e EnrollmentFrequencyChanged.

## Permissões conhecidas

Proprietária define catálogo/políticas e exceções; Secretária executa contratação e mudanças dentro da alçada; Desenvolvedor não possui autoridade comercial implícita.

## Dependências

People/Patients e ResponsiblePayer; Pilates para ClassMembership; Billing cria Receivables na ativação/fechamento da contratação; Finance recebe apenas efeitos financeiros públicos.

## Parâmetros configuráveis

Pausa máxima de 15 dias. Catálogo de PlanVersion contém H/A e condições comerciais versionadas, não constantes globais.

## Questões não bloqueantes

Definir, durante modelagem, a semântica exata de Entitlement/Benefit e a precedência operacional quando pausa, cancelamento e mudança de frequência ocorrem na mesma data.

## Referências

- Decisões oficiais do Gate M1, DOM-016.
- Caderno Mestre, capítulos 16 e 46, observando as superações registradas na auditoria.
- `docs/decisions/DECISIONS.md`.

