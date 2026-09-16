# Fisiofit CRM 2.0

Fundação técnica do Fisiofit CRM 2.0, organizada como monólito modular em ASP.NET Core e frontend React + TypeScript. A fonte operacional do projeto é o [PROJECT_OS.md](./PROJECT_OS.md).

## Pré-requisitos

- .NET SDK 10 (LTS)
- Node.js compatível com o toolchain (BOOT-001 foi validado com Node 26)
- npm 11 ou compatível

Não é necessário PostgreSQL nesta etapa.

## Estrutura

- `src/backend/Fisiofit.Api`: composition root e Host ASP.NET Core.
- `src/backend/Fisiofit.BuildingBlocks`: primitives técnicas compartilhadas mínimas.
- `src/backend/Fisiofit.ModuleContracts`: contratos públicos futuros, separados por owner/context.
- `src/backend/Modules`: os 10 module assemblies aprovados.
- `src/frontend/Fisiofit.Web`: aplicação React/TypeScript feature-first.
- `tests/backend`: testes de arquitetura, unidade, integração e API.
- `docs/implementation`: documentação das tarefas de implementação.

## Backend

Na raiz do repositório:

```bash
dotnet restore Fisiofit.slnx
dotnet build Fisiofit.slnx --no-restore
dotnet test Fisiofit.slnx --no-build --no-restore
dotnet run --project src/backend/Fisiofit.Api/Fisiofit.Api.csproj
```

O Host usa `http://localhost:5080` no profile local. O health check está em `GET /health`.

## Frontend

```bash
cd src/frontend/Fisiofit.Web
npm install
npm run dev
npm run build
npm run lint
npm run typecheck
```

`VITE_API_BASE_URL` pode sobrescrever a URL local da API; consulte `.env.example`. Nenhum secret é necessário ou deve ser commitado.

## Escopo atual

BOOT-001 contém somente skeleton técnico. Não há regra de negócio, endpoint funcional, autenticação, banco, `DbContext`, migration, outbox/inbox, Docker ou CI/CD implementados.

Antes de implementar qualquer funcionalidade, leia `PROJECT_OS.md` e a documentação canônica relacionada à tarefa autorizada.

## Project Status Dashboard

Para visualizar localmente o status registrado no `PROJECT_OS.md`, execute na raiz:

```bash
python3 -m http.server 8000
```

Acesse `http://localhost:8000/project-status/`.
