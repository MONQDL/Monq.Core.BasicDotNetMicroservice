namespace Monq.Core.BasicDotNetMicroservice;

/// <summary>
/// Constants to configure authentication.
/// </summary>
public static class AuthConstants
{
    internal const string AuthenticationScheme = "Bearer";

    internal static class AuthenticationConfiguration
    {
        public const string Authority = "AuthenticationEndpoint";
        public const string ScopeName = "ApiResource:Login";
        public const string ScopeSecret = "ApiResource:Password";

        public const string RequireHttpsMetadata = "RequireHttpsMetadata";
        public const string EnableCaching = "EnableCaching";
    }

    /// <summary>
    /// Authorization scopes.
    /// </summary>
    public static class AuthorizationScopes
    {
        /// <summary>
        /// Read scope.
        /// </summary>
        public const string Read = "read";
        /// <summary>
        /// Write scope.
        /// </summary>
        public const string Write = "write";
    }
}
