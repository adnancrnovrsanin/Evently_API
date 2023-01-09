using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IUserAccessor _userAccessor;

        public AdminController(DataContext context, IUserAccessor userAccessor)
        {
            _context = context;
            _userAccessor = userAccessor;
        }

        [HttpGet("allUsers")]
        public async Task<ActionResult<List<AppUser>>> GetAllUsers()
        {
            if (_userAccessor.GetUsername() != "admin") return Unauthorized();
            var users = await _context.Users.ToListAsync();
            if (users == null) return NotFound();
            return users;
        }

        [HttpDelete("deleteUser/{id}")]
        public async Task<ActionResult> DeleteUser(string id)
        {
            if (_userAccessor.GetUsername() != "admin") return Unauthorized();

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("reportedUsers")]
        public async Task<ActionResult<List<AppUser>>> GetReportedUsers()
        {
            if (_userAccessor.GetUsername() != "admin") return Unauthorized();
            var users = await _context.Users.Where(x => x.Reported).ToListAsync();
            if (users == null) return NotFound();
            return users;
        }
    }
}