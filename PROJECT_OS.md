# FISIOFIT CRM 2.0 — PROJECT OS

> **Fonte operacional única do projeto**
>
> Este arquivo é o ponto de entrada obrigatório para qualquer pessoa ou IA que trabalhe no Fisiofit CRM.
> Ele existe para impedir perda de contexto, checklists paralelos, decisões esquecidas, retrabalho e desenvolvimento sem direção.
>
> **Regra central:** conversa de chat não é fonte de verdade. Se uma decisão importante foi tomada, ela deve voltar para este arquivo ou para um documento canônico referenciado por ele.

---

# 0. METADADOS DO PROJETO

```yaml
project:
  name: Fisiofit CRM 2.0
  status: imp_002_design_done_patient_search_list_ready
  architecture_direction: modular_monolith
  frontend: React + TypeScript
  backend: ASP.NET Core + C#
  database: PostgreSQL
  infra: Docker + CI/CD
  automation: n8n somente como orquestrador de borda
  current_priority: implementar IMP-002 Patient Search / List conforme design aprovado

control:
  single_operational_source_of_truth: PROJECT_OS.md
  active_backlog_location: PROJECT_OS.md
  active_checklist_location: PROJECT_OS.md
  chat_is_source_of_truth: false
  allowed_task_states:
    - TODO
    - READY
    - IN_PROGRESS
    - BLOCKED
    - REVIEW
    - DONE
```

## 0.1 MARCO ATUAL

**FASE ATUAL:** design do segundo vertical slice backend concluído
**MARCO CONCLUÍDO:** IMP-002-DESIGN — Patient Search / List — DONE / PASS
**PRÓXIMO MARCO:** IMP-002 — Patient Search / List
**ÚLTIMA TAREFA CONCLUÍDA:** IMP-002-DESIGN — busca/listagem administrativa sem join cross-schema
**PRÓXIMA TAREFA:** implementar somente IMP-002 conforme `docs/implementation/IMP_002_PATIENT_SEARCH_LIST_DESIGN.md`

| Domínio | Status |
|---|---|
| DOM-011 — People / Patients | DONE — APROVADO PARA MODELAGEM |
| DOM-012 — CRM | DONE — APROVADO PARA MODELAGEM |
| DOM-013 — Scheduling / Agenda | DONE — APROVADO PARA MODELAGEM |
| DOM-014 — Pilates / Turmas | DONE — APROVADO PARA MODELAGEM |
| DOM-015 — Clinical | DONE — APROVADO PARA MODELAGEM |
| DOM-016 — Plans / Contracts / Enrollment | DONE — APROVADO PARA MODELAGEM |
| DOM-017 — Billing / Payments / Delinquency | DONE — APROVADO PARA MODELAGEM |
| DOM-018 — Finance / Cash / Closing | DONE — APROVADO PARA MODELAGEM |

Sequência oficial imediata:

1. `BOOT-001` — Solution Skeleton — concluído;
2. `IMP-001-DESIGN` — primeiro slice pequeno e verificável — concluído;
3. `IMP-000` — Organization / Unit Baseline — concluído e validado em PostgreSQL;
4. `IMP-001` — adulto/self-payer backend POST/GET — concluído, mantendo minors/payer diferente e produção externa gated.

Modelagem conceitual, máquinas de estado, catálogo final de eventos, autorização conceitual, arquitetura física, modelo lógico, contratos de aplicação/API e o skeleton físico estão concluídos. IMP-000 e IMP-001 estão `DONE / PASS`. IMP-002-DESIGN definiu o próximo recorte: listagem/busca administrativa com Patients como query/composition owner, People como owner do filtro civil e Organization reutilizado para Unit, sem join ou persistence cross-context. IMP-002 está `READY`.

---

# 1. COMO USAR ESTE ARQUIVO

## 1.1 O que fica aqui

Este arquivo centraliza:

- estado atual do projeto;
- decisões aprovadas;
- decisões pendentes;
- blockers;
- roadmap;
- backlog mestre;
- ordem de execução;
- vertical slices;
- Definition of Ready;
- Definition of Done;
- arquitetura alvo;
- guardrails;
- riscos;
- handoff entre sessões;
- próximas ações;
- índice dos documentos detalhados.

## 1.2 O que NÃO deve existir fora daqui

Para evitar fragmentação:

- não criar outro backlog concorrente;
- não criar checklist geral em README, Notion, chat ou outro arquivo;
- não criar roadmap paralelo;
- não manter lista de "próximos passos" em vários lugares;
- não deixar decisão importante apenas em conversa;
- não deixar blocker apenas na cabeça da equipe.

Documentos em `/docs` podem conter detalhes técnicos e de domínio, mas **não devem ter um backlog concorrente**.

## 1.3 Regra de encerramento de sessão

Toda sessão relevante deve terminar com atualização de:

1. tarefas trabalhadas;
2. status das tarefas;
3. decisões novas;
4. blockers;
5. arquivos alterados;
6. testes executados;
7. riscos;
8. `Último Handoff`;
9. `Próximas 3 ações`.

Se isso não aconteceu, a sessão não está operacionalmente encerrada.

---

# 2. HIERARQUIA DE FONTE DE VERDADE

Quando houver conflito entre informações:

1. **Decisão APROVADA mais recente registrada neste PROJECT_OS**
2. **Documento canônico de domínio/arquitetura referenciado por este arquivo**
3. **ADR aprovado**
4. **Contrato de API/modelo lógico aprovado**
5. **Código e migrations já implementados**
6. **Caderno de descoberta**
7. **Chats/prompts antigos**

Código existente **não pode silenciosamente invalidar regra de negócio aprovada**.

---

# 3. VISÃO DO PRODUTO

O Fisiofit CRM 2.0 deverá organizar de forma integrada:

- pessoas;
- pacientes;
- responsáveis;
- profissionais;
- duas unidades;
- grade fixa recorrente;
- turmas;
- pacientes fixos por horário e fisioterapeuta;
- presença e falta;
- reposição;
- agenda avulsa;
- avaliação;
- prontuário;
- evoluções;
- planos;
- contratos;
- matrículas;
- cobrança;
- pagamentos;
- financeiro;
- CRM comercial;
- comunicação;
- automações;
- documentos;
- permissões;
- auditoria;
- relatórios.

O sistema legado é fonte de evidência, **não blueprint obrigatório**.

---

# 4. OBJETIVOS DE NEGÓCIO

## P0

- organização operacional das duas unidades;
- visão precisa de turmas, horários, profissionais e pacientes;
- preservação de histórico;
- redução de conflito de agenda;
- prontuário/evoluções consistentes;
- controle de planos, matrículas e recebíveis;
- segurança e rastreabilidade.

## P1

- CRM comercial;
- follow-up;
- reposições estruturadas;
- automações;
- indicadores operacionais e financeiros.

## P2

- otimizações avançadas;
- analytics sofisticado;
- integrações adicionais;
- recursos não essenciais ao MVP.

---

# 5. PRINCÍPIOS NÃO NEGOCIÁVEIS

1. Uma pessoa real não deve ser duplicada por possuir papéis diferentes.
2. Regras de negócio pertencem ao domínio.
3. O presente não reescreve o passado.
4. Histórico relevante não é apagado silenciosamente.
5. Dinheiro é modelado por eventos e obrigações reais.
6. Dado clínico é separado do administrativo.
7. Registro clínico finalizado preserva autoria e integridade.
8. Autorização é deny-by-default.
9. Autoridade técnica não significa autoridade de negócio.
10. Exceções não alteram silenciosamente a regra geral.
11. n8n executa/orquestra; não decide regra central.
12. Integrações devem assumir falhas.
13. Operações críticas devem ser auditáveis.
14. Regra crítica deve possuir uma fonte de verdade.
15. Grade recorrente e ocorrência são conceitos diferentes.
16. Remover paciente da turma não apaga seu histórico.
17. O sistema deve impedir estados impossíveis.
18. Segurança e privacidade são requisitos arquiteturais.
19. Comportamento crítico precisa de teste.
20. Complexidade técnica só entra quando resolve um problema real.
21. Telas não definem o domínio.
22. Toda mudança relevante de regra deve ter vigência quando necessário.
23. Nenhuma funcionalidade entra só porque existia no legado.

---

# 6. PAPÉIS DO SISTEMA

## Proprietária / Gestora

Pode decidir:

- regras comerciais;
- regras operacionais;
- preços;
- planos;
- descontos;
- exceções;
- políticas;
- permissões;
- prioridades de produto.

## Secretária / Recepção

Opera:

- cadastro administrativo;
- agenda;
- turmas;
- CRM;
- comunicação;
- cobrança/pagamento dentro da alçada.

Não altera prontuário clínico finalizado.

## Fisioterapeuta

Pode:

- consultar própria agenda;
- consultar próprias turmas;
- visualizar pacientes necessários ao atendimento;
- adicionar/remover pacientes de turmas conforme regra;
- registrar presença/falta;
- realizar avaliação;
- registrar evolução;
- consultar histórico clínico necessário.

## Desenvolvedor / TI

É custodiante técnico.

Não ganha autoridade de negócio por ter acesso técnico.

---

# 7. MAPA DE DOMÍNIOS

| Domínio | Responsabilidade |
|---|---|
| Identity | autenticação, conta, sessão, roles e permissions |
| Organization | clínica, unidades, salas informativas, calendário |
| People | identidade civil, contatos e relações |
| Patients | papel de paciente e dados administrativos específicos |
| Staff | profissional, vínculo e disponibilidade |
| CRM | Opportunity, pipeline, atividades e tarefas |
| Scheduling | grade, compromissos, conflitos e exceções |
| Pilates | turma, recorrência, ocorrência, vínculo, presença e reposição |
| Clinical | episódio, avaliação, evolução, retificação e prontuário |
| Plans/Enrollment | plano, versão, contrato, matrícula e benefícios |
| Billing | recebíveis, pagamentos, alocações e estornos |
| Finance | despesas, caixa, fechamento e gestão financeira |
| Communication | mensagens, templates e tentativas de entrega |
| Documents | storage, arquivo, versão e metadados |
| Privacy/Audit | privacidade, trilhas e auditoria |
| Reports | read models, indicadores e métricas |

---

# 8. DECISÕES DE DOMÍNIO JÁ APROVADAS

## 8.1 People / Patients

- `Person` é identidade central.
- pessoa pode existir sem CPF.
- CPF, quando informado, deve ser único.
- `PatientProfile` não duplica dados civis.
- `ProfessionalProfile` é separado de `UserAccount`.
- menor e responsável são pessoas separadas.
- pagador pode ser diferente do paciente.
- contatos são estruturados.
- merge exige revisão humana e auditoria.

## 8.2 CRM

- `Opportunity` é a entidade comercial.
- Lead é visão operacional de uma pessoa com oportunidade.
- uma pessoa pode possuir várias oportunidades ao longo do tempo.
- histórico comercial não é apagado.
- LossReason é obrigatório.
- experimental integra a agenda avulsa.

Pipeline inicial:

`NOVO`
→ `CONTATO_INICIADO`
→ `QUALIFICADO`
→ `EXPERIMENTAL_AGENDADA`
→ `EXPERIMENTAL_REALIZADA`
→ `PROPOSTA_APRESENTADA`
→ `NEGOCIACAO`
→ `CONVERTIDO`

Saídas:

- `PERDIDO`
- `DESQUALIFICADO`

## 8.3 Agenda

- grade principal é fixa e recorrente;
- segunda a sexta;
- 06:00–21:00;
- último slot 20:00–21:00;
- sala é somente informativa;
- equipamentos não são agendáveis;
- conflito de profissional é impeditivo;
- conflito de paciente é impeditivo;
- capacidade é impeditiva;
- mudança permanente cria nova vigência;
- mudança pontual afeta somente ocorrência;
- avaliação, experimental, reposição e encaixe são compromissos avulsos.

## 8.4 Pilates

Conceitos separados:

- `Class`
- `ClassSchedule`
- `ClassMembership`
- `ClassOccurrence`
- `Attendance`
- `MakeupCredit`
- `MakeupReservation`

Princípios:

- não reescrever histórico;
- vínculo possui vigência;
- reposição não altera turma fixa;
- overbooking não permitido inicialmente;
- sala não define capacidade.

## 8.5 Clinical

- dado clínico é segregado;
- avaliação e evolução são diferentes;
- evolução pode ficar em rascunho;
- finalização protege o conteúdo;
- correções usam retificação/adendo;
- anexos usam storage privado;
- exportação é operação sensível;
- desligamento remove acesso e preserva autoria;
- break-glass é excepcional e auditado.

## 8.6 Plans / Contracts / Enrollment

- estrutura: `Plan → PlanVersion → Contract → Enrollment → ClassMembership`;
- planos mensal, trimestral e semestral; frequências 1x, 2x e 3x por semana;
- `PlanVersion` preserva preço/condições e `Contract` é snapshot aceito;
- pró-rata = aulas restantes × H/A da `PlanVersion`;
- pausa: máximo 15 dias, libera vaga, não cobra o período e não prolonga o contrato;
- cancelamento: imediato, sem multa, preserva histórico e elimina cobranças futuras;
- renovação cria novo `Contract`; frequência/unidade mudam com vigência sem recriar paciente.

## 8.7 Billing / Payments / Delinquency

- todos os `Receivables` do contrato são criados no fechamento/ativação da contratação;
- vencimentos: 5, 10, 15, 20 ou 25; fim de semana/feriado passa ao próximo útil;
- pagamento parcial e antecipado são permitidos; sem multa e sem juros;
- tolerância financeira de 5 dias; primeira ação de cobrança em D+2;
- inadimplência usa `FinancialRestriction`, nunca `Enrollment.PAUSED` como sinônimo;
- `Payment` confirmado não é apagado; correção usa `PaymentReversal`; estorno parcial e `Refund` são permitidos;
- conta de recebimento e usuário registrador são obrigatórios.

## 8.8 Finance / Cash / Closing

- duas contas bancárias e um caixa físico compartilhado, sem caixa por unidade;
- saldo deriva das movimentações; transferências não são receita nem despesa;
- despesas/contas a pagar usam escopo `UNIT` ou `GLOBAL`;
- fechamento é mensal, inclui previsto x realizado e pode ocorrer com pendências;
- reabertura preserva snapshots/versionamento;
- Secretária e Proprietária podem fechar/reabrir conforme permissão; Desenvolvedor somente com permissão financeira explícita;
- comissão não existe e está fora do MVP.

---

# 9. PENDÊNCIAS

## OPEN QUESTIONS

As regras fechadas no Gate M1 estão nos documentos canônicos em `/docs`. As questões abaixo não podem ser resolvidas por inferência de código legado, UI ou acesso técnico.

### BLOCKERS BEFORE CONCEPTUAL MODEL

Nenhum blocker conhecido. DOM-011 a DOM-018 estão aprovados para Context Map, Ownership Map e modelagem conceitual.

### BLOCKERS BEFORE IMPLEMENTATION

- definir valores/aprovadores das alçadas ainda abertas de desconto, negociação, reversal, refund e transferência;
- fechar mecanismo concreto de autenticação, MFA/step-up, sessão, revogação e recuperação antes de implementar Identity/autorização;
- definir o tratamento de Receivables vencidos no cancelamento: não presumir perdão automático;
- definir alçadas e limites de desconto/negociação; perdão arbitrário não é permitido;
- definir o efeito da pausa sobre disponibilidade e expiração de MakeupCredits, sem prolongar o Contract;
- detalhar precedência de operações financeiras concorrentes, como pausa, cancelamento, reversão e reembolso na mesma data.
- definir allowlist/formato, tamanho máximo, scan/quarantine e provider antes de aceitar uploads;
- definir contratos provider-specific antes de criar inbound webhooks;
- definir quotas concretas por ambiente/finalidade antes de expor superfícies externas dependentes de rate limit.

### BLOCKERS BEFORE GO-LIVE

- validar retenção clínica e documental com responsável técnico e orientação jurídica/regulatória;
- validar campos clínicos obrigatórios, assinatura/finalização, exportação e regras para menores;
- concluir inventário LGPD: finalidades, bases, compartilhamentos, legal hold e solicitações de titulares;
- definir e testar backup, restore, RPO/RTO, resposta a incidente e operação de suporte privilegiado;
- inventariar fontes legadas, transformação, corte, reconciliação e aceite da migração.

### NON-BLOCKING / LATER

- provider final de WhatsApp, e-mail, storage e observabilidade;
- design visual e Design System definitivo;
- calendário/fonte final de feriados;
- catálogo inicial de categorias de despesas e motivos de ajustes;
- política avançada de deslocamento entre unidades;
- comprovantes formais/anexos de Payment e Expense;
- KPIs e fórmulas gerenciais detalhadas;
- Commission continua fora do MVP.

---

# 10. ROADMAP OFICIAL

## Fase 0 — Governança e Project OS

- [x] visão inicial;
- [x] princípios;
- [x] papéis;
- [x] mapa de domínio inicial;
- [x] decisões iniciais de People;
- [x] decisões iniciais de CRM;
- [x] decisões iniciais de Agenda;
- [x] decisões iniciais de Pilates;
- [x] decisões iniciais de Clinical;
- [x] Project OS criado;
- [x] colocar este arquivo na raiz do repositório;
- [x] garantir que nenhum outro backlog geral seja usado;
- [x] criar baseline canônica de domínio, regras, processos e decisões.

### Gate
Project OS na raiz e adotado como ponto de entrada.

---

## Fase 1 — Fechar regras de negócio operacionais

### P0
- [x] capítulo 16 — Plans/Contracts/Enrollment;
- [x] capítulo 17 — Billing;
- [x] capítulo 18 — Finance;
- [x] matriz de permissões;
- [ ] regras de segurança-base.

### P1
- [ ] Communication;
- [ ] Reports;
- [ ] Privacy detalhada;
- [ ] migração.

### Gate
**CONCLUÍDO PARA MODELAGEM CONCEITUAL:** nenhum blocker de domínio em DOM-011 a DOM-018. Permissões e segurança-base continuam como gates antes da implementação correspondente.

---

## Fase 2 — Modelagem conceitual — DONE

`ARC-001`, `ARC-002` e `MODEL-001` a `MODEL-005` foram concluídos. A baseline integrada passou sem blocker para STATE-001.

### People / Patients / Staff / Organization
- [x] Person
- [x] ContactPoint
- [x] Address
- [x] PersonRelationship
- [x] PersonMerge / MergeManifest
- [x] PatientProfile
- [x] GuardianLink
- [x] AdministrativeResponsibleLink
- [x] ResponsiblePayerLink
- [x] EmergencyContact
- [x] ProfessionalProfile
- [x] EmploymentLink
- [x] ProfessionalUnitLink
- [x] Availability
- [x] ProfessionalLeave
- [x] Clinic
- [x] Unit
- [x] Room
- [x] InstitutionalCalendar / Holiday

### Scheduling / Pilates
- [x] ScheduleRule/FixedSchedule geral (não-turma)
- [x] Appointment
- [x] CalendarException
- [x] Class
- [x] ClassSchedule
- [x] ClassMembership
- [x] ClassOccurrence
- [x] Attendance
- [x] MakeupCredit
- [x] MakeupReservation

### Clinical
- [x] CareEpisode
- [x] Assessment
- [x] ClinicalTemplate / ClinicalTemplateVersion
- [x] ClinicalEntry
- [x] Rectification / Addendum
- [x] ClinicalDocumentLink
- [x] BreakGlassAccess
- [x] ClinicalExportOperation / ClinicalFinalizationPolicy

### Plans / Billing / Finance
- [x] Plan
- [x] PlanVersion
- [x] Contract
- [x] Enrollment
- [x] Benefit/Entitlement classificados como conceitos deferred
- [x] Receivable
- [x] Payment
- [x] PaymentAllocation
- [x] PaymentReversal
- [x] Refund
- [x] Expense
- [x] FinancialAccount (incluindo CASH)
- [x] FinancialTransaction
- [x] Closing / ClosingSnapshot

### Gate
Modelo conceitual integrado aprovado antes de desenhar SQL definitivo.

---

## Fase 3 — Máquinas de estado — DONE

Obrigatórias:

- [x] Opportunity
- [x] Appointment
- [x] Enrollment
- [x] Contract
- [x] ClassOccurrence
- [x] MakeupCredit
- [x] ClinicalEntry
- [x] Assessment
- [x] Receivable
- [x] Payment
- [x] Task
- [x] PrivacyRequest classificada como DEFERRED por ausência de lifecycle aprovado

Para cada transição definir:

- origem;
- destino;
- comando;
- ator;
- pré-condição;
- regra;
- side effects;
- evento;
- auditoria;
- reversibilidade.

### Gate
Máquinas prioritárias formalizadas, lifecycles restantes classificados e nenhum blocker para EVT-001.

---

## Fase 4 — Arquitetura

- [x] confirmar monólito modular;
- [x] definir estrutura física de módulos;
- [x] definir dependências permitidas;
- [x] definir contratos públicos entre módulos;
- [x] definir application layer;
- [x] definir domain layer;
- [x] definir infrastructure layer;
- [x] definir Unit of Work;
- [x] definir transações;
- [x] definir domain events conceituais — EVT-001;
- [x] definir integration events conceituais — EVT-001;
- [x] definir Outbox/inbox por critério de confiabilidade;
- [x] definir abstração de background processing; provider deferred;
- [x] definir fronteira do n8n;
- [x] definir error model arquitetural;
- [x] definir logging;
- [x] definir correlation ID;
- [x] definir storage privado e autorização herdada;
- [x] definir configuração por ambiente.

### Gate
ARC-003 aprovado. ADR-001 a ADR-007 estão aceitas; DB-001 fechou as estruturas lógicas seletivas de Outbox/Inbox R2 da ADR-004 e API-001 fechou a baseline HTTP na ADR-007 sem reabrir boundaries.

---

## Fase 5 — Segurança e autorização

- [x] Role model conceitual;
- [x] Permission model conceitual;
- [x] resource-level policies conceituais;
- [x] escopo por unidade definido conceitualmente; atribuição default permanece aberta;
- [x] escopo por profissional;
- [x] escopo por paciente;
- [x] acesso Clinical conceitual;
- [x] break-glass conceitual;
- [ ] MFA/step-up;
- [ ] sessões;
- [ ] revogação;
- [ ] rate limit;
- [ ] proteção de upload;
- [ ] redaction de logs;
- [ ] secrets management.

---

## Fase 6 — Modelo lógico e banco

- [x] convenção de IDs;
- [x] PK/FK;
- [x] uniques;
- [x] nullability lógica relevante;
- [x] checks;
- [x] índices candidatos classificados;
- [x] created_at/updated_at;
- [x] autoria;
- [x] optimistic locking seletivo;
- [x] concorrência de capacidade;
- [x] concorrência financeira;
- [x] precisão monetária;
- [x] timezone/time strategy;
- [x] delete policy;
- [ ] migrations;
- [ ] seed strategy;
- [ ] backup;
- [ ] restore test.

### Gate
Modelo lógico aprovado antes da primeira migration produtiva.

---

## Fase 7 — UX e arquitetura de informação

- [ ] navegação principal;
- [ ] Home por perfil;
- [ ] busca global;
- [ ] Agenda;
- [ ] detalhe da turma;
- [ ] cadastro de pessoa/paciente;
- [ ] perfil do paciente;
- [ ] prontuário;
- [ ] evolução;
- [ ] CRM;
- [ ] Billing;
- [ ] Finance;
- [ ] Admin.

Cada tela deve possuir:

- loading;
- empty;
- error;
- forbidden;
- validation;
- confirmation;
- undo quando aplicável;
- responsividade;
- acessibilidade.

---

## Fase 8 — Scaffold técnico

BOOT-001 pode iniciar antes de uma vertical slice funcional estar `READY`, porque cria somente a estrutura física sem materializar o catálogo API.

Pode criar:

- [x] solution e projects;
- [x] module assemblies, namespaces/folders e registration surfaces;
- [x] BuildingBlocks e ModuleContracts mínimos;
- [x] Host e configuration baseline;
- [x] test projects e dependency/architecture rules;
- [x] frontend skeleton.

Não faz parte de BOOT-001:

- implementar os 137 commands ou os 77 queries;
- criar todos os endpoints;
- implementar regras de negócio ou state machines;
- criar tabelas, schemas físicos ou migrations;
- resolver decisões `GATED`/`DEFERRED`.

PostgreSQL funcional, migrations, auth funcional, Docker/CI operacional e a primeira vertical slice exigem tarefas próprias e seus gates.

---

## Fase 9 — Vertical Slices

### VS-01 — Cadastrar paciente e colocar em turma fixa

Deve validar:

- Identity;
- People;
- Patients;
- Staff;
- Organization;
- Pilates;
- Scheduling;
- autorização;
- banco;
- API;
- frontend;
- auditoria;
- testes.

### VS-02 — Abrir turma do dia e realizar chamada

Deve validar:

- ocorrência;
- pacientes esperados;
- presença/falta;
- permissões;
- histórico;
- concorrência.

### VS-03 — Da turma para evolução clínica

Deve validar:

- Clinical;
- CareEpisode;
- ClinicalEntry;
- rascunho;
- finalização;
- escopo de acesso;
- auditoria.

### VS-04 — Matrícula → Recebível → Pagamento

Só entra quando Plans/Billing estiverem sem blocker.

---

# 11. BACKLOG MESTRE

## Convenção

- `P0`: necessário para fundação/MVP;
- `P1`: importante após fundação;
- `P2`: melhoria/posterior.

---

## EPIC GOV — Governança

| ID | Tarefa | Prioridade | Status |
|---|---|---:|---|
| GOV-001 | Adotar PROJECT_OS na raiz do repo | P0 | DONE |
| GOV-002 | Remover/arquivar backlog geral concorrente | P0 | DONE |
| GOV-003 | Criar índice de documentos canônicos | P0 | DONE |
| GOV-004 | Criar Decision Log | P0 | DONE |
| GOV-005 | Criar ADR index | P0 | TODO |
| GOV-006 | Adotar protocolo de handoff | P0 | DONE |

---

## EPIC DOM — Definição de domínio

| ID | Tarefa | Prioridade | Status |
|---|---|---:|---|
| DOM-011 | Fechar People/Patients | P0 | DONE |
| DOM-012 | Fechar CRM | P0 | DONE |
| DOM-013 | Fechar Scheduling/Agenda | P0 | DONE |
| DOM-014 | Fechar Pilates/Turmas | P0 | DONE |
| DOM-015 | Fechar Clinical | P0 | DONE |
| DOM-016 | Fechar Plans/Contracts/Enrollment | P0 | DONE |
| DOM-017 | Fechar Billing | P0 | DONE |
| DOM-018 | Fechar Finance | P0 | DONE |
| DOM-019 | Fechar Communication | P1 | TODO |
| DOM-020 | Fechar Staff/Operation | P1 | TODO |
| DOM-021 | Fechar Reports/KPIs | P1 | TODO |

---

## EPIC MODEL — Modelagem conceitual

| ID | Tarefa | Prioridade | Status |
|---|---|---:|---|
| MODEL-001 | People / Patients / Staff / Organization | P0 | DONE |
| MODEL-002 | Scheduling / Pilates | P0 | DONE |
| MODEL-003 | Clinical | P0 | DONE |
| MODEL-004 | Plans / Billing / Finance | P0 | DONE |
| MODEL-005 | Modelo Conceitual Integrado | P0 | DONE |

---

## EPIC STATE / EVT / AUTH — Próxima sequência

| ID | Tarefa | Prioridade | Status |
|---|---|---:|---|
| STATE-001 | Máquinas de Estado | P0 | DONE |
| EVT-001 | Catálogo final de Eventos de Domínio | P0 | DONE |
| AUTH-001 | Matriz de Permissões e Policies | P0 | DONE |

---

## EPIC ARC — Arquitetura

| ID | Tarefa | Prioridade | Status |
|---|---|---:|---|
| ARC-001 | Context Map definitivo | P0 | DONE |
| ARC-002 | Matriz de ownership | P0 | DONE |
| ARC-003 | Physical Architecture / Modular Monolith Design | P0 | DONE |
| ARC-004 | ADR monólito modular | P0 | DONE |
| ARC-005 | Definir application/domain/infrastructure | P0 | DONE |
| ARC-006 | Definir transaction boundaries | P0 | DONE |
| ARC-007 | Definir event strategy | P0 | DONE |
| ARC-008 | Definir Outbox/inbox por criticidade | P1 | DONE |
| ARC-009 | Definir storage strategy arquitetural | P1 | DONE |

---

## EPIC DB / API — Próxima sequência após ARC-003

| ID | Tarefa | Prioridade | Status |
|---|---|---:|---|
| DB-001 | Logical Data Model | P0 | DONE |
| API-001 | Application/API Contracts | P0 | DONE |

> API-001 e BOOT-001 estão concluídos. A revisão final de IMP-001-DESIGN tornou IMP-000 o próximo prerequisite obrigatório antes de qualquer implementação de Register Patient.

---

## EPIC BOOT — Bootstrap técnico

| ID | Tarefa | Prioridade | Status |
|---|---|---:|---|
| BOOT-001 | Solution Skeleton backend/frontend e testes | P0 | DONE |

> DB-001 e API-001 forneceram os boundaries e contracts necessários. BOOT-001 pode criar somente o skeleton; detalhes deferred continuam gates das slices afetadas.

> O catálogo de 137 commands + 77 queries não é backlog de BOOT-001. `READY` em API-001 significa implementável futuramente, não selecionado para implementação imediata.

---

## EPIC IMP — Vertical slices de implementação

| ID | Tarefa | Prioridade | Status |
|---|---|---:|---|
| IMP-001-DESIGN | Register Patient Vertical Slice Design | P0 | DONE — PASS |
| IMP-000 | Organization / Unit Baseline | P0 | DONE — PASS |
| IMP-001 | Register Patient Vertical Slice | P0 | DONE — PASS (adult + SELF + backend + administrative POST/GET only) |
| IMP-002-DESIGN | Patient Search / List Design | P0 | DONE — PASS |
| IMP-002 | Patient Search / List | P0 | READY |

> Escopo normativo de IMP-001: `docs/implementation/IMP_001_REGISTER_PATIENT_DESIGN.md`; resultado executado: `docs/implementation/IMP_001_REGISTER_PATIENT.md`. `DONE` significa somente adulto + `SELF` + backend + POST/GET administrativo. People/Patients não estão completos. Menores, payer diferente, UI e ativação externa/produção sem IAM/Audit concretos permanecem GATED.

> Escopo normativo de IMP-002: `docs/implementation/IMP_002_PATIENT_SEARCH_LIST_DESIGN.md`. `READY` significa somente GET collection administrativo, filtros/search/paginação/sort aprovados e composição Patients/People/Organization. Não inclui frontend, detalhe novo, update, relacionamentos, fuzzy/dedup, Reports ou infraestrutura de busca.

---

## EPIC IAM — Identity & Access

| ID | Tarefa | Prioridade | Status |
|---|---|---:|---|
| IAM-001 | Modelar UserAccount | P0 | TODO |
| IAM-002 | Definir Role | P0 | TODO |
| IAM-003 | Definir Permission | P0 | TODO |
| IAM-004 | Definir resource policies | P0 | TODO |
| IAM-005 | Definir sessões | P0 | TODO |
| IAM-006 | Definir MFA/step-up | P1 | BLOCKED |
| IAM-007 | Definir recovery | P1 | TODO |

---

## EPIC PPL — People

| ID | Tarefa | Prioridade | Status |
|---|---|---:|---|
| PPL-001 | Modelar Person | P0 | DONE |
| PPL-002 | Modelar ContactPoint | P0 | DONE |
| PPL-003 | Modelar Address | P1 | DONE |
| PPL-004 | Modelar PersonRelationship | P0 | DONE |
| PPL-005 | Modelar PersonMerge | P1 | DONE |
| PPL-006 | Definir deduplicação | P1 | DONE |

---

## EPIC PAC — Patients

| ID | Tarefa | Prioridade | Status |
|---|---|---:|---|
| PAC-001 | Modelar PatientProfile | P0 | DONE |
| PAC-002 | Modelar GuardianLink | P0 | DONE |
| PAC-003 | Modelar ResponsiblePayerLink | P0 | DONE |
| PAC-004 | Modelar EmergencyContact | P1 | DONE |
| PAC-005 | Caso de uso criar paciente | P0 | TODO |
| PAC-006 | Modelar AdministrativeResponsibleLink | P0 | DONE |

---

## EPIC STF — Staff

| ID | Tarefa | Prioridade | Status |
|---|---|---:|---|
| STF-001 | Modelar ProfessionalProfile | P0 | DONE |
| STF-002 | Modelar EmploymentLink | P0 | DONE |
| STF-003 | Modelar unidade de atuação | P0 | DONE |
| STF-004 | Modelar Availability | P1 | DONE |
| STF-005 | Modelar Leave | P1 | DONE |

---

## EPIC ORG — Organization

| ID | Tarefa | Prioridade | Status |
|---|---|---:|---|
| ORG-001 | Modelar Clinic | P0 | DONE |
| ORG-002 | Modelar Unit | P0 | DONE |
| ORG-003 | Modelar Room informativa | P0 | DONE |
| ORG-004 | Modelar InstitutionalCalendar | P1 | DONE |
| ORG-005 | Modelar Holiday | P1 | DONE |

---

## EPIC AGD — Scheduling

| ID | Tarefa | Prioridade | Status |
|---|---|---:|---|
| AGD-001 | Modelar grade fixa | P0 | DONE |
| AGD-002 | Modelar Appointment | P0 | DONE |
| AGD-003 | Modelar CalendarException | P1 | DONE |
| AGD-004 | Regra conflito de profissional | P0 | DONE |
| AGD-005 | Regra conflito de paciente | P0 | DONE |
| AGD-006 | Visão por unidade | P0 | DONE |
| AGD-007 | Visão por profissional | P0 | DONE |

---

## EPIC PIL — Pilates

| ID | Tarefa | Prioridade | Status |
|---|---|---:|---|
| PIL-001 | Modelar Class | P0 | DONE |
| PIL-002 | Modelar ClassSchedule | P0 | DONE |
| PIL-003 | Modelar ClassMembership | P0 | DONE |
| PIL-004 | Modelar ClassOccurrence | P0 | DONE |
| PIL-005 | Modelar Attendance | P0 | DONE |
| PIL-006 | Regra de capacidade | P0 | DONE |
| PIL-007 | Transferência de turma | P0 | DONE |
| PIL-008 | Modelar MakeupCredit | P1 | DONE |
| PIL-009 | Modelar MakeupReservation | P1 | DONE |

---

## EPIC CLI — Clinical

| ID | Tarefa | Prioridade | Status |
|---|---|---:|---|
| CLI-001 | Modelar CareEpisode | P0 | DONE |
| CLI-002 | Modelar Assessment | P0 | DONE |
| CLI-003 | Modelar ClinicalEntry | P0 | DONE |
| CLI-004 | Modelar finalização | P0 | DONE |
| CLI-005 | Modelar Rectification/Addendum | P0 | DONE |
| CLI-006 | Definir requisitos conceituais de permissões clínicas | P0 | DONE |
| CLI-007 | Definir anexos | P1 | DONE |
| CLI-008 | Definir exportação | P1 | DONE |
| CLI-009 | Definir break-glass | P1 | DONE |

---

## EPIC CRM — Comercial

| ID | Tarefa | Prioridade | Status |
|---|---|---:|---|
| CRM-001 | Modelar Opportunity | P1 | TODO |
| CRM-002 | Modelar Pipeline/Stage | P1 | TODO |
| CRM-003 | Modelar Activity | P1 | TODO |
| CRM-004 | Modelar Task | P1 | TODO |
| CRM-005 | Integrar experimental à agenda | P1 | TODO |
| CRM-006 | Modelar conversão | P1 | TODO |
| CRM-007 | Modelar LossReason | P1 | TODO |

---

## EPIC PLN — Plans/Enrollment

| ID | Tarefa | Prioridade | Status |
|---|---|---:|---|
| PLN-001 | Fechar política comercial | P0 | DONE |
| PLN-002 | Modelar Plan | P0 | DONE |
| PLN-003 | Modelar PlanVersion | P0 | DONE |
| PLN-004 | Modelar Contract | P0 | DONE |
| PLN-005 | Modelar Enrollment | P0 | DONE |
| PLN-006 | Modelar pausa/retomada | P0 | DONE |
| PLN-007 | Modelar cancelamento | P0 | DONE |

---

## EPIC BIL — Billing

| ID | Tarefa | Prioridade | Status |
|---|---|---:|---|
| BIL-001 | Fechar política de cobrança | P0 | DONE |
| BIL-002 | Modelar Receivable | P0 | DONE |
| BIL-003 | Modelar Payment | P0 | DONE |
| BIL-004 | Modelar PaymentAllocation | P0 | DONE |
| BIL-005 | Modelar pagamento parcial | P0 | DONE |
| BIL-006 | Modelar reversão/estorno | P0 | DONE |
| BIL-007 | Modelar inadimplência | P1 | DONE |

---

## EPIC FIN — Finance

| ID | Tarefa | Prioridade | Status |
|---|---|---:|---|
| FIN-001 | Modelar Expense | P1 | DONE |
| FIN-002 | Modelar FinancialAccount | P1 | DONE |
| FIN-003 | Modelar FinancialTransaction/Transfer | P1 | DONE |
| FIN-004 | Fechar semântica de Closing | P1 | DONE |
| FIN-005 | Validar comissão — fora do MVP | P2 | DONE |

---

# 12. DEFINITION OF READY

Uma tarefa só pode mudar para `READY` se:

- [ ] objetivo está claro;
- [ ] ator está definido;
- [ ] processo está definido;
- [ ] fluxo principal existe;
- [ ] alternativas relevantes existem;
- [ ] regras relacionadas existem;
- [ ] estados estão definidos;
- [ ] entidades envolvidas estão definidas;
- [ ] permissões estão definidas;
- [ ] impacto em dados está definido;
- [ ] API/use case está claro;
- [ ] UI/jornada está clara quando aplicável;
- [ ] auditoria foi avaliada;
- [ ] segurança foi avaliada;
- [ ] privacidade foi avaliada;
- [ ] critérios de aceitação existem;
- [ ] estratégia de testes existe;
- [ ] não existe blocker operacional aberto.

---

# 13. DEFINITION OF DONE

Uma tarefa só pode mudar para `DONE` se:

> Para tarefas exclusivamente documentais, de descoberta ou modelagem, itens de código, migrations e testes executáveis são `N/A`; `DONE` significa artefato aprovado e critérios do Gate atendidos. Isso se aplica a DOM-011 a DOM-018 neste marco.

- [ ] critérios de aceitação atendidos;
- [ ] código implementado;
- [ ] review concluído;
- [ ] testes unitários relevantes passando;
- [ ] testes de integração relevantes passando;
- [ ] E2E crítico passando quando aplicável;
- [ ] autorização negativa testada;
- [ ] erros tratados;
- [ ] loading tratado;
- [ ] empty state tratado;
- [ ] auditoria implementada quando necessária;
- [ ] logs implementados;
- [ ] observabilidade avaliada;
- [ ] acessibilidade verificada quando UI;
- [ ] migrations revisadas;
- [ ] documentação canônica atualizada;
- [ ] PROJECT_OS atualizado;
- [ ] handoff atualizado.

---

# 14. REGRAS PARA QUALQUER IA

## Antes de trabalhar

A IA deve:

1. ler `PROJECT_OS.md`;
2. identificar a tarefa exata do backlog;
3. ler somente os documentos canônicos relacionados;
4. verificar status e blockers;
5. verificar `git status` e mudanças pendentes;
6. não trabalhar em tarefa `BLOCKED`;
7. não transformar `TODO` em implementação se ainda não estiver `READY`.

## Durante

A IA deve:

- citar IDs de backlog;
- respeitar boundaries;
- não acessar tabelas de outro módulo por atalho;
- não introduzir tecnologia sem justificativa;
- não mudar regra aprovada silenciosamente;
- não criar nova migration para corrigir uma decisão ainda indefinida;
- não colocar regra central no frontend;
- não colocar regra central em n8n;
- não apagar histórico para simplificar código.

## Ao terminar

A IA deve atualizar:

- status;
- resumo;
- decisões;
- arquivos alterados;
- migrations;
- testes;
- blockers;
- riscos;
- próximo passo;
- handoff.

---

# 15. PROMPT PADRÃO PARA CONTINUAR O PROJETO EM QUALQUER IA

Copiar e usar:

```text
Você está trabalhando no Fisiofit CRM 2.0.

Antes de alterar qualquer arquivo:
1. leia PROJECT_OS.md inteiro;
2. leia o último HANDOFF;
3. identifique a tarefa do backlog atualmente READY ou IN_PROGRESS;
4. leia apenas os documentos canônicos ligados a essa tarefa;
5. inspecione o estado atual do repositório e as alterações pendentes.

Regras:
- não invente regra de negócio;
- não implemente item BLOCKED;
- não crie backlog ou checklist paralelo;
- PROJECT_OS.md é o controle operacional central;
- preserve histórico;
- respeite ownership dos módulos;
- n8n não é dono da regra;
- frontend não é autoridade de regra;
- atualize testes e documentação junto do código.

Ao terminar:
- atualize o status da tarefa;
- atualize PROJECT_OS.md;
- registre decisões novas;
- registre blockers;
- registre testes executados;
- escreva novo HANDOFF com as próximas 3 ações.

Se encontrar conflito entre código, documentação e PROJECT_OS, pare e sinalize antes de escolher por conta própria.
```

---

# 16. TEMPLATE DE TAREFA

```md
## XXX-000 — Título

Status: TODO | READY | IN_PROGRESS | BLOCKED | REVIEW | DONE
Prioridade: P0 | P1 | P2
Owner:
Dependências:

### Objetivo
...

### Contexto
...

### Regras relacionadas
- RB-...

### Entidades envolvidas
- ...

### Critérios de aceitação
- [ ] ...

### Testes esperados
- [ ] ...

### Segurança / Privacidade
...

### Observabilidade
...

### Arquivos esperados
...

### Handoff
...
```

---

# 17. TEMPLATE DE DECISÃO

```md
## DEC-XXX-000 — Título

Status: PROPOSTA | APROVADA | SUPERADA
Data:
Dono:

### Problema
...

### Contexto
...

### Alternativas
1. ...
2. ...

### Decisão
...

### Consequências
...

### Impacta
- Domínio:
- Banco:
- API:
- UI:
- Testes:
- Migração:
```

---

# 18. TEMPLATE DE HANDOFF

```md
## HANDOFF — YYYY-MM-DD HH:mm

### Objetivo da sessão
...

### Backlog trabalhado
- XXX-000

### Status final
...

### Concluído
- ...

### Arquivos alterados
- ...

### Decisões tomadas
- DEC-...

### Migrations
- ...

### Testes executados
- ...

### Pendências
- ...

### Blockers
- ...

### Riscos
- ...

### Próximas 3 ações
1. ...
2. ...
3. ...

### Instrução para a próxima IA
Comece por...
```

---

# 19. ESTRUTURA RECOMENDADA DO REPOSITÓRIO

```text
f​isiofit-crm/
├── PROJECT_OS.md
├── README.md
├── docs/
│   ├── domain/
│   ├── processes/
│   ├── business-rules/
│   ├── architecture/
│   ├── modeling/
│   ├── database/
│   ├── api/
│   ├── ui-ux/
│   ├── security/
│   ├── privacy/
│   ├── testing/
│   ├── migration/
│   └── adr/
├── apps/
│   ├── web/
│   └── api/
├── tests/
└── infra/
```

## Regra de documentação

`PROJECT_OS.md` controla o trabalho.

`/docs` explica o sistema.

`/apps` implementa o sistema.

`/tests` prova o comportamento.

`Git` registra a evolução.

## 19.1 Índice canônico do projeto

| Documento | Finalidade |
|---|---|
| `docs/domain/GLOSSARY.md` | Vocabulário e ownership terminológico |
| `docs/domain/BUSINESS_PARAMETERS.md` | Separação entre regra e valor configurável |
| `docs/domain/01-people-patients.md` a `08-finance.md` | Baseline dos oito domínios aprovados para modelagem |
| `docs/business-rules/RULES_INDEX.md` | IDs canônicos de regras |
| `docs/processes/PROCESS_INDEX.md` | IDs e estado dos processos |
| `docs/decisions/DECISIONS.md` | Decisões oficiais e consequências |
| `docs/GATE_M1_AUDIT.md` | Conflitos, correções e pendências |
| `docs/architecture/CONTEXT_MAP.md` | Boundaries, responsabilidades e relações oficiais entre contextos |
| `docs/architecture/OWNERSHIP_MAP.md` | Owner único, acessos cross-context, snapshots e ownership de eventos |
| `docs/modeling/MODEL_001_PEOPLE_PATIENTS_STAFF_ORGANIZATION.md` | Modelo conceitual de Organization, People, Patients e Staff |
| `docs/modeling/MODEL_002_SCHEDULING_PILATES.md` | Modelo conceitual de Scheduling e Pilates |
| `docs/modeling/MODEL_003_CLINICAL.md` | Modelo conceitual de Clinical, histórico, acesso excepcional e exportação |
| `docs/modeling/MODEL_004_PLANS_BILLING_FINANCE.md` | Modelo conceitual de Plans & Enrollment, Billing e Finance, com snapshots, reversões, concorrência e fechamento versionado |
| `docs/modeling/MODEL_005_INTEGRATED_CONCEPTUAL_MODEL.md` | Baseline conceitual integrada dos 16 contextos, com catálogos globais, invariantes, cobertura e fluxos end-to-end |
| `docs/state-machines/STATE_001_STATE_MACHINES.md` | Lifecycles, máquinas de estado, transições, guards, condições derivadas, temporalidade, dependências e auditoria |
| `docs/architecture/DOMAIN_EVENTS.md` | Catálogo canônico de domain/integration events, owners, payloads, consumers, sensibilidade, ordering, idempotência e cobertura |
| `docs/security/AUTH_001_PERMISSIONS_POLICIES.md` | Modelo canônico de roles, scopes, permissions, resource policies, negações, auditoria e cobertura de autorização |
| `docs/architecture/ARC_003_PHYSICAL_ARCHITECTURE.md` | Arquitetura física do modular monolith, módulos, layers, contracts, persistência, comunicação, segurança, deployment e testes |
| `docs/database/DB_001_LOGICAL_DATA_MODEL.md` | Modelo lógico PostgreSQL dos 16 schemas, tabelas, ownership, referências, constraints, concorrência, migrations e Outbox/Inbox R2 |
| `docs/api/API_001_APPLICATION_API_CONTRACTS.md` | Commands, queries, read models, endpoints, contratos públicos, autorização, idempotência, concorrência, erros, workflows e readiness do bootstrap |
| `docs/adr/ADR-001-MODULAR-MONOLITH-PHYSICAL-STRUCTURE.md` | Estrutura física híbrida do modular monolith |
| `docs/adr/ADR-002-MODULE-PERSISTENCE-ISOLATION.md` | PostgreSQL, schemas, DbContexts e referências cross-context — Accepted; detalhes físicos seguem para DB-001 |
| `docs/adr/ADR-003-MODULE-COMMUNICATION-STRATEGY.md` | Contratos tipados in-process, events e read models sem HTTP interno |
| `docs/adr/ADR-004-EVENT-RELIABILITY-STRATEGY.md` | Tiers R0/R1/R2, outbox/inbox seletivos e ausência de broker inicial — Accepted após DB-001 |
| `docs/adr/ADR-005-CLINICAL-DATA-ISOLATION.md` | Isolamento de código, dados, autorização, events, logs e documentos clínicos |
| `docs/adr/ADR-006-IDENTIFIER-STRATEGY.md` | UUID como padrão opaco e nativo, com UUIDv7 preferencial e UUIDv4 fallback |
| `docs/adr/ADR-007-HTTP-API-CONTRACT-BASELINE.md` | `/api/v1`, Problem Details RFC 9457, paginação offset inicial e whitelists de filtro/sort |
| `docs/product/Fisiofit_CRM_2.0_Caderno_Mestre_CONSOLIDADO.docx` | Fonte de descoberta preservada; consultar a auditoria para decisões superadas |

---

# 20. PRIMEIRA VERTICAL SLICE

## VS-01 — Cadastrar paciente e colocá-lo em uma turma fixa

### Objetivo

Validar a fundação da arquitetura ponta a ponta sem tentar construir o CRM inteiro.

### Deve atravessar

- Identity;
- Organization;
- People;
- Patients;
- Staff;
- Scheduling;
- Pilates;
- autorização;
- banco;
- API;
- UI;
- audit;
- testes.

### Checklist de Ready

- [x] Person modelada;
- [x] PatientProfile modelado;
- [x] ProfessionalProfile modelado;
- [x] Unit modelada;
- [x] Class modelada;
- [x] ClassSchedule modelado;
- [x] ClassMembership modelado;
- [x] capacidade definida conceitualmente;
- [x] conflito de paciente definido conceitualmente;
- [x] conflito de profissional definido conceitualmente;
- [ ] permissões definidas;
- [ ] use cases definidos;
- [ ] API definida;
- [ ] wireframe mínimo definido;
- [ ] critérios de aceitação definidos;
- [ ] testes planejados.

### Critérios de aceitação

- [ ] usuário autorizado faz login;
- [ ] busca pessoa existente;
- [ ] cria Person quando necessário;
- [ ] cria PatientProfile;
- [ ] seleciona unidade;
- [ ] localiza turma;
- [ ] visualiza capacidade;
- [ ] sistema bloqueia conflito;
- [ ] adiciona paciente;
- [ ] histórico registra autoria;
- [ ] operação persiste corretamente;
- [ ] UI trata loading;
- [ ] UI trata vazio;
- [ ] UI trata conflito;
- [ ] testes passam.

---

# 21. CHECKLIST PARA COMEÇAR IMPLEMENTAÇÃO FUNCIONAL

Este checklist bloqueia vertical slices, regras de negócio, endpoints funcionais e persistência. Ele não bloqueia o skeleton estritamente limitado de BOOT-001 definido na Fase 8.

Não iniciar implementação funcional até:

- [ ] PROJECT_OS adotado;
- [ ] primeira slice escolhida;
- [ ] arquitetura macro aprovada;
- [ ] boundaries aprovados;
- [ ] matriz de dependências aprovada;
- [ ] estratégia de identidade aprovada;
- [ ] estratégia de autorização-base aprovada;
- [x] convenção de IDs definida;
- [x] timezone definido;
- [x] estratégia monetária definida;
- [ ] audit baseline definido;
- [ ] VS-01 em READY.

Pode existir protótipo isolado antes disso, mas não deve ser confundido com base definitiva do produto.

---

# 22. RISCOS ATUAIS

| Risco | Impacto | Mitigação |
|---|---|---|
| começar banco antes do domínio | alto | modelagem conceitual primeiro |
| espalhar checklists | alto | um backlog único neste arquivo |
| IA inventar regra | alto | blockers explícitos + DoR |
| frontend virar regra | alto | backend/domínio como autoridade |
| n8n virar core | alto | n8n apenas borda |
| apagar histórico | crítico | vigência/eventos/auditoria |
| misturar clínico e administrativo | crítico | boundaries + authorization |
| reintroduzir regra superada do Caderno | crítico | consultar Decision Log e auditoria do Gate M1 |
| excesso de arquitetura | médio | monólito modular |
| código sem handoff | alto | protocolo obrigatório |

---

# 23. PRÓXIMOS PASSOS OFICIAIS

## Agora

1. revisar/aceitar `IMP-002-DESIGN` sem ampliar o slice;
2. implementar `IMP-002 — Patient Search / List` conforme o documento normativo;
3. preservar os gates de IMP-001 e não iniciar frontend, guardian/payer ou busca fuzzy.

## Em seguida

4. fechar os detalhes deferred de IAM/Audit necessários à ativação externa/produção;
5. medir query plans/cardinalidade antes de promover índices candidatos ou projection;
6. desenhar guardian/payer diferente e frontend em slices próprias quando estiverem READY.

---

# 24. ÚLTIMO HANDOFF

## HANDOFF — 2026-09-19 — IMP-002 PATIENT SEARCH / LIST DESIGN

### Objetivo da sessão
Definir exclusivamente o próximo vertical slice administrativo de busca/listagem de pacientes, sem implementar código.

### Status atual
IMP-002-DESIGN — `DONE / PASS`. IMP-002 — `READY`. Próxima tarefa: `IMP-002 — PATIENT SEARCH / LIST`.

### Concluído
- `GET /api/v1/patients` definido com `search`, Unit, status, `page/pageSize` e sort de nome;
- nome case-insensitive/accent-sensitive contains; CPF e telefone completos por exact match normalizado;
- response mínimo com CPF/telefone mascarados, sem birth date, e `Cache-Control: no-store`;
- algoritmo em duas fases: Patients materializa candidates scoped; People filtra/ordena/pagina/count; Organization resolve Units da página;
- paginação correta e `totalCount` exato sem join, projection ou persistence cross-context;
- autorização por permissions + `UNIT_SCOPE`, com Physiotherapist/Developer fora da listagem administrativa;
- persistence `NO MIGRATION`; índices existentes e candidatos classificados;
- planos de teste, arquivos, riscos, DoD e readiness fechados.

### Arquivos alterados
- criado `docs/implementation/IMP_002_PATIENT_SEARCH_LIST_DESIGN.md`;
- atualizado `PROJECT_OS.md`.

### Decisões tomadas
- Patients Application owns a rota e a composição;
- People recebe somente personIds já elegíveis e owns filtro civil, ordenação por nome, paginação e count;
- `page=1`, `pageSize=25`, máximo 100, sort `name|-name`, `totalCount` obrigatório;
- `startDate` filter/sort e filtros dedicados por campo ficam deferred/out of scope;
- nenhuma projection, read DB, Reports, Redis, Elasticsearch ou índice especulativo.

### Migrations
N/A — design documental. O futuro IMP-002 está definido como `NO MIGRATION`.

### Testes executados
- inspeção das fontes canônicas e da implementação real de IMP-001;
- validação documental de ownership, autorização, composição, paginação, PII, persistence e testes;
- `git diff --check`, status, stat e branch registrados no fechamento.

### Blockers
Nenhum blocker para implementar o slice backend/testes aprovado. IAM/Audit de produção, minors, payer diferente e retenção de receipts continuam gates externos ao slice.

### Riscos
- coleção de personIds candidatos e scans de nome/telefone precisam de medição com cardinalidade real;
- offset pagination pode mudar entre requests concorrentes, sem invalidar a consistência interna de cada request;
- query-string PII exige redaction/suppression no access logging.

### Próximas 3 ações
1. implementar somente `IMP-002 — PATIENT SEARCH / LIST`;
2. provar composição/paginação e isolamento em PostgreSQL/API/Architecture tests;
3. não iniciar frontend, projection, índice especulativo ou relacionamento de paciente.

### Instrução para a próxima IA
Comece por `docs/implementation/IMP_002_PATIENT_SEARCH_LIST_DESIGN.md`. Preserve Patients como owner, faça People paginar somente depois de receber todo o conjunto elegível scoped e não introduza join cross-schema, projection ou migration.

## HANDOFF — 2026-09-18 — IMP-001 REGISTER PATIENT DONE

### Objetivo da sessão
Implementar exclusivamente o vertical slice backend aprovado para cadastro e consulta administrativa de paciente adulto, self-payer.

### Status atual
IMP-000 — `DONE / PASS` após validação PostgreSQL. IMP-001 — `DONE / PASS` somente para adulto + `SELF` + backend + POST/GET administrativo. Isso não declara People ou Patients completos.

### Concluído
- Person, ContactPoint PHONE e PatientProfile ACTIVE com UUIDv7;
- PeopleDbContext e PatientsDbContext isolados, histories e migrations independentes;
- contracts públicos mínimos People create/read e Organization read;
- POST/GET, Problem Details, Location, no-store e replay idempotente;
- authorization por permission, conta/deny e UNIT_SCOPE, com test auth somente em ApiTests;
- receipts distintos, hash canônico, Unit revalidation e recovery F1..F6;
- testes Unit, Application/Integration PostgreSQL, API e Architecture.

### Migrations
- `Organization_InitialUnitBaseline`;
- `People_InitialPatientRegistrationSlice`;
- `Patients_InitialPatientRegistrationSlice`.

### Testes executados
- baseline prerequisite: PASS 24/24;
- restore/build: PASS, zero warnings/errors;
- UnitTests 19/19, IntegrationTests PostgreSQL 23/23, ApiTests 9/9 e ArchitectureTests 11/11: PASS (62/62);
- migrations aplicadas em PostgreSQL descartável; containers descartados;
- health/startup sem migration automática e git checks: PASS.

### Blockers e gates
- minors exigem GuardianLink e permanecem gated;
- payer diferente exige ResponsiblePayerLink e permanece gated;
- ativação externa/produção permanece gated por IAM concreto, Audit durável e provisioning real;
- retenção definitiva dos receipts permanece decisão operacional.

### Riscos
- Person legítima pode permanecer sem profile após falha/Unit inativada; retry da mesma key recupera;
- lease de idempotência é fixo em 30 segundos;
- contratos People/Patients devem permanecer purpose-specific nas próximas slices.

### Próximas 3 ações
1. revisar/aceitar IMP-001 sem iniciar outro slice;
2. escolher o menor próximo slice, preferencialmente Patient Search/List;
3. produzir DoR/design separado antes de implementar relacionamentos, guardian ou frontend.

### Instrução para a próxima IA
Não interprete IMP-001 DONE como People/Patients completos. Preserve minors, payer diferente, durable Audit e produção IAM como gates. Comece pelo DoR do menor próximo slice, não por um módulo inteiro.

---

## HANDOFF — 2026-09-16 — IMP-000 BLOCKED BY POSTGRESQL TEST ENVIRONMENT

### Objetivo da sessão
Implementar exclusivamente o baseline Organization/Unit e validá-lo em PostgreSQL real.

### Status atual
IMP-000 — `BLOCKED`. A implementação compila sem warnings e UnitTests/ArchitectureTests passam, mas o ambiente não possui Docker ou endpoint compatível; a migration não pôde ser aplicada em PostgreSQL real. IMP-001 permanece `BLOCKED_BY_PREREQUISITE`.

### Concluído
- Clinic e Unit com UUIDv7 preferencial e lifecycle ACTIVE/INACTIVE;
- `OrganizationDbContext` exclusivo do schema `organization`;
- mappings, FK interna restritiva, checks e migration `Organization_InitialUnitBaseline`;
- public contract `IValidateUnitForPatientRegistration` e implementação owner;
- registration no Registry/Host sem migration automática;
- UnitTests, IntegrationTests Testcontainers e ArchitectureTests;
- documentação `IMP_000_ORGANIZATION_UNIT_BASELINE.md`.

### Migrations
- criada a primeira migration do owner Organization;
- migration history configurada como `organization.__organization_migrations_history`;
- aplicação real bloqueada porque `unix:///var/run/docker.sock` está indisponível.

### Testes executados
- restore: PASS;
- build: PASS, zero warnings/errors;
- UnitTests: PASS, 9/9;
- ArchitectureTests: PASS, 5/5;
- IntegrationTests: BLOCKED pelo ambiente; 8 testes Organization não iniciaram sem Docker;
- nenhuma substituição InMemory/SQLite foi feita.

### Blockers
- instalar/iniciar Docker Desktop, Colima, Podman compatível ou fornecer outro endpoint Docker aceito pelo Testcontainers;
- executar `dotnet test Fisiofit.slnx` e obter PASS antes de promover o backlog.

### Riscos
- a migration foi gerada e revisada, mas ainda não há evidência de aplicação em PostgreSQL real;
- marcar IMP-000 como DONE antes dessa prova violaria os critérios de pass.

### Próximas 3 ações
1. disponibilizar runtime Docker;
2. executar `dotnet test Fisiofit.slnx` e revisar a aplicação da migration;
3. se tudo passar, marcar IMP-000 DONE e promover IMP-001 para READY_WITH_GATED_BRANCHES sem iniciá-lo.

### Instrução para a próxima IA
Não implemente IMP-001. Primeiro rode os IntegrationTests Organization com Docker/PostgreSQL real; só atualize os status se toda a suite e `git diff --check` passarem.

---

## HANDOFF — 2026-09-16 — IMP-001-DESIGN FINAL CORRECTIVE REVIEW

### Objetivo da sessão
Revisar exclusivamente a lacuna de Organization/Unit, ownership de idempotência, failure windows, teste de autorização, audit e readiness do design, sem implementar código.

### Status atual
IMP-001-DESIGN — DONE / PASS. IMP-001 — **BLOCKED_BY_PREREQUISITE**. Próxima tarefa: `IMP-000 — ORGANIZATION / UNIT BASELINE`.

### Decisões tomadas
- a validação de `primaryUnitId` não pode funcionar legitimamente no skeleton atual: não existem Unit persistida, `OrganizationDbContext` nem handler owner;
- foi escolhida a alternativa B para preservar o tamanho do slice: IMP-000 precede IMP-001;
- IMP-000 materializa somente `organization.clinic` e `organization.unit` (Clinic é indispensável por `INV-ORG-001`), `OrganizationDbContext`, migration owner, lifecycle ACTIVE/INACTIVE, public contract de validação e testes PostgreSQL;
- IMP-000 não inclui UI, CRUD completo, Room, Holiday/Calendar ou seed de produção; fixture persistida é permitida somente nos testes do owner;
- `patients.command_receipt` é o único owner da `Idempotency-Key` HTTP e do workflow completo;
- `people.command_receipt` deduplica somente `CreatePersonForPatientRegistration` por operation key derivada `{workflowId}/person-step/v1` e hash do subrequest People;
- falhas antes/depois dos commits People/Patients, resposta perdida, concorrência da mesma key e corrida de CPF possuem recuperação determinística sem transação cross-context;
- policies e application boundary são reais; principal/authentication handler fake existe somente em ApiTests/IntegrationTests e nunca no Host normal;
- business audit não é logging operacional. Sem superfície Audit real, ativação externa/produção continua gated e nenhuma implementação improvisada entra no slice.

### Persistence e migrations
- IMP-000: `organization.clinic`, `organization.unit`, migration `Organization_InitialUnitBaseline` e history Organization;
- IMP-001 futuro: `people.person`, `people.contact_point`, `people.command_receipt`, `patients.patient_profile`, `patients.command_receipt`;
- migrations People/Patients permanecem independentes e sem FK cross-schema.

### Blockers e gates
- blocker técnico único para iniciar IMP-001: IMP-000 ainda não executado;
- minors/guardian e payer diferente permanecem branches gated;
- ativação externa/produção permanece gated por IAM concreto, Audit e Unit real provisionada pelo owner.

### Testes executados
- releitura das fontes canônicas solicitadas e inspeção do design/PROJECT_OS;
- revisão documental dos seis failure windows, receipts e boundaries;
- `git diff --check` e `git status` serão registrados no fechamento desta execução.

### Próximas 3 ações
1. `IMP-000 — ORGANIZATION / UNIT BASELINE`;
2. validar baseline em PostgreSQL e fechar seu DoD, sem iniciar Patient registration;
3. somente então promover `IMP-001` para implementação dos branches aprovados.

### Instrução para a próxima IA
Não inicie IMP-001. Implemente primeiro somente o baseline owner Organization descrito no design; não use Unit hardcoded/config/fake, não crie CRUD/UI e não mova a validação para Patients.

---

## HANDOFF — 2026-09-16 — IMP-001-DESIGN

> Histórico anterior à revisão corretiva final; o handoff imediatamente acima o substitui para readiness e próxima tarefa.

### Objetivo da sessão
Definir, sem código, o primeiro vertical slice funcional `REGISTER PATIENT` e provar que People e Patients permanecem isolados dentro do assembly Registry.

### Status atual
IMP-001-DESIGN — DONE / PASS. IMP-001 — READY_WITH_GATED_BRANCHES para cadastro adulto/self-payer e GET administrativo backend-only.

### Concluído
- POST `/api/v1/patients` e GET `/api/v1/patients/{patientId}` especificados;
- Patients Application escolhida como orchestration owner; Host permanece sem regra;
- People cria Person/ContactPoint em transaction própria e Patients cria PatientProfile em outra;
- estratégia A escolhida: falha Patients preserva Person legítima; mesma idempotency key retoma deterministicamente;
- `Idempotency-Key` obrigatório com receipt/hash context-local em People e Patients;
- CPF, telefone, birth date, Unit, authorization, read composition e Problem Details definidos;
- minors/guardian e payer diferente classificados como GATED; frontend como DEFERRED;
- persistence limitada a `people.person`, `people.contact_point`, receipts locais e `patients.patient_profile`;
- duas migrations independentes e plano de testes com PostgreSQL/Testcontainers definidos.

### Arquivos alterados
- criado `docs/implementation/IMP_001_REGISTER_PATIENT_DESIGN.md`;
- atualizado `PROJECT_OS.md`.

### Decisões tomadas
- rota pertence logicamente a Patients; `RegisterPatient` é facade de Application, não novo bounded context;
- `CreatePerson` é public contract mínimo de People e não expõe entity;
- GET compõe Patients + People + Organization por contratos síncronos, sem join cross-schema;
- persistência cross-context atômica e saga compensatória foram rejeitadas;
- Person já criada não é apagada; retry reutiliza `personId` pelo receipt People;
- produção externa continua gated pelo IAM/Audit concreto, sem bloquear os testes e o subfluxo backend aprovado.

### Migrations
N/A nesta tarefa documental. Planejadas: `People_InitialPatientRegistrationSlice` e `Patients_InitialPatientRegistrationSlice`, sem FK cross-schema.

### Testes executados
- inspeção integral das fontes obrigatórias e da estrutura real do skeleton;
- validação documental de ownership, transactions, idempotência, erro, persistence e test plan;
- `git diff --check` e inspeções finais registradas no encerramento desta execução.

### Blockers restantes
- nenhum para o subfluxo adulto/self-payer em desenvolvimento/testes com principal/scopes explícitos;
- cadastro de menor bloqueado por GuardianLink e requisitos completos de representação/consentimento;
- payer diferente bloqueado por fluxo de outra Person/ResponsiblePayerLink;
- ativação externa/produção bloqueada por IAM/session/revocation/Unit grants e Audit concretos.

### Riscos
- Person legítima sem PatientProfile após falha parcial;
- duplicate/race de CPF ou profile;
- Unit mudar entre validações;
- bypass acidental entre DbContexts no mesmo assembly;
- contratos públicos ou persistence crescerem além do slice.

### Próximas 3 ações
1. `IMP-001 — REGISTER PATIENT VERTICAL SLICE`;
2. implementar somente o branch adulto/self-payer e GET, com PostgreSQL/Testcontainers e testes negativos;
3. não iniciar guardian, payer diferente, frontend ou turma nesta execução futura.

### Instrução para a próxima IA
Comece por `docs/implementation/IMP_001_REGISTER_PATIENT_DESIGN.md`. Preserve duas transactions/DbContexts/migrations, exija Idempotency-Key e não amplie os branches GATED.

---

## HANDOFF — 2026-09-16 — BOOT-001

### Objetivo da sessão
Criar somente o solution skeleton físico, compilável e testável do Fisiofit CRM 2.0, sem implementação funcional.

### Status atual
BOOT-001 — DONE / PASS. IMP-001 ainda não foi iniciado nem promovido para READY.

### Concluído
- solution `Fisiofit.slnx`, target `net10.0` e configuração comum;
- Host ASP.NET Core como composition root, route group vazio `/api/v1` e health check `/health`;
- BuildingBlocks minimalista e ModuleContracts particionado pelos 16 owners;
- exatamente 10 module assemblies, com layers e boundaries internos conforme ARC-003;
- registration e endpoint mapping surfaces dos 10 módulos, todas compostas pelo Host;
- frontend React + TypeScript + Vite feature-first, sem tela de negócio;
- quatro projetos de teste e três verificações executáveis de dependências;
- documentação do bootstrap e README de desenvolvimento.

### Arquivos alterados
- `PROJECT_OS.md`, `README.md` e `.gitignore` revisado sem necessidade de mudança;
- novos arquivos sob `src/`, `tests/` e `docs/implementation/`;
- `Directory.Build.props` e `Fisiofit.slnx`.

### Decisões tomadas
- .NET 10 LTS foi usado por ser o único SDK suportado disponível no ambiente;
- Node 26/npm 11 foram usados como toolchain disponível e compatível, sem virar regra permanente;
- xUnit/Test SDK foram as únicas dependências backend externas, sem biblioteca de architecture testing;
- projetos Unit/Integration/API possuem somente um smoke test estrutural cada, sem comportamento artificial.

### Migrations
- N/A — nenhum DbContext, schema, SQL ou migration foi criado.

### Testes executados
- `dotnet restore Fisiofit.slnx` — PASS;
- `dotnet build Fisiofit.slnx --no-restore --disable-build-servers` — PASS, zero warnings/errors;
- `dotnet test Fisiofit.slnx --no-build --no-restore --disable-build-servers` — PASS, 3 architecture tests e 3 smoke tests estruturais;
- Host iniciado e `GET /health` — PASS, HTTP 200; processo encerrado;
- `npm install` — PASS, 0 vulnerabilities reportadas;
- `npm run build`, `npm run lint`, `npm run typecheck` — PASS;
- Vite iniciado e smoke HTTP — PASS, HTTP 200; processo encerrado;
- `git diff --check` e inspeções finais — PASS.

### Blockers restantes
- nenhum para BOOT-001;
- os blockers/deferred canônicos continuam válidos para as implementações correspondentes;
- IMP-001 precisa ser delimitado e passar por DoR antes de código funcional.

### Riscos
- isolamento entre contexts agrupados exigirá regras arquiteturais adicionais quando houver código real;
- projetos de teste não arquiteturais possuem apenas smoke tests estruturais por design;
- não há persistence, auth, Docker, CI/CD ou observability completa nesta baseline.

### Próximas 3 ações
1. definir `IMP-001` como o menor recorte vertical útil derivado de VS-01;
2. fechar IAM/persistência/API/UI e critérios de teste estritamente necessários a esse recorte;
3. implementar somente após `IMP-001` estar READY, preservando ARC-003 e API-001.

### Instrução para a próxima IA
Não expanda o skeleton nem implemente o catálogo inteiro. Primeiro delimite a menor slice de VS-01, registre seus gates no Project OS e só programe quando ela estiver READY.

---

## HANDOFF — 2026-09-16 — API-001

### Objetivo da sessão
Transformar os modelos, states, authorization, events, arquitetura física e DB-001 em contratos canônicos de aplicação, ModuleContracts e HTTP, sem implementar código.

### Status atual
API-001 — DONE / PASS. BOOT-001 — READY_WITH_DEFERRED_DETAILS e próxima tarefa oficial. Nenhum skeleton, endpoint, DTO, banco ou OpenAPI foi criado.

### Concluído
- princípios de commands/queries/read models, transactions e ownership dos 16 contexts;
- 137 commands e 77 queries catalogados com actor, autorização, inputs/results, idempotência e concorrência;
- catálogo mestre de endpoints `/api/v1` com owner, permission, resposta e sensibilidade;
- RFC 9457, status codes, validação por boundary e códigos externos estáveis;
- pagination offset inicial, filtros/sorts por whitelist e ETag seletivo;
- catálogo de ModuleContracts e contratos síncronos mínimos sem entities/shared services;
- mapeamento command→event limitado ao catálogo canônico;
- Clinical segregado, Billing/Finance separados e Documents/Reports com autorização herdada;
- fluxos cross-context, exemplos críticos e oito diagramas Mermaid;
- validação 37/37 processos, 75/75 regras, AUTH-001, DB-001, STATE-001 e DOMAIN_EVENTS;
- ADR-007 aceita para versionamento por path, Problem Details e paginação inicial;
- revisão final separou `READY`/`GATED`/`DEFERRED`, validou 137 command IDs únicos + 77 query IDs e fixou o limite estrito de BOOT-001.

### Arquivos criados

- `docs/api/API_001_APPLICATION_API_CONTRACTS.md`;
- `docs/adr/ADR-007-HTTP-API-CONTRACT-BASELINE.md`.

### Arquivos alterados

- `PROJECT_OS.md`.

### Decisões tomadas
- rotas externas iniciam em `/api/v1`;
- erros usam RFC 9457 com `code`, `traceId` e `errors`;
- listagens comuns iniciam com `page/pageSize/totalCount`; cursor exige evidência por endpoint;
- ETag/If-Match é seletivo nos hotspots, não universal;
- Idempotency-Key é obrigatório nos efeitos críticos/retryable, não em toda operação;
- `DEFERRED` resulta em deny/fail explícito, nunca fallback permissivo;
- o catálogo completo é cobertura contratual, não backlog de implementação imediata;
- BOOT-001 pode criar o skeleton e enforcement points, mas não implementar commands, queries, endpoints, regras, banco/migrations ou gates.

### Migrations

- N/A — tarefa documental; nenhuma migration, SQL ou DbContext criado.

### Testes executados

- leitura e cruzamento das fontes obrigatórias e do handoff DB-001;
- revisão de ownership, permissions/policies, states, R2, idempotência e concorrência;
- validação de 137 command IDs/names únicos, 77 query IDs/names únicos, 37 processos, 75 regras, oito diagramas e critérios de PASS;
- `git diff --check`, revisão de diff/stat/status e confirmação da branch ao encerrar.

### Blockers restantes

- nenhum para BOOT-001;
- alçadas/step-up, precedência/reallocation, vencidos no cancelamento, MakeupCredit na pausa, RT/export/retention clínica, IAM concreto, upload/webhooks/rate limits e privacy/legal hold bloqueiam somente as implementações/go-live correspondentes.

### Riscos

- catálogo amplo virar CRUD ou implementation scope prematuro;
- ModuleContracts virar shared domain model;
- workflows eventuais serem interpretados como transação distribuída;
- vazamento Clinical/Financial por Reports, Documents, Audit ou listagens;
- deferred authority ser implementada permissivamente;
- drift entre catálogo, OpenAPI futuro e endpoints.

### Próximas 3 ações

1. `BOOT-001` — Solution Skeleton;
2. fechar detalhes deferred exigidos pela primeira vertical slice antes da implementação correspondente;
3. iniciar `IMP-001` ou a sequência de implementação definida pelo Project OS após o bootstrap.

### Instrução para a próxima IA
Comece por BOOT-001 usando ARC-003, DB-001 e API-001 como baselines. Crie somente topology, registration/enforcement/test skeletons autorizados; não materialize todos os endpoints, não resolva deferred por inferência e não altere ownership/contratos.

---

# 25. REGRA FINAL

> **O repositório é a memória operacional do projeto.**

Se uma decisão só existe no chat, ela ainda não está incorporada ao projeto.

Se uma IA terminou código mas não atualizou status, testes e handoff, a tarefa não terminou.

Se existem dois backlogs gerais, o projeto está desorganizado.

Se uma feature está `BLOCKED`, a IA deve parar e pedir decisão — não inventá-la.

Se uma feature não está `READY`, ela não entra em desenvolvimento.

Se uma feature não cumpre `DONE`, ela não está concluída.
