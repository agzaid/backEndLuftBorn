using AngularApi.Configuration;
using AngularApi.Models.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AngularApi.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class AuthManagementController : Controller
    {
        private readonly ILogger<AuthManagementController> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IOptionsMonitor<JwtConfig> _optionsMonitor;
        private readonly IConfiguration _configuration;

        public AuthManagementController(ILogger<AuthManagementController> logger, UserManager<IdentityUser> userManager
            , IOptionsMonitor<JwtConfig> optionsMonitor, IConfiguration configuration)
        {
            _logger = logger;
            _userManager = userManager;
            _optionsMonitor = optionsMonitor;
            _configuration = configuration;
        }

        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> Register([FromBody] UserRegistrationRequestDto requestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid request");
            }
            var emailExist = await _userManager.FindByEmailAsync(requestDto.Email);
            if (emailExist != null)
            {
                return BadRequest("email already exists");
            }
            var newUser = new IdentityUser()
            {
                Email = requestDto.Email,
                UserName = requestDto.Name
            };

            var isCreated = await _userManager.CreateAsync(newUser, requestDto.Password);
            if (isCreated.Succeeded)
            {
                var token = GenerateJWTToken(newUser);
                return Ok(new RegistrationRequestResponse()
                {
                    Result = true,
                    Token = token
                });
            }
            else
                return BadRequest(isCreated.Errors.Select(s => s.Description).ToList());
        }

        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login([FromBody] UserLoginRequestDto requestDto)
        {
            if (ModelState.IsValid)
            {
                var existinguser = await _userManager.FindByEmailAsync(requestDto.Email);
                if (existinguser == null)
                {
                    return BadRequest("Invalid authentication");
                }
                var isPasswordValid = await _userManager.CheckPasswordAsync(existinguser, requestDto.Password);
                if (isPasswordValid)
                {
                    var token = GenerateJWTToken(existinguser);
                    return Ok(new LoginRequestResponse()
                    {
                        Token = token,
                        Result = true
                    });
                }
                else
                    return BadRequest("Invalid authentication");
            }
            else
                return BadRequest("Model Is not valid");
        }


        private string GenerateJWTToken(IdentityUser user)
        {
            //var jwtTokenHandler = new JwtSecurityTokenHandler();
            //var key = Encoding.ASCII.GetBytes(_configuration.GetSection("JwtConfig:Secret").Value);
            ////var key = Encoding.ASCII.GetBytes(builder.Configuration.GetSection("JwtConfig:Secret").Value);
            //var tokenDescriptor = new SecurityTokenDescriptor()
            //{
            //    Subject = new ClaimsIdentity(new[]
            //    {
            //        new Claim("Id",user.Id),
            //        new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            //        new Claim(JwtRegisteredClaimNames.Email, user.Email),
            //        new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString())
            //    }),
            //    Expires = DateTime.Now.AddMinutes(120),
            //    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha512),

            //};
            //var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            //var jwtToken = jwtTokenHandler.WriteToken(token);
            //return jwtToken;

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtConfig:Secret"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(_configuration["JwtConfig:Issuer"],
              _configuration["JwtConfig:Issuer"],
              null,
              expires: DateTime.Now.AddMinutes(120),
              signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
