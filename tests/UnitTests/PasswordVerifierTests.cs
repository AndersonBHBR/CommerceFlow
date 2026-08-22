using CommerceFlow.Identity.Api.Auth;
using Xunit;

namespace CommerceFlow.UnitTests;

public sealed class PasswordVerifierTests
{
    private const string Salt = "YLbLvyIQuJVrZ/rKhhA+aQ==";
    private const string Hash = "xBLZJYEKo6G7xwr6UVLQZ7SEJA+G5my2UFD2qmAzIeQ=";

    private readonly Pbkdf2PasswordVerifier _sut = new();

    [Fact]
    public void Verify_WithCorrectPassword_ReturnsTrue()
    {
        var result = _sut.Verify("CommerceFlow#2026", Salt, Hash);

        Assert.True(result);
    }

    [Fact]
    public void Verify_WithIncorrectPassword_ReturnsFalse()
    {
        var result = _sut.Verify("senha-incorreta", Salt, Hash);

        Assert.False(result);
    }
}
