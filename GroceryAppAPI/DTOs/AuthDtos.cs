namespace GroceryOrderingApp.Backend.DTOs
{
    public class LoginRequestDto
    {
        public string? UserId { get; set; }
        public string? MobileNumber { get; set; }
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? ShopImageUrl { get; set; }
    }

    public class RegisterRequestDto
    {
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? ShopImageUrl { get; set; }
    }

    public class RegisterResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int UserId { get; set; }
    }
}
