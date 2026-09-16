# ADR-001 — Modular Monolith Physical Structure

- **Status:** Accepted
- **Date:** 2026-09-16
- **Decision owner:** ARC-003

## Context

O Fisiofit possui 16 bounded contexts, equipe pequena e deploy inicial único. Um projeto por contexto e camada produziria até 64 projetos; um único projeto de negócio apagaria boundaries.

## Decision

Adotar modular monolith híbrido com:

- `Fisiofit.Api` como composition root;
- 10 module assemblies: Access, Registry, CRM, Operations, Clinical, Revenue, Communication, Documents, Audit e Reports;
- `Domain`, `Application` e `Infrastructure` como folders/namespaces internos;
- `Fisiofit.ModuleContracts` contract-only e `Fisiofit.BuildingBlocks` minimalista;
- nenhum module assembly referenciando outro module assembly.

Contexts agrupados mantêm namespaces, contracts, `DbContext`, schema, migrations e testes próprios.

## Consequences

Menos assemblies e bootstrap que a alternativa 16×4, com enforcement por visibility, dependency rules e ArchitectureTests. Registry, Operations e Revenue exigem testes adicionais para impedir acesso a siblings internos.

## Rejected

- microservices agora;
- assembly por context × camada;
- God Project único;
- regras no Host.

