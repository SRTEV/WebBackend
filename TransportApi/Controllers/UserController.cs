using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TransportApi.Models;
using TransportApi.Services;
using Microsoft.AspNetCore.Authorization;

namespace TransportApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        
        public UserController(AppDbContext context, IConfiguration configuration, IEmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
        }

        // GET: api/User
        [Authorize]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            var users = await _context.Users
                .Include(u => u.Role)
                .ToListAsync();

            return Ok(users);
        }

        // GET: api/User/5
        [Authorize]
            [HttpGet("{id}")]
            public async Task<ActionResult<User>> GetUser(int id)
            {
                var user = await _context.Users.FindAsync(id);
    
                if (user == null)
                {
                    return NotFound();
                }

                return Ok(user);
            }


            [HttpPost("Delete/{id}")]
            [Authorize]
            public async Task<IActionResult> DeleteUser(int id)
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                user.Deleted = true;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "User deleted successfully" });
            }


[HttpPost("login/app")]
public async Task<IActionResult> LoginApp([FromBody] LoginRequest request)
{
    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
    bool isPasswordValid = user != null && BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
 if (user == null || !isPasswordValid)
    {
        return Unauthorized(new { message = "Invalid email or password" });
    }
    if (user.Deleted == true)
    {
        return Unauthorized(new { message = "User is deleted" });
    }

   


    var secretKey = _configuration["Jwt:Key"]; 
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var claims = new[] { 
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email)
    };
    
    var token = new JwtSecurityToken(
        issuer: _configuration["Jwt:Issuer"],
        audience: _configuration["Jwt:Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddDays(7),
        signingCredentials: creds
    );

    return Ok(new
    {
        message = "Login successful",
        id = user.Id,
        email = user.Email,
        token = new JwtSecurityTokenHandler().WriteToken(token)
    });
}
[HttpPost("register/app")]
public async Task<IActionResult> RegisterApp([FromBody] RegisterRequest request)
{
    var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
    if (existingUser != null)
    {
        return BadRequest(new { message = "User with this email already exists" });
    }
    string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
    var user = new User
    {
        Name = request.Name,
        PasswordHash = passwordHash,
        Email = request.Email,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        Deleted = false,
        OustandingBalances = 0,
        RoleId = 1
    };

    _context.Users.Add(user);
    await _context.SaveChangesAsync();

    var secretKey = _configuration["Jwt:Key"];
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var claims = new[] { 
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email) 
    };

    var token = new JwtSecurityToken(
        issuer: _configuration["Jwt:Issuer"],
        audience: _configuration["Jwt:Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddDays(7),
        signingCredentials: creds
    );

    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

    return Ok(new
    {
        message = "User registered successfully",
        id = user.Id,
        email = user.Email,
        token = tokenString
    });
}
        // POST: api/User/login
        //логін на адмінку
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == request.Email);
            var salt = _configuration["SALT"];

            if (string.IsNullOrWhiteSpace(salt))
            {
                return StatusCode(500, new { message = "SALT is not configured" });
            }

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password, salt);

            if (user == null || hashedPassword != user.PasswordHash)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            var secretKey = _configuration["Jwt:Key"];
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "User") 
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new
            {
                message = "Login successful",
                token = tokenString, 
                userId = user.Id,
                name = user.Name
            });
        }


[HttpPost("ResetPassword")]
public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
{
  
    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

    if (user == null)
    {
        return Ok(new { message = "If such a user exists, the password reset link has been sent to your email." });
    }

    user.ResetLink = Guid.NewGuid().ToString();
       
    await _context.SaveChangesAsync();

    try 
    {
        await _emailService.SendResetPasswordEmail(user.Email, user.ResetLink);
        
        return Ok(new { message = "The password reset link has been sent to your email." });
    }
    catch (Exception ex)
    {
       
       Console.WriteLine($"EMAIL ERROR: {ex.Message}");
        if (ex.InnerException != null) 
            Console.WriteLine($"INNER EXCEPTION: {ex.InnerException.Message}");

        return StatusCode(500, new { message = "An error occurred while sending the email. Please try again later.", details = ex.Message });
    }
}

[HttpPost("ChangePassword")]
public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
{
    // Знаходимо користувача за токеном (email тут навіть не обов'язковий, бо токен унікальний)
    var user = await _context.Users.FirstOrDefaultAsync(u => u.ResetLink == request.Token);

    if (user == null)
    {
        return NotFound(new { message = "Invalid or expired token" });
    }

    // Змінюємо пароль
    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
    user.ResetLink = null; // Обов'язково видаляємо токен після використання
    user.UpdatedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();

    return Ok(new { message = "Password was changed successfully" });
}


        
        public class LoginRequest
        {
            public string Email { get; set; } = null!;
            public string Password { get; set; } = null!;
        }

        public class RegisterRequest
        {
            public string Email { get; set; } = null!;

            public string Password { get; set; } = null!;

            public string Name { get; set; } = null!;
        }

  public class ChangePasswordRequest
{
    public string NewPassword { get; set; } = null!;
    public string Token { get; set; } = null!;
    }

        
    }
}