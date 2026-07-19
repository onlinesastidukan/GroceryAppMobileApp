using GroceryOrderingApp.Backend.Models;
using GroceryOrderingApp.Backend.Repositories;
using GroceryOrderingApp.Backend.DTOs;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace GroceryOrderingApp.Backend.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IConfiguration _configuration;
        private readonly PasswordHasher<User> _passwordHasher;

        public AuthService(IUserRepository userRepository, IConfiguration configuration, ICategoryRepository categoryRepository)
        {
            _userRepository = userRepository;
            _categoryRepository = categoryRepository;
            _configuration = configuration;
            _passwordHasher = new PasswordHasher<User>();
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
        {
            var loginId = !string.IsNullOrWhiteSpace(request.MobileNumber)
                ? request.MobileNumber.Trim()
                : request.UserId?.Trim();
            if (string.IsNullOrWhiteSpace(loginId))
                return null;

            var user = await _userRepository.GetUserByUserIdAsync(loginId);
            if (user == null)
            {
                user = await _userRepository.GetUserByMobileNumberAsync(loginId);
            }

            if (user == null || !user.IsActive)
                return null;

            if (!string.Equals(user.Role?.Name, "Admin", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(user.Role?.Name, "Dealer", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (result == PasswordVerificationResult.Failed)
                return null;

            var token = GenerateToken(user);
            return new LoginResponseDto
            {
                Token = token,
                Role = user.Role?.Name ?? "Customer",
                UserId = user.Id,
                FullName = user.FullName,
                MobileNumber = user.MobileNumber,
                Address = user.Address
            };
        }

        public async Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.FullName) ||
                string.IsNullOrWhiteSpace(request.MobileNumber) ||
                string.IsNullOrWhiteSpace(request.Address))
            {
                return new RegisterResponseDto
                {
                    Success = false,
                    Message = "Password, FullName, MobileNumber, and Address are required"
                };
            }

            var userId = request.MobileNumber.Trim();
            var existingUser = await _userRepository.GetUserByUserIdAsync(userId);
            if (existingUser == null)
            {
                existingUser = await _userRepository.GetUserByMobileNumberAsync(userId);
            }
            if (existingUser != null)
            {
                return new RegisterResponseDto
                {
                    Success = false,
                    Message = "Mobile number already exists"
                };
            }

            var user = new User
            {
                UserId = userId,
                FullName = request.FullName.Trim(),
                MobileNumber = request.MobileNumber.Trim(),
                Address = request.Address.Trim(),
                RoleId = 3,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
            var createdUser = await _userRepository.CreateUserAsync(user);

            var shopName = request.FullName.Trim();
            var existingShops = await _categoryRepository.GetShopsByDealerAsync(createdUser.Id);
            if (!existingShops.Any(s => s.IsActive && string.Equals(s.Name, shopName, StringComparison.OrdinalIgnoreCase)))
            {
                await _categoryRepository.CreateCategoryAsync(new Category
                {
                    Name = shopName,
                    Description = $"Shop for {shopName}",
                    PhotoUrl = string.IsNullOrWhiteSpace(request.ShopImageUrl) ? null : request.ShopImageUrl.Trim(),
                    DealerId = createdUser.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            return new RegisterResponseDto
            {
                Success = true,
                Message = "Registration successful",
                UserId = createdUser.Id
            };
        }

        public string GenerateToken(User user)
        {
            var jwtSecret = _configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "GroceryApp";
            var jwtAudience = _configuration["Jwt:Audience"] ?? "GroceryAppUsers";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim("userId", user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserId),
                new Claim(ClaimTypes.Role, user.Role?.Name ?? "Customer")
            };

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
