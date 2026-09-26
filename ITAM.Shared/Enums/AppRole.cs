namespace ITAM.Shared.Enums;

public enum AppRole
{
    Administrador = 1,
    Operador = 2
}

public static class AppRoles
{
    public const string Administrador = nameof(AppRole.Administrador);
    public const string Operador = nameof(AppRole.Operador);

    public static readonly string[] All = [Administrador, Operador];
}
