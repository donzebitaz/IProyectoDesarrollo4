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
            var user = new IdentityUser() {UserName = credentials.Username};//tipo JSON
            //user.UserName = credentials.Username;
            var result = await _userManager.CreateAsync(user, credentials.Password);

            if(!result.Succeeded)
            {
                return BadRequest(result.Errors.Select(e => e.Description)); //esto cuando ocurre un error
            }

            return StatusCode(StatusCodes.Status201Created, new {message = "User created successfully"});//201 created
        }

        [HttpPost("login")]
        public async Task<ActionResult> Login(Credentials credentials)
        {
            var user = await _userManager.FindByNameAsync(credentials.Username);//veo si el usuario es correcto
            if(user == null)
                return Unauthorized("Invalid username or password");//por temas de seguridad

                                                //este check lo que hace es que con el password en texto plano, le concatena el security stamp, lo procesa y tiene que dar el hash
            var passwordValid = await _userManager.CheckPasswordAsync(user, credentials.Password);//veo si la contraseña es correcta
            if(!passwordValid) 
                return Unauthorized("Invalid username or password");

            //se puede devolver un mensaje de éxito o un tóken
            //este tóken consiste en no tener que iniciar sesión todas las veces, sino, dejar la sesión abierta
            //antes teníamos:  return Ok(new {message = "Login seccessfull"});, pero considerando el tóken, queda así:

            var jwtSettings = _configuration.GetSection("Jwt");
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));    
            var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
           
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName!)
            };

            var expireInMinutes = double.Parse(jwtSettings["ExpireInMinutes"]!);//ese ! es para que el sistema confíe en mí de que no va a ser nulo

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expireInMinutes),//seión abierta
                signingCredentials: signingCredentials
            );

            return Ok(new {token = new JwtSecurityTokenHandler().WriteToken(token)});
        }
    }
}