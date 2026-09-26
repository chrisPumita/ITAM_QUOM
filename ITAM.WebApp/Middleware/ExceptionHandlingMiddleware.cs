using Serilog;

namespace ITAM.WebApp.Middleware;

/// <summary>
/// Captura excepciones en MVC, las registra con Serilog y muestra error sin bucles de redirect.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(RequestDelegate next, IHostEnvironment env)
    {
        _next = next;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Excepción no controlada en MVC {Method} {Path}",
                context.Request.Method, context.Request.Path);

            if (context.Response.HasStarted)
                throw;

            // Evitar ERR_TOO_MANY_REDIRECTS si /Home/Error o el layout también fallan.
            var path = context.Request.Path.Value ?? "";
            if (path.StartsWith("/Home/Error", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/Account/Login", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "text/html; charset=utf-8";
                var detail = _env.IsDevelopment()
                    ? $"<pre>{System.Net.WebUtility.HtmlEncode(ex.ToString())}</pre>"
                    : "<p>Revise los logs del WebApp.</p>";
                await context.Response.WriteAsync(
                    "<!DOCTYPE html><html><body style='font-family:sans-serif;padding:2rem'>" +
                    "<h1>Error</h1><p>No se pudo mostrar la página de error.</p>" +
                    detail +
                    "</body></html>");
                return;
            }

            if (_env.IsDevelopment())
            {
                // En desarrollo preferimos el stack en la respuesta, no un redirect opaco.
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "text/plain; charset=utf-8";
                await context.Response.WriteAsync(ex.ToString());
                return;
            }

            context.Response.Redirect("/Home/Error");
        }
    }
}
