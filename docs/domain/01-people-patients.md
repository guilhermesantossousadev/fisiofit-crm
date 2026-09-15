# People e Patients

## Status

DOM-011 — DONE — APROVADO PARA MODELAGEM CONCEITUAL.

## Objetivo

Manter uma identidade única para cada pessoa e representar separadamente seus papéis de paciente, profissional, usuário, responsável e pagador.

## Responsabilidades

- identidade civil e contatos estruturados;
- PatientProfile administrativo sem conteúdo clínico;
- relações entre pessoas, responsáveis e pagadores;
- deduplicação e merge auditado;
- inativação preservando histórico.

## Fora de escopo

- autenticação e credenciais, pertencentes a Identity & Access;
- prontuário, pertencente a Clinical;
- contrato, recebíveis e pagamentos;
- vínculos de turma.

## Conceitos

Person, Patient, PatientProfile, ProfessionalProfile, UserAccount, GuardianLink, vínculo administrativo, ResponsiblePayerLink, ContactPoint, PersonMerge e MergeManifest.

## Entidades candidatas

Person, ContactPoint, Address, PersonRelationship, PatientProfile, GuardianLink, ResponsiblePayerLink, EmergencyContact, PersonMerge e MergeManifest.

## Relacionamentos conhecidos

- uma Person pode acumular vários papéis;
- PatientProfile, ProfessionalProfile e UserAccount se vinculam à Person sem duplicá-la;
- paciente menor e responsável são Persons diferentes;
- pagador pode ser o próprio paciente ou outra Person;
- uma Person/PatientProfile pode se relacionar com as duas unidades.

## Regras de negócio

- Person é a identidade central e pode existir sem CPF.
- CPF, quando informado, é único; conflito bloqueia a alteração e direciona à revisão/merge.
- PatientProfile não replica identidade civil nem conteúdo clínico.
- Uma pessoa não é recriada ao mudar de unidade ou papel.
- Merge exige revisão humana, preserva referências, histórico e auditoria; não é operação manual normal de TI.
- Pacientes não sofrem hard delete operacional; vínculos são encerrados ou inativados.
- Uma Person pode ter apenas uma conta interna ativa inicialmente, com múltiplos papéis/permissões.

## Estados conhecidos

- PatientProfile: ativo/inativo, sujeito a refinamento na máquina de estados.
- PersonMerge: proposta/revisão/conclusão/reversão devem ser detalhadas depois; o histórico é obrigatório.

## Processos

- PROC-PPL-001 Criar pessoa.
- PROC-PAC-001 Cadastrar paciente.
- Deduplicar, revisar e realizar merge.
- Vincular responsável/pagador.
- Inativar ou reativar paciente.

## Eventos conhecidos

PersonCreated, PatientProfileActivated, ResponsibleLinked, PayerChanged, PersonMergeCompleted e PatientDeactivated. Nomes são candidatos e serão confirmados na modelagem de eventos.

## Permissões conhecidas

- Secretária e Proprietária operam cadastro administrativo conforme alçada.
- Fisioterapeuta pode sinalizar duplicidade, não concluir merge.
- Merge sensível requer Proprietária.
- Desenvolvedor não ganha autoridade de cadastro/merge por acesso técnico.

## Dependências

Organization para unidade principal; Identity para UserAccount; Clinical, Plans, Billing e Pilates referenciam a identidade por contratos públicos.

## Parâmetros configuráveis

- janela de reversão direta de merge de 7 dias consta como política inicial no Caderno; deve permanecer configurável e ser validada na modelagem do processo.

## Questões não bloqueantes

- validade jurídica detalhada das autorizações de responsáveis por menores antes do go-live;
- política completa de retenção/anonymização;
- critérios operacionais exatos para reversão de merge.

## Referências

- `PROJECT_OS.md`, seções 5, 8 e OPEN QUESTIONS.
- Caderno Mestre, capítulos 4, 6 e 11.
- `docs/business-rules/RULES_INDEX.md`.

