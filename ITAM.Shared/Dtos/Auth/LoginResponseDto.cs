namespace ITAM.Shared.Dtos.Auth;

/// <summary>
/// Response de la API. Extiende LoginResultDto con JWT y expiración.
/// </summary>
public class LoginResponseDto : LoginResultDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiraEn { get; set; }
}
