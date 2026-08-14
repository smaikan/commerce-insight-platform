using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.Dtos;
using ECommerce.Application.Products.Queries.GetPublishedProductSearchSuggestions;
using ECommerce.Application.Products.Services;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class PublishedProductSearchTests
{
    // Burada Türkçe I/İ/ı/i, aksan ve boşluk varyasyonlarının aynı metne normalize edildiğini doğruluyorum.
    [Theory]
    [InlineData("  ŞÖNİL   IŞIK  ", "sonil isik")]
    [InlineData("şönil ışık", "sonil isik")]
    [InlineData("SONIL ISIK", "sonil isik")]
    public void Normalizer_Should_Produce_Stable_Turkish_Search_Text(string input, string expected)
    {
        ProductSearchTextNormalizer.Normalize(input).Should().Be(expected);
    }

    // Burada iki karakterden kısa veya yüz karakterden uzun suggestion sorgularını reddediyorum.
    [Fact]
    public void Suggestion_Validator_Should_Enforce_Normalized_Query_And_Limit_Bounds()
    {
        var validator = new GetPublishedProductSearchSuggestionsQueryValidator();

        validator.TestValidate(new GetPublishedProductSearchSuggestionsQuery(" a ", 0))
            .ShouldHaveValidationErrorFor(query => query.Query);
        validator.TestValidate(new GetPublishedProductSearchSuggestionsQuery("ab", 11))
            .ShouldHaveValidationErrorFor(query => query.Limit);
        validator.TestValidate(new GetPublishedProductSearchSuggestionsQuery(new string('a', 101), 10))
            .ShouldHaveValidationErrorFor(query => query.Query);
    }

    // Burada varsayılan limitin on olduğunu ve normalize tokenların reader'a aktarıldığını doğruluyorum.
    [Fact]
    public async Task Suggestion_Handler_Should_Forward_Normalized_Search_With_Default_Limit()
    {
        var reader = new Mock<IPublishedProductSearchReader>();
        reader.Setup(item => item.GetSuggestionsAsync(It.IsAny<PublishedProductSearchFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PublishedProductSearchSuggestionsDto([], false));
        var handler = new GetPublishedProductSearchSuggestionsQueryHandler(reader.Object);

        await handler.Handle(new GetPublishedProductSearchSuggestionsQuery(" Şönil   yüzük "), CancellationToken.None);

        reader.Verify(item => item.GetSuggestionsAsync(
            It.Is<PublishedProductSearchFilter>(filter =>
                filter.Limit == 10 &&
                filter.NormalizedQuery == "sonil yuzuk" &&
                filter.Tokens.SequenceEqual(new[] { "sonil", "yuzuk" }) &&
                filter.CandidateGrams.SequenceEqual(new[] { "son", "yuz" })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada HTTP/Application iptal tokenının reader katmanına değiştirilmeden taşındığını doğruluyorum.
    [Fact]
    public async Task Suggestion_Handler_Should_Forward_CancellationToken()
    {
        using var source = new CancellationTokenSource();
        var reader = new Mock<IPublishedProductSearchReader>();
        reader.Setup(item => item.GetSuggestionsAsync(It.IsAny<PublishedProductSearchFilter>(), source.Token))
            .ReturnsAsync(new PublishedProductSearchSuggestionsDto([], false));
        var handler = new GetPublishedProductSearchSuggestionsQueryHandler(reader.Object);

        await handler.Handle(new GetPublishedProductSearchSuggestionsQuery("kolye"), source.Token);

        reader.Verify(item => item.GetSuggestionsAsync(
            It.IsAny<PublishedProductSearchFilter>(),
            source.Token), Times.Once);
    }
}
