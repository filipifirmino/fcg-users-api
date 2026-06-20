using FCG.UsersAPI.Api.Authorization;
using FCG.UsersAPI.Application.DTOs;
using FCG.UsersAPI.Application.Interfaces;
using FCG.UsersAPI.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCG.UsersAPI.Api.Controllers
{
    /// <summary>
    /// Gerenciamento de usuários da plataforma FCG Games.
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    public class UserController : BaseController
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Registra um novo usuário na plataforma.
        /// </summary>
        /// <param name="request">Dados de registro do usuário (nome, e-mail e senha).</param>
        /// <returns>Dados do usuário criado.</returns>
        /// <response code="200">Usuário registrado com sucesso.</response>
        /// <response code="400">Dados inválidos ou usuário já cadastrado.</response>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(RegisterResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Register([FromBody] RegisterRequestDto request)
        {
            var result = await _userService.RegisterAsync(request);

            if (!result!.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result.Data);
        }

        /// <summary>
        /// Retorna todos os usuários cadastrados. Restrito a administradores.
        /// </summary>
        /// <returns>Lista de usuários.</returns>
        /// <response code="200">Lista de usuários retornada com sucesso.</response>
        /// <response code="401">Não autenticado.</response>
        /// <response code="403">Acesso negado — requer perfil de administrador.</response>
        [HttpGet("get-all")]
        [Authorize(Policy = Policies.AdminOnly)]
        [ProducesResponseType(typeof(IEnumerable<User>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> GetUsersAsync()
        {
            var result = await _userService.GetAll();
            return Ok(result);
        }

        /// <summary>
        /// Retorna um usuário pelo identificador. Restrito a administradores.
        /// </summary>
        /// <param name="id">Identificador único do usuário.</param>
        /// <returns>Dados do usuário encontrado.</returns>
        /// <response code="200">Usuário encontrado e retornado.</response>
        /// <response code="401">Não autenticado.</response>
        /// <response code="403">Acesso negado — requer perfil de administrador.</response>
        [HttpGet("get-by-id")]
        [Authorize(Policy = Policies.AdminOnly)]
        [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> GetUserByIdAsync([FromHeader] Guid id)
        {
            var result = await _userService.GetById(id);
            return Ok(result);
        }

        /// <summary>
        /// Atualiza os dados de um usuário. Permitido ao próprio usuário ou administrador.
        /// </summary>
        /// <param name="id">Identificador único do usuário a ser atualizado.</param>
        /// <param name="request">Campos a atualizar (nome e status ativo).</param>
        /// <returns>Usuário com dados atualizados.</returns>
        /// <response code="200">Usuário atualizado com sucesso.</response>
        /// <response code="401">Não autenticado.</response>
        /// <response code="403">Acesso negado.</response>
        [HttpPatch("update-by-id")]
        [Authorize(Policy = Policies.UserOrAdmin)]
        [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> UpdateUserAsync([FromBody] UpdateUserDto request, [FromQuery] Guid id)
        {
            var result = await _userService.Update(id, request);
            return Ok(result);
        }
    }
}
