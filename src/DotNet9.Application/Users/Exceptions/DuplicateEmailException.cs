namespace DotNet9.Application.Users.Exceptions;

public sealed class DuplicateEmailException : Exception
{
    public DuplicateEmailException(string email)
        : base($"Email '{email}' already in use.") { }
}
