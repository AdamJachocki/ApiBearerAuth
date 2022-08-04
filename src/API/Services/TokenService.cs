using API.Dto;
using API.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace API.Services
{
    public class TokenService
    {
        private readonly TokenOptions _tokenOptions;

        public TokenService(IOptions<TokenOptions> tokenOptions)
        {
            _tokenOptions = tokenOptions.Value;
        }

        public string GenerateBearerToken()
        {
            var expiry = DateTimeOffset.Now.AddMinutes(15); //ważny przez 15 minut
            var userClaims = GetClaimsForUser(1);
            return CreateToken(expiry, userClaims);
        }

        public string GenerateRefreshToken()
        {
            var expiry = DateTimeOffset.Now.AddDays(30); //ważny przez 30 dni
            var userClaims = GetClaimsForUser(1); 
            return CreateToken(expiry, userClaims); 
        }

        public TokenInfoDto RefreshBearerToken(TokenInfoDto oldTokens)
        {
            //pobierz ClaimsPrincipali z tokenów
            ClaimsPrincipal accessPrincipal = GetPrincipalFromToken(oldTokens.AccessToken);
            ClaimsPrincipal refreshPrincipal = GetPrincipalFromToken(oldTokens.RefreshToken);

            //jeśli chociaż jednego z nich brakuje, to coś jest nie tak - nie pozwól odświeżyć tokenów
            if (accessPrincipal == null || refreshPrincipal == null)
                return null;

            //jeśli chociaż jeden z nich nie ma Claimsa z ID - coś jest nie tak. Nie pozwól odświeżyć
            var accessPrincipalId = accessPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var refreshPrincipalId = refreshPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (accessPrincipalId == null || refreshPrincipalId == null || accessPrincipalId != refreshPrincipalId)
                return null;

            //tutaj wiemy, że id są te same - odświeżamy tokeny
            TokenInfoDto result = new TokenInfoDto
            {
                AccessToken = GenerateBearerToken(),
                RefreshToken = GenerateRefreshToken()
            };

            return result;

        }

        private ClaimsPrincipal GetPrincipalFromToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();

            TokenValidationParameters tvp = new TokenValidationParameters();
            tvp.ValidateIssuer = false;
            tvp.ValidateAudience = false;
            tvp.ValidateIssuerSigningKey = true;
            tvp.ValidateLifetime = false;
            tvp.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_tokenOptions.SigningKey));

            SecurityToken secureToken;
            return handler.ValidateToken(token, tvp, out secureToken);
        }

        private string CreateToken(DateTimeOffset expiryDate, IEnumerable<Claim> claims)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_tokenOptions.SigningKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var securityToken = new JwtSecurityToken(
                issuer: _tokenOptions.Issuer,
                audience: _tokenOptions.Audience,
                claims: claims,
                notBefore: DateTime.Now,
                expires: expiryDate.DateTime,
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(securityToken);
        }

        private IEnumerable<Claim> GetClaimsForUser(int userId)
        {
            var claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.Email, "user@example.com"));
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
            claims.Add(new Claim(ClaimTypes.Role, "User"));

            return claims;
        }
    }
}
