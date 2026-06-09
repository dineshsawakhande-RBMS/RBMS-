namespace RBMS.Application.Common.Exceptions;

/// <summary>Thrown when input fails validation. Mapped to HTTP 400 with field errors.</summary>
public class ValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation failures occurred.")
        => Errors = errors;
}

/// <summary>Thrown when a requested entity does not exist. Mapped to HTTP 404.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string name, object key)
        : base($"\"{name}\" ({key}) was not found.") { }

    public NotFoundException(string message) : base(message) { }
}

/// <summary>Thrown when the caller lacks permission. Mapped to HTTP 403.</summary>
public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException(string message = "Access denied.") : base(message) { }
}

/// <summary>Thrown for domain/business-rule violations. Mapped to HTTP 409/422.</summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
