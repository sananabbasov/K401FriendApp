using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using WebApi.Data;
using WebApi.Models;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PhotoController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public PhotoController(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost("upload")]
        [Authorize]
        public async Task<IActionResult> UploadPhoto(IFormFile photoFile)
        {
            if (photoFile == null || photoFile.Length == 0)
            {
                return BadRequest("Invalid file.");
            }

            var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            var handler = new JwtSecurityTokenHandler();
            var jwtSecurityToken = handler.ReadJwtToken(_bearer_token);
            var userId = jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier).Value;

            var currentUser = _context.Users.FirstOrDefault(x => x.Id == userId);

            // Save the photo to a location (e.g., file system or cloud storage)
            // Obtain the photo URL
            string photoUrl = "https://example.com/photos/your-photo.jpg"; // Replace with your URL logic

            var photo = new Photo
            {
                PhotoUrl = photoUrl,
                UserId = currentUser.Id
            };

            _context.Photos.Add(photo);
            await _context.SaveChangesAsync();

            return Ok("Photo uploaded successfully.");
        }



        [HttpGet("feed")]
        [Authorize]
        public async Task<IActionResult> GetPhotoFeed()
        {
            var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            var handler = new JwtSecurityTokenHandler();
            var jwtSecurityToken = handler.ReadJwtToken(_bearer_token);
            var userId = jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier).Value;

            var currentUser = _context.Users.FirstOrDefault(x => x.Id == userId);

            var followerIds = _context.UserFollows
                .Where(uf => uf.FollowedUserId == currentUser.Id && uf.IsAccepted)
                .Select(uf => uf.FollowerUserId)
                .ToList();

            followerIds.Add(currentUser.Id); // Include the current user

            var photos = _context.Photos
                .Include(x=>x.User)
                .Where(p => followerIds.Contains(p.UserId))
                .Select(x=> new {UserName=x.User.UserName, PhotoUrl=x.PhotoUrl})
                .ToList();

            return Ok(photos);
        }



    }
}
