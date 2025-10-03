using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RestaurantManagementSystem.Utilities;
using RestaurantManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;

namespace RestaurantManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthUtilsController : ControllerBase
    {
        private readonly PasswordResetTool _passwordResetTool;
        private readonly IAuthService _authService;
        private readonly ILogger<AuthUtilsController> _logger;

        public AuthUtilsController(
            PasswordResetTool passwordResetTool,
            IAuthService authService,
            ILogger<AuthUtilsController> logger)
        {
            _passwordResetTool = passwordResetTool;
            _authService = authService;
            _logger = logger;
        }

        [HttpGet("check-connection")]
        public async Task<IActionResult> CheckConnection()
        {
            var result = await _passwordResetTool.CheckConnection();
            return Ok(new { Success = result, Message = result ? "Connection successful" : "Connection failed" });
        }

        [HttpGet("check-hash/{username}")]
        public async Task<IActionResult> CheckHash(string username)
        {
            var hash = await _passwordResetTool.GetUserPasswordHash(username);
            
            if (string.IsNullOrEmpty(hash))
            {
                return NotFound(new { Message = $"No hash found for user {username}" });
            }
            
            return Ok(new { 
                Hash = hash,
                Length = hash.Length,
                Prefix = hash.Length > 10 ? hash.Substring(0, 10) : hash,
                IsBcryptFormat = hash.StartsWith("$2a$") || hash.StartsWith("$2b$") || hash.StartsWith("$2y$")
            });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.NewPassword))
            {
                return BadRequest(new { Message = "Username and new password are required" });
            }
            
            var result = await _passwordResetTool.ResetUserPassword(request.Username, request.NewPassword);
            
            if (!result)
            {
                return NotFound(new { Message = $"Failed to reset password for user {request.Username}" });
            }
            
            return Ok(new { Message = $"Password reset successful for {request.Username}" });
        }

        [HttpPost("test-auth")]
        public async Task<IActionResult> TestAuth([FromBody] LoginRequest request)
        {
            try
            {
                _logger.LogInformation("Testing authentication for {Username}", request.Username);
                var result = await _passwordResetTool.TestPasswordVerification(request.Username, request.Password);
                return Ok(new { Success = result, Message = result ? "Authentication successful" : "Authentication failed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing authentication");
                return StatusCode(500, new { Message = "An error occurred during authentication testing", Error = ex.Message });
            }
        }
    }

    public class ResetPasswordRequest
    {
        public string Username { get; set; }
        public string NewPassword { get; set; }
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
