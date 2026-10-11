using VRepo.Configuration.Versions.V1Alpha1.Models;
using VRepo.Configuration.Versions.V1Alpha1.Validation;
using VRepo.Helm.Specification.Models.Charts;
using VRepo.Helm.Specification.Models.Repositories;
using VRepo.Helm.Specification.Models.Repositories.References;

namespace VRepo.Configuration.Tests;

public class VirtualRepositoryConfigValidatorTests
{
    [Fact]
    public void Validate_DuplicateRepositoryUrls_Fails()
    {
        var config = new VirtualRepositoryConfig
        {
            Upstream = new VirtualRepositoryConfig.UpstreamConfig
            {
                Repositories =
                [
                    new VirtualRepositoryConfig.UpstreamRepositoryConfig
                    {
                        Url = new HttpUrlRepository("https://example.com/charts"),
                        Charts = new Dictionary<string, VirtualRepositoryConfig.UpstreamRepositoryConfig.ChartConfig>
                        {
                            ["a"] = new()
                        }
                    },
                    new VirtualRepositoryConfig.UpstreamRepositoryConfig
                    {
                        Url = new HttpUrlRepository("https://example.com/charts"),
                        Charts = new Dictionary<string, VirtualRepositoryConfig.UpstreamRepositoryConfig.ChartConfig>
                        {
                            ["b"] = new()
                        }
                    }
                ]
            }
        };

        var validator = new VirtualRepositoryConfigValidator();

        var result = validator.Validate(null, config);

        Assert.False(result.Succeeded);
        Assert.Contains("has been specified by multiple repositories", result.FailureMessage);
    }

    [Fact]
    public void Validate_DuplicateRepositoryUrls_WithDifferentFormats_Fails()
    {
        var config = new VirtualRepositoryConfig
        {
            Upstream = new VirtualRepositoryConfig.UpstreamConfig
            {
                Repositories =
                [
                    new VirtualRepositoryConfig.UpstreamRepositoryConfig
                    {
                        Url = new HttpUrlRepository("https://example.com/charts"),
                        Charts = new Dictionary<string, VirtualRepositoryConfig.UpstreamRepositoryConfig.ChartConfig>
                        {
                            ["a"] = new()
                        }
                    },
                    new VirtualRepositoryConfig.UpstreamRepositoryConfig
                    {
                        Url = new HttpUrlRepository("https://example.com/charts/index.yaml"),
                        Charts = new Dictionary<string, VirtualRepositoryConfig.UpstreamRepositoryConfig.ChartConfig>
                        {
                            ["b"] = new()
                        }
                    }
                ]
            }
        };

        var validator = new VirtualRepositoryConfigValidator();

        var result = validator.Validate(null, config);

        Assert.False(result.Succeeded);
        Assert.Contains("has been specified by multiple repositories", result.FailureMessage);
    }

    [Fact]
    public void Validate_RepositoryWithNoCharts_Fails()
    {
        var config = new VirtualRepositoryConfig
        {
            Upstream = new VirtualRepositoryConfig.UpstreamConfig
            {
                Repositories =
                [
                    new VirtualRepositoryConfig.UpstreamRepositoryConfig
                    {
                        Url = new HttpUrlRepository("https://repo.one/charts"),
                        Charts = null!
                    }
                ]
            }
        };

        var validator = new VirtualRepositoryConfigValidator();

        var result = validator.Validate(null, config);

        Assert.False(result.Succeeded);
        Assert.Contains("Repository[0] has no charts specified", result.FailureMessage);
    }

    [Fact]
    public void Validate_DuplicateChartNamesAcrossRepositories_Fails()
    {
        var config = new VirtualRepositoryConfig
        {
            Upstream = new VirtualRepositoryConfig.UpstreamConfig
            {
                Repositories =
                [
                    new VirtualRepositoryConfig.UpstreamRepositoryConfig
                    {
                        Url = new HttpUrlRepository("https://repo.one/charts"),
                        Charts = new Dictionary<string, VirtualRepositoryConfig.UpstreamRepositoryConfig.ChartConfig>
                        {
                            ["shared-chart"] = new()
                        }
                    },
                    new VirtualRepositoryConfig.UpstreamRepositoryConfig
                    {
                        Url = new HttpUrlRepository("https://repo.two/charts"),
                        Charts = new Dictionary<string, VirtualRepositoryConfig.UpstreamRepositoryConfig.ChartConfig>
                        {
                            ["shared-chart"] = new()
                        }
                    }
                ]
            }
        };

        var validator = new VirtualRepositoryConfigValidator();

        var result = validator.Validate(null, config);

        Assert.False(result.Succeeded);
        Assert.Contains("has already been specified", result.FailureMessage);
    }

    [Fact]
    public void Validate_ChartNames_CaseInsensitive_Fails()
    {
        var config = new VirtualRepositoryConfig
        {
            Upstream = new VirtualRepositoryConfig.UpstreamConfig
            {
                Repositories =
                [
                    new VirtualRepositoryConfig.UpstreamRepositoryConfig
                    {
                        Url = new HttpUrlRepository("https://repo.one/charts"),
                        Charts = new Dictionary<string, VirtualRepositoryConfig.UpstreamRepositoryConfig.ChartConfig>
                        {
                            ["Shared-Chart"] = new()
                        }
                    },
                    new VirtualRepositoryConfig.UpstreamRepositoryConfig
                    {
                        Url = new HttpUrlRepository("https://repo.two/charts"),
                        Charts = new Dictionary<string, VirtualRepositoryConfig.UpstreamRepositoryConfig.ChartConfig>
                        {
                            ["shared-chart"] = new()
                        }
                    }
                ]
            }
        };

        var validator = new VirtualRepositoryConfigValidator();

        var result = validator.Validate(null, config);

        Assert.False(result.Succeeded);
        Assert.Contains("has already been specified", result.FailureMessage);
    }

    [Theory]
    [InlineData("auth.example.com:8443")]
    [InlineData("https://auth.example.com")]
    [InlineData("auth.example.com/token")]
    [InlineData("auth example.com")]
    public void Validate_InvalidOciAuthenticationRealmHostname_Fails(string authenticationRealmHostname)
    {
        var config = CreateOciRepositoryConfig(authenticationRealmHostname);

        var validator = new VirtualRepositoryConfigValidator();

        var result = validator.Validate(null, config);

        Assert.False(result.Succeeded);
        Assert.Contains("Repository[0] OCI authentication realm hostname", result.FailureMessage);
    }

    [Theory]
    [InlineData("auth.example.com")]
    [InlineData("localhost")]
    [InlineData("10.0.0.1")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_ValidOrBlankOciAuthenticationRealmHostname_Succeeds(string? authenticationRealmHostname)
    {
        var config = CreateOciRepositoryConfig(authenticationRealmHostname);

        var validator = new VirtualRepositoryConfigValidator();

        var result = validator.Validate(null, config);

        Assert.True(result.Succeeded);
    }

    private static VirtualRepositoryConfig CreateOciRepositoryConfig(string? authenticationRealmHostname) => new()
    {
        Upstream = new VirtualRepositoryConfig.UpstreamConfig
        {
            Repositories =
            [
                new VirtualRepositoryConfig.UpstreamRepositoryConfig
                {
                    Url = new OciUrlRepository("oci://registry.example.com/charts"),
                    OciRepository = new()
                    {
                        AuthenticationRealmHostname = authenticationRealmHostname
                    },
                    Charts = new Dictionary<string, VirtualRepositoryConfig.UpstreamRepositoryConfig.ChartConfig>
                    {
                        ["oci-chart"] = new()
                    }
                }
            ]
        }
    };

    [Fact]
    public void Validate_ManualRepositoryChartConflictsWithUpstream_Fails()
    {
        var config = new VirtualRepositoryConfig
        {
            Upstream = new VirtualRepositoryConfig.UpstreamConfig
            {
                Repositories =
                [
                    new VirtualRepositoryConfig.UpstreamRepositoryConfig
                    {
                        Url = new HttpUrlRepository("https://repo.one/charts"),
                        Charts = new Dictionary<string, VirtualRepositoryConfig.UpstreamRepositoryConfig.ChartConfig>
                        {
                            ["manual-chart"] = new()
                        }
                    }
                ]
            },
            ManualRepository = new VirtualRepositoryConfig.ManualRepositoryIndex
            {
                Entries = new Dictionary<string, IReadOnlyList<RepositoryChartMetadata>>
                {
                    ["manual-chart"] =
                    [
                        new RepositoryChartMetadata
                        {
                            ApiVersion = ChartMetadata.LatestStableApiVersion,
                            Name = "manual-chart",
                            Version = "1.0.0",
                            Urls = []
                        }
                    ]
                }
            }
        };

        var validator = new VirtualRepositoryConfigValidator();

        var result = validator.Validate(null, config);

        Assert.False(result.Succeeded);
        Assert.Contains("has already been specified in a repository", result.FailureMessage);
    }

    [Fact]
    public void Validate_ManualOnly_NoUpstream_Succeeds()
    {
        var config = new VirtualRepositoryConfig
        {
            Upstream = null,
            ManualRepository = new VirtualRepositoryConfig.ManualRepositoryIndex
            {
                Entries = new Dictionary<string, IReadOnlyList<RepositoryChartMetadata>>
                {
                    ["manual-only-chart"] =
                    [
                        new RepositoryChartMetadata
                        {
                            ApiVersion = ChartMetadata.LatestStableApiVersion,
                            Name = "manual-only-chart",
                            Version = "1.0.0",
                            Urls = []
                        }
                    ]
                }
            }
        };

        var validator = new VirtualRepositoryConfigValidator();

        var result = validator.Validate(null, config);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_EmptyUpstreamRepositories_Succeeds()
    {
        var config = new VirtualRepositoryConfig
        {
            Upstream = new VirtualRepositoryConfig.UpstreamConfig
            {
                Repositories = []
            },
            ManualRepository = new VirtualRepositoryConfig.ManualRepositoryIndex
            {
                Entries = new Dictionary<string, IReadOnlyList<RepositoryChartMetadata>>
                {
                    ["manual-only-chart-2"] =
                    [
                        new RepositoryChartMetadata
                        {
                            ApiVersion = ChartMetadata.LatestStableApiVersion,
                            Name = "manual-only-chart-2",
                            Version = "2.0.0",
                            Urls = []
                        }
                    ]
                }
            }
        };

        var validator = new VirtualRepositoryConfigValidator();

        var result = validator.Validate(null, config);

        Assert.True(result.Succeeded);
    }
}
