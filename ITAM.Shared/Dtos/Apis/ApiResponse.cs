using System.Net;

namespace ITAM.Shared.Dtos.Apis;

public class ApiResponse<T> : Result<T>
{
    public HttpStatusCode Code { get; set; }
    public new bool IsSuccess => Code is >= HttpStatusCode.OK and < HttpStatusCode.BadRequest;
}
