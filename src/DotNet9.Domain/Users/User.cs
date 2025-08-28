namespace DotNet9.Domain.Users;

public sealed class User
{
    private User() { } // EF için

    public Guid Id { get; private set; }
    public string Email { get; private set; } = default!;
    public string Username { get; private set; } = default!;

    private User(Guid id, string email, string username)
    {
        Id = id;
        Email = email;
        Username = username;
    }

    public static User Register(string email, string username)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email required");
        if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("Username required");

        return new User(Guid.NewGuid(), email.Trim(), username.Trim());
    }
}
