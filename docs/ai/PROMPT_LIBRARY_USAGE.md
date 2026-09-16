# Prompt Library Usage

## O que é

`.prompts` expõe no repositório uma biblioteca local compartilhada de prompts reutilizáveis. Ela fornece métodos de descoberta, auditoria, planejamento, implementação, validação e produção sem se tornar fonte de verdade do Fisiofit.

## Onde está

No computador atual:

```text
.prompts -> /Users/guilhermesantos/Prompts
```

O link é local, está no `.gitignore` e não é versionado. A biblioteca externa também não deve ser modificada como parte de tarefas do Fisiofit.

## Fonte do projeto

O contexto e as decisões do produto vêm de `PROJECT_OS.md`, de `docs/ai/FISIOFIT_AI_PROFILE.md` e dos documentos canônicos em `docs/`. A biblioteca `.prompts` define somente **como** conduzir o trabalho; ela não decide **o que** o Fisiofit deve fazer.

Se uma orientação genérica conflitar com o projeto, prevalece a hierarquia descrita no perfil de IA. Em especial, o perfil local do Fisiofit fica neste repositório, não em `08-projetos` da biblioteca global.

## Fluxo recomendado

1. descobrir a tarefa autorizada em `PROJECT_OS.md`;
2. ler `docs/ai/FISIOFIT_AI_PROFILE.md`;
3. ler o último handoff e os documentos canônicos da tarefa;
4. encontrar o módulo ou workflow apropriado em `.prompts`;
5. ler o workflow, seu manifesto e os módulos referenciados;
6. montar um prompt contextualizado, se necessário;
7. executar apenas o escopo autorizado;
8. revisar a saída contra as fontes do Fisiofit;
9. atualizar os artefatos e o handoff exigidos pela tarefa;
10. não avançar automaticamente para a tarefa seguinte.

## Uso da ferramenta

A ferramenta real encontrada é `.prompts/tools/promptkit.py`. Neste ambiente, use `python3`:

```bash
python3 .prompts/tools/promptkit.py --help
python3 .prompts/tools/promptkit.py list
python3 .prompts/tools/promptkit.py build <workflow>
```

`list` apenas lista os workflows. `build` concatena os módulos do manifesto e imprime o prompt no terminal. As opções verificadas de `build` são:

```bash
python3 .prompts/tools/promptkit.py build <workflow> --profile <arquivo.md>
python3 .prompts/tools/promptkit.py build <workflow> --output <arquivo.md>
```

`--profile` aceita caminho relativo à raiz da biblioteca ou absoluto. Para o Fisiofit, prefira fornecer `docs/ai/FISIOFIT_AI_PROFILE.md` por caminho absoluto somente no comando local, ou usar os arquivos diretamente sem composição. Não grave saídas geradas no repositório sem uma necessidade explícita e não edite o diretório externo nesta integração.

Os workflows retornados por `list` são:

- `auditar-feature-sem-editar`;
- `corrigir-fluxos-regras`;
- `projeto-existente-completo`;
- `projeto-novo`;
- `redesign-uiux`;
- `refatoracao-arquitetural`.

Também existem scripts auxiliares reais para criar o symlink em um projeto:

- `.prompts/tools/usar-em-projeto.sh` para macOS/Linux;
- `.prompts/tools/usar-em-projeto.ps1` para PowerShell.

Eles devem ser usados apenas quando `.prompts` ainda não existir.

## Biblioteca disponível

| Pasta | Objetivo real | Quando usar no Fisiofit |
|---|---|---|
| `00-guia` | Mapa, ordem principal, instruções de uso e migração da biblioteca. | Para entender a composição e escolher o fluxo correto antes de executar. |
| `01-core` | Contrato de execução, descoberta, fontes, escopo, evidências, regras/estados/permissões, qualidade e conclusão. | Em toda tarefa, subordinado ao `PROJECT_OS.md` e ao perfil do Fisiofit. |
| `02-auditoria` | Auditorias de sistema, feature, regras funcionais, UI/UX e arquitetura, além de contexto documental. | Para diagnóstico autorizado, especialmente quando a tarefa não permite editar o produto. |
| `03-planejamento` | Planejamento de projeto novo, gap AS-IS/TO-BE, UI/UX e refatoração arquitetural. | Para estruturar uma tarefa já autorizada e explicitar decisões ou blockers antes de implementar. |
| `04-implementacao` | Execução de TO-BE funcional, UI/UX e refatoração arquitetural gradual. | Somente quando a tarefa do Fisiofit estiver autorizada e as decisões necessárias estiverem aprovadas. |
| `05-validacao` | Validação funcional/técnica, regressão e revisão final. | Para provar critérios de aceite e verificar regressões antes de concluir uma tarefa. |
| `06-producao` | Preparação de produção e deploy. | Apenas em tarefa específica de release/deploy, com ambiente e autorização definidos. |
| `07-workflows` | Sequências compostas e manifestos JSON consumidos pelo PromptKit. | Quando um workflow existente corresponder ao escopo; leia os módulos referenciados antes de usar. |
| `08-projetos` | Templates e perfis específicos opcionais da biblioteca. | Não usar como autoridade do Fisiofit; o perfil canônico deste projeto está em `docs/ai/`. |
| `tools` | `promptkit.py` e scripts de criação de link. | Para listar/compor workflows ou recriar o symlink local. |
| `build` | Saída gerada e descartável da biblioteca. | Somente para composição temporária; não é fonte canônica. |
| `90-legado` | Prompts originais preservados para conferência e migração. | Somente para consulta histórica, migração ou comparação; nunca como fonte primária de novas tarefas. |

## Escolha de workflow

- `auditar-feature-sem-editar`: diagnóstico profundo de uma página, módulo ou feature sem editar o produto.
- `corrigir-fluxos-regras`: auditoria AS-IS, plano TO-BE, implementação e validação de fluxos/regras.
- `projeto-existente-completo`: ciclo amplo de descoberta até deploy; use apenas quando todo esse escopo estiver autorizado.
- `projeto-novo`: planejamento e execução greenfield; normalmente não se aplica ao CRM existente.
- `redesign-uiux`: auditoria, planejamento, implementação e validação de UI/UX.
- `refatoracao-arquitetural`: auditoria e refatoração arquitetural gradual com validação.

Um workflow amplo não concede autorização para suas fases posteriores. O estado da tarefa em `PROJECT_OS.md` e o prompt do usuário limitam o que pode ser executado.

## Legado

`90-legado` contém material explicitamente histórico. Ele não deve orientar novas tarefas por padrão. Seu uso permitido é consulta histórica, apoio a migração e comparação com os módulos atuais; nenhuma parte deve ser apagada ou alterada por tarefas do Fisiofit.

## Portabilidade

Em outro computador, instale ou localize a biblioteca fora do repositório e recrie o link na raiz do Fisiofit:

```bash
ln -s /CAMINHO/PARA/Prompts .prompts
```

Alternativamente, no macOS/Linux, use o script da própria biblioteca passando o caminho do projeto:

```bash
/CAMINHO/PARA/Prompts/tools/usar-em-projeto.sh /CAMINHO/PARA/FisioCenter
```

No PowerShell:

```powershell
& "C:\CAMINHO\Prompts\tools\usar-em-projeto.ps1" -ProjectPath "C:\CAMINHO\FisioCenter"
```

O caminho `/Users/guilhermesantos/Prompts` é apenas o exemplo desta máquina. `.prompts` é conveniência opcional: build, testes, execução, CI e deploy do CRM não podem depender do link ou da biblioteca.
