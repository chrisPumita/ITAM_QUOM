using System.Net;
using ITAM.Shared.Dtos.Apis;
using Microsoft.AspNetCore.Mvc;

namespace ITAM.Api.Controllers;

/// <summary>
/// Mapea <see cref="Result{T}"/> → HTTP + <see cref="ApiResponse{T}"/>.
/// El status del response y el campo <c>code</c> del body siempre coinciden.
/// </summary>
internal static class ApiResponseFactory
{
    /// <summary>
    /// Éxito: usa <paramref name="successCode"/> (200, 201…).
    /// Error: NotFound→404, Duplicate→409, resto→400.
    /// </summary>
    public static ActionResult<ApiResponse<T>> FromResult<T>(Result<T> result, HttpStatusCode successCode)
    {
        if (!result.IsSuccess)
        {
            var errorCode = result.Error switch
            {
                "NotFound" => HttpStatusCode.NotFound,
                "Duplicate" => HttpStatusCode.Conflict,
                _ => HttpStatusCode.BadRequest
            };
            return Status(result, errorCode);
        }

        return Status(result, successCode);
    }

    public static ActionResult<ApiResponse<T>> InvalidModel<T>() =>
        Status(new Result<T>
        {
            IsSuccess = false,
            Message = "Datos no válidos.",
            Error = "ModelState inválido"
        }, HttpStatusCode.BadRequest);

    private static ActionResult<ApiResponse<T>> Status<T>(Result<T> result, HttpStatusCode code) =>
        new ObjectResult(new ApiResponse<T>
        {
            Code = code,
            Message = result.Message,
            Error = result.Error,
            Data = result.Data
        })
        {
            StatusCode = (int)code
        };
}
