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
