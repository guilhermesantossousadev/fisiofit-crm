# Scheduling e Agenda

## Status

DOM-013 — DONE — APROVADO PARA MODELAGEM CONCEITUAL.

## Objetivo

Representar regras recorrentes da agenda geral que não sejam turmas, compromissos ad-hoc, conflitos e exceções das duas unidades, sem reescrever o histórico. A recorrência de turmas pertence a Pilates.

## Responsabilidades

ScheduleRule/FixedSchedule da agenda geral não-turma, Appointment, calendário operacional, alterações permanentes/pontuais, conflitos temporais e visões por unidade, profissional e paciente.

## Fora de escopo

Capacidade física de sala, reserva de equipamentos, prontuário, preço, cobrança e regras de crédito de reposição.

## Conceitos

Recorrência e ocorrência são diferentes. Sala é informativa. Equipamento não participa do agendamento. Mudança permanente usa vigência; mudança pontual afeta somente a ocorrência.

## Entidades candidatas

ScheduleRule/FixedSchedule, Appointment, ScheduleBlock e CalendarException. ClassSchedule e ClassOccurrence pertencem a Pilates.

## Relacionamentos conhecidos

Toda grade/ocorrência/Appointment está em uma Unit, envolve intervalo e profissional; paciente é obrigatório quando o compromisso é individual; sala pode ser informada sem exclusividade.

## Regras de negócio

- funcionamento principal: segunda a sexta, 06:00–21:00, último slot 20:00–21:00;
- slot padrão de 60 minutos;
- profissional e paciente não podem ter compromissos conflitantes;
- capacidade da turma é impeditiva, mas pertence a Pilates/ClassSchedule;
- sala não bloqueia conflito e equipamento não é recurso agendável;
- avaliação, experimental, reposição, extraordinário e encaixe são ad-hoc;
- cancelamento e reagendamento preservam histórico;
- sobreposição entre unidades também é conflito do profissional.

## Estados conhecidos

ClassOccurrence: PLANNED, opcionalmente IN_PROGRESS, COMPLETED ou CANCELLED. Appointment: SCHEDULED, CONFIRMED, COMPLETED, CANCELLED ou NO_SHOW. Reagendamento é operação com histórico.

## Processos

- PROC-AGD-001 Gerenciar horário fixo.
- PROC-AGD-002 Criar compromisso ad-hoc.
- Aplicar feriado/recesso, substituição, cancelamento e reagendamento.

## Eventos conhecidos

ScheduleEffectiveDated, AppointmentScheduled, AppointmentRescheduled, AppointmentCancelled, CalendarExceptionApplied e ProfessionalSubstituted.

## Permissões conhecidas

Proprietária e Secretária operam agenda ampla; fisioterapeuta opera sua agenda/turmas dentro da permissão; Desenvolvedor não possui autoridade operacional implícita.

## Dependências

Organization fornece Unit/Room/calendário; People/Patients e Staff fornecem referências; Pilates é dono da turma/capacidade; CRM vincula experimental; Clinical referencia atendimento.

## Parâmetros configuráveis

Duração padrão de 60 minutos e tolerância operacional inicial de atraso de 10 minutos.

## Questões não bloqueantes

Fonte do calendário de feriados; eventual intervalo mínimo futuro entre unidades; durações adicionais por tipo de serviço.

## Referências

- Caderno Mestre, capítulo 13.
- `docs/domain/BUSINESS_PARAMETERS.md`.
- `docs/business-rules/RULES_INDEX.md`.
