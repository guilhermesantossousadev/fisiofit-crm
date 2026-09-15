# CRM

## Status

DOM-012 — DONE — APROVADO PARA MODELAGEM CONCEITUAL.

## Objetivo

Gerenciar ciclos comerciais associados a Persons, da entrada do interesse à conversão, perda ou desqualificação, preservando atividades e responsabilidade.

## Responsabilidades

Opportunity, pipeline, origem, owner, Activity, Task, próxima ação, proposta, perda, reativação e integração da experimental com Scheduling.

## Fora de escopo

Identidade duplicada de lead; avaliação clínica; regra de agenda; contrato, cobrança e pagamento; envio técnico de mensagens.

## Conceitos

Opportunity é a entidade comercial. Lead é uma visão operacional de Person com oportunidade aberta. Uma Person pode ter múltiplos ciclos e, quando interesses forem distintos, oportunidades simultâneas.

## Entidades candidatas

Opportunity, Pipeline/Stage, Activity, Task, Source, CampaignReference, Proposal e LossReason.

## Relacionamentos conhecidos

- Opportunity pertence a uma Person e possui owner comercial.
- Experimental é Appointment ligado à Opportunity.
- Conversão mantém Person e cria/ativa PatientProfile, Contract e Enrollment conforme o processo.

## Regras de negócio

- pipeline inicial: NOVO → CONTATO_INICIADO → QUALIFICADO → EXPERIMENTAL_AGENDADA → EXPERIMENTAL_REALIZADA → PROPOSTA_APRESENTADA → NEGOCIACAO → CONVERTIDO;
- saídas: PERDIDO e DESQUALIFICADO;
- LossReason é obrigatório em perda;
- perda por silêncio não é automática;
- próxima ação é obrigatória após primeiro contato, salvo evento futuro já registrado;
- histórico comercial, owner anterior e proposta não são apagados;
- conversão requer contrato/matrícula aceitos, não primeiro pagamento;
- decisões objetivas podem ser automatizadas; perda subjetiva permanece humana.

## Estados conhecidos

Os estágios do pipeline acima. Task: OPEN, IN_PROGRESS, DONE ou CANCELLED; OVERDUE é derivado.

## Processos

Capturar/qualificar oportunidade, atribuir owner, registrar contato, agendar experimental, apresentar proposta, converter, perder, desqualificar e reativar.

## Eventos conhecidos

OpportunityCreated, FirstContactAttempted, OpportunityStageChanged, ExperimentalScheduled, ExperimentalCompleted, ProposalPresented, OpportunityConverted, OpportunityLost e OpportunityReactivated.

## Permissões conhecidas

Secretária e Proprietária movimentam o pipeline; fisioterapeuta não é owner comercial por padrão; Desenvolvedor não possui autoridade comercial.

## Dependências

People é identidade; Scheduling fornece Appointment experimental; Plans/Enrollment confirma conversão; Communication executa mensagens a partir de regras/eventos públicos.

## Parâmetros configuráveis

SLA inicial de primeiro contato de 2 horas úteis; 3 tentativas; follow-up pós-experimental de 1 dia útil; follow-up de proposta de 2 dias úteis; política de experimental por serviço.

## Questões não bloqueantes

Calendário comercial do SLA, catálogo final de origens e política/alçada detalhada de desconto em proposta.

## Referências

- Caderno Mestre, capítulo 12.
- `docs/domain/BUSINESS_PARAMETERS.md`.
- `docs/business-rules/RULES_INDEX.md`.

