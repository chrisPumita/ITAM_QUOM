(() => {
  const el = document.getElementById("api-connection");
  if (!el) return;

  const label = el.querySelector(".api-connection__label");
  const pingUrl = "/api/connection/ping";
  const intervalMs = 10000;
  let timerId = null;
  let inFlight = false;

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
        return;
      }

      const data = await res.json();
      if (data && data.connected === true) {
        const detail = data.message || "Conectado";
        setState("ok", "API online", detail);
      } else {
        setState("down", "API offline", (data && data.message) || "Sin conexión");
      }
    } catch (err) {
      setState("down", "API offline", err && err.message ? err.message : "Error de red");
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
