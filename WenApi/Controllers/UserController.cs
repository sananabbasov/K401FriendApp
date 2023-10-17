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
    public class UserController : ControllerBase
    {


        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpContextAccessor _contextAccessor;

        public UserController(AppDbContext context, IHttpContextAccessor contextAccessor)
        {
            _context = context;
            _contextAccessor = contextAccessor;
        }

        [HttpPost("follow/{userIdToFollow}")]
        [Authorize]
        public async Task<IActionResult> FollowUser(string userIdToFollow)
        {
         

            var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            var handler = new JwtSecurityTokenHandler();
            var jwtSecurityToken = handler.ReadJwtToken(_bearer_token);
            var userId = jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier).Value;

            var currentUser = _context.Users.FirstOrDefault(x=>x.Id == userId);


            if (currentUser.Id == userIdToFollow)
            {
                // You can't follow yourself
                return BadRequest("You can't follow yourself.");
            }

            var followExists = _context.UserFollows.Where(uf =>
                uf.FollowerUserId == currentUser.Id && uf.FollowedUserId == userIdToFollow);

            if (followExists.Any())
            {
                // User is already following this user
                return BadRequest("You are already following this user.");
            }

            var userFollow = new UserFollow
            {
                FollowerUserId = currentUser.Id,
                FollowedUserId = userIdToFollow,
                IsAccepted = false // Set as not accepted initially
            };

            _context.UserFollows.Add(userFollow);
            await _context.SaveChangesAsync();

            return Ok("Follow request sent.");
        }

        [HttpPost("unfollow/{userIdToUnfollow}")]
        [Authorize]
        public async Task<IActionResult> UnfollowUser(string userIdToUnfollow)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var userFollow = _context.UserFollows.FirstOrDefault(uf =>
                uf.FollowerUserId.ToString() == currentUser.Id && uf.FollowedUserId.ToString() == userIdToUnfollow);

            if (userFollow == null)
            {
                return BadRequest("You are not following this user.");
            }

            _context.UserFollows.Remove(userFollow);
            await _context.SaveChangesAsync();

            return Ok("Unfollowed successfully.");
        }


        [HttpPost("acceptfollow/{followerUserId}")]
        [Authorize]
        public async Task<IActionResult> AcceptFollowRequest(string followerUserId)
        {
            var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            var handler = new JwtSecurityTokenHandler();
            var jwtSecurityToken = handler.ReadJwtToken(_bearer_token);
            var userId = jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier).Value;

            var currentUser = _context.Users.FirstOrDefault(x => x.Id == userId);

            var followRequest = _context.UserFollows.FirstOrDefault(uf =>
                uf.FollowerUserId == followerUserId && uf.FollowedUserId == currentUser.Id && !uf.IsAccepted);

            if (followRequest == null)
            {
                return BadRequest("No pending follow request found from this user.");
            }

            followRequest.IsAccepted = true;
            await _context.SaveChangesAsync();

            return Ok("Follow request accepted.");
        }


        [HttpGet("followrequests")]
        [Authorize]
        public IActionResult GetFollowRequests()
        {
            var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            var handler = new JwtSecurityTokenHandler();
            var jwtSecurityToken = handler.ReadJwtToken(_bearer_token);
            var userId = jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier).Value;

            var currentUser = _context.Users.FirstOrDefault(x => x.Id == userId);

            // Find follow requests that are not accepted
            var pendingFollowRequests = _context.UserFollows
                .Where(uf => uf.FollowedUserId == currentUser.Id && !uf.IsAccepted)
                .Select(uf => uf.Follower) // Select the users who sent the requests
                .Select(x=> new {Id=x.Id, UserName=x.UserName})
                .ToList();

            return Ok(pendingFollowRequests);
        }
    }
}
