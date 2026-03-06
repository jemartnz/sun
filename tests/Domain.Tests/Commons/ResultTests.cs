using Domain.Commons;
using FluentAssertions;

namespace Domain.Tests.Commons;

public sealed class ResultTests
{
    private static readonly Error TestError = new("Test.Error", "Error de prueba.");

    [Fact]
    public void Success_IsSuccess_IsTrue()
    {
        var result = Result.Success();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Success_IsFailure_IsFalse()
    {
        var result = Result.Success();
        result.IsFailure.Should().BeFalse();
    }

    [Fact]
    public void Failure_IsFailure_IsTrue()
    {
        var result = Result.Failure(TestError);
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Failure_IsSuccess_IsFalse()
    {
        var result = Result.Failure(TestError);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Failure_HasCorrectError()
    {
        var result = Result.Failure(TestError);
        result.Error.Should().Be(TestError);
    }

    [Fact]
    public void SuccessT_HasCorrectValue()
    {
        var result = Result<int>.Success(42);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void FailureT_ValueIsDefault()
    {
        var result = Result<int>.Failure(TestError);
        result.IsFailure.Should().BeTrue();
        result.Value.Should().Be(default);
    }
}
