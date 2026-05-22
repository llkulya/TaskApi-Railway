using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskApi.Services;

namespace TaskApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SsoController : ControllerBase
    {
        private readonly ISsoService _ssoService;

        public SsoController(ISsoService ssoService)
        {
            _ssoService = ssoService;
        }

        /// <summary>
        /// Ініціалізація входу через Google
        /// </summary>
        [HttpGet("login/google")]
        public IActionResult LoginWithGoogle()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(
                    nameof(GoogleCallback), "Sso", null, Request.Scheme)
            };

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// Callback після успішної аутентифікації через Google
        /// </summary>
        [HttpGet("callback")]
        public async Task<IActionResult> GoogleCallback()
        {
            try
            {
                // 1. Отримуємо результат від Google через cookie
                var result = await HttpContext.AuthenticateAsync("Cookies");

                if (result == null || !result.Succeeded)
                    return BadRequest(new
                    {
                        message = "Google authentication failed",
                        detail = result?.Failure?.Message ?? "Unknown error"
                    });

                // 2. Читаємо claims — тільки отримуємо дані, логіки немає
                var claims = result.Principal?.Claims.ToList();
                if (claims == null || !claims.Any())
                    return BadRequest(new { message = "No claims received" });

                var googleId = claims.FirstOrDefault(c =>
                    c.Type == ClaimTypes.NameIdentifier)?.Value;
                var email = claims.FirstOrDefault(c =>
                    c.Type == ClaimTypes.Email)?.Value;

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(googleId))
                    return BadRequest(new { message = "Email or GoogleId not received" });

                var response = await _ssoService.HandleGoogleCallbackAsync(
                    email, googleId);

                return Ok(new
                {
                    message = "Google SSO successful",
                    accessToken = response.Token,
                    refreshToken = response.RefreshToken,
                    expiresIn = response.ExpiresIn,
                    user = response.User
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message,
                    type = "ServerError"
                });
            }
        }
    }
}