# Project Status Dashboard

Dashboard local, estático e somente leitura para visualizar o estado operacional registrado no `PROJECT_OS.md`.

## Fonte oficial

O `PROJECT_OS.md`, na raiz do repositório, continua sendo a única fonte oficial. O dashboard apenas lê e apresenta esse arquivo; não persiste nem edita dados.

## Como executar

Na raiz do projeto, execute:

```bash
python3 -m http.server 8000
```

Abra no navegador:

```text
http://localhost:8000/project-status/
```

Não abra o `index.html` diretamente por `file://`: navegadores podem bloquear o `fetch` usado para carregar `../PROJECT_OS.md`.

Alterações de fase, tarefas, blockers e demais status devem ser feitas somente no `PROJECT_OS.md`. Depois de uma mudança, recarregue a página ou clique em **Atualizar** para reler a fonte oficial.
