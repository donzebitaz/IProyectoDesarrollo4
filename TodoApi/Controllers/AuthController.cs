using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using TodoApi.Models;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace TodoApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        public readonly UserManager<IdentityUser> _userManager;
        public readonly IConfiguration _configuration;

        public AuthController (UserManager<IdentityUser> userManager, IConfiguration configuration)
        {
           _userManager = userManager;
           _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<ActionResult> Register(Credentials credentials)
        {
            // JSON-like object initializer
            var user = new IdentityUser() {UserName = credentials.Username};
            var result = await _userManager.CreateAsync(user, credentials.Password);

            if(!result.Succeeded)
            {
                // Returned when an error occurs
                return BadRequest(result.Errors.Select(e => e.Description));
            }

            return StatusCode(StatusCodes.Status201Created, new {message = "User created successfully"}); // Created (201)
        }

        [HttpPost("login")]
        public async Task<ActionResult> Login(Credentials credentials)
        {
            // Check that the user exists
            var user = await _userManager.FindByNameAsync(credentials.Username);
            if(user == null)
                return Unauthorized("Invalid username or password"); // Same message in both cases for security reasons

            // This check takes the plain-text password, concatenates the security stamp,
            // processes it, and the result must match the stored hash
            var passwordValid = await _userManager.CheckPasswordAsync(user, credentials.Password);
            if(!passwordValid) 
                return Unauthorized("Invalid username or password");

            // A success message or a token can be returned
            // The token avoids having to log in every time, because it keeps the session open
            // Before: return Ok(new {message = "Login successful"});, now with the token:

            var jwtSettings = _configuration.GetSection("Jwt");
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));    
            var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
           
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName!)
            };

            // The ! tells the compiler to trust that the value will not be null
            var expireInMinutes = double.Parse(jwtSettings["ExpireInMinutes"]!);

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expireInMinutes), // Keeps the session open
                signingCredentials: signingCredentials
            );

            return Ok(new {token = new JwtSecurityTokenHandler().WriteToken(token)});
        }
    }
}