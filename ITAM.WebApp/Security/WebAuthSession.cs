using System.Security.Claims;
using ITAM.Shared.Dtos.Auth;
using ITAM.WebApp.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ITAM.WebApp.Security;

public interface IWebAuthSession
{
    Task SignInAsync(HttpContext http, LoginResponseDto login, bool rememberMe);
    Task SignOutAsync(HttpContext http);
    bool IsAuthenticated(ClaimsPrincipal user);
    string? GetAccessToken(ClaimsPrincipal user);
}

/// <summary>
/// Sesión MVC con cookie: claims de identidad + JWT para la API.
/// </summary>
public sealed class WebAuthSession : IWebAuthSession
{
    public async Task SignInAsync(HttpContext http, LoginResponseDto login, bool rememberMe)
    {
        var role = login.Role?.ToString() ?? string.Empty;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, login.IdentityUserId.ToString()),
            new(ClaimTypes.Email, login.Email),
            new(ClaimTypes.Name, string.IsNullOrWhiteSpace(login.DisplayName) ? login.Email : login.DisplayName),
            new(AuthClaimTypes.AccessToken, login.Token),
            new(AuthClaimTypes.TokenExpiresUtc, login.ExpiraEn.ToUniversalTime().ToString("O"))
        };

        if (!string.IsNullOrWhiteSpace(role))
            claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var props = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            AllowRefresh = true,
            IssuedUtc = DateTimeOffset.UtcNow,
            ExpiresUtc = login.ExpiraEn.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(login.ExpiraEn, DateTimeKind.Utc)
                : login.ExpiraEn.ToUniversalTime()
        };

        await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);
    }

    public Task SignOutAsync(HttpContext http)
        => http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    public bool IsAuthenticated(ClaimsPrincipal user)
        => user.Identity?.IsAuthenticated == true
           && !string.IsNullOrWhiteSpace(GetAccessToken(user));

    public string? GetAccessToken(ClaimsPrincipal user)
        => user.FindFirstValue(AuthClaimTypes.AccessToken);
}
