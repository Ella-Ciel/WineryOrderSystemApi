using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WineryOrderSystemAPI.Data;
using WineryOrderSystemAPI.DTOs;
using WineryOrderSystemAPI.Models;
using WineryOrderSystemAPI.Services;

namespace WineryOrderSystemAPI.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly WineryDbContext _context;
        private readonly TokenService _tokenService;
        private readonly IMapper _mapper;

        public AuthController(WineryDbContext context, TokenService tokenService, IMapper mapper)
        {
            _context = context;
            _tokenService = tokenService;
            _mapper = mapper;
        }

        // POST: api/v1/Auth/register
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest("Email already registered.");

            var (hash, salt) = PasswordHelper.HashPassword(dto.Password);

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Phone = dto.Phone,
                Address = dto.Address,
                PasswordHash = hash,
                PasswordSalt = salt,
                RoleId = 2,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await _context.Entry(user).Reference(u => u.Role).LoadAsync();

            var response = _mapper.Map<AuthResponseDto>(user);
            response.Token = _tokenService.GenerateToken(user);

            return Ok(response);
        }

        // POST: api/v1/Auth/login
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null || !PasswordHelper.VerifyPassword(dto.Password, user.PasswordHash, user.PasswordSalt))
                return Unauthorized("Invalid email or password.");

            var response = _mapper.Map<AuthResponseDto>(user);
            response.Token = _tokenService.GenerateToken(user);

            return Ok(response);
        }
    }
}