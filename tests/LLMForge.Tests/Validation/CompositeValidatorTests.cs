using FluentAssertions;
using LLMForge.Validation;
using Moq;
using Xunit;

namespace LLMForge.Tests.Validation;

public class CompositeValidatorTests
{
    [Fact]
    public async Task ValidateAsync_AllValidatorsPass_ShouldReturnSuccess()
    {
        // Arrange
        var validator1 = new Mock<IResponseValidator>();
        validator1.Setup(v => v.Name).Returns("V1");
        validator1.Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success("V1"));

        var validator2 = new Mock<IResponseValidator>();
        validator2.Setup(v => v.Name).Returns("V2");
        validator2.Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success("V2"));

        var sut = new CompositeValidator(new[] { validator1.Object, validator2.Object });

        // Act
        var result = await sut.ValidateAsync("test content");

        // Assert
        result.IsValid.Should().BeTrue();
        result.ValidatorName.Should().Be("Composite");
    }

    [Fact]
    public async Task ValidateAsync_OneValidatorFails_ShouldReturnFailureWithDetail()
    {
        // Arrange
        var validator1 = new Mock<IResponseValidator>();
        validator1.Setup(v => v.Name).Returns("V1");
        validator1.Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success("V1"));

        var validator2 = new Mock<IResponseValidator>();
        validator2.Setup(v => v.Name).Returns("V2");
        validator2.Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("V2", "Content too short"));

        var sut = new CompositeValidator(new[] { validator1.Object, validator2.Object });

        // Act
        var result = await sut.ValidateAsync("test");

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("V2");
        result.ErrorMessage.Should().Contain("Content too short");
    }

    [Fact]
    public async Task ValidateAsync_FirstValidatorFails_ShouldShortCircuitAndNotCallRest()
    {
        // Arrange
        var validator1 = new Mock<IResponseValidator>();
        validator1.Setup(v => v.Name).Returns("V1");
        validator1.Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("V1", "Failed early"));

        var validator2 = new Mock<IResponseValidator>();
        validator2.Setup(v => v.Name).Returns("V2");

        var sut = new CompositeValidator(new[] { validator1.Object, validator2.Object });

        // Act
        var result = await sut.ValidateAsync("test");

        // Assert
        result.IsValid.Should().BeFalse();
        validator2.Verify(
            v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Add_FluentChaining_ShouldRegisterAndRunAllValidators()
    {
        // Arrange
        var sut = new CompositeValidator();

        var validator1 = new Mock<IResponseValidator>();
        validator1.Setup(v => v.Name).Returns("V1");
        validator1.Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success("V1"));

        var validator2 = new Mock<IResponseValidator>();
        validator2.Setup(v => v.Name).Returns("V2");
        validator2.Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success("V2"));

        sut.Add(validator1.Object).Add(validator2.Object);

        // Act
        var result = await sut.ValidateAsync("test");

        // Assert
        result.IsValid.Should().BeTrue();
        sut.Validators.Should().HaveCount(2);
    }

    [Fact]
    public async Task ValidateAllAsync_MixedResults_ShouldReturnIndividualResultsForEach()
    {
        // Arrange
        var validator1 = new Mock<IResponseValidator>();
        validator1.Setup(v => v.Name).Returns("V1");
        validator1.Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success("V1"));

        var validator2 = new Mock<IResponseValidator>();
        validator2.Setup(v => v.Name).Returns("V2");
        validator2.Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("V2", "Failed"));

        var sut = new CompositeValidator(new[] { validator1.Object, validator2.Object });

        // Act
        var results = await sut.ValidateAllAsync("test");

        // Assert
        results.Should().HaveCount(2);
        results[0].IsValid.Should().BeTrue();
        results[1].IsValid.Should().BeFalse();
    }
}
