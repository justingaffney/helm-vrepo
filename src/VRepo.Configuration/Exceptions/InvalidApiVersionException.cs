using System.ComponentModel.DataAnnotations;
using VRepo.Configuration.Versions;

namespace VRepo.Configuration.Exceptions;

public sealed class InvalidApiVersionException(string apiVersionValue) : ValidationException($"`{ApiVersion.YamlMemberName}` value \"{apiVersionValue}\" is not valid. Only values [{string.Join(", ", ApiVersion.AllValues.Select(value => $"\"{value}\""))}] are allowed.");
