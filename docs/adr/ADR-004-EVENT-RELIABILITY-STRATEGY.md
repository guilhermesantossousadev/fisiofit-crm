# ADR-004 — Event Reliability Strategy

- **Status:** Accepted
- **Date:** 2026-09-16
- **Decision owner:** ARC-003 / DB-001

## Context

Nem todo event exige durabilidade, mas perder fatos financeiros, contratuais, de merge, restrição ou audit pode criar inconsistência material.

## Decision

Usar delivery in-process inicialmente, em três tiers:

- R0: event local na transaction do context;
- R1: post-commit para projection/notificação reconstruível;
- R2: outbox no producer, at-least-once e inbox/deduplicação no consumer.

R2 cobre Contract/Enrollment críticos, Payment/reversal/refund → Finance, merge, revogação/elegibilidade crítica, restrições e evidência clínica/security relevante. Não adotar broker inicialmente. Ordering é por aggregate/subject, nunca global.

DB-001 fechou as estruturas lógicas de outbox/inbox e a matriz de producers/consumers R2; bootstrap definirá dispatcher/scheduler e retention.

Para DB-001, a consequência é explicitamente a opção **C**: modelar Outbox somente no schema/`DbContext` dos producers de fluxos R2 e Inbox/receipt somente no schema/`DbContext` dos consumers desses fluxos R2, conforme a classificação do ARC-003. DB-001 não deve criar estruturas genéricas de Outbox/Inbox para os 16 contexts nem adiar toda a modelagem para uma etapa posterior. R0 e R1 não recebem essas estruturas por default.

DB-001 fechou o modelo lógico e a localização dessas estruturas em `docs/database/DB_001_LOGICAL_DATA_MODEL.md`, seção 37. A ADR passa a `Accepted`. Permanecem deferred e não bloqueiam a decisão: implementação do dispatcher/scheduler, biblioteca/provider, retention, replay operacional e eventual broker.

## Consequences

O sistema aceita duplicates e atraso com idempotência, retries, metrics e reconciliation. Escala futura pode adicionar broker sem mudar contratos semânticos.

## Rejected

- best-effort para todos os fatos;
- broker obrigatório para todo event;
- distributed transaction.
