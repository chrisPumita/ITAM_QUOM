(function () {
  const form = document.getElementById("assignForm");
  if (!form) return;

  const availableUrl = form.dataset.availableUrl || "";
  const empUrl = form.dataset.empUrl || "";
  let preselected = [];
  let initialAssets = [];
  try { preselected = JSON.parse(form.dataset.preselected || "[]"); } catch { /* ignore */ }
  try { initialAssets = JSON.parse(form.dataset.initialAssets || "[]"); } catch { /* ignore */ }

  const selected = new Map();
  (preselected || []).forEach(id => {
    const hit = (initialAssets || []).find(a => String(a.id) === String(id));
    if (hit) {
      selected.set(String(hit.id), {
        id: String(hit.id),
        assetCode: hit.assetCode,
        description: hit.description
      });
    } else {
      selected.set(String(id), { id: String(id), assetCode: "…", description: "" });
    }
  });

  const tbody = document.getElementById("assetsTbody");
  const tableCount = document.getElementById("tableCount");
  const summaryList = document.getElementById("summaryList");
  const summaryCount = document.getElementById("summaryCount");
  const summaryEmpty = document.getElementById("summaryEmpty");
  const summaryHint = document.getElementById("summaryEmployeeHint");
  const btnAssign = document.getElementById("btnAssign");
  const selectedHost = document.getElementById("selectedIdsHost");
  const empSelect = document.getElementById("employeeSelect");
  const searchInput = document.getElementById("assetSearch");
  const modelChips = document.getElementById("modelChips");
  const filterBar = document.getElementById("assignFilters");
  let allModels = [];
  try { allModels = JSON.parse(modelChips.getAttribute("data-models") || "[]"); } catch { allModels = []; }

  function checkedValues(group) {
    return [...document.querySelectorAll(`[data-filter="${group}"] input:checked`)].map(i => i.value);
  }

  function syncChipStyles() {
    document.querySelectorAll(".filter-chip").forEach(lab => {
      const on = lab.querySelector("input")?.checked;
      lab.classList.toggle("is-active", !!on);
    });
  }

  function rebuildModelChips() {
    const brandIds = new Set(checkedValues("brandIds").map(Number));
    const prev = new Set(checkedValues("modelIds"));
    modelChips.querySelectorAll("label.filter-chip").forEach(n => n.remove());

    if (brandIds.size === 0) {
      syncChipStyles();
      return;
    }

    allModels.filter(m => brandIds.has(m.BrandId)).forEach(m => {
      const lab = document.createElement("label");
      lab.className = "filter-chip";
      lab.innerHTML = `<input type="checkbox" value="${m.Id}" ${prev.has(String(m.Id)) ? "checked" : ""} /><span>${m.BrandName} ${m.Name}</span>`;
      modelChips.appendChild(lab);
    });
    syncChipStyles();
  }

  function buildQuery() {
    const params = new URLSearchParams();
    const q = searchInput.value.trim();
    if (q) params.set("search", q);
    checkedValues("kinds").forEach(v => params.append("kinds", v));
    checkedValues("conditions").forEach(v => params.append("conditions", v));
    checkedValues("brandIds").forEach(v => params.append("brandIds", v));
    checkedValues("categoryIds").forEach(v => params.append("categoryIds", v));
    checkedValues("modelIds").forEach(v => params.append("modelIds", v));
    return params.toString();
  }

  function escapeHtml(s) {
    return String(s || "").replace(/[&<>"']/g, c =>
      ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
  }

  function renderTable(items) {
    tbody.innerHTML = "";
    tableCount.textContent = `${items.length} disponible${items.length === 1 ? "" : "s"}`;
    if (!items.length) {
      tbody.innerHTML = '<tr><td colspan="6" class="text-muted text-center py-3">No hay activos con esos filtros.</td></tr>';
      return;
    }
    items.forEach(a => {
      const id = String(a.id);
      const tr = document.createElement("tr");
      tr.dataset.id = id;
      const checked = selected.has(id) ? "checked" : "";
      tr.innerHTML = `
        <td><input type="checkbox" class="form-check-input asset-check" value="${id}" ${checked} /></td>
        <td class="font-mono">${escapeHtml(a.assetCode)}</td>
        <td class="font-mono small">${escapeHtml(a.serialNumber || "—")}</td>
        <td>${escapeHtml(a.description || "")}</td>
        <td>${escapeHtml(a.conditionLabel || a.condition || "")}</td>
        <td>${escapeHtml(a.kindLabel || "")}</td>`;
      const cb = tr.querySelector(".asset-check");
      cb.addEventListener("change", () => {
        if (cb.checked) {
          selected.set(id, { id, assetCode: a.assetCode, description: a.description || "" });
        } else {
          selected.delete(id);
        }
        renderSummary();
      });
      tbody.appendChild(tr);
    });
  }

  function renderSummary() {
    summaryList.innerHTML = "";
    summaryCount.textContent = String(selected.size);
    summaryEmpty.classList.toggle("d-none", selected.size > 0);
    selected.forEach(item => {
      const li = document.createElement("li");
      li.innerHTML = `
        <div>
          <span class="font-mono fw-semibold">${escapeHtml(item.assetCode)}</span>
          <div class="small text-muted">${escapeHtml(item.description || "")}</div>
        </div>
        <button type="button" class="btn btn-sm btn-link text-danger p-0" data-remove="${item.id}" title="Quitar">×</button>`;
      summaryList.appendChild(li);
    });
    summaryList.querySelectorAll("[data-remove]").forEach(btn => {
      btn.addEventListener("click", () => {
        selected.delete(btn.getAttribute("data-remove"));
        const rowCb = tbody.querySelector(`.asset-check[value="${btn.getAttribute("data-remove")}"]`);
        if (rowCb) rowCb.checked = false;
        renderSummary();
      });
    });

    selectedHost.innerHTML = "";
    selected.forEach((_, id) => {
      const input = document.createElement("input");
      input.type = "hidden";
      input.name = "SelectedAssetIds";
      input.value = id;
      selectedHost.appendChild(input);
    });

    const empOk = !!empSelect.value;
    const empName = empOk ? (empSelect.options[empSelect.selectedIndex]?.text || "") : "";
    summaryHint.textContent = empOk
      ? `Entrega a ${empName}`
      : "Seleccione colaborador y activos.";
    btnAssign.disabled = !(empOk && selected.size > 0);
  }

  let debounceTimer;
  async function reloadAssets() {
    syncChipStyles();
    tbody.innerHTML = '<tr><td colspan="6" class="text-muted text-center py-3">Cargando…</td></tr>';
    try {
      const qs = buildQuery();
      const res = await fetch(availableUrl + (qs ? "?" + qs : ""), { headers: { Accept: "application/json" } });
      const data = await res.json();
      renderTable(Array.isArray(data) ? data : []);
    } catch {
      tbody.innerHTML = '<tr><td colspan="6" class="text-danger text-center py-3">No se pudo cargar el listado.</td></tr>';
    }
    renderSummary();
  }

  function scheduleReload() {
    clearTimeout(debounceTimer);
    debounceTimer = setTimeout(reloadAssets, 280);
  }

  if (filterBar) {
    filterBar.addEventListener("change", e => {
      if (!(e.target instanceof HTMLInputElement)) return;
      if (e.target.closest('[data-filter="brandIds"]')) rebuildModelChips();
      scheduleReload();
    });
  }

  searchInput.addEventListener("input", scheduleReload);
  document.getElementById("btnClearFilters").addEventListener("click", () => {
    searchInput.value = "";
    document.querySelectorAll(".filter-chip input").forEach(i => { i.checked = false; });
    rebuildModelChips();
    reloadAssets();
  });

  const wrap = document.getElementById("empAssignmentsWrap");
  const list = document.getElementById("empAssignList");
  const countBadge = document.getElementById("empAssignCount");

  async function loadEmpAssignments() {
    const id = empSelect.value;
    renderSummary();
    if (!id) {
      wrap.classList.add("d-none");
      list.innerHTML = "";
      return;
    }
    try {
      const res = await fetch(empUrl + "?employeeId=" + encodeURIComponent(id), {
        headers: { Accept: "application/json" }
      });
      const data = await res.json();
      list.innerHTML = "";
      if (!Array.isArray(data) || data.length === 0) {
        countBadge.textContent = "0";
        list.innerHTML = '<li class="text-muted">Sin activos asignados actualmente.</li>';
      } else {
        countBadge.textContent = String(data.length);
        data.forEach(a => {
          const li = document.createElement("li");
          li.innerHTML = `<span class="font-monospace">${escapeHtml(a.assetCode)}</span> · ${escapeHtml(a.description || "")} <span class="text-muted">(${escapeHtml(a.assignedAt || "")})</span>`;
          list.appendChild(li);
        });
      }
      wrap.classList.remove("d-none");
    } catch {
      wrap.classList.add("d-none");
    }
  }
  empSelect.addEventListener("change", loadEmpAssignments);

  form.addEventListener("submit", async function (e) {
    e.preventDefault();
    renderSummary();
    if (!empSelect.value) {
      await Swal.fire({ icon: "warning", title: "Seleccione un colaborador", confirmButtonColor: "#9C5F26" });
      return;
    }
    if (selected.size === 0) {
      await Swal.fire({ icon: "warning", title: "Seleccione al menos un activo", confirmButtonColor: "#9C5F26" });
      return;
    }
    const empName = empSelect.options[empSelect.selectedIndex]?.text || "colaborador";
    const codes = [...selected.values()].map(x => x.assetCode);
    const listHtml = codes.slice(0, 8).map(c => `<li class="font-monospace">${escapeHtml(c)}</li>`).join("")
      + (codes.length > 8 ? `<li>… y ${codes.length - 8} más</li>` : "");
    const ok = await Swal.fire({
      icon: "question",
      title: "¿Confirmar asignación?",
      html: `<p>Se asignarán <strong>${selected.size}</strong> activo(s) a <strong>${escapeHtml(empName)}</strong> y se emitirá la responsiva.</p><ul class="text-start small">${listHtml}</ul>`,
      showCancelButton: true,
      confirmButtonText: "Sí, asignar",
      cancelButtonText: "Cancelar",
      confirmButtonColor: "#9C5F26"
    });
    if (ok.isConfirmed) {
      renderSummary();
      e.target.submit();
    }
  });

  rebuildModelChips();
  renderTable(initialAssets || []);
  renderSummary();
  if (empSelect.value) loadEmpAssignments();
})();
