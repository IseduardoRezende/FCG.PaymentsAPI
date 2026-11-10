using FCG.Payments.Domain.Commons.Result;
using Microsoft.AspNetCore.Mvc;

namespace FCG.Payments.Api.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result, int successStatusCode = StatusCodes.Status200OK)
    {
        return result switch
        {
            SuccessResult<T> => successStatusCode switch
            {
                StatusCodes.Status200OK => new OkObjectResult(result),
                StatusCodes.Status204NoContent => new StatusCodeResult(successStatusCode),
                _ => new ObjectResult(result) { StatusCode = successStatusCode }
            },
            InvalidResult<T> => new UnprocessableEntityObjectResult(result),
            NotFoundResult<T> => new NotFoundObjectResult(result),
            ConflictResult<T> => new ConflictObjectResult(result),
            ForbiddenResult<T> => new ObjectResult(result) { StatusCode = StatusCodes.Status403Forbidden },
            _ => new StatusCodeResult(StatusCodes.Status500InternalServerError)
        };
    }
}
