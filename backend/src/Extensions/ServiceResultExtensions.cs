using Microsoft.AspNetCore.Mvc;

using Parlamento.Application.Abstractions;

namespace backend.Extensions;

public static class ServiceResultExtensions
{
    public static IActionResult ToActionResult<T>(this ControllerBase controller, ServiceResult<T> result)
    {
        if (result.IsSuccess)
        {
            return controller.Ok(result.Value);
        }

        if (result.StatusCode == 404)
        {
            return controller.NotFound(result.ErrorMessage);
        }

        return controller.StatusCode(result.StatusCode, result.ErrorMessage);
    }
}
