"use strict";

const PROJECT_OS_URL = "../PROJECT_OS.md";
const KNOWN_STATUSES = ["DONE", "READY", "IN_PROGRESS", "BLOCKED", "REVIEW", "TODO", "PASS", "FAIL"];
const COUNTER_STATUSES = ["DONE", "IN_PROGRESS", "READY", "BLOCKED", "REVIEW", "TODO"];
const FALLBACK_TEXT = "Não informado no PROJECT_OS.md";

const elements = {
  dashboard: document.querySelector("#dashboard"),
  documents: document.querySelector("#documents"),
  documentsTotal: document.querySelector("#documents-total"),
  errorMessage: document.querySelector("#error-message"),
  errorState: document.querySelector("#error-state"),
  handoff: document.querySelector("#handoff"),
  lastUpdate: document.querySelector("#last-update"),
  liveStatus: document.querySelector("#live-status"),
  nextActions: document.querySelector("#next-actions"),
  overallStatus: document.querySelector("#overall-status"),
  progressBar: document.querySelector("#progress-bar"),
  progressLabel: document.querySelector("#progress-label"),
  progressPercent: document.querySelector("#progress-percent"),
  progressTrack: document.querySelector("#progress-track"),
  projectTitle: document.querySelector("#project-title"),
  refreshButton: document.querySelector("#refresh-button"),
  retryButton: document.querySelector("#retry-button"),
  roadmap: document.querySelector("#roadmap"),
  roadmapTotal: document.querySelector("#roadmap-total"),
  statusCounters: document.querySelector("#status-counters"),
  summaryGrid: document.querySelector("#summary-grid"),
  blockers: document.querySelector("#blockers")
};

document.addEventListener("DOMContentLoaded", () => {
  elements.refreshButton.addEventListener("click", loadProjectOS);
  elements.retryButton.addEventListener("click", loadProjectOS);
  loadProjectOS();
});

async function loadProjectOS() {
  setLoadingState();

  try {
    const response = await fetch(PROJECT_OS_URL, { cache: "no-store" });
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}`);
    }

    const markdown = await response.text();
    if (!markdown.trim()) {
      throw new Error("empty-file");
    }

    const project = parseProjectOS(markdown);
    renderDashboard(project);
    showDashboard();
  } catch (error) {
    console.warn("Falha ao carregar PROJECT_OS.md:", error.message);
    showError();
  }
}

function parseProjectOS(markdown) {
  const sections = extractSections(markdown);
  const meta = extractProjectMeta(markdown);
  const tasks = extractTasks(sections);
  const handoff = extractHandoff(sections);

  return {
    meta,
    currentPhase: extractLabeledValue(markdown, ["FASE ATUAL"]),
    currentTask: extractCurrentTask(markdown, meta, tasks),
    nextTask: extractLabeledValue(markdown, ["PRÓXIMA TAREFA", "PRÓXIMO MARCO"]),
    lastMilestone: extractLabeledValue(markdown, ["ÚLTIMA TAREFA CONCLUÍDA", "MARCO CONCLUÍDO"]),
    tasks,
    blockers: extractBlockers(sections),
    nextActions: extractNextActions(sections, handoff),
    handoff,
    documents: extractCanonicalDocuments(sections),
    progress: calculateProgress(tasks)
  };
}

function extractSections(markdown) {
  const lines = markdown.replace(/\r\n?/g, "\n").split("\n");
  const sections = [];
  let current = { level: 0, title: "Documento", lines: [], start: 0 };

  lines.forEach((line, index) => {
    const heading = line.match(/^(#{1,6})\s+(.+?)\s*$/);
    if (heading) {
      sections.push(current);
      current = {
        level: heading[1].length,
        title: cleanMarkdown(heading[2]),
        lines: [],
        start: index
      };
      return;
    }
    current.lines.push(line);
  });
  sections.push(current);

  return sections.filter((section) => section.title !== "Documento" || section.lines.some((line) => line.trim()));
}

function extractProjectMeta(markdown) {
  const yamlMatch = markdown.match(/```ya?ml\s*\n([\s\S]*?)```/i);
  const yaml = yamlMatch ? yamlMatch[1] : "";
  const readKey = (key) => {
    const match = yaml.match(new RegExp(`^\\s*${escapeRegExp(key)}\\s*:\\s*(.+?)\\s*$`, "mi"));
    return match ? cleanMarkdown(match[1]) : "";
  };

  return {
    name: readKey("name") || "Fisiofit CRM 2.0",
    status: readKey("status") || FALLBACK_TEXT,
    currentPriority: readKey("current_priority") || ""
  };
}

function extractLabeledValue(markdown, labels) {
  for (const label of labels) {
    const pattern = new RegExp(`^\\s*(?:\\*\\*)?${escapeRegExp(label)}\\s*:(?:\\*\\*)?\\s*(.+?)\\s*$`, "mi");
    const match = markdown.match(pattern);
    if (match) return cleanMarkdown(match[1]);
  }
  return FALLBACK_TEXT;
}

function extractCurrentTask(markdown, meta, tasks) {
  const labeled = extractLabeledValue(markdown, ["TAREFA ATUAL", "PRIORIDADE ATUAL"]);
  const value = labeled !== FALLBACK_TEXT ? labeled : meta.currentPriority;
  if (!value) return { label: FALLBACK_TEXT, status: "" };

  const code = value.match(/\b[A-Z]{2,10}-\d{3}\b/)?.[0];
  const task = code ? tasks.find((candidate) => candidate.code === code) : null;
  return { label: value, status: task?.status || "" };
}

function extractTasks(sections) {
  const backlogIndex = sections.findIndex((section) => matchesTitle(section.title, ["BACKLOG MESTRE", "MASTER BACKLOG"]));
  if (backlogIndex < 0) return [];

  const backlogLevel = sections[backlogIndex].level;
  const tasks = [];
  let group = "Backlog";

  for (let index = backlogIndex + 1; index < sections.length; index += 1) {
    const section = sections[index];
    if (section.level <= backlogLevel) break;
    if (section.level === backlogLevel + 1) group = section.title;

    parseMarkdownTable(section.lines).forEach((row) => {
      const normalized = normalizeRowKeys(row);
      const status = findStatus(normalized.status || "");
      const code = cleanMarkdown(normalized.id || normalized.codigo || normalized.code || "");
      const description = cleanMarkdown(normalized.tarefa || normalized.task || normalized.nome || "");
      if (!code || !status || (!description && !/^[A-Z]{2,10}-\d{3}$/.test(code))) return;

      tasks.push({
        code,
        description: description || code,
        priority: cleanMarkdown(normalized.prioridade || normalized.priority || ""),
        status,
        statusOriginal: cleanMarkdown(normalized.status),
        group
      });
    });
  }

  return uniqueBy(tasks, (task) => task.code);
}

function parseMarkdownTable(lines) {
  const rows = [];
  for (let index = 0; index < lines.length - 1; index += 1) {
    if (!isTableRow(lines[index]) || !isSeparatorRow(lines[index + 1])) continue;

    const headers = splitTableRow(lines[index]).map(normalizeKey);
    index += 2;
    while (index < lines.length && isTableRow(lines[index])) {
      const values = splitTableRow(lines[index]);
      const row = {};
      headers.forEach((header, cellIndex) => {
        row[header] = values[cellIndex] || "";
      });
      rows.push(row);
      index += 1;
    }
    index -= 1;
  }
  return rows;
}

function splitTableRow(line) {
  return line.trim().replace(/^\|/, "").replace(/\|$/, "").split("|").map((cell) => cell.trim());
}

function isTableRow(line) {
  return /^\s*\|.+\|\s*$/.test(line);
}

function isSeparatorRow(line) {
  return isTableRow(line) && splitTableRow(line).every((cell) => /^:?-{3,}:?$/.test(cell));
}

function normalizeRowKeys(row) {
  return Object.fromEntries(Object.entries(row).map(([key, value]) => [normalizeKey(key), value]));
}

function normalizeKey(value) {
  return value.normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLowerCase().replace(/[^a-z0-9]+/g, "");
}

function extractBlockers(sections) {
  const pendingIndex = sections.findIndex((section) => matchesTitle(section.title, ["PENDÊNCIAS", "OPEN QUESTIONS"]));
  if (pendingIndex < 0) return { blocking: [], nonBlocking: [] };

  const rootLevel = sections[pendingIndex].level;
  const blocking = [];
  const nonBlocking = [];

  for (let index = pendingIndex + 1; index < sections.length; index += 1) {
    const section = sections[index];
    if (section.level <= rootLevel) break;
    const items = extractListItems(section.lines);
    const title = section.title.toUpperCase();

    if (/NON[- ]?BLOCKING|NÃO BLOQUEANTE|LATER/.test(title)) {
      nonBlocking.push(...items);
    } else if (/BLOCKER/.test(title) && !section.lines.join(" ").match(/nenhum blocker conhecido/i)) {
      blocking.push({ title: humanizeBlockerTitle(section.title), items });
    }
  }

  return { blocking: blocking.filter((group) => group.items.length), nonBlocking };
}

function extractNextActions(sections, handoff) {
  const handoffActions = handoff.subsections.find((section) => matchesTitle(section.title, ["PRÓXIMAS 3 AÇÕES", "PROXIMAS 3 ACOES"]));
  if (handoffActions) return extractListItems(handoffActions.lines).slice(0, 3);

  const officialIndex = sections.findIndex((section) => matchesTitle(section.title, ["PRÓXIMOS PASSOS OFICIAIS", "PRÓXIMAS AÇÕES"]));
  if (officialIndex >= 0) {
    const rootLevel = sections[officialIndex].level;
    const lines = [];
    for (let index = officialIndex; index < sections.length; index += 1) {
      if (index > officialIndex && sections[index].level <= rootLevel) break;
      lines.push(...sections[index].lines);
    }
    return extractListItems(lines).slice(0, 3);
  }
  return [];
}

function extractHandoff(sections) {
  const rootIndex = findLastIndex(sections, (section) => matchesTitle(section.title, ["ÚLTIMO HANDOFF", "LAST HANDOFF"]));
  if (rootIndex < 0) return { title: FALLBACK_TEXT, preview: FALLBACK_TEXT, subsections: [] };

  const root = sections[rootIndex];
  const subsections = [];
  let handoffTitle = "";

  for (let index = rootIndex + 1; index < sections.length; index += 1) {
    const section = sections[index];
    if (section.level <= root.level) break;
    if (!handoffTitle && /HANDOFF/i.test(section.title)) handoffTitle = section.title;
    if (section.level >= root.level + 2) subsections.push(section);
  }

  const objective = subsections.find((section) => matchesTitle(section.title, ["OBJETIVO DA SESSÃO", "OBJETIVO"]));
  const status = subsections.find((section) => matchesTitle(section.title, ["STATUS ATUAL", "STATUS FINAL"]));
  const previewParts = [firstParagraph(objective?.lines || []), firstParagraph(status?.lines || [])].filter(Boolean);

  return {
    title: handoffTitle || root.title,
    preview: previewParts.join(" ") || FALLBACK_TEXT,
    subsections
  };
}

function extractCanonicalDocuments(sections) {
  const index = sections.findIndex((section) => matchesTitle(section.title, ["ÍNDICE CANÔNICO DO PROJETO", "DOCUMENTOS CANÔNICOS"]));
  if (index < 0) return [];

  const rows = parseMarkdownTable(sections[index].lines);
  const documents = [];
  const pathPattern = /docs\/[A-Za-z0-9_.\/-]+\.(?:md|docx)/gi;

  rows.forEach((row) => {
    const normalized = normalizeRowKeys(row);
    const documentCell = normalized.documento || normalized.document || "";
    const description = cleanMarkdown(normalized.finalidade || normalized.purpose || normalized.descricao || "");
    const paths = documentCell.match(pathPattern) || [];
    paths.forEach((path) => documents.push({ path, description }));
  });

  return uniqueBy(documents, (document) => document.path);
}

function calculateProgress(tasks) {
  const done = tasks.filter((task) => task.status === "DONE").length;
  const total = tasks.length;
  return { done, total, percent: total ? Math.round((done / total) * 100) : 0 };
}

function renderDashboard(project) {
  elements.projectTitle.textContent = project.meta.name;
  setStatusBadge(elements.overallStatus, project.meta.status, findStatus(project.meta.status));
  renderSummary(project);
  renderProgress(project.progress);
  renderCounters(project.tasks);
  renderRoadmap(project.tasks);
  renderBlockers(project.blockers);
  renderActions(project.nextActions);
  renderHandoff(project.handoff);
  renderDocuments(project.documents);
  elements.lastUpdate.textContent = `Último registro: ${project.handoff.title || project.lastMilestone}`;
}

function renderSummary(project) {
  elements.summaryGrid.replaceChildren();
  const cards = [
    { label: "Fase atual", value: project.currentPhase },
    { label: "Tarefa / prioridade atual", value: project.currentTask.label, status: project.currentTask.status },
    { label: "Próxima tarefa", value: project.nextTask },
    { label: "Último marco concluído", value: project.lastMilestone, status: "DONE" }
  ];

  cards.forEach((card) => {
    const article = createElement("article", "summary-card");
    article.append(createTextElement("p", "label", card.label));
    article.append(createTextElement("p", "value", card.value || FALLBACK_TEXT));
    if (card.status) article.append(createStatusBadge(card.status));
    elements.summaryGrid.append(article);
  });
}

function renderProgress(progress) {
  elements.progressPercent.textContent = `${progress.percent}%`;
  elements.progressBar.style.width = `${progress.percent}%`;
  elements.progressLabel.textContent = `${progress.done} / ${progress.total} tarefas concluídas`;
  elements.progressTrack.setAttribute("aria-valuenow", String(progress.percent));
  elements.progressTrack.setAttribute("aria-valuetext", `${progress.done} de ${progress.total} tarefas concluídas`);
}

function renderCounters(tasks) {
  elements.statusCounters.replaceChildren();
  const counts = Object.fromEntries(COUNTER_STATUSES.map((status) => [status, 0]));
  tasks.forEach((task) => {
    if (task.status in counts) counts[task.status] += 1;
  });

  COUNTER_STATUSES.forEach((status) => {
    const card = createElement("article", "counter-card");
    card.style.setProperty("--status-color", `var(--${statusClass(status).replace("status-", "")})`);
    card.append(createTextElement("strong", "count", String(counts[status])));
    card.append(createTextElement("span", "counter-label", status));
    card.setAttribute("aria-label", `${status}: ${counts[status]} tarefas`);
    elements.statusCounters.append(card);
  });
}

function renderRoadmap(tasks) {
  elements.roadmap.replaceChildren();
  elements.roadmapTotal.textContent = `${tasks.length} tarefas no backlog mestre`;

  if (!tasks.length) {
    elements.roadmap.append(createTextElement("p", "empty-message", FALLBACK_TEXT));
    return;
  }

  const groups = groupBy(tasks, (task) => task.group);
  Array.from(groups.entries()).forEach(([name, groupTasks], groupIndex) => {
    const details = createElement("details", "roadmap-group");
    if (groupIndex === 0 || groupTasks.some((task) => task.status === "READY" || task.status === "IN_PROGRESS")) {
      details.open = true;
    }
    const summary = document.createElement("summary");
    summary.append(createTextElement("span", "group-title", name));
    summary.append(createTextElement("span", "group-count", `${groupTasks.length} itens`));
    details.append(summary);

    const list = createElement("ol", "task-list");
    groupTasks.forEach((task) => {
      const item = createElement("li", "task-item");
      item.style.setProperty("--status-color", `var(--${statusClass(task.status).replace("status-", "")})`);
      item.append(createElement("span", "task-dot"));

      const content = document.createElement("div");
      const title = createTextElement("div", "task-code", task.code);
      if (task.priority) title.append(createTextElement("span", "task-priority", task.priority));
      content.append(title);
      content.append(createTextElement("p", "task-description", task.description));
      item.append(content);
      item.append(createStatusBadge(task.statusOriginal || task.status, task.status));
      list.append(item);
    });

    details.append(list);
    elements.roadmap.append(details);
  });
}

function renderBlockers(blockers) {
  elements.blockers.replaceChildren();
  if (!blockers.blocking.length) {
    elements.blockers.append(createTextElement("p", "empty-message", "Sem blockers para a próxima etapa."));
  } else {
    blockers.blocking.forEach((group) => {
      const wrapper = createElement("div", "blocker-group");
      wrapper.append(createTextElement("h3", "", group.title));
      wrapper.append(renderTextList(group.items));
      elements.blockers.append(wrapper);
    });
  }

  if (blockers.nonBlocking.length) {
    const wrapper = createElement("div", "question-group");
    wrapper.append(createTextElement("h3", "", "Questões abertas não bloqueantes"));
    wrapper.append(renderTextList(blockers.nonBlocking));
    elements.blockers.append(wrapper);
  }
}

function renderActions(actions) {
  elements.nextActions.replaceChildren();
  if (!actions.length) {
    const item = document.createElement("li");
    item.textContent = FALLBACK_TEXT;
    elements.nextActions.append(item);
    return;
  }
  actions.slice(0, 3).forEach((action) => elements.nextActions.append(createTextElement("li", "", action)));
}

function renderHandoff(handoff) {
  elements.handoff.replaceChildren();
  elements.handoff.append(createTextElement("h3", "handoff-heading", handoff.title));
  elements.handoff.append(createTextElement("p", "handoff-preview", handoff.preview));

  const usefulSections = handoff.subsections.filter((section) =>
    /CONCLU[IÍ]DO|BLOCKERS|RISCOS|INSTRU[CÇ][AÃ]O/i.test(section.title)
  );
  if (!usefulSections.length) return;

  const details = createElement("details", "handoff-details");
  details.append(createTextElement("summary", "", "Expandir resumo"));
  const content = createElement("div", "handoff-detail-content");

  usefulSections.forEach((section) => {
    content.append(createTextElement("h3", "", section.title));
    const items = extractListItems(section.lines);
    if (items.length) content.append(renderTextList(items));
    else content.append(createTextElement("p", "", firstParagraph(section.lines) || FALLBACK_TEXT));
  });

  details.append(content);
  details.addEventListener("toggle", () => {
    details.querySelector("summary").textContent = details.open ? "Recolher resumo" : "Expandir resumo";
  });
  elements.handoff.append(details);
}

function renderDocuments(documents) {
  elements.documents.replaceChildren();
  elements.documentsTotal.textContent = `${documents.length} referências com caminho local`;
  if (!documents.length) {
    elements.documents.append(createTextElement("p", "empty-message", FALLBACK_TEXT));
    return;
  }

  documents.forEach((documentInfo) => {
    const card = createElement("article", "document-card");
    const link = document.createElement("a");
    link.href = `../${documentInfo.path}`;
    link.textContent = documentInfo.path;
    card.append(link);
    if (documentInfo.description) card.append(createTextElement("p", "", documentInfo.description));
    elements.documents.append(card);
  });
}

function renderTextList(items) {
  const list = createElement("ul", "plain-list");
  items.forEach((item) => list.append(createTextElement("li", "", item)));
  return list;
}

function setLoadingState() {
  elements.refreshButton.disabled = true;
  elements.retryButton.disabled = true;
  elements.liveStatus.textContent = "Carregando status do projeto...";
  elements.liveStatus.hidden = false;
  elements.dashboard.hidden = true;
  elements.errorState.hidden = true;
}

function showDashboard() {
  elements.liveStatus.textContent = "Status do projeto carregado.";
  elements.liveStatus.hidden = true;
  elements.dashboard.hidden = false;
  elements.errorState.hidden = true;
  elements.refreshButton.disabled = false;
  elements.retryButton.disabled = false;
}

function showError() {
  elements.liveStatus.hidden = true;
  elements.dashboard.hidden = true;
  elements.errorState.hidden = false;
  elements.errorMessage.textContent =
    "Execute o dashboard através de um servidor HTTP iniciado na raiz do projeto e tente novamente.";
  elements.refreshButton.disabled = false;
  elements.retryButton.disabled = false;
  setStatusBadge(elements.overallStatus, "Erro de leitura", "FAIL");
}

function createStatusBadge(label, normalizedStatus = findStatus(label)) {
  const badge = createElement("span", `status-badge ${statusClass(normalizedStatus)}`);
  badge.textContent = label;
  return badge;
}

function setStatusBadge(element, label, normalizedStatus) {
  element.className = `status-badge ${statusClass(normalizedStatus)}`;
  element.textContent = label || FALLBACK_TEXT;
}

function statusClass(status) {
  return status ? `status-${status.toLowerCase().replaceAll("_", "-")}` : "status-neutral";
}

function findStatus(value) {
  const normalized = String(value).toUpperCase().replace(/[\s-]+/g, "_");
  return KNOWN_STATUSES.find((status) => new RegExp(`(?:^|_)${status}(?:_|$)`).test(normalized)) || "";
}

function extractListItems(lines) {
  return lines
    .map((line) => line.match(/^\s*(?:[-*+] |\d+[.)]\s+)(.+?)\s*$/)?.[1])
    .filter(Boolean)
    .map(cleanMarkdown);
}

function firstParagraph(lines) {
  const paragraphs = [];
  let current = [];
  for (const line of lines) {
    const trimmed = line.trim();
    if (!trimmed || /^[-*+] |^\d+[.)]\s+|^\||^```/.test(trimmed)) {
      if (current.length) break;
      continue;
    }
    current.push(trimmed);
  }
  if (current.length) paragraphs.push(current.join(" "));
  return cleanMarkdown(paragraphs[0] || "");
}

function humanizeBlockerTitle(title) {
  return title
    .replace(/BLOCKERS?\s+BEFORE\s+/i, "Antes de ")
    .replace(/CONCEPTUAL MODEL/i, "modelagem conceitual")
    .replace(/IMPLEMENTATION/i, "implementação")
    .replace(/GO-LIVE/i, "go-live");
}

function cleanMarkdown(value) {
  return String(value)
    .replace(/`([^`]+)`/g, "$1")
    .replace(/\*\*([^*]+)\*\*/g, "$1")
    .replace(/__([^_]+)__/g, "$1")
    .replace(/\[([^\]]+)]\([^)]+\)/g, "$1")
    .replace(/\\([\\`*_{}[\]()#+.!-])/g, "$1")
    .trim();
}

function matchesTitle(title, candidates) {
  const normalizedTitle = normalizeComparable(title);
  return candidates.some((candidate) => normalizedTitle.includes(normalizeComparable(candidate)));
}

function normalizeComparable(value) {
  return String(value)
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/^\d+(?:\.\d+)*\.?\s*/, "")
    .replace(/[^A-Za-z0-9]+/g, " ")
    .trim()
    .toUpperCase();
}

function groupBy(items, getKey) {
  const groups = new Map();
  items.forEach((item) => {
    const key = getKey(item);
    if (!groups.has(key)) groups.set(key, []);
    groups.get(key).push(item);
  });
  return groups;
}

function uniqueBy(items, getKey) {
  const seen = new Set();
  return items.filter((item) => {
    const key = getKey(item);
    if (seen.has(key)) return false;
    seen.add(key);
    return true;
  });
}

function findLastIndex(items, predicate) {
  for (let index = items.length - 1; index >= 0; index -= 1) {
    if (predicate(items[index], index)) return index;
  }
  return -1;
}

function escapeRegExp(value) {
  return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

function createElement(tagName, className) {
  const element = document.createElement(tagName);
  if (className) element.className = className;
  return element;
}

function createTextElement(tagName, className, text) {
  const element = createElement(tagName, className);
  element.textContent = text;
  return element;
}
