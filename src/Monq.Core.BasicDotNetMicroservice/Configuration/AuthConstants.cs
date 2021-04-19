namespace Monq.Core.BasicDotNetMicroservice
{
    public static class AuthConstants
    {
        internal static class AuthenticationConfiguration
        {
            public const string Authority = "AuthenticationEndpoint";
            public const string ScopeName = "ApiResource:Login";
            public const string ScopeSecret = "ApiResource:Password";

            public const string RequireHttpsMetadata = "RequireHttpsMetadata";
            public const string EnableCaching = "EnableCaching";
        }

        public static class AuthorizationScopes
        {
            public const string Read = "read";
            public const string Write = "write";
            public const string SmonAdmin = "smon-admin";
            public const string CloudAdmin = "cloud-admin";
        }
    }
}
