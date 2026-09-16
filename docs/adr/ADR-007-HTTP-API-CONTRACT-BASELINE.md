# ADR-007 — HTTP API Contract Baseline

- **Status:** Accepted
- **Date:** 2026-09-16
- **Decision owner:** API-001

## Context

API-001 precisa de uma convenção externa simples e estável para versionamento, erros e listagens antes do bootstrap. Sem baseline, módulos poderiam divergir em rotas, status, formato de erro e paginação.

## Decision

1. A API HTTP externa inicia com versionamento no path: `/api/v1/...`.
2. Erros usam Problem Details conforme RFC 9457, com extensões estáveis `code`, `traceId` e `errors` quando aplicável.
3. Listagens comuns usam inicialmente paginação offset `page`/`pageSize` e resposta com `items`, `page`, `pageSize`, `totalCount`; default e máximo são configuração operacional. Cursor só será adotado por endpoint quando volume/estabilidade demonstrarem necessidade.
4. Filtros e sort fields são whitelists por query. Nenhum nome de coluna interna é aceito.
5. API externa, ModuleContracts e integration events evoluem de forma independente.
6. Ações de state machine permanecem comandos/rotas explícitos; `DELETE` não substitui cancel, reverse, finalize, close, reopen, pause, resume ou outras transições de negócio.
7. DTOs HTTP e contratos públicos não expõem entities, aggregates, modelos de persistência, JSONB cru ou internals de storage.
8. `Fisiofit.ModuleContracts` contém contratos mínimos, imutáveis e namespaced pelo owner; não é um shared domain model.

## Consequences

O Host pode centralizar parsing e serialização HTTP sem centralizar regra de negócio. OpenAPI e contract tests futuros têm uma baseline verificável. Uma breaking change de rota ou representação exige nova major version; mudanças aditivas compatíveis permanecem em v1.

## Rejected

- versionamento somente por header/media type no MVP, por complexidade sem necessidade;
- API sem versão explícita;
- envelope genérico para toda resposta;
- offset e cursor simultaneamente em todas as listagens;
- filtros/sorts arbitrários por coluna;
- formato próprio de erro sem padrão HTTP;
- CRUD/`DELETE` genérico para transições e fatos históricos;
- reutilização de entities como DTOs ou de ModuleContracts como domínio compartilhado.
