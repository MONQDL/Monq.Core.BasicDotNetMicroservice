namespace Monq.Core.BasicDotNetMicroservice.Sinks.Loki;

internal static class LokiRouteBuilder
{
    public static string BuildPostUri(string host) =>
        host.Substring(host.Length - 1) != "/" ? $"{host}{PostDataUri}" : $"{host.TrimEnd('/')}{PostDataUri}";

    public const string PostDataUri = "/loki/api/v1/push";
}