# Clinical

## Status

DOM-015 — DONE — APROVADO PARA MODELAGEM CONCEITUAL. Validações regulatórias permanecem como blocker de go-live, não da modelagem.

## Objetivo

Preservar registros assistenciais segregados, com autoria, finalização, correção não destrutiva e acesso por necessidade clínica.

## Responsabilidades

CareEpisode, Assessment, ClinicalEntry, templates clínicos, Rectification/Addendum, documentos clínicos, exportação e acesso excepcional.

## Fora de escopo

Cadastro administrativo, agenda, presença, contrato, billing, propriedade da clínica como autorização e observações administrativas da recepção.

## Conceitos

CareEpisode agrupa período/objetivo assistencial; Assessment é avaliação estruturada; ClinicalEntry é evolução longitudinal; Rectification/Addendum corrige ou complementa sem substituir o original.

## Entidades candidatas

CareEpisode, Assessment, ClinicalTemplate/Version, ClinicalEntry, Rectification/Addendum e ClinicalDocumentLink.

## Relacionamentos conhecidos

Registros pertencem a Patient/CareEpisode e preservam Professional autora; podem referenciar Appointment/ClassOccurrence; documentos usam storage privado.

## Regras de negócio

- Clinical é separado do administrativo, comercial e financeiro.
- ClinicalEntry pode ser DRAFT ou FINALIZED; FINALIZED não é sobrescrito nem volta a DRAFT.
- Correção posterior usa Rectification/Addendum e preserva original, autoria e timestamps.
- Secretária não acessa prontuário integral por padrão; Proprietária não recebe acesso apenas por propriedade.
- Acesso depende de papel, recurso e contexto; break-glass é excepcional, justificado, auditado e revisável.
- Anexo clínico usa storage privado; exportação é sensível e auditada.
- Desligamento remove acesso, não autoria.

## Estados conhecidos

ClinicalEntry e Assessment: DRAFT → FINALIZED, com retificações/adendos relacionados. CareEpisode: OPEN ↔ PAUSED → CLOSED.

## Processos

PROC-CLI-001 Abrir episódio; PROC-CLI-002 Realizar avaliação; PROC-CLI-003 Registrar evolução; PROC-CLI-004 Finalizar evolução; PROC-CLI-005 Retificar registro.

## Eventos conhecidos

CareEpisodeOpened/Closed, AssessmentFinalized, ClinicalEntryDrafted/Finalized, ClinicalEntryRectified, BreakGlassUsed e ClinicalRecordExported.

## Permissões conhecidas

Fisioterapeuta acessa pacientes necessários ao atendimento e seus próprios rascunhos; RT/usuário clínico autorizado pode exercer alçadas adicionais; Secretária e Desenvolvedor não têm acesso clínico padrão; Proprietária precisa de permissão clínica explícita.

## Dependências

Patients e Staff fornecem identidades; Scheduling/Pilates fornecem contexto assistencial; Documents fornece storage; Identity/Privacy/Audit aplicam autorização e rastreabilidade.

## Parâmetros configuráveis

Prazo operacional inicial de 24h para finalização sem justificativa e limite inicial de upload de 10 MB; o segundo está marcado VALIDAR OPERAÇÃO.

## Questões não bloqueantes

- Antes do go-live: retenção jurídica/regulatória, campos formalmente obrigatórios, requisitos de assinatura, exportação e representação de menores.
- Antes da implementação clínica: fechar política de MFA/step-up e matriz detalhada de permissões.

## Referências

- Caderno Mestre, capítulo 15.
- `PROJECT_OS.md`, princípios e OPEN QUESTIONS.
- `docs/domain/BUSINESS_PARAMETERS.md`.

