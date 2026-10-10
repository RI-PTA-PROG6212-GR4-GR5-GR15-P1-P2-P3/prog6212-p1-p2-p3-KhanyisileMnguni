using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly RaceDayDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(
            RaceDayDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.Role))
            {
                return BadRequest("Name, email, password and role are required.");
            }

            string role = request.Role.Trim();

            if (!role.Equals("Organiser", StringComparison.OrdinalIgnoreCase) &&
                !role.Equals("Participant", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Role must be Organiser or Participant.");
            }

            bool organiserExists = await _context.Organisers
                .AnyAsync(o => o.Email == request.Email);

            bool participantExists = await _context.Participants
                .AnyAsync(p => p.Email == request.Email);

            if (organiserExists || participantExists)
            {
                return Conflict("Email is already in use.");
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            if (role.Equals("Organiser", StringComparison.OrdinalIgnoreCase))
            {
                var organiser = new Organiser
                {
                    Name = request.Name,
                    Email = request.Email,
                    Password = passwordHash,
                    OrganisationName = request.OrganisationName ?? string.Empty
                };

                _context.Organisers.Add(organiser);
                await _context.SaveChangesAsync();

                return Created("", new
                {
                    userID = organiser.OrganiserID,
                    role = "Organiser"
                });
            }

            var participant = new Participant
            {
                Name = request.Name,
                Email = request.Email,
                Password = passwordHash,
                DateOfBirth = DateTime.MinValue,
                EmergencyContact = string.Empty
            };

            _context.Participants.Add(participant);
            await _context.SaveChangesAsync();

            return Created("", new
            {
                userID = participant.ParticipantID,
                role = "Participant"
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var organiser = await _context.Organisers
                .FirstOrDefaultAsync(o => o.Email == request.Email);

            if (organiser != null &&
                BCrypt.Net.BCrypt.Verify(request.Password, organiser.Password))
            {
                string token = GenerateToken(
                    organiser.OrganiserID,
                    organiser.Email,
                    "Organiser");

                return Ok(new
                {
                    token,
                    role = "Organiser"
                });
            }

            var participant = await _context.Participants
                .FirstOrDefaultAsync(p => p.Email == request.Email);

            if (participant != null &&
                BCrypt.Net.BCrypt.Verify(request.Password, participant.Password))
            {
                string token = GenerateToken(
                    participant.ParticipantID,
                    participant.Email,
                    "Participant");

                return Ok(new
                {
                    token,
                    role = "Participant"
                });
            }

            return Unauthorized("Invalid email or password.");
        }

        private string GenerateToken(int userId, string email, string role)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    _configuration["Jwt:Key"]!));

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}