namespace ITAM.Shared.Services.Identity;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    public int ExpirationHours { get; set; } = 12;
}
