# Fisiofit CRM 2.0 — AI Project Profile

## 1. Purpose

Este perfil orienta agentes de IA e CLIs que trabalham no Fisiofit CRM 2.0. Ele define como consultar as fontes do projeto e executar uma tarefa sem duplicar as regras canônicas.

## 2. Source of Truth Hierarchy

Em caso de conflito, consulte nesta ordem:

1. decisão `APPROVED` mais recente;
2. `PROJECT_OS.md`;
3. documentos arquiteturais canônicos;
4. modelos conceituais;
5. state machines;
6. autorização e policies;
7. documentos de domínio;
8. `docs/business-rules/RULES_INDEX.md`;
9. `docs/processes/PROCESS_INDEX.md`;
10. `docs/decisions/DECISIONS.md`;
11. `.prompts`, somente como método de execução.

Uma decisão genérica da biblioteca `.prompts` não tem autoridade para sobrescrever uma decisão do Fisiofit. Conflitos devem ser registrados e não resolvidos silenciosamente.

## 3. Technology Direction

Direções já aprovadas:

- frontend: React + TypeScript;
- backend: ASP.NET Core / C#;
- database: PostgreSQL;
- arquitetura: monólito modular;
- containers: Docker;
- CI/CD;
- n8n somente para automação ou orquestração periférica;
- storage privado S3-compatible quando necessário;
- Redis somente se houver justificativa concreta.

Detalhes físicos ainda não aprovados pertencem a `ARC-003` e não devem ser antecipados por este perfil.

## 4. Official Contexts

Os 16 contexts atuais são:

- Identity & Access
- Organization
- People
- Patients
- Staff
- CRM
- Scheduling
- Pilates
- Clinical
- Plans & Enrollment
- Billing
- Finance
- Communication
- Documents
- Privacy & Audit
- Reports

## 5. Non-Negotiable Architecture Principles

- `PROJECT_OS.md` é a fonte operacional central.
- Um dado transacional possui um único owner.
- Nenhum módulo escreve diretamente nos internals de outro.
- Clinical permanece segregado.
- Billing != Finance.
- Payment != FinancialTransaction.
- Contract != Enrollment.
- Appointment != ClassOccurrence.
- Attendance != ClinicalEntry.
- O frontend não acessa o banco.
- n8n não contém regra central de negócio.
- Histórico não é reescrito.
- Snapshots não são a source of truth atual.
- Authorization = deny by default.
- Papel técnico não implica autoridade de negócio.
- O presente não reescreve o passado.

## 6. AI Execution Protocol

Antes e durante qualquer tarefa, o agente deve:

1. ler `PROJECT_OS.md`;
2. identificar a tarefa `READY` ou `IN_PROGRESS` autorizada pelo escopo recebido;
3. ler o último handoff;
4. verificar `git status`;
5. verificar a branch;
6. ler os documentos diretamente relacionados à tarefa;
7. não iniciar a tarefa seguinte;
8. não resolver blocker silenciosamente;
9. não alterar decisão aprovada sem registrar o conflito;
10. atualizar documentação e handoff quando a tarefa exigir;
11. não executar commit quando o prompt explicitamente proibir.

## 7. Git Workflow

Fluxo adotado:

```text
uma tarefa
→ uma branch
→ um agente/terminal
→ revisão
→ commit
→ push
→ PR
→ merge
→ main atualizada
→ próxima tarefa
```

Agentes simultâneos alterando o mesmo repositório não são o fluxo padrão.

## 8. Definition of Ready

Uma tarefa só fica `READY` quando tem objetivo, atores, fluxo, regras, estados, entidades, permissões, impactos, critérios de aceitação e estratégia de testes definidos, sem blocker operacional aberto. A lista canônica e completa está em `PROJECT_OS.md`, seção **Definition of Ready**.

## 9. Definition of Done

Uma tarefa só fica `DONE` quando seus critérios foram atendidos e as evidências proporcionais ao risco — revisão, testes aplicáveis, segurança, auditoria, documentação e handoff — estão concluídas. A lista canônica, inclusive os itens `N/A` para tarefas documentais, está em `PROJECT_OS.md`, seção **Definition of Done**.

## 10. Sensitive Data

Nunca colocar no Git:

- dados reais de paciente;
- prontuário;
- documentos clínicos;
- credenciais;
- secrets;
- tokens;
- arquivos `.env`;
- dados bancários sensíveis.

## 11. Using the Prompt Library

`.prompts/` é uma biblioteca reutilizável de métodos de execução, não uma fonte de verdade do produto.

Antes de usar um workflow:

1. verificar se o workflow e seus módulos realmente existem;
2. ler as regras de `01-core` correspondentes;
3. combinar o método com este perfil;
4. combinar o método com `PROJECT_OS.md`;
5. combinar o método com os documentos canônicos da tarefa.

Nunca execute um prompt genérico ignorando o contexto específico do Fisiofit. Perfis ou exemplos externos não substituem este arquivo nem os documentos canônicos do repositório.

## 12. Current Development Phase

A fase/tarefa atual deve sempre ser consultada em `PROJECT_OS.md`.
