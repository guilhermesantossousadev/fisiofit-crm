# Gate M1 Auditoria de Consistência

## Escopo e método

Auditoria realizada em 2026-09-15 sobre o `PROJECT_OS.md`, o Caderno Mestre consolidado e os documentos criados neste Gate. O DOCX foi lido por extração textual local não destrutiva; o original não foi alterado. Como não havia outros arquivos Markdown em `/docs`, não existiam duplicatas documentais ou shells prévios a consolidar.

Regra de resolução: decisões oficiais fornecidas para o Gate M1 prevalecem sobre conteúdos anteriores quando o conflito é inequívoco. Divergências sem resolução oficial permanecem pendências classificadas no `PROJECT_OS.md`.

## Achados

| ID | Severidade | Problema | Arquivos | Recomendação | Status |
|---|---|---|---|---|---|
| AUD-001 | CRITICAL | Caderno 46 permite pausa sem limite, prorroga contrato/créditos; decisão oficial limita a 15 dias, não mantém vaga e não prolonga contrato. | Caderno 46; `PROJECT_OS.md`; `docs/domain/06-plans-enrollment.md` | Usar DEC-011/012 e manter o Caderno como evidência histórica. | CORRIGIDO NA BASELINE |
| AUD-002 | CRITICAL | Caderno 46 suspende matrícula após 15 dias e mistura inadimplência com pausa; decisão oficial usa tolerância de 5 dias e FinancialRestriction independente. | Caderno 46; `docs/domain/07-billing.md` | Modelar FinancialRestriction em Billing; nunca usar PAUSED como sinônimo. | CORRIGIDO NA BASELINE |
| AUD-003 | HIGH | Caderno 46 define vencimento no dia da contratação; decisão oficial restringe a 5/10/15/20/25. | Caderno 46; `docs/domain/07-billing.md` | Aplicar DEC-014 e próximo dia útil. | CORRIGIDO NA BASELINE |
| AUD-004 | HIGH | Fórmula de pró-rata do Caderno usa valor mensal equivalente e aulas do mês completo; decisão oficial fixa aulas restantes × H/A da PlanVersion. | Caderno 46; `docs/domain/06-plans-enrollment.md` | Aplicar DEC-010. | CORRIGIDO NA BASELINE |
| AUD-005 | HIGH | Caderno 46 permite descontos sem limite pela Secretária/Proprietária; Gate apenas aprova desconto de antecipação opcional/configurável e veda perdão arbitrário. | Caderno 46; `docs/domain/07-billing.md` | Não importar “sem limite”; definir alçadas antes da implementação. | ABERTO — BLOCKER BEFORE IMPLEMENTATION |
| AUD-006 | HIGH | Caderno 46 restringe fechar/reabrir à Proprietária; decisão oficial permite Secretária e Proprietária, e Desenvolvedor somente com permissão explícita. | Caderno 46; `docs/domain/08-finance.md` | Aplicar DEC-025 e detalhar matriz de permissão. | CORRIGIDO NA BASELINE |
| AUD-007 | HIGH | Caderno 46 oferece comissão opcional; decisão oficial afirma que comissão não existe e está fora do MVP. | Caderno 46; `docs/domain/08-finance.md` | Remover Commission do escopo do MVP e dos modelos iniciais. | CORRIGIDO NA BASELINE |
| AUD-008 | HIGH | `PROJECT_OS.md` antigo marcava DOM-016/017/018 e decisões de Plans/Billing/Finance como blockers. | `PROJECT_OS.md` seções 9, 10, 11, 23 e 24 | Atualizar status, perguntas e handoff para o Gate M1. | CORRIGIDO |
| AUD-009 | HIGH | Capacidade aparecia como regra de Agenda e também de Pilates, criando ownership ambíguo. | Caderno 9, 13 e 14; `RULES_INDEX.md` | Pilates é owner da capacidade; Scheduling consulta validação por contrato. | CORRIGIDO NA BASELINE |
| AUD-010 | MEDIUM | `Class/Classes`, “Turma”, `FixedSchedule` e `ClassSchedule` aparecem como nomes sobrepostos. | Caderno 4, 6, 13 e 29 | Fixar Class/ClassSchedule para Pilates e ScheduleRule/Appointment para Scheduling; confirmar no Context Map. | ABERTO PARA ARC-001 |
| AUD-011 | MEDIUM | Valores 4, 30 dias, 2/mês, 90 dias e 4h poderiam ser lidos como invariantes fixas. | Caderno 14 | Registrar como parâmetros iniciais configuráveis com vigência. | CORRIGIDO |
| AUD-012 | MEDIUM | Tolerância clínica/operacional de 10 min, prazo clínico de 24h e upload de 10 MB são valores operacionais; o último não foi reconfirmado no Gate. | Caderno 13–15; `BUSINESS_PARAMETERS.md` | Manter configuráveis; marcar 10 MB como VALIDAR OPERAÇÃO. | CORRIGIDO |
| AUD-013 | CRITICAL | `admin=true`, papel de Proprietária ou papel técnico poderiam ser interpretados como autorização clínica/financeira suficiente. | Caderno 3, 15, 31 e 46; `PROJECT_OS.md` | Exigir permission + resource/context; autoridade técnica não é autoridade de negócio. | CORRIGIDO NA BASELINE |
| AUD-014 | HIGH | Finance e Billing eram misturados em “financeiro”, especialmente pagamento, caixa e fechamento. | Caderno 2, 6, 17–18 e 29 | Fixar Billing como obrigações/liquidações de cliente e Finance como contas/movimentos/despesas/closing. | CORRIGIDO NA BASELINE |
| AUD-015 | HIGH | Dados administrativos e Clinical poderiam se misturar via perfil do paciente e acesso da recepção. | Caderno 11 e 15 | PatientProfile não contém prontuário; Clinical tem autorização própria. | CORRIGIDO NA BASELINE |
| AUD-016 | HIGH | Sala e equipamentos poderiam reaparecer como recursos impeditivos por linguagem genérica de agenda. | Caderno 13; documentos de domínio | Fixar sala informativa e equipamento fora de Scheduling. | CORRIGIDO NA BASELINE |
| AUD-017 | HIGH | Alterações diretas poderiam sobrescrever membership, ocorrência, ClinicalEntry, Payment ou Closing. | Caderno 5, 11–18; documentos de domínio | Usar vigência, eventos de correção/reversão e snapshots versionados. | CORRIGIDO NA BASELINE |
| AUD-018 | MEDIUM | O Caderno marca Q-PIL-001, Q-PIL-002, Q-CLI-001/002, Q-CRM-001 e Q-AGD-001 como pendentes apesar de capítulos posteriores os resolverem para modelagem. | Caderno parte F vs capítulos 11–15 | Tratar capítulos aprovados posteriores e decisões do Gate como resolução; manter apenas detalhes de implementação/go-live. | CORRIGIDO NA BASELINE |
| AUD-019 | MEDIUM | “Comprovante” aparece como dado de Expense no Caderno; decisões oficiais dizem que não é obrigatório no MVP, assim como comprovante formal de Payment. | Caderno 18/46; decisões Gate | Campo/anexo pode ser opcional; não criar requisito obrigatório. | CORRIGIDO NA BASELINE |
| AUD-020 | MEDIUM | Caderno 46 cancela também vencidas não pagas no cancelamento; decisão oficial afirma apenas que futuras cobranças deixam de ser devidas. | Caderno 46; `docs/domain/06-plans-enrollment.md` | Não perdoar vencidas automaticamente; definir tratamento em negociação/ajuste antes da implementação. | ABERTO — BLOCKER BEFORE IMPLEMENTATION |
| AUD-021 | MEDIUM | Efeito dos MakeupCredits durante pausa diverge: Caderno sugere congelar/prorrogar; decisão oficial diz que contrato não é prolongado, mas não define o crédito. | Caderno 14.32 e 46; decisão Gate | Definir expiração/disponibilidade de créditos durante/ao fim da pausa sem prolongar o Contract. | ABERTO — BLOCKER BEFORE IMPLEMENTATION |
| AUD-022 | MEDIUM | Saldo digitado poderia emergir do conceito de “fechamento/caixa”. | Caderno 18/46; `docs/domain/08-finance.md` | Saldo sempre derivado; ajustes usam ReconciliationAdjustment auditado. | CORRIGIDO NA BASELINE |
| AUD-023 | LOW | Não existia documentação Markdown canônica em `/docs`; toda regra residia em `PROJECT_OS.md` e DOCX. | Repositório | Criar estrutura canônica sem copiar backlog geral. | CORRIGIDO |
| AUD-024 | LOW | Pastas pedidas para fases futuras não possuíam conteúdo. | `docs/architecture`, `state-machines`, `database`, `api`, `ui-ux`, `security`, `privacy`, `testing`, `migration`, `adr` | Manter pastas sem arquivos shell; preencher quando a fase correspondente iniciar. | ACEITO |

## Duplicidades identificadas

- Regras de preservação de histórico aparecem em vários capítulos do Caderno; foram especializadas por agregado no índice, sem IDs genéricos duplicados.
- `Refund/Reversal` aparecia como conceito combinado; a baseline separa Refund de PaymentReversal.
- “Responsável”, “responsável legal”, “administrativo” e “pagador” foram separados em papéis/vínculos sem duplicar Person.
- “Lead” e “Opportunity” foram normalizados: Lead é visão, Opportunity é entidade.

## Dependências circulares implícitas

- Pilates precisa validar elegibilidade de Enrollment, mas Enrollment não deve possuir ClassMembership. O Context Map deve definir contrato unidirecional e eventos para evitar ciclo.
- Billing aplica FinancialRestriction consumida por Scheduling/Pilates; Enrollment não deve chamar Billing para derivar seu próprio estado.
- Finance recebe eventos/referências de Billing; Billing referencia FinancialAccount para conta de recebimento por contrato público, sem escrita cruzada. ARC-001/002 deve explicitar a direção.

## Resultado

Não há conflito crítico conhecido sem registro. Todos os conflitos resolvidos inequivocamente pelas decisões oficiais foram corrigidos na baseline. Restam quatro achados abertos: a fronteira terminológica Class/Schedule será resolvida em ARC-001, e três temas estão classificados antes da implementação; nenhum bloqueia Context Map/modelagem conceitual.
