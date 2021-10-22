using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Fake.API.Dtos;
using Fake.API.Models;
using Fake.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Fake.API.Controllers
{
    [Route("api/auth/")]
    [ApiController]
    public class AuthenticateController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ITouristRouteRepository _touristRouteRepository;

        public AuthenticateController(
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ITouristRouteRepository touristRouteRepository
        )
        {
            _configuration = configuration;
            _userManager = userManager;
            _signInManager = signInManager;
            _touristRouteRepository = touristRouteRepository;
        }


        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync([FromBody] LoginDto loginDto)
        {
            //1. 驗證帳號密碼
            var loginResult = await _signInManager.PasswordSignInAsync(
                loginDto.Email,
                loginDto.Password,
                false,
                false
            );

            if (!loginResult.Succeeded) return BadRequest();

            var user = await _userManager.FindByNameAsync(loginDto.Email);

            //2. 產jwt  (header -> payload -> signature)
            //header
            var signingAlgorithm = SecurityAlgorithms.HmacSha256;
            //payload
            var claims = new List<Claim>
            {
                //sub
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                //new Claim(ClaimTypes.Role, "Admin")
            };

            var roleNames = await _userManager.GetRolesAsync(user);//用戶角色列表
            foreach(var roleName in roleNames)
            {
                var roleClaim = new Claim(ClaimTypes.Role, roleName);
                claims.Add(roleClaim);
            }

            //signature
            var secretByte = Encoding.UTF8.GetBytes(_configuration["Authentication:SecretKey"]);
            var signingKey = new SymmetricSecurityKey(secretByte); //非對稱式算法
            var signingCredentails = new SigningCredentials(signingKey, signingAlgorithm);

            var token = new JwtSecurityToken(
                issuer: _configuration["Authentication:Issuer"],
                audience: _configuration["Authentication:Audience"],
                claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentails
            );

            //以字符串的形式輸出token
            var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);

            //3. return 200 OK + jwt
            return Ok(tokenStr);
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> RegisterAsync([FromBody] RegisterDto registerDto)
        {
            //1. 使用用戶名，創建用戶對象
            var user = new ApplicationUser()
            {
                UserName = registerDto.Email,
                Email = registerDto.Email
            };

            //2. hash密碼，保存用戶
            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded) return BadRequest();

            //3. 購物車初始化
            var shoppingCart = new ShoppingCart()
            {
                Id = Guid.NewGuid(),
                UserId = user.Id
            };

            await _touristRouteRepository.CreateShoppingCartAsync(shoppingCart);
            await _touristRouteRepository.SaveAsync();


            //4. return
            return Ok();
        }
    }
}
