# ADR-005 — Clinical Data Isolation

- **Status:** Accepted
- **Date:** 2026-09-16
- **Decision owner:** ARC-003 / AUTH-001

## Context

Clinical contém dados sensíveis e AUTH-001 proíbe acesso implícito por Owner/Manager, Developer/IT ou contexto administrativo.

## Decision

Clinical terá assembly, layers, schema, `ClinicalDbContext`, migrations, contracts, policies, read models e observability controls próprios. Public contracts são mínimos; integration events carregam apenas `CLINICAL_METADATA`. CRM, Revenue e Reports não importam internals clínicos.

Document access exige autorização clínica atual; arquivos ficam privados, com download mediado ou signed access curto. Logs genéricos não contêm conteúdo clínico. Reads/mutations/export/break-glass recebem audit proporcional.

## Consequences

Há mais policy checks, projeções específicas e cuidado operacional, mas o risco de vazamento e bypass diminui. Documents e Reports herdam a sensibilidade do owner.

## Rejected

- tabela/schema clínico compartilhado;
- role administrativo como acesso clínico;
- eventos com conteúdo do prontuário;
- URL pública permanente;
- suporte técnico com acesso irrestrito.
