# ADR-006 — Identifier Strategy

- **Status:** Accepted
- **Date:** 2026-09-16
- **Decision owner:** DB-001

## Context

Os 16 bounded contexts trocam identificadores opacos, persistem em PostgreSQL e não usam FK cross-schema por default. A estratégia precisa evitar IDs sequenciais expostos, funcionar sem coordenação entre contexts e manter operação e tooling simples.

## Options considered

| Option | Strengths | Costs / risks |
|---|---|---|
| UUID | tipo PostgreSQL nativo, opaco, geração independente e amplo suporte | índice maior que BIGINT; UUID aleatório tem menor localidade |
| ULID | opaco e aproximadamente ordenável | não é tipo PostgreSQL nativo; exige convenção/codec e aumenta risco de representações divergentes |
| BIGINT | índice compacto, ordenação e operação simples | sequência previsível quando exposta; geração/coordenação e importação cross-context exigem mais cuidado |

## Decision

Adotar **UUID** como identificador padrão de entidades, aggregates, eventos, idempotency keys persistidas e referências cross-context.

- A representação lógica é UUID; APIs devem tratá-lo como string opaca, sem semântica embutida.
- A geração preferencial é UUID version 7 quando a biblioteca/runtime aprovado oferecer suporte estável; UUID version 4 é fallback compatível. O contrato não depende da versão.
- Chaves técnicas ordinais podem usar inteiro apenas dentro de uma estrutura local e sem exposição, por exemplo número da parcela, versão de snapshot ou sequência de correção.
- Identificadores naturais, CPF, códigos externos e idempotency keys nunca substituem a PK.
- Exceção a UUID exige evidência e ADR, não decisão isolada de migration.

## Consequences

Referências entre contexts permanecem opacas e não revelam cardinalidade. PostgreSQL oferece tipo nativo e validação simples. Índices são maiores que BIGINT, portanto índices compostos devem ser deliberados. A escolha de UUIDv7 melhora localidade quando disponível, sem transformar ordenação do ID em ordem de negócio; datas e sequências explícitas continuam autoritativas.

## Rejected

- ULID como padrão, por adicionar representação não nativa sem necessidade real de ordenação lexicográfica distribuída;
- BIGINT como padrão, por expor sequência e aumentar acoplamento operacional entre owners;
- estratégia diferente por context sem justificativa;
- IDs que codificam schema, tipo, unidade, data ou informação pessoal.
