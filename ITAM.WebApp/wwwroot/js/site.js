(() => {
  const el = document.getElementById("api-connection");
  if (!el) return;

  const label = el.querySelector(".api-connection__label");
  const footerApi = document.getElementById("api-version-footer");
  const pingUrl = "/api/connection/ping";
  const intervalMs = 10000;
  let timerId = null;
  let inFlight = false;
  const webVersion = el.dataset.webVersion || "";
  const webUpdated = el.dataset.webUpdated || "";

  function setState(state, text, title) {
    el.classList.remove(
      "api-connection--checking",
      "api-connection--ok",
      "api-connection--down"
    );
    el.classList.add(`api-connection--${state}`);
    if (label) label.textContent = text;
    el.title = title || text;
  }

  function setFooterApi(version, lastUpdate) {
    if (!footerApi) return;
    if (version) {
      footerApi.textContent = ` · API ${version}` + (lastUpdate ? ` · ${lastUpdate}` : "");
    } else {
      footerApi.textContent = "";
    }
  }

  async function checkConnection() {
    if (inFlight) return;
    inFlight = true;

    try {
      const res = await fetch(pingUrl, {
        method: "GET",
        headers: { Accept: "application/json" },
        cache: "no-store"
      });

      if (!res.ok) {
        setState("down", "API offline", `Error HTTP ${res.status}`);
        setFooterApi(null);
        return;
      }

      const data = await res.json();
      const webHint = webVersion
        ? `Web ${webVersion}` + (webUpdated ? ` (${webUpdated})` : "")
        : "";

      if (data && data.connected === true) {
        const apiVer = data.version || "";
        const apiUpd = data.lastUpdate || "";
        const detail = [
          data.message || "Conectado",
          apiVer ? `API ${apiVer}` : null,
          apiUpd ? `API upd ${apiUpd}` : null,
          webHint || null
        ].filter(Boolean).join(" · ");
        setState("ok", "API online", detail);
        setFooterApi(apiVer, apiUpd);
      } else {
        setState("down", "API offline", (data && data.message) || "Sin conexión");
        setFooterApi(data && data.version, data && data.lastUpdate);
      }
    } catch (err) {
      setState("down", "API offline", err && err.message ? err.message : "Error de red");
      setFooterApi(null);
    } finally {
      inFlight = false;
    }
  }

  setState("checking", "API…", "Comprobando API…");
  checkConnection();
  timerId = window.setInterval(checkConnection, intervalMs);

  document.addEventListener("visibilitychange", () => {
    if (document.visibilityState === "visible") checkConnection();
  });

  window.addEventListener("beforeunload", () => {
    if (timerId) window.clearInterval(timerId);
  });
})();

/** Marca → Modelo: bloquea modelo si no hay marca. */
window.itamBindBrandModel = function (brandId, modelId) {
  const brand = document.getElementById(brandId);
  const model = document.getElementById(modelId);
  if (!brand || !model) return;

  let models = [];
  try { models = JSON.parse(model.getAttribute("data-models") || "[]"); } catch { models = []; }
  const selected = model.getAttribute("data-selected") || "";

  function fill() {
    const bid = brand.value ? parseInt(brand.value, 10) : null;
    model.innerHTML = "";
    if (!bid) {
      model.disabled = true;
      model.innerHTML = '<option value="">Seleccione marca…</option>';
      return;
    }
    model.disabled = false;
    const all = document.createElement("option");
    all.value = "";
    all.textContent = "Todos los modelos";
    model.appendChild(all);
    models.filter(m => m.BrandId === bid).forEach(m => {
      const o = document.createElement("option");
      o.value = m.Id;
      o.textContent = (m.BrandName ? m.BrandName + " " : "") + m.Name;
      if (String(m.Id) === selected) o.selected = true;
      model.appendChild(o);
    });
  }

  brand.addEventListener("change", () => {
    model.setAttribute("data-selected", "");
    fill();
  });
  fill();
};

document.addEventListener("submit", async function (e) {
  const form = e.target;
  if (!(form instanceof HTMLFormElement) || !form.classList.contains("js-confirm-baja")) return;
  e.preventDefault();
  const code = form.getAttribute("data-code") || "este activo";
  const result = await Swal.fire({
    icon: "warning",
    title: "¿Dar de baja?",
    html: `El activo <strong class="font-monospace">${code}</strong> pasará a estado Baja.`,
    showCancelButton: true,
    confirmButtonText: "Sí, dar de baja",
    cancelButtonText: "Cancelar",
    confirmButtonColor: "#C1432E"
  });
  if (result.isConfirmed) form.submit();
});
