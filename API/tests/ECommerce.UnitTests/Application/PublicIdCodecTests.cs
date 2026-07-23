using ECommerce.Application.Common.Identifiers;
using FluentAssertions;

namespace ECommerce.UnitTests.Application;

public sealed class PublicIdCodecTests
{
    [Theory]
    [InlineData(1, "P00001")]
    [InlineData(35, "P0000Z")]
    [InlineData(36, "P00010")]
    [InlineData(60466175, "PZZZZZ")]
    public void Product_Id_Should_Round_Trip(long id, string expected)
    {
        PublicIdCodec.EncodeProductId(id).Should().Be(expected);
        PublicIdCodec.DecodeProductId(expected).Should().Be(id);
    }

    [Theory]
    [InlineData(1, "U00001")]
    [InlineData(78364164095, "UZZZZZZZ")]
    public void User_Id_Should_Round_Trip(long id, string expected)
    {
        PublicIdCodec.EncodeUserId(id).Should().Be(expected);
        PublicIdCodec.DecodeUserId(expected).Should().Be(id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("P00000")]
    [InlineData("p00001")]
    [InlineData("U00001")]
    [InlineData("P000001")]
    [InlineData("P00-01")]
    public void Product_Id_Should_Reject_NonCanonical_Values(string? value)
    {
        PublicIdCodec.TryDecodeProductId(value, out _).Should().BeFalse();
    }

    [Fact]
    public void Id_Should_Reject_Values_Outside_Public_Range()
    {
        var act = () => PublicIdCodec.EncodeProductId(78364164096);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
