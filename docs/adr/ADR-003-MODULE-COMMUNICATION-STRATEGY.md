# ADR-003 — Module Communication Strategy

- **Status:** Accepted
- **Date:** 2026-09-16
- **Decision owner:** ARC-003

## Context

MODEL-005 exige validações síncronas, events, workflows e read models sem ciclos de escrita ou acesso direto a dados.

## Decision

- consultas/comandos imediatos usam contratos tipados em `Fisiofit.ModuleContracts`, despachados in-process ao owner;
- domain events internos não atravessam contexts;
- integration events versionados propagam fatos já decididos;
- read models/projections são explícitos, reconstruíveis e não owners;
- HTTP interno, broker para query e project reference entre modules são proibidos.

API externa e contratos entre módulos são boundaries distintos.

## Consequences

Chamadas têm baixo overhead e ownership explícito. `ModuleContracts` precisa de owner por namespace, payload mínimo, versionamento e ArchitectureTests para não virar modelo global.

## Rejected

- HTTP interno;
- acesso direto a tabela/`DbContext`;
- shared entities;
- evento usado como consulta síncrona.

