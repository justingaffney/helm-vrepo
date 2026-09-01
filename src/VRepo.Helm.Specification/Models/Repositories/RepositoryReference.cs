using System.Diagnostics.CodeAnalysis;
using VRepo.Helm.Specification.Models.Repositories.References;

namespace VRepo.Helm.Specification.Models.Repositories;

public readonly union RemoteRepositoryReference
(
    HttpUrlRepository,
    OciUrlRepository
)
{
    public static RemoteRepositoryReference Parse(string value)
    {
        if (HttpUrlRepository.Matches(value))
        {
            return new HttpUrlRepository(value);
        }
        else if (OciUrlRepository.Matches(value))
        {
            return new OciUrlRepository(value);
        }

        throw new FormatException($"Value \"{value}\" is not a valid remote repository reference.");
    }

    public static bool TryParse(string value, [NotNullWhen(true)] out RemoteRepositoryReference? result)
    {
        try
        {
            result = Parse(value);
            return true;
        }
        catch (FormatException)
        {
            result = null;
            return false;
        }
    }

    public override readonly string ToString()
    {
        return this switch
        {
            HttpUrlRepository repository => repository.ToString(),
            OciUrlRepository repository => repository.ToString(),
            _ => null!
        };
    }
}

public readonly union LocalRepositoryReference
(
    LocalChartsDirectoryRepository,
    LocalFilesystemUrlRepository
)
{
    public static LocalRepositoryReference Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new LocalChartsDirectoryRepository();
        }
        else if (LocalFilesystemUrlRepository.Matches(value))
        {
            return new LocalFilesystemUrlRepository(value);
        }

        throw new FormatException($"Value \"{value}\" is not a valid local repository reference.");
    }

    public static bool TryParse(string value, [NotNullWhen(true)] out LocalRepositoryReference? result)
    {
        try
        {
            result = Parse(value);
            return true;
        }
        catch (FormatException)
        {
            result = null;
            return false;
        }
    }

    public override readonly string ToString()
    {
        return this switch
        {
            LocalChartsDirectoryRepository repository => repository.ToString(),
            LocalFilesystemUrlRepository repository => repository.ToString(),
            _ => null!
        };
    }
}

public readonly union RepositoryReference
(
    LocalChartsDirectoryRepository,
    LocalFilesystemUrlRepository,
    HttpUrlRepository,
    OciUrlRepository,
    AliasRepository
)
{
    public static RepositoryReference Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new LocalChartsDirectoryRepository();
        }
        else if (LocalFilesystemUrlRepository.Matches(value))
        {
            return new LocalFilesystemUrlRepository(value);
        }
        else if (HttpUrlRepository.Matches(value))
        {
            return new HttpUrlRepository(value);
        }
        else if (OciUrlRepository.Matches(value))
        {
            return new OciUrlRepository(value);
        }
        else if (AliasRepository.Matches(value))
        {
            return new AliasRepository(value);
        }

        throw new FormatException($"Value \"{value}\" is not a valid repository reference.");
    }

    public static bool TryParse(string value, [NotNullWhen(true)] out RepositoryReference? result)
    {
        try
        {
            result = Parse(value);
            return true;
        }
        catch (FormatException)
        {
            result = null;
            return false;
        }
    }

    public override readonly string ToString()
    {
        return this switch
        {
            LocalChartsDirectoryRepository repository => repository.ToString(),
            LocalFilesystemUrlRepository repository => repository.ToString(),
            HttpUrlRepository repository => repository.ToString(),
            OciUrlRepository repository => repository.ToString(),
            AliasRepository repository => repository.ToString(),
            _ => null!
        };
    }
}
