using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace Microsoft.Extensions.DependencyInjection;

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(List<string>))]
internal partial class MigrationListContext : JsonSerializerContext
{
}

/// <summary>
/// NativeAOT-compatible extension methods for Db Schema initialization.
/// Uses pre-generated SQL scripts instead of runtime migration execution.
/// </summary>
public static class NativeAotDbSchemaExtensions
{
    /// <summary>
    /// Create schema on empty database or validate migrations if schema exists.
    /// NativeAOT-compatible version that uses pre-generated SQL scripts.
    /// </summary>
    /// <typeparam name="T">The concrete database context.</typeparam>
    /// <param name="app">The <see cref="IApplicationBuilder"/> object.</param>
    /// <param name="getSchemaSql">Function that returns the SQL script for schema creation.</param>
    /// <param name="terminateOnException">If true when the exception occurs the application will be terminated.</param>
    /// <param name="sleepBeforeTerminate">If true when <paramref name="terminateOnException"/> the main thread will sleep before terminate.</param>
    /// <param name="terminationSleepMilliseconds">The sleep interval when <paramref name="terminateOnException"/> is true and <paramref name="sleepBeforeTerminate"/> is true.</param>
    public static void CreateDbSchemaOnFirstRunNative<T>(
        this IApplicationBuilder app,
        Func<string> getSchemaSql,
        bool terminateOnException = true,
        bool sleepBeforeTerminate = true,
        int terminationSleepMilliseconds = 10000)
            where T : DbContext
    {
        using var scope = app.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;
        var factory = services.GetRequiredService<ILoggerFactory>();
        var logger = factory.CreateLogger("DbInitializer");
        var exceptionOccurred = false;
        try
        {
            var context = services.GetRequiredService<T>();
            var sql = getSchemaSql();

            if (HasTables(context))
            {
                var serviceMigrations = ParseMigrationsFromSql(sql);
                CheckMigrationsHistory(context, serviceMigrations);
            }
            else
            {
                InitializeSchemaFromSql(context, sql);
            }
        }
        catch (DbSchemaValidationException e)
        {
            logger.LogCritical(e, "An error occurred during validation the DB schema.");
            exceptionOccurred = true;
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "An error occurred on creating the DB schema.");
            exceptionOccurred = true;
        }
        if (exceptionOccurred && terminateOnException)
        {
            if (sleepBeforeTerminate)
                Thread.Sleep(terminationSleepMilliseconds);
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Create schema on empty database or validate migrations if schema exists.
    /// NativeAOT-compatible version that loads SQL from embedded resource.
    /// </summary>
    /// <typeparam name="T">The concrete database context.</typeparam>
    /// <param name="app">The <see cref="IApplicationBuilder"/> object.</param>
    /// <param name="sqlAssembly">Assembly containing the embedded SQL resource.</param>
    /// <param name="resourceName">The resource name (e.g., "MyService.PgSchema.sql").</param>
    /// <param name="terminateOnException">If true when the exception occurs the application will be terminated.</param>
    /// <param name="sleepBeforeTerminate">If true when <paramref name="terminateOnException"/> the main thread will sleep before terminate.</param>
    /// <param name="terminationSleepMilliseconds">The sleep interval when <paramref name="terminateOnException"/> is true and <paramref name="sleepBeforeTerminate"/> is true.</param>
    public static void CreateDbSchemaOnFirstRunNative<T>(
        this IApplicationBuilder app,
        Assembly sqlAssembly,
        string resourceName = "PgSchema.sql",
        bool terminateOnException = true,
        bool sleepBeforeTerminate = true,
        int terminationSleepMilliseconds = 10000)
            where T : DbContext
    {
        app.CreateDbSchemaOnFirstRunNative<T>(
            () =>
            {
                var stream = sqlAssembly.GetManifestResourceStream(resourceName)
                    ?? throw new InvalidOperationException(
                        $"Embedded resource '{resourceName}' not found in assembly '{sqlAssembly.FullName}'.");
                using var reader = new StreamReader(stream, Encoding.UTF8);
                return reader.ReadToEnd();
            },
            terminateOnException,
            sleepBeforeTerminate,
            terminationSleepMilliseconds);
    }

    static void InitializeSchemaFromSql(DbContext context, string sql)
    {
        context.Database.ExecuteSqlRaw(sql);
    }

    static bool HasTables(DbContext context)
    {
        const string sql = @"
                SELECT CASE WHEN COUNT(*) = 0 THEN FALSE ELSE TRUE end as ""Value""
                    FROM information_schema.tables
                WHERE table_schema NOT IN ('pg_catalog', 'information_schema') and table_type = 'BASE TABLE'
                ";
        var creatorContext = context.Database.SqlQueryRaw<bool>(sql);
        return creatorContext.First<bool>();
    }

    static List<string> GetAppliedMigrationsFromDb(DbContext context)
    {
        const string sql = @"
            SELECT migration_id FROM ""__EFMigrationsHistory"" ORDER BY migration_id
            ";
        var result = context.Database.SqlQueryRaw<string>(sql).ToList();
        return result;
    }

    static List<string> ParseMigrationsFromSql(string sql)
    {
        using var reader = new StringReader(sql);
        var firstLine = reader.ReadLine();
        if (firstLine == null || !firstLine.StartsWith("-- MONQ_MIGRATIONS:"))
        {
            throw new InvalidOperationException(
                "SQL script does not contain migration metadata. " +
                "Expected first line: '-- MONQ_MIGRATIONS: [\"Migration1\", \"Migration2\"]'. " +
                "Use the MSBuild target to generate the script with embedded migration info.");
        }

        var json = firstLine.Substring("-- MONQ_MIGRATIONS:".Length).Trim();
        var migrations = JsonSerializer.Deserialize(json, MigrationListContext.Default.ListString)
            ?? throw new InvalidOperationException("Failed to parse migrations metadata from SQL script.");
        return migrations;
    }

    static void CheckMigrationsHistory(DbContext context, List<string> serviceMigrations)
    {
        var appliedMigrations = GetAppliedMigrationsFromDb(context).OrderBy(x => x);
        var sortedServiceMigrations = serviceMigrations.OrderBy(x => x);
        var diffMigrations = sortedServiceMigrations.Except(appliedMigrations);

        if (diffMigrations.Any())
            throw new DbSchemaValidationException("Error during Database schema validation. " +
                $"There are difference between applied migrations and service migrations.{Environment.NewLine}" +
                $"Applied migrations: {FormatMigrationsAsJson(appliedMigrations)}{Environment.NewLine}" +
                $"Service migrations: {FormatMigrationsAsJson(sortedServiceMigrations)}{Environment.NewLine}" +
                $"Difference: {FormatMigrationsAsJson(diffMigrations)}");
    }

    static string FormatMigrationsAsJson(IEnumerable<string> migrations)
    {
        var migrationArray = migrations.ToArray();
        if (migrationArray.Length == 0)
            return "[]";

        var result = new StringBuilder("[\n");
        for (int i = 0; i < migrationArray.Length; i++)
        {
            result.Append($"  \"{migrationArray[i]}\"");
            if (i < migrationArray.Length - 1)
                result.Append(",");
            result.Append("\n");
        }
        result.Append("]");
        return result.ToString();
    }

    /// <summary>
    /// Exception throws on Database schema validation error.
    /// </summary>
    public class DbSchemaValidationException : Exception
    {
        /// <summary>Initializes a new instance of the <see cref="DbSchemaValidationException" /> class with a specified error message.</summary>
        /// <param name="message">The message that describes the error.</param>
        public DbSchemaValidationException(string message) : base(message)
        {
        }
    }
}
