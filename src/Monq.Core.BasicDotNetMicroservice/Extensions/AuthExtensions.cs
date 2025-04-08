using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Monq.Core.BasicDotNetMicroservice.Extensions;

public static class AuthExtensions
{
    static readonly IEnumerable<string> _scopeClaimTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
        "http://schemas.microsoft.com/identity/claims/scope",
        "scope"
    };

    public static AuthorizationPolicyBuilder RequireScope(this AuthorizationPolicyBuilder builder, params string[] scopes)
    {
        return builder.RequireAssertion(context =>
            context.User
                .Claims
                .Where(c => _scopeClaimTypes.Contains(c.Type))
                .SelectMany(c => c.Value.Split(' '))
                .Any(s => scopes.Contains(s, StringComparer.Ordinal))
        );
    }
}
