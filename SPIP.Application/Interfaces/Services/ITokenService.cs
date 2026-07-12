namespace SPIP.Application.Interfaces.Services;

public interface ITokenService
{
    /// <summary>
    /// Generates a JWT token for the given user identity and role claims.
    /// Kept primitive-typed (no Identity types) so Application has no dependency on Infrastructure.
    /// </summary>
    string GenerateToken(Guid userId, string email, string userName, IList<string> roles, IList<string> permissions);
}
