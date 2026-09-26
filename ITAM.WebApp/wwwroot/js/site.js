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
