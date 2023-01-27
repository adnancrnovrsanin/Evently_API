using System.Security.Claims;
using System.Text;
using API.DTOs;
using API.Services;
using Domain;
using Infrastructure.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly EmailSender _emailSender;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly TokenService _tokenService;
        
        public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, TokenService tokenService, EmailSender emailSender)
        {
            _tokenService = tokenService;
            _signInManager = signInManager;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto) {
            var user = await _userManager.Users.Include(p => p.Photos)
                .FirstOrDefaultAsync(x => x.Email == loginDto.Email);

            if (user == null) return Unauthorized("Email not found");

            if (user.UserName == "bob") user.EmailConfirmed = true;

            if (!user.EmailConfirmed) return Unauthorized($"Email not verified|{user.Email}");
            
            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);

            if (result.Succeeded) {
                return CreateUserObject(user);
            }

            return Unauthorized("Invalid password");
        }

        [HttpPost("googleAuth")]
        public async Task<ActionResult<UserDto>> GoogleAuth(GoogleAuthDto googleAuthDto) {
            var user = await _userManager.Users.Include(p => p.Photos)
                .FirstOrDefaultAsync(x => x.Email == googleAuthDto.Email);

            if (user == null) {

                string tempUsername = googleAuthDto.Username;
                while (true)
                {
                    var existingUser = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == tempUsername);
                    if (existingUser == null)
                    {
                        googleAuthDto.Username = tempUsername;
                        break;
                    }
                    tempUsername = googleAuthDto.Username + Guid.NewGuid().ToString().Substring(0, 4);
                }

                user = new AppUser {
                    DisplayName = googleAuthDto.DisplayName,
                    Email = googleAuthDto.Email,
                    UserName = googleAuthDto.Username,
                    EmailConfirmed = true,
                };

                var result = await _userManager.CreateAsync(user, googleAuthDto.GoogleId);

                if (!result.Succeeded) return BadRequest("Problem registering user");
            }

            var resultAuth = await _signInManager.CheckPasswordSignInAsync(user, googleAuthDto.GoogleId, false);

            if (!resultAuth.Succeeded) return BadRequest("Problem signing in user with Google");

            return CreateUserObject(user);
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto) {
            if (await _userManager.Users.AnyAsync(x => x.Email == registerDto.Email)) {
                ModelState.AddModelError("email", "Email taken");
                return ValidationProblem();
            }

            if (await _userManager.Users.AnyAsync(x => x.UserName == registerDto.Username)) {
                ModelState.AddModelError("username", "Username taken");
                return ValidationProblem();
            }

            var user = new AppUser {
                DisplayName = registerDto.DisplayName,
                Email = registerDto.Email,
                UserName = registerDto.Username
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded) return BadRequest("Problem registering user");

            var origin = Request.Headers["origin"];
            var host = "https://evently.herokuapp.com";
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var verifyUrl = $"{host}/account/verifyEmail?token={token}&email={user.Email}";
            var message = $"<p>Please click the below link to verify your email address:</p><p><a href='{verifyUrl}'>Click here to verify your email</a></p>";

            await _emailSender.SendEmailAsync(user.Email, "Please verify your email", message);

            return Ok("Registration successful, please check your email to verify your account");
        }

        [HttpPost("verifyEmail")]
        public async Task<ActionResult> VerifyEmail(string token, string email) {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null) return Unauthorized("Invalid email");

            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));

            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (result.Succeeded) return Ok("Email verified");

            return BadRequest("Email not verified");
        }

        [HttpGet("resendEmailConfirmationLink")]
        public async Task<ActionResult> ResendEmailConfirmationLink(string email) {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null) return Unauthorized("Invalid email");

            var origin = Request.Headers["origin"];
            var host = "https://evently.herokuapp.com";
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var verifyUrl = $"{host}/account/verifyEmail?token={token}&email={user.Email}";
            var message = $"<p>Please click the below link to verify your email address:</p><p><a href='{verifyUrl}'>Click here to verify your email</a></p>";

            await _emailSender.SendEmailAsync(user.Email, "Please verify your email", message);

            return Ok("Email confirmation link sent");
        }

        [HttpPost("forgotPassword")]
        public async Task<ActionResult> ForgotPassword(string email) {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null) return Unauthorized("Invalid email");

            var origin = Request.Headers["origin"];
            var host = "https://evently.herokuapp.com";
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var resetUrl = $"{host}/account/resetPassword?token={token}&email={user.Email}";
            var message = $"<p>Please click the below link to reset your password:</p><p><a href='{resetUrl}'>Click here to reset your password</a></p>";

            await _emailSender.SendEmailAsync(user.Email, "Reset your password", message);

            return Ok("Password reset link sent");
        }

        [HttpPost("resetPassword")]
        public async Task<ActionResult> ResetPassword(ResetPasswordDto resetPasswordDto) {
            var user = await _userManager.FindByEmailAsync(resetPasswordDto.Email);

            if (user == null) return Unauthorized("Invalid email");

            resetPasswordDto.Token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(resetPasswordDto.Token));

            var result = await _userManager.ResetPasswordAsync(user, resetPasswordDto.Token, resetPasswordDto.Password);

            if (result.Succeeded) return Ok("Password reset successful");

            return BadRequest("Password reset failed");
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<UserDto>> GetCurrentUser() {
            var user = await _userManager.Users.Include(p => p.Photos)
            .FirstOrDefaultAsync(x => x.Email == User.FindFirstValue(ClaimTypes.Email));

            return CreateUserObject(user);
        }

        private UserDto CreateUserObject(AppUser user) {
            return new UserDto {
                DisplayName = user.DisplayName,
                Image = user?.Photos?.FirstOrDefault(x => x.IsMain)?.Url,
                Token = _tokenService.CreateToken(user),
                Username = user.UserName
            };
        }
    }
}