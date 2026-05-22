using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TaskApi.Dto.Auth;
using TaskApi.Exceptions;
using TaskApi.Services;

namespace TaskApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IRefreshTokenService _refreshTokenService;

        public AuthController(
            IAuthService authService,
            IRefreshTokenService refreshTokenService)
        {
            _authService = authService;
            _refreshTokenService = refreshTokenService;
        }

        /// <summary>
        /// Вхід у систему
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AuthResponse>> Login(
            [FromBody] AuthRequest request)
        {
            try
            {
                var response = await _authService.AuthenticateAsync(request);
                return Ok(response);
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new { message = ex.Message, type = "UnauthorizedError" });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message, type = "ValidationError" });
            }
        }

        /// <summary>
        /// Реєстрація нового користувача
        /// </summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AuthResponse>> Register(
            [FromBody] RegisterRequest request)
        {
            try
            {
                var response = await _authService.RegisterAsync(request);
                return CreatedAtAction(nameof(GetCurrentUser),
                    new { id = response.User.Id }, response);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message, type = "ValidationError" });
            }
        }

        /// <summary>
        /// Отримання даних поточного користувача
        /// </summary>
        [Authorize]
        [HttpGet("me")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                               ?? User.FindFirst(JwtRegisteredClaimNames.Sub);

                if (userIdClaim == null)
                    return Unauthorized();

                var userId = int.Parse(userIdClaim.Value);
                var user = await _authService.GetCurrentUserDataAsync(userId);

                if (user == null)
                    return NotFound();

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Оновлення токенів через refresh токен
        /// </summary>
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AuthResponse>> RefreshToken(
            [FromBody] RefreshTokenRequest request)
        {
            try
            {
                var response = await _refreshTokenService.RefreshAsync(request.RefreshToken);
                return Ok(response);
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new { message = ex.Message, type = "UnauthorizedError" });
            }
        }

        /// <summary>
        /// Вихід з системи (відкликання токена)
        /// </summary>
        [Authorize]
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
        {
            try
            {
                await _refreshTokenService.RevokeAsync(request.RefreshToken);
                return Ok(new { message = "Logged out successfully" });
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Вихід з усіх пристроїв
        /// </summary>
        [Authorize]
        [HttpPost("logout-all")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> LogoutAll()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                           ?? User.FindFirst(JwtRegisteredClaimNames.Sub);

            if (userIdClaim == null)
                return Unauthorized();

            var userId = int.Parse(userIdClaim.Value);
            await _refreshTokenService.RevokeAllAsync(userId);

            return Ok(new { message = "Logged out from all devices successfully" });
        }
    }
}