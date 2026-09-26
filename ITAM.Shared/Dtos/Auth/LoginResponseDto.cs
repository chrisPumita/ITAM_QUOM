namespace ITAM.Shared.Dtos.Auth;

public class LoginResponseDto : LoginResultDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiraEn { get; set; }
}
