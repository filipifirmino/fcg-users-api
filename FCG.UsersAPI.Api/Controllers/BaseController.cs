using FCG.UsersAPI.Api.Common;
using FCG.UsersAPI.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace FCG.UsersAPI.Api.Controllers
{
    [ApiController]
    public class BaseController : ControllerBase
    {
        protected IActionResult CustomResponse<T>(Result<T> result,
            int successStatusCode = StatusCodes.Status200OK,
            int errorStatusCode = StatusCodes.Status400BadRequest
        )
        {
            if (result.IsSuccess)
                return StatusCode(successStatusCode, ApiResponse<T>.SuccessResponse(result.Value!));

            var errorResponse = ApiResponse<T>.ErrorResponse(result.Errors);
            return StatusCode(errorStatusCode, errorResponse);
        }
    }
}
