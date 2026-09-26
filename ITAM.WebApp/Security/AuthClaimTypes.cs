namespace ITAM.WebApp.Security;

/// <summary>Claims propios de la sesión MVC (además de ClaimTypes estándar).</summary>
public static class AuthClaimTypes
{
    /// <summary>JWT de la API para llamadas ApiConnect (Bearer).</summary>
    public const string AccessToken = "access_token";

    /// <summary>Expiración UTC del JWT (ISO-8601).</summary>
    public const string TokenExpiresUtc = "token_expires_utc";
}
