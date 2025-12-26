namespace Monq.Core.BasicDotNetMicroservice.Sinks.Loki;

#pragma warning disable CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена
public class NoAuthCredentials : LokiCredentialsBase
{
    public NoAuthCredentials(string url)
        : base(url) { }
}

public class BasicAuthCredentials : LokiCredentialsBase
{
    public BasicAuthCredentials(string url, string username, string password)
        : base(url)
    {
        Url = url;
        Username = username;
        Password = password;
    }

    public string Username { get; }

    public string Password { get; }
}

public abstract class LokiCredentialsBase
{
    public string Url { get; protected set; }

    protected LokiCredentialsBase(string url)
    {
        Url = url;
    }
}
