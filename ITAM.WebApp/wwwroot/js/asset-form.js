(function () {
  const form = document.getElementById("altaForm") || document.getElementById("editForm");
  if (!form) return;

  const modelSelect = document.getElementById("modelSelect");
  const filterBrand = document.getElementById("filterBrand");
  if (!modelSelect || !filterBrand) return;

  let models = [];
  try { models = JSON.parse(modelSelect.getAttribute("data-models") || "[]"); } catch { models = []; }

  function fillModels() {
    const brandId = filterBrand.value ? parseInt(filterBrand.value, 10) : null;
    const selectedModel = modelSelect.getAttribute("data-selected") || "";
    modelSelect.innerHTML = '<option value="">Seleccione…</option>';
    if (!brandId) {
      modelSelect.disabled = true;
      return;
    }
    modelSelect.disabled = false;
    const prev = modelSelect.value || selectedModel;
    models.filter(m => m.BrandId === brandId).forEach(m => {
      const opt = document.createElement("option");
      opt.value = m.Id;
      opt.textContent = `${m.BrandName} ${m.Name} — ${m.CategoryName}`;
      if (String(m.Id) === String(prev)) opt.selected = true;
      modelSelect.appendChild(opt);
    });
  }

  filterBrand.addEventListener("change", () => {
    modelSelect.setAttribute("data-selected", "");
    fillModels();
  });
  fillModels();

  const purchase = document.getElementById("purchaseDate");
  const warranty = document.getElementById("warrantyEndDate");
  if (purchase && warranty && form.dataset.syncWarranty === "true") {
    purchase.addEventListener("change", () => {
      if (!purchase.value) return;
      const d = new Date(purchase.value + "T00:00:00");
      d.setFullYear(d.getFullYear() + 1);
      const yyyy = d.getFullYear();
      const mm = String(d.getMonth() + 1).padStart(2, "0");
      const dd = String(d.getDate()).padStart(2, "0");
      warranty.value = `${yyyy}-${mm}-${dd}`;
    });
  }

  const brandUrl = form.dataset.quickBrandUrl;
  const modelUrl = form.dataset.quickModelUrl;
  const tokenEl = form.querySelector('input[name="__RequestVerificationToken"]');
  const token = tokenEl ? tokenEl.value : "";

  const btnSaveBrand = document.getElementById("btnSaveBrand");
  if (btnSaveBrand && brandUrl) {
    btnSaveBrand.addEventListener("click", async () => {
      const name = document.getElementById("newBrandName").value.trim();
      if (!name) return;
      const headers = { "Content-Type": "application/json" };
      if (token) headers["RequestVerificationToken"] = token;
      const res = await fetch(brandUrl, {
        method: "POST",
        headers,
        body: JSON.stringify({ name })
      });
      const data = await res.json();
      if (!data.ok) { alert(data.message || "Error"); return; }
      const opt = document.createElement("option");
      opt.value = data.id;
      opt.textContent = data.name;
      opt.selected = true;
      filterBrand.appendChild(opt);
      const newModelBrand = document.getElementById("newModelBrand");
      if (newModelBrand) newModelBrand.appendChild(opt.cloneNode(true));
      filterBrand.value = data.id;
      fillModels();
      document.getElementById("newBrandName").value = "";
      const collapse = document.getElementById("quickBrand");
      if (collapse) bootstrap.Collapse.getOrCreateInstance(collapse).hide();
    });
  }

  const btnSaveModel = document.getElementById("btnSaveModel");
  if (btnSaveModel && modelUrl) {
    btnSaveModel.addEventListener("click", async () => {
      const brandId = parseInt(document.getElementById("newModelBrand").value, 10);
      const categoryId = parseInt(document.getElementById("newModelCat").value, 10);
      const name = document.getElementById("newModelName").value.trim();
      const specs = document.getElementById("newModelSpecs").value.trim();
      if (!name || !brandId || !categoryId) return;
      const headers = { "Content-Type": "application/json" };
      if (token) headers["RequestVerificationToken"] = token;
      const res = await fetch(modelUrl, {
        method: "POST",
        headers,
        body: JSON.stringify({ name, brandId, categoryId, specs: specs || null })
      });
      const data = await res.json();
      if (!data.ok) { alert(data.message || "Error"); return; }
      models.push({
        Id: data.id,
        Name: data.name,
        BrandId: data.brandId,
        BrandName: data.brandName,
        CategoryName: data.categoryName
      });
      filterBrand.value = data.brandId;
      modelSelect.setAttribute("data-selected", String(data.id));
      fillModels();
      modelSelect.value = String(data.id);
      document.getElementById("newModelName").value = "";
      document.getElementById("newModelSpecs").value = "";
      const collapse = document.getElementById("quickModel");
      if (collapse) bootstrap.Collapse.getOrCreateInstance(collapse).hide();
    });
  }
})();
