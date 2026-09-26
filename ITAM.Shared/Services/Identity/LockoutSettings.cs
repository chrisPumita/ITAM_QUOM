namespace ITAM.Shared.Services.Identity;

public class LockoutSettings
{
    public const string SectionName = "LockoutSettings";

    public int MaxFailedAccessAttempts { get; set; } = 3;

    public int DefaultLockoutMinutes { get; set; } = 15;

    public bool AllowedForNewUsers { get; set; } = true;
}
