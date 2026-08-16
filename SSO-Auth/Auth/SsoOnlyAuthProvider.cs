using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Authentication;

namespace Jellyfin.Plugin.SSO_Auth.Auth;

/// <summary>
/// An authentication provider that always rejects local login, used to lock a user to SSO-only login.
/// </summary>
public class SsoOnlyAuthProvider : IAuthenticationProvider
{
    /// <inheritdoc />
    public string Name => "SSO Only (local login disabled)";

    /// <inheritdoc />
    public bool IsEnabled => true;

    /// <inheritdoc />
    public bool HasPassword(User user)
    {
        return false;
    }

    /// <inheritdoc />
    public Task<ProviderAuthenticationResult> Authenticate(string username, string password)
    {
        // This message is not surfaced to the end user; Jellyfin genericizes all
        // local login failures before they reach the client. Kept descriptive for server logs only.
        throw new AuthenticationException("Local login is disabled for this account; SSO-only login is enforced.");
    }

    /// <inheritdoc />
    public Task ChangePassword(User user, string newPassword)
    {
        throw new AuthenticationException("Password changes are disabled while SSO-only login is enforced.");
    }
}
