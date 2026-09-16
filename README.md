# Fisiofit CRM 2.0

Sistema de gestão da Clínica Fisiofit.

## Estado atual

O projeto está atualmente na fase de modelagem conceitual.

A fonte operacional central do projeto é:

- [PROJECT_OS.md](./PROJECT_OS.md)

## Documentação

A documentação canônica está em:

- `/docs/domain`
- `/docs/business-rules`
- `/docs/processes`
- `/docs/decisions`
- `/docs/architecture`

## Arquitetura alvo

- React + TypeScript
- ASP.NET Core / C#
- PostgreSQL
- Monólito modular

> Antes de desenvolver qualquer funcionalidade, leia `PROJECT_OS.md`.

## Project Status Dashboard

Para visualizar localmente o status registrado no `PROJECT_OS.md`, execute na raiz:

```bash
python3 -m http.server 8000
```

Acesse `http://localhost:8000/project-status/`. Consulte as instruções completas em [`project-status/README.md`](./project-status/README.md).
