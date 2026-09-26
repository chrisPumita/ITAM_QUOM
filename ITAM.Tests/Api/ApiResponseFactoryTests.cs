using System.Net;
using ITAM.Api.Controllers;
using ITAM.Shared.Dtos.Apis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ITAM.Tests.Api;

public class ApiResponseFactoryTests
{
    [Fact]
    public void FromResult_WhenSuccess_UsesProvidedStatusCode()
    {
        var result = new Result<int>
        {
            IsSuccess = true,
            Message = "Creado",
            Data = 1
        };

        var action = ApiResponseFactory.FromResult(result, HttpStatusCode.Created);
        var objectResult = Assert.IsType<ObjectResult>(action.Result);
        var body = Assert.IsType<ApiResponse<int>>(objectResult.Value);

        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);
        Assert.Equal(HttpStatusCode.Created, body.Code);
        Assert.True(body.IsSuccess);
        Assert.Equal(1, body.Data);
    }

    [Fact]
    public void FromResult_WhenNotFound_Returns404()
    {
        var result = new Result<string>
        {
            IsSuccess = false,
            Message = "No existe",
            Error = "NotFound"
        };

        var action = ApiResponseFactory.FromResult(result, HttpStatusCode.OK);
        var objectResult = Assert.IsType<ObjectResult>(action.Result);

        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
    }

    [Fact]
    public void FromResult_WhenDuplicate_Returns409()
    {
        var result = new Result<int>
        {
            IsSuccess = false,
            Message = "Duplicado",
            Error = "Duplicate"
        };

        var action = ApiResponseFactory.FromResult(result, HttpStatusCode.Created);
        var objectResult = Assert.IsType<ObjectResult>(action.Result);
        var body = Assert.IsType<ApiResponse<int>>(objectResult.Value);

        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, body.Code);
        Assert.False(body.IsSuccess);
    }

    [Fact]
    public void FromResult_WhenValidation_Returns400()
    {
        var result = new Result<bool>
        {
            IsSuccess = false,
            Message = "Inválido",
            Error = "Validation"
        };

        var action = ApiResponseFactory.FromResult(result, HttpStatusCode.OK);
        var objectResult = Assert.IsType<ObjectResult>(action.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
    }
}
