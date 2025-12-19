// ActiveDirectoryAuthenticator.cs
using System;
using System.DirectoryServices.AccountManagement;

public class ActiveDirectoryAuthenticator : IDisposable
{
    private PrincipalContext context;

    // Connect to the domain
    public ActiveDirectoryAuthenticator(string domain)
    {
        context = new PrincipalContext(ContextType.Domain, domain);
    }

    // Authenticate user credentials
    public bool Authenticate(string username, string password, out UserPrincipal user)
    {
        user = null;
        try
        {
            if (context.ValidateCredentials(username, password))
            {
                user = UserPrincipal.FindByIdentity(context, username);
                return user != null;
            }
        }
        catch
        {
            // Do Nada
        }
        return false;
    }

    // Check if the user is in a specific AD group
    public bool IsInRole(UserPrincipal user, string groupName)
    {
        if (user == null) return false;
        foreach (var group in user.GetAuthorizationGroups())
        {
            if (group.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    public void Dispose()
    {
        context?.Dispose();
    }
}
