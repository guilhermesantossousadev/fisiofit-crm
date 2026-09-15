# Pilates e Turmas

## Status

DOM-014 — DONE — APROVADO PARA MODELAGEM CONCEITUAL.

## Objetivo

Gerenciar turmas recorrentes, vínculos, ocorrências, presença e reposições com capacidade e histórico preservados.

## Responsabilidades

Class, ClassSchedule, ClassMembership, ClassOccurrence, Attendance, MakeupCredit, MakeupReservation, capacidade e chamada.

## Fora de escopo

Contrato/preço, recebíveis, prontuário, reserva de sala/equipamento e agenda comercial.

## Conceitos

Class identifica a turma; ClassSchedule define recorrência vigente; ClassMembership liga paciente à turma; ClassOccurrence representa uma data; Attendance registra participação; reposição usa crédito e reserva próprios.

## Entidades candidatas

Class, ClassSchedule, ClassMembership, ClassOccurrence, Attendance, MakeupCredit e MakeupReservation.

## Relacionamentos conhecidos

- Class possui schedules versionados por vigência e ocorrências;
- ClassMembership liga Patient à Class com início/fim;
- Attendance liga Patient à ClassOccurrence;
- MakeupReservation consome/reserva MakeupCredit em ocorrência compatível sem alterar membership.

## Regras de negócio

- recorrência não é ocorrência;
- remover paciente encerra vínculo, não apaga histórico;
- capacidade pertence à turma e não pode ser excedida; overbooking não é permitido inicialmente;
- reposição ocupa vaga real, não altera turma fixa e não gera cadeia automática de novos créditos;
- alterações permanentes usam vigência e pontuais alteram somente ocorrência;
- ausência pontual pode liberar vaga daquela ocorrência sem encerrar membership;
- outra turma/unidade é permitida quando compatível, com vaga e sem conflito.

## Estados conhecidos

Attendance: PENDING, PRESENT, LATE, ABSENT_JUSTIFIED, ABSENT_UNJUSTIFIED, CANCELLED_IN_ADVANCE ou CANCELLED_LATE. MakeupCredit: AVAILABLE, RESERVED, CONSUMED, EXPIRED ou CANCELLED. ClassOccurrence: PLANNED, IN_PROGRESS quando necessário, COMPLETED ou CANCELLED.

## Processos

- PROC-PIL-001 Criar turma.
- PROC-PIL-002 Adicionar paciente à turma.
- PROC-PIL-003 Transferir paciente.
- PROC-PIL-004 Realizar chamada.
- PROC-PIL-005 Solicitar/usar reposição.

## Eventos conhecidos

ClassCreated, ClassScheduleChanged, ClassMembershipStarted/Ended, AttendanceRecorded, MakeupCreditGranted, MakeupReserved, MakeupConsumed e MakeupExpired.

## Permissões conhecidas

Secretária opera turmas e reservas; fisioterapeuta gerencia suas turmas dentro da alçada e realiza chamada; Proprietária altera políticas/exceções; Desenvolvedor não tem alçada operacional.

## Dependências

Scheduling para conflitos e calendário; Patients e Staff para participantes; Enrollment para elegibilidade operacional; Clinical pode referenciar ocorrência, sem possuir a turma.

## Parâmetros configuráveis

Capacidade padrão 4, antecedência 4h, validade 30 dias, limite 2/mês e horizonte 90 dias são valores operacionais iniciais configuráveis, não constantes nem invariantes universais.

## Questões não bloqueantes

Detalhar alçadas de exceção e efeitos dos créditos durante a pausa de 15 dias sem contrariar que a pausa não prolonga o contrato.

## Referências

- Caderno Mestre, capítulo 14.
- Decisões oficiais do Gate M1.
- `docs/domain/BUSINESS_PARAMETERS.md`.
