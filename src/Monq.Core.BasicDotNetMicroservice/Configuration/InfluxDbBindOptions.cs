using System;

namespace Monq.Core.BasicDotNetMicroservice.Configuration;

/// <summary>
/// Bind-compatible options for InfluxDB connectivity.
/// Only contains properties that can be configured from configuration sources.
/// </summary>
public class InfluxDbBindOptions
{
    /// <summary>
    /// The base URI of the InfluxDB API where metrics are flushed.
    /// </summary>
    public Uri? BaseUri { get; set; }

    /// <summary>
    /// The InfluxDB database name where metrics are flushed.
    /// </summary>
    public string? Database { get; set; }

    /// <summary>
    /// The InfluxDB database password.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// The InfluxDB database's retention policy to target.
    /// </summary>
    public string? RetentionPolicy { get; set; }

    /// <summary>
    /// The InfluxDB database username.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Whether to create the database if it does not exist.
    /// </summary>
    public bool CreateDataBaseIfNotExists { get; set; } = true;

    /// <summary>
    /// The InfluxDB node write consistency.
    /// </summary>
    public string? Consistenency { get; set; }

    /// <summary>
    /// Maps to App.Metrics.Reporting.InfluxDB.InfluxDbOptions.
    /// </summary>
    public App.Metrics.Reporting.InfluxDB.InfluxDbOptions ToInfluxDbOptions()
    {
        return new App.Metrics.Reporting.InfluxDB.InfluxDbOptions
        {
            BaseUri = BaseUri,
            Database = Database,
            Password = Password,
            RetentionPolicy = RetentionPolicy,
            UserName = UserName,
            CreateDataBaseIfNotExists = CreateDataBaseIfNotExists,
            Consistenency = Consistenency
        };
    }
}
