using Application.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
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
        private readonly IMapper _mapper;
        private readonly DataContext _context;
        private readonly IUserAccessor _userAccessor;
        private readonly IPhotoAccessor _photoAccessor;

        public AdminController(DataContext context, IUserAccessor userAccessor, IMapper mapper, IPhotoAccessor photoAccessor)
        {
            _mapper = mapper;
            _context = context;
            _userAccessor = userAccessor;
            _photoAccessor = photoAccessor;
        }

        [HttpGet("allUsers")]
        public async Task<ActionResult<List<AppUser>>> GetAllUsers()
        {
            if (_userAccessor.GetUsername() != "admin") return Unauthorized();
            var users = await _context.Users.ToListAsync();
            if (users == null) return NotFound();
            return users;
        }

        [HttpDelete("deleteUser/{username}")]
        public async Task<ActionResult> DeleteUser(string username)
        {
            if (_userAccessor.GetUsername() != "admin") return Unauthorized();

            var user = await _context.Users.Include(p => p.Photos).SingleOrDefaultAsync(x => x.UserName == username);
            var usersEvents = await _context.Events.Where(x => x.Attendees.SingleOrDefault(a => a.IsHost).AppUser.UserName == username).ToListAsync();
            var usersComments = await _context.Comments.Where(x => x.Author.UserName == username).ToListAsync();
            if (user == null)
            {
                return NotFound();
            }

            foreach(var photo in user.Photos)
                await _photoAccessor.DeletePhoto(photo.Id);
    
            _context.Comments.RemoveRange(usersComments);
            _context.Events.RemoveRange(usersEvents);
            _context.Remove(user);
            
            var result = await _context.SaveChangesAsync() > 0;

            if (!result) return BadRequest("Failed to delete user");

            return NoContent();
        }

        [HttpGet("reportedUsers")]
        public async Task<ActionResult<List<Application.Profiles.Profile>>> GetReportedUsers()
        {
            if (_userAccessor.GetUsername() != "admin") return Unauthorized();

            List<Application.Profiles.Profile> users = await _context.Users.Where(x => x.Reported)
                .ProjectTo<Application.Profiles.Profile>(_mapper.ConfigurationProvider, new { currentUsername = _userAccessor.GetUsername() })
                .ToListAsync();

            if (users == null) return NotFound();
            
            return users;
        }
    }
}