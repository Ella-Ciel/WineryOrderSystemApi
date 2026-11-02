using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using WineryOrderSystemAPI.Data;
using WineryOrderSystemAPI.DTOs.WineryOrderSystemAPI.DTOs;
using WineryOrderSystemAPI.Models;

namespace WineryOrderSystemAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly WineryDbContext _context;

        public AuthController(WineryDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserRegisterDto request)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                return BadRequest("Email address is already in use.");

            CreatePasswordHash(request.Password, out string passwordHash, out string passwordSalt);

            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                Phone = request.Phone,
                Address = request.Address,
                RoleId = 2 // System default set to 2 (Standard Customer)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Registration successful." });
        }

        [HttpPost("user-login")]
        public async Task<IActionResult> UserLogin(UserLoginDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || !VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt))
                return Unauthorized("Invalid email or password.");

            if (user.RoleId == 1)
                return Unauthorized("Administrators must log in using the Admin Portal.");

            return Ok(new { Message = "User authentication successful.", UserId = user.UserId, RoleId = user.RoleId });
        }

        [HttpPost("admin-login")]
        public async Task<IActionResult> AdminLogin(UserLoginDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || !VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt))
                return Unauthorized("Invalid email or password.");

            if (user.RoleId != 1)
                return Unauthorized("Access denied. Administrator rights required.");

            return Ok(new { Message = "Admin authentication successful.", UserId = user.UserId, RoleId = user.RoleId });
        }

        private void CreatePasswordHash(string password, out string passwordHash, out string passwordSalt)
        {
            using (var hmac = new HMACSHA512())
            {
                passwordSalt = Convert.ToBase64String(hmac.Key);
                passwordHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
            }
        }

        private bool VerifyPasswordHash(string password, string storedHash, string storedSalt)
        {
            var saltBytes = Convert.FromBase64String(storedSalt);
            using (var hmac = new HMACSHA512(saltBytes))
            {
                var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(computedHash) == storedHash;
            }
        }
    }
}