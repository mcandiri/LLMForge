using FluentAssertions;
using LLMForge.Validation;
using Xunit;

namespace LLMForge.Tests.Validation;

public class JsonSchemaValidatorTests
{
    [Fact]
    public async Task ValidateAsync_ValidJson_ShouldReturnSuccess()
    {
        // Arrange
        var sut = new JsonSchemaValidator();
        var json = """{"name": "Alice", "age": 30}""";

        // Act
        var result = await sut.ValidateAsync(json);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ValidatorName.Should().Be("JsonSchema");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_InvalidJson_ShouldReturnFailure()
    {
        // Arrange
        var sut = new JsonSchemaValidator();
        var invalidJson = "not a json string {broken";

        // Act
        var result = await sut.ValidateAsync(invalidJson);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid JSON");
    }

    [Fact]
    public async Task ValidateAsync_RequiredPropertiesPresent_ShouldReturnSuccess()
    {
        // Arrange
        var sut = new JsonSchemaValidator(new[] { "name", "age" });
        var json = """{"name": "Alice", "age": 30, "city": "NYC"}""";

        // Act
        var result = await sut.ValidateAsync(json);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_RequiredPropertiesMissing_ShouldReturnFailureWithMissingNames()
    {
        // Arrange
        var sut = new JsonSchemaValidator(new[] { "name", "email", "phone" });
        var json = """{"name": "Alice", "age": 30}""";

        // Act
        var result = await sut.ValidateAsync(json);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("email");
        result.ErrorMessage.Should().Contain("phone");
        result.ErrorMessage.Should().NotContain("name");
    }

    [Fact]
    public async Task ValidateAsync_JsonInMarkdownCodeBlock_ShouldExtractAndValidate()
    {
        // Arrange
        var sut = new JsonSchemaValidator(new[] { "result" });
        var markdownWrappedJson = """
            ```json
            {"result": "success", "data": [1, 2, 3]}
            ```
            """;

        // Act
        var result = await sut.ValidateAsync(markdownWrappedJson);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
