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
  status: discovery_and_domain_definition
  architecture_direction: modular_monolith
  frontend: React + TypeScript
  backend: ASP.NET Core + C#
  database: PostgreSQL
  infra: Docker + CI/CD
  automation: n8n somente como orquestrador de borda
  current_priority: fechar regras de negócio e modelagem antes do scaffold definitivo

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

---

# 9. DECISÕES AINDA NÃO FECHADAS

Estas decisões não podem ser inventadas durante implementação.

## 9.1 Plans / Enrollment

- [ ] significado exato de plano mensal;
- [ ] significado exato de trimestral;
- [ ] significado exato de semestral;
- [ ] preço representa mensalidade ou valor total do período;
- [ ] frequência semanal faz parte da versão do plano;
- [ ] pró-rata;
- [ ] início no meio do mês;
- [ ] renovação;
- [ ] mudança de plano;
- [ ] pausa;
- [ ] retomada;
- [ ] cancelamento;
- [ ] efeito da pausa sobre turma;
- [ ] efeito da pausa sobre cobrança;
- [ ] efeito do cancelamento sobre recebíveis futuros;
- [ ] retroatividade.

## 9.2 Billing

- [ ] quando nasce um Receivable;
- [ ] regra do vencimento;
- [ ] dia inexistente no mês;
- [ ] fim de semana/feriado;
- [ ] juros;
- [ ] multa;
- [ ] desconto;
- [ ] pagamento parcial;
- [ ] pagamento antecipado;
- [ ] negociação;
- [ ] inadimplência;
- [ ] efeito de estorno;
- [ ] pagador terceiro.

## 9.3 Finance

- [ ] competência vs caixa;
- [ ] despesas;
- [ ] contas a pagar;
- [ ] caixa físico/digital;
- [ ] abertura de caixa;
- [ ] fechamento;
- [ ] reabertura;
- [ ] conciliação;
- [ ] comissão;
- [ ] necessidade real de comissão no MVP.

## 9.4 Security

- [ ] MFA por papel;
- [ ] step-up;
- [ ] duração de sessão;
- [ ] refresh;
- [ ] política de dispositivos;
- [ ] recuperação de conta.

## 9.5 Privacy / LGPD

- [ ] inventário de categorias de dados;
- [ ] finalidades;
- [ ] compartilhamentos;
- [ ] retenção;
- [ ] legal hold;
- [ ] fluxo de solicitação de titular;
- [ ] resposta a incidente.

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
- [ ] colocar este arquivo na raiz do repositório;
- [ ] garantir que nenhum outro backlog geral seja usado.

### Gate
Project OS na raiz e adotado como ponto de entrada.

---

## Fase 1 — Fechar regras de negócio restantes

### P0
- [ ] capítulo 16 — Plans/Contracts/Enrollment;
- [ ] capítulo 17 — Billing;
- [ ] capítulo 18 — Finance;
- [ ] matriz de permissões;
- [ ] regras de segurança-base.

### P1
- [ ] Communication;
- [ ] Reports;
- [ ] Privacy detalhada;
- [ ] migração.

### Gate
Nenhum blocker de domínio da primeira vertical slice.

---

## Fase 2 — Modelagem conceitual

### People / Patients / Staff / Organization
- [ ] Person
- [ ] ContactPoint
- [ ] Address
- [ ] PersonRelationship
- [ ] PatientProfile
- [ ] GuardianLink
- [ ] ResponsiblePayerLink
- [ ] ProfessionalProfile
- [ ] EmploymentLink
- [ ] Clinic
- [ ] Unit
- [ ] Room

### Scheduling / Pilates
- [ ] FixedSchedule
- [ ] Appointment
- [ ] CalendarException
- [ ] Class
- [ ] ClassSchedule
- [ ] ClassMembership
- [ ] ClassOccurrence
- [ ] Attendance
- [ ] MakeupCredit
- [ ] MakeupReservation

### Clinical
- [ ] CareEpisode
- [ ] Assessment
- [ ] ClinicalTemplate
- [ ] ClinicalEntry
- [ ] Rectification
- [ ] ClinicalDocumentLink

### Plans / Billing / Finance
- [ ] Plan
- [ ] PlanVersion
- [ ] Contract
- [ ] Enrollment
- [ ] Benefit/Entitlement
- [ ] Receivable
- [ ] Payment
- [ ] PaymentAllocation
- [ ] Refund/Reversal
- [ ] Expense
- [ ] CashSession
- [ ] CashTransaction
- [ ] Closing

### Gate
Modelo conceitual aprovado antes de desenhar SQL definitivo.

---

## Fase 3 — Máquinas de estado

Obrigatórias:

- [ ] Opportunity
- [ ] Appointment
- [ ] Enrollment
- [ ] Contract
- [ ] ClassOccurrence
- [ ] MakeupCredit
- [ ] ClinicalEntry
- [ ] Assessment
- [ ] Receivable
- [ ] Payment
- [ ] Task
- [ ] PrivacyRequest

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

---

## Fase 4 — Arquitetura

- [ ] confirmar monólito modular;
- [ ] definir estrutura física de módulos;
- [ ] definir dependências permitidas;
- [ ] definir contratos públicos entre módulos;
- [ ] definir application layer;
- [ ] definir domain layer;
- [ ] definir infrastructure layer;
- [ ] definir Unit of Work;
- [ ] definir transações;
- [ ] definir domain events;
- [ ] definir integration events;
- [ ] definir Outbox;
- [ ] definir worker;
- [ ] definir fronteira do n8n;
- [ ] definir error contract;
- [ ] definir logging;
- [ ] definir correlation ID;
- [ ] definir storage;
- [ ] definir configuração por ambiente.

### Gate
ADR de arquitetura aprovado.

---

## Fase 5 — Segurança e autorização

- [ ] Role model;
- [ ] Permission model;
- [ ] resource-level policies;
- [ ] escopo por unidade;
- [ ] escopo por profissional;
- [ ] escopo por paciente;
- [ ] acesso Clinical;
- [ ] break-glass;
- [ ] MFA/step-up;
- [ ] sessões;
- [ ] revogação;
- [ ] rate limit;
- [ ] proteção de upload;
- [ ] redaction de logs;
- [ ] secrets management.

---

## Fase 6 — Modelo lógico e banco

- [ ] convenção de IDs;
- [ ] PK/FK;
- [ ] uniques;
- [ ] nullability;
- [ ] checks;
- [ ] índices;
- [ ] created_at/updated_at;
- [ ] autoria;
- [ ] optimistic locking;
- [ ] concorrência de capacidade;
- [ ] concorrência financeira;
- [ ] precisão monetária;
- [ ] timezone;
- [ ] delete policy;
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

Somente depois dos gates anteriores da primeira slice.

- [ ] repo definitivo;
- [ ] frontend;
- [ ] backend;
- [ ] PostgreSQL;
- [ ] Docker;
- [ ] migrations;
- [ ] CI;
- [ ] testes;
- [ ] logging;
- [ ] health checks;
- [ ] auth skeleton;
- [ ] module skeleton.

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
| GOV-001 | Adotar PROJECT_OS na raiz do repo | P0 | TODO |
| GOV-002 | Remover/arquivar backlog geral concorrente | P0 | TODO |
| GOV-003 | Criar índice de documentos canônicos | P0 | TODO |
| GOV-004 | Criar Decision Log | P0 | TODO |
| GOV-005 | Criar ADR index | P0 | TODO |
| GOV-006 | Adotar protocolo de handoff | P0 | TODO |

---

## EPIC DOM — Regras de domínio pendentes

| ID | Tarefa | Prioridade | Status |
|---|---|---:|---|
| DOM-016 | Fechar Plans/Contracts/Enrollment | P0 | TODO |
| DOM-017 | Fechar Billing | P0 | TODO |
| DOM-018 | Fechar Finance | P0 | TODO |
| DOM-019 | Fechar Communication | P1 | TODO |
| DOM-020 | Fechar Staff/Operation | P1 | TODO |
| DOM-021 | Fechar Reports/KPIs | P1 | TODO |

---

## EPIC ARC — Arquitetura

| ID | Tarefa | Prioridade | Status |
|---|---|---:|---|
| ARC-001 | Diagrama de contextos | P0 | TODO |
| ARC-002 | Matriz de ownership | P0 | TODO |
| ARC-003 | Matriz de dependências | P0 | TODO |
| ARC-004 | ADR monólito modular | P0 | TODO |
| ARC-005 | Definir application/domain/infrastructure | P0 | TODO |
| ARC-006 | Definir transaction boundaries | P0 | TODO |
| ARC-007 | Definir event strategy | P0 | TODO |
| ARC-008 | Definir Outbox | P1 | TODO |
| ARC-009 | Definir storage strategy | P1 | TODO |

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
| PPL-001 | Modelar Person | P0 | TODO |
| PPL-002 | Modelar ContactPoint | P0 | TODO |
| PPL-003 | Modelar Address | P1 | TODO |
| PPL-004 | Modelar PersonRelationship | P0 | TODO |
| PPL-005 | Modelar PersonMerge | P1 | TODO |
| PPL-006 | Definir deduplicação | P1 | TODO |

---

## EPIC PAC — Patients

| ID | Tarefa | Prioridade | Status |
|---|---|---:|---|
| PAC-001 | Modelar PatientProfile | P0 | TODO |
| PAC-002 | Modelar GuardianLink | P0 | TODO |
| PAC-003 | Modelar ResponsiblePayerLink | P0 | TODO |
| PAC-004 | Modelar EmergencyContact | P1 | TODO |
| PAC-005 | Caso de uso criar paciente | P0 | TODO |

---

## EPIC STF — Staff

| ID | Tarefa | Prioridade | Status |
|---|---|---:|---|
| STF-001 | Modelar ProfessionalProfile | P0 | TODO |
| STF-002 | Modelar EmploymentLink | P0 | TODO |
| STF-003 | Modelar unidade de atuação | P0 | TODO |
| STF-004 | Modelar Availability | P1 | TODO |
| STF-005 | Modelar Leave | P1 | TODO |

---

## EPIC AGD — Scheduling

| ID | Tarefa | Prioridade | Status |
|---|---|---:|---|
| AGD-001 | Modelar grade fixa | P0 | TODO |
| AGD-002 | Modelar Appointment | P0 | TODO |
| AGD-003 | Modelar CalendarException | P1 | TODO |
| AGD-004 | Regra conflito de profissional | P0 | TODO |
| AGD-005 | Regra conflito de paciente | P0 | TODO |
| AGD-006 | Visão por unidade | P0 | TODO |
| AGD-007 | Visão por profissional | P0 | TODO |

---

## EPIC PIL — Pilates

| ID | Tarefa | Prioridade | Status |
|---|---|---:|---|
| PIL-001 | Modelar Class | P0 | TODO |
| PIL-002 | Modelar ClassSchedule | P0 | TODO |
| PIL-003 | Modelar ClassMembership | P0 | TODO |
| PIL-004 | Modelar ClassOccurrence | P0 | TODO |
| PIL-005 | Modelar Attendance | P0 | TODO |
| PIL-006 | Regra de capacidade | P0 | TODO |
| PIL-007 | Transferência de turma | P0 | TODO |
| PIL-008 | Modelar MakeupCredit | P1 | TODO |
| PIL-009 | Modelar MakeupReservation | P1 | TODO |

---

## EPIC CLI — Clinical

| ID | Tarefa | Prioridade | Status |
|---|---|---:|---|
| CLI-001 | Modelar CareEpisode | P0 | TODO |
| CLI-002 | Modelar Assessment | P0 | TODO |
| CLI-003 | Modelar ClinicalEntry | P0 | TODO |
| CLI-004 | Modelar finalização | P0 | TODO |
| CLI-005 | Modelar Rectification/Addendum | P0 | TODO |
| CLI-006 | Definir permissões clínicas | P0 | TODO |
| CLI-007 | Definir anexos | P1 | TODO |
| CLI-008 | Definir exportação | P1 | TODO |
| CLI-009 | Definir break-glass | P1 | TODO |

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
| PLN-001 | Fechar política comercial | P0 | BLOCKED |
| PLN-002 | Modelar Plan | P0 | TODO |
| PLN-003 | Modelar PlanVersion | P0 | TODO |
| PLN-004 | Modelar Contract | P0 | TODO |
| PLN-005 | Modelar Enrollment | P0 | TODO |
| PLN-006 | Modelar pausa/retomada | P0 | BLOCKED |
| PLN-007 | Modelar cancelamento | P0 | BLOCKED |

---

## EPIC BIL — Billing

| ID | Tarefa | Prioridade | Status |
|---|---|---:|---|
| BIL-001 | Fechar política de cobrança | P0 | BLOCKED |
| BIL-002 | Modelar Receivable | P0 | TODO |
| BIL-003 | Modelar Payment | P0 | TODO |
| BIL-004 | Modelar PaymentAllocation | P0 | TODO |
| BIL-005 | Modelar pagamento parcial | P0 | TODO |
| BIL-006 | Modelar reversão/estorno | P0 | TODO |
| BIL-007 | Modelar inadimplência | P1 | TODO |

---

## EPIC FIN — Finance

| ID | Tarefa | Prioridade | Status |
|---|---|---:|---|
| FIN-001 | Modelar Expense | P1 | TODO |
| FIN-002 | Modelar CashSession | P1 | TODO |
| FIN-003 | Modelar CashTransaction | P1 | TODO |
| FIN-004 | Fechar semântica de Closing | P1 | BLOCKED |
| FIN-005 | Validar comissão | P2 | BLOCKED |

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

- [ ] Person modelada;
- [ ] PatientProfile modelado;
- [ ] ProfessionalProfile modelado;
- [ ] Unit modelada;
- [ ] Class modelada;
- [ ] ClassSchedule modelado;
- [ ] ClassMembership modelado;
- [ ] capacidade definida;
- [ ] conflito de paciente definido;
- [ ] conflito de profissional definido;
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

# 21. CHECKLIST PARA COMEÇAR A CODAR

Não iniciar scaffold definitivo até:

- [ ] PROJECT_OS adotado;
- [ ] primeira slice escolhida;
- [ ] arquitetura macro aprovada;
- [ ] boundaries aprovados;
- [ ] matriz de dependências aprovada;
- [ ] estratégia de identidade aprovada;
- [ ] estratégia de autorização-base aprovada;
- [ ] convenção de IDs definida;
- [ ] timezone definido;
- [ ] estratégia monetária definida;
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
| modelar plano errado | crítico | fechar capítulos 16–18 antes da slice financeira |
| excesso de arquitetura | médio | monólito modular |
| código sem handoff | alto | protocolo obrigatório |

---

# 23. PRÓXIMOS PASSOS OFICIAIS

## Agora

1. `GOV-001` — colocar este arquivo na raiz do repositório.
2. `DOM-016` — fechar Planos, Contratos, Matrículas e Benefícios.
3. `DOM-017` — fechar Billing, Pagamentos e Inadimplência.
4. `DOM-018` — fechar Financeiro, Caixa e Fechamento.

## Em seguida

5. `ARC-001` — diagrama de contextos.
6. `ARC-002` — ownership.
7. modelar People/Patients/Staff.
8. modelar Scheduling/Pilates.
9. modelar Clinical.
10. matriz de permissões.
11. máquinas de estado.
12. deixar VS-01 em READY.

---

# 24. ÚLTIMO HANDOFF

## HANDOFF — 2026-09-15

### Objetivo da sessão
Criar um sistema central de continuidade do projeto e impedir fragmentação de contexto/checklists.

### Status atual
Discovery e definição de domínio.

### Concluído
- visão do produto;
- princípios;
- papéis principais;
- decisões iniciais de People;
- decisões iniciais de CRM;
- decisões iniciais de Agenda;
- decisões iniciais de Pilates;
- decisões iniciais de Clinical;
- criação deste Project OS.

### Blockers principais
- Plans/Enrollment;
- Billing;
- Finance;
- segurança final;
- LGPD final.

### Próximas 3 ações
1. fechar capítulo 16;
2. fechar capítulo 17;
3. fechar capítulo 18.

### Instrução para a próxima IA
Não comece scaffold definitivo. Comece fechando `DOM-016`, a menos que o usuário explicitamente mude a prioridade.

---

# 25. REGRA FINAL

> **O repositório é a memória operacional do projeto.**

Se uma decisão só existe no chat, ela ainda não está incorporada ao projeto.

Se uma IA terminou código mas não atualizou status, testes e handoff, a tarefa não terminou.

Se existem dois backlogs gerais, o projeto está desorganizado.

Se uma feature está `BLOCKED`, a IA deve parar e pedir decisão — não inventá-la.

Se uma feature não está `READY`, ela não entra em desenvolvimento.

Se uma feature não cumpre `DONE`, ela não está concluída.
