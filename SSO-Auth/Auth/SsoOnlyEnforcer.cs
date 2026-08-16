using System;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SSO_Auth.Auth;

/// <summary>
/// Shared logic to lock a user out of local login when SSO-only enforcement is enabled.
/// </summary>
public static class SsoOnlyEnforcer
{
    /// <summary>
    /// The <see cref="MediaBrowser.Model.Users.UserPolicy.AuthenticationProviderId"/> Jellyfin's
    /// own built-in local login provider is registered under. Referenced by fully-qualified name
    /// (rather than <c>typeof</c>) because it lives in an internal server assembly this plugin
    /// does not reference.
    /// </summary>
    private const string DefaultAuthProviderId = "Jellyfin.Server.Implementations.Users.DefaultAuthenticationProvider";

    /// <summary>
    /// Locks the given user to <see cref="SsoOnlyAuthProvider"/> if SSO-only enforcement is
    /// enabled for the plugin and the user is not on the exemption list. If the user is on the
    /// exemption list but was previously locked out by this feature, restores their local login.
    /// </summary>
    /// <param name="userManager">The user manager.</param>
    /// <param name="logger">The logger to report failures to.</param>
    /// <param name="user">The user to evaluate.</param>
    /// <returns>A task representing the operation.</returns>
    public static async Task EnforceAsync(IUserManager userManager, ILogger logger, User user)
    {
        var config = SSOPlugin.Instance.Configuration;
        if (!config.EnforceSsoOnly)
        {
            return;
        }

        var isExempt = config.SsoOnlyExemptUsernames.Any(exempt => string.Equals(exempt, user.Username, StringComparison.OrdinalIgnoreCase));
        var policy = userManager.GetUserDto(user).Policy;
        var isLockedByThisFeature = string.Equals(policy.AuthenticationProviderId, typeof(SsoOnlyAuthProvider).FullName, StringComparison.Ordinal);

        if (isExempt)
        {
            if (!isLockedByThisFeature)
            {
                return;
            }

            logger.LogInformation("Restoring local login for exempt user {Username}", user.Username);
            policy.AuthenticationProviderId = DefaultAuthProviderId;
            await userManager.UpdatePolicyAsync(user.Id, policy).ConfigureAwait(false);
            return;
        }

        if (isLockedByThisFeature)
        {
            return;
        }

        logger.LogInformation("Enforcing SSO-only login for user {Username}", user.Username);
        policy.AuthenticationProviderId = typeof(SsoOnlyAuthProvider).FullName;
        await userManager.UpdatePolicyAsync(user.Id, policy).ConfigureAwait(false);
    }
}
