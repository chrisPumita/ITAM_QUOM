using System.Net.Http.Headers;
using System.Security.Claims;
using ITAM.WebApp.Security;

namespace ITAM.WebApp.ApiConnect;

/// <summary>
/// Adjunta automáticamente el JWT de la cookie de sesión a las llamadas ApiConnect.
/// Más limpio que Daikin (token en NameIdentifier): claim dedicado + handler central.
/// </summary>
public sealed class BearerTokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BearerTokenHandler(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var token = user.FindFirstValue(AuthClaimTypes.AccessToken);
            if (!string.IsNullOrWhiteSpace(token) && request.Headers.Authorization is null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
