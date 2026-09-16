# ADR-002 — Module Persistence Isolation

- **Status:** Accepted
- **Date:** 2026-09-16
- **Decision owner:** ARC-003 / DB-001

## Context

O modular monolith precisa de operação simples sem permitir que um context navegue ou migre dados de outro.

## Decision

Adotar um PostgreSQL database por ambiente, com schema e `DbContext` por bounded context. Cada context owns suas migrations e transactions. Referências cross-context usam IDs opacos e, por default, não possuem FK cross-schema.

DB-001 definirá nomes físicos, tipos, constraints locais, índices, migration history e mecanismos de reconciliação dentro desses boundaries. Esses detalhes não reabrem a decisão de isolamento. Qualquer exceção de FK cross-context exige revisão desta ADR.

## Consequences

Backup e deploy permanecem simples, enquanto ownership é visível. Integridade cross-context depende de contrato síncrono, events, snapshots e reconciliação, não de navegação EF ou cascade externo.

## Rejected

- schema único por convenção;
- `DbContext` global;
- múltiplos databases no MVP;
- FK cross-context por default.
