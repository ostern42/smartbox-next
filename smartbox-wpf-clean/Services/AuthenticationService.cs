using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace SmartBoxNext.Services
{
    /// <summary>
    /// JWT-based authentication service for SmartBox Next medical streaming
    /// </summary>
    public class AuthenticationService
    {
        private readonly string _jwtSecret;
        private readonly string _issuer = "SmartBoxNext";
        private readonly string _audience = "SmartBoxMedicalStreaming";
        private readonly TimeSpan _tokenExpiration = TimeSpan.FromHours(8);
        private readonly TimeSpan _refreshTokenExpiration = TimeSpan.FromDays(7);
        
        // In-memory user store (replace with database in production)
        private readonly Dictionary<string, UserAccount> _users = new();
        private readonly Dictionary<string, RefreshToken> _refreshTokens = new();
        
        public AuthenticationService(string? jwtSecret = null)
        {
            _jwtSecret = jwtSecret ?? GenerateSecretKey();
            
            // Initialize with default admin account
            CreateUser("admin", "SmartBox2024!", UserRole.Administrator);
        }
        
        /// <summary>
        /// Authenticate user and generate JWT token
        /// </summary>
        public async Task<AuthenticationResult> AuthenticateAsync(string username, string password)
        {
            if (!_users.TryGetValue(username.ToLowerInvariant(), out var user))
            {
                return new AuthenticationResult { Success = false, Error = "Invalid credentials" };
            }
            
            if (!VerifyPassword(password, user.PasswordHash, user.Salt))
            {
                return new AuthenticationResult { Success = false, Error = "Invalid credentials" };
            }
            
            var accessToken = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken(user.Username);
            
            return new AuthenticationResult
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = (int)_tokenExpiration.TotalSeconds,
                User = new UserInfo
                {
                    Username = user.Username,
                    Role = user.Role.ToString(),
                    DisplayName = user.DisplayName
                }
            };
        }
        
        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        public async Task<AuthenticationResult> RefreshTokenAsync(string refreshToken)
        {
            if (!_refreshTokens.TryGetValue(refreshToken, out var storedToken))
            {
                return new AuthenticationResult { Success = false, Error = "Invalid refresh token" };
            }
            
            if (storedToken.ExpiresAt < DateTime.UtcNow)
            {
                _refreshTokens.Remove(refreshToken);
                return new AuthenticationResult { Success = false, Error = "Refresh token expired" };
            }
            
            if (!_users.TryGetValue(storedToken.Username.ToLowerInvariant(), out var user))
            {
                return new AuthenticationResult { Success = false, Error = "User not found" };
            }
            
            // Revoke old refresh token
            _refreshTokens.Remove(refreshToken);
            
            // Generate new tokens
            var accessToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken(user.Username);
            
            return new AuthenticationResult
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                ExpiresIn = (int)_tokenExpiration.TotalSeconds,
                User = new UserInfo
                {
                    Username = user.Username,
                    Role = user.Role.ToString(),
                    DisplayName = user.DisplayName
                }
            };
        }
        
        /// <summary>
        /// Validate JWT token and extract claims
        /// </summary>
        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret)),
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
                
                var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
                return principal;
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// Create a new user account
        /// </summary>
        public bool CreateUser(string username, string password, UserRole role = UserRole.Viewer, string? displayName = null)
        {
            var normalizedUsername = username.ToLowerInvariant();
            if (_users.ContainsKey(normalizedUsername))
            {
                return false;
            }
            
            var salt = GenerateSalt();
            var passwordHash = HashPassword(password, salt);
            
            _users[normalizedUsername] = new UserAccount
            {
                Username = username,
                PasswordHash = passwordHash,
                Salt = salt,
                Role = role,
                DisplayName = displayName ?? username,
                CreatedAt = DateTime.UtcNow
            };
            
            return true;
        }
        
        /// <summary>
        /// Revoke all refresh tokens for a user
        /// </summary>
        public void RevokeUserTokens(string username)
        {
            var tokensToRemove = _refreshTokens
                .Where(kvp => kvp.Value.Username.Equals(username, StringComparison.OrdinalIgnoreCase))
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (var token in tokensToRemove)
            {
                _refreshTokens.Remove(token);
            }
        }
        
        private string GenerateJwtToken(UserAccount user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSecret);
            
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("DisplayName", user.DisplayName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.Add(_tokenExpiration),
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };
            
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        
        private string GenerateRefreshToken(string username)
        {
            var randomBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            
            var refreshToken = Convert.ToBase64String(randomBytes);
            
            _refreshTokens[refreshToken] = new RefreshToken
            {
                Token = refreshToken,
                Username = username,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(_refreshTokenExpiration)
            };
            
            return refreshToken;
        }
        
        private static string GenerateSecretKey()
        {
            var randomBytes = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return Convert.ToBase64String(randomBytes);
        }
        
        private static byte[] GenerateSalt()
        {
            var salt = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }
        
        private static string HashPassword(string password, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256))
            {
                var hash = pbkdf2.GetBytes(32);
                var hashBytes = new byte[64];
                Array.Copy(salt, 0, hashBytes, 0, 32);
                Array.Copy(hash, 0, hashBytes, 32, 32);
                return Convert.ToBase64String(hashBytes);
            }
        }
        
        private static bool VerifyPassword(string password, string storedHash, byte[] salt)
        {
            var hashBytes = Convert.FromBase64String(storedHash);
            var hash = new byte[32];
            Array.Copy(hashBytes, 32, hash, 0, 32);
            
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256))
            {
                var testHash = pbkdf2.GetBytes(32);
                return testHash.SequenceEqual(hash);
            }
        }
        
        // Data models
        private class UserAccount
        {
            public string Username { get; set; } = string.Empty;
            public string PasswordHash { get; set; } = string.Empty;
            public byte[] Salt { get; set; } = Array.Empty<byte>();
            public UserRole Role { get; set; }
            public string DisplayName { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
        }
        
        private class RefreshToken
        {
            public string Token { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public DateTime ExpiresAt { get; set; }
        }
    }
    
    public enum UserRole
    {
        Viewer,
        Operator,
        Administrator
    }
    
    public class AuthenticationResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
        public UserInfo? User { get; set; }
    }
    
    public class UserInfo
    {
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }
}