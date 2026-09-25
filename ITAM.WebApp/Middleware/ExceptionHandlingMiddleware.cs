using Serilog;

namespace ITAM.WebApp.Middleware;

/// <summary>
/// Captura excepciones en MVC, las registra con Serilog y redirige a página de error genérica.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next) => _next = next;

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

            context.Response.Redirect("/Home/Error");
        }
    }
}
