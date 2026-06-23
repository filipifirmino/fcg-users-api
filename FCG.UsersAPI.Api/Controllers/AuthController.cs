using FCG.UsersAPI.Application.DTOs;
using FCG.UsersAPI.Application.Interfaces;
using FCG.UsersAPI.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCG.UsersAPI.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : BaseController
    {
        private readonly ILogger<AuthController> _logger;
        private readonly IAuthService _authService;

        public AuthController(ILogger<AuthController> logger, IAuthService authService)
        {
            _logger = logger;
            _authService = authService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (!request.IsValid())
            {
                return CustomResponse(Result<LoginResponseDto>.Failure("Invalid request data"));
            }

            var result = await _authService.AuthenticateAsync(request);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed login attempt for user: {RequestUsername}", request.Email);
                return CustomResponse(result, errorStatusCode: StatusCodes.Status401Unauthorized);
            }

            _logger.LogInformation("User {RequestUsername} logged in successfully", request.Email);
            return CustomResponse(result);
        }
    }

}
