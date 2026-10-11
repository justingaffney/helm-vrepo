using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using NSubstitute;
using VRepo.Configuration.Exceptions;
using VRepo.Configuration.Versions.V1Alpha1.Models;
using VRepo.Engine.Services;

namespace VRepo.Engine.Tests;

public class VirtualRepositoryReaderTests
{
    [Fact]
    public async Task Read_ThrowsValidationException_WhenConfigurationIsBlank()
    {
        var validator = Substitute.For<IValidateOptions<VirtualRepositoryConfig>>();
        var reader = new VirtualRepositoryReader(validator);

        await Assert.ThrowsAsync<ValidationException>(async () =>
            await reader.Read(new StringReader("   "), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Read_ThrowsOnInvalidApiVersion()
    {
        var validator = Substitute.For<IValidateOptions<VirtualRepositoryConfig>>();
        var reader = new VirtualRepositoryReader(validator);

        await Assert.ThrowsAsync<InvalidApiVersionException>(async () =>
            await reader.Read(new StringReader("apiVersion: not-supported\n"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Read_UsesValidatorResultAndMapsConfiguration()
    {
        var validator = Substitute.For<IValidateOptions<VirtualRepositoryConfig>>();
        validator
            .Validate(Arg.Any<string?>(), Arg.Any<VirtualRepositoryConfig>())
            .Returns(ValidateOptionsResult.Success);

        var reader = new VirtualRepositoryReader(validator);
        var validYaml = VirtualRepositoryConfig.CreateSerializedExample();

        var repository = await reader.Read(new StringReader(validYaml), TestContext.Current.CancellationToken);

        Assert.NotNull(repository);
        Assert.NotEmpty(repository.UpstreamRepositories.Http);
        Assert.NotNull(repository.ManualRepository);
    }

    [Theory]
    [InlineData("upstream: {}\n")]
    [InlineData("apiVersion: ''\n")]
    public async Task Read_ThrowsValidationException_WhenApiVersionIsMissingOrBlank(string yaml)
    {
        var validator = Substitute.For<IValidateOptions<VirtualRepositoryConfig>>();
        var reader = new VirtualRepositoryReader(validator);

        await Assert.ThrowsAsync<ValidationException>(async () =>
            await reader.Read(new StringReader(yaml), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Read_ThrowsValidationException_WhenConfigurationCannotBeDeserialized()
    {
        var validator = Substitute.For<IValidateOptions<VirtualRepositoryConfig>>();
        var reader = new VirtualRepositoryReader(validator);

        var yaml = @"apiVersion: v1alpha1
upstream:
  repositories:
    - url: not-a-repository-url
      charts:
        chart-a:
";

        var exception = await Assert.ThrowsAsync<ValidationException>(async () =>
            await reader.Read(new StringReader(yaml), TestContext.Current.CancellationToken));

        Assert.StartsWith("Failed to deserialize configuration YAML", exception.Message, StringComparison.Ordinal);
        validator.DidNotReceiveWithAnyArgs().Validate(default, default!);
    }

    [Fact]
    public async Task Read_ThrowsGenericValidationException_WhenValidatorReportsOnlyBlankFailures()
    {
        var validator = Substitute.For<IValidateOptions<VirtualRepositoryConfig>>();
        validator
            .Validate(Arg.Any<string?>(), Arg.Any<VirtualRepositoryConfig>())
            .Returns(ValidateOptionsResult.Fail([" "]));

        var reader = new VirtualRepositoryReader(validator);

        var exception = await Assert.ThrowsAsync<ValidationException>(async () =>
            await reader.Read(new StringReader(VirtualRepositoryConfig.CreateSerializedExample()), TestContext.Current.CancellationToken));

        Assert.Equal("Configuration is not valid.", exception.Message);
    }

    [Fact]
    public async Task Read_ThrowsValidationException_WhenValidatorReportsFailures()
    {
        var validator = Substitute.For<IValidateOptions<VirtualRepositoryConfig>>();
        validator
            .Validate(Arg.Any<string?>(), Arg.Any<VirtualRepositoryConfig>())
            .Returns(ValidateOptionsResult.Fail(["duplicate chart name"]));

        var reader = new VirtualRepositoryReader(validator);

        await Assert.ThrowsAsync<ValidationException>(async () =>
            await reader.Read(new StringReader(VirtualRepositoryConfig.CreateSerializedExample()), TestContext.Current.CancellationToken));
    }
}
