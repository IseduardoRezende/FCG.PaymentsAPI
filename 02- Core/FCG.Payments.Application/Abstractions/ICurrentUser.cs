namespace FCG.Payments.Application.Abstractions;

public interface ICurrentUser
{
    long? UserId { get; }

    string? Role { get; }

    bool IsAdministrator { get; }
}
