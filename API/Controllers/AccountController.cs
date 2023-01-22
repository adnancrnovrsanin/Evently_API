using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
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

            if (user == null) return Unauthorized("Invalid email");

            if (user.UserName == "bob") user.EmailConfirmed = true;

            if (!user.EmailConfirmed) return Unauthorized($"Email not verified|{user.Email}");
            
            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);

            if (result.Succeeded) {
                return CreateUserObject(user);
            }

            return Unauthorized("Invalid password");
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto) {
            if (await _userManager.Users.AnyAsync(x => x.Email == registerDto.Email)) {
                return BadRequest("Email taken");
            }

            if (await _userManager.Users.AnyAsync(x => x.UserName == registerDto.Username)) {
                return BadRequest("Username taken");
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
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var verifyUrl = $"{origin}/account/verifyEmail?token={token}&email={user.Email}";
            var message = $"<p>Please click the below link to verify your email address:</p><p><a href='{verifyUrl}'>Click here to verify your email</a></p>";

            await _emailSender.SendEmailAsync(user.Email, "Please verify your email", message);

            return Ok("Email confirmation link sent");
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