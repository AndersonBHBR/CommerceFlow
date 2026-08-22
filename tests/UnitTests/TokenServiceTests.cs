using System.IdentityModel.Tokens.Jwt;
using CommerceFlow.Identity.Api.Auth;
using CommerceFlow.ServiceDefaults;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace CommerceFlow.UnitTests;

public sealed class TokenServiceTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 8, 18, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Issue_ProducesExpectedClaimsAndLifetime()
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "commerceflow.identity",
            Audience = "commerceflow.apis",
            SigningKey = "unit-test-signing-key-with-more-than-thirty-two-bytes",
            ExpirationMinutes = 30
        });
        var user = new DemoUser
        {
            Id = Guid.Parse("7cb20af7-3332-4ea5-a414-7559c360b1c4"),
            Login = "sales@commerceflow.local",
            IsActive = true,
            Roles = [SecurityRoles.SalesUser]
        };
        var service = new TokenService(options, new FixedTimeProvider(FixedNow));

        var result = service.Issue(user);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);

        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal(1800, result.ExpiresIn);
        Assert.Equal(FixedNow.AddMinutes(30), result.ExpiresAtUtc);
        Assert.Equal("commerceflow.identity", token.Issuer);
        Assert.Contains("commerceflow.apis", token.Audiences);
        Assert.Equal(SecurityAlgorithms.HmacSha256, token.Header.Alg);
        Assert.Contains(token.Claims, claim => claim.Type == "sub" && claim.Value == user.Id.ToString());
        Assert.Contains(token.Claims, claim => claim.Type == "name" && claim.Value == user.Login);
        Assert.Contains(token.Claims, claim => claim.Type == "role" && claim.Value == SecurityRoles.SalesUser);
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
