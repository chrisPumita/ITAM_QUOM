using System.Net;
using ITAM.Shared.Dtos.Apis;

namespace ITAM.Api.Controllers;

/// <summary>
/// Mapea <see cref="Result{T}"/> del servicio a <see cref="ApiResponse{T}"/> de la API.
/// Controllers delgados: solo HTTP + roles; sin lógica de negocio.
/// </summary>
internal static class ApiResponseFactory
{
    public static ApiResponse<T> From<T>(Result<T> result, HttpStatusCode code) => new()
    {
        Code = code,
        Message = result.Message,
        Error = result.Error,
        Data = result.Data
    };

    public static ApiResponse<T> InvalidModel<T>() => new()
    {
        Code = HttpStatusCode.BadRequest,
        Message = "Datos no válidos.",
        Error = "ModelState inválido"
    };
}
