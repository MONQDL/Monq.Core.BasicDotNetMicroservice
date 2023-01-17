using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DbSchemaExtensions
    {
        static readonly JsonSerializerOptions _intendedSerializerOptions =
            new JsonSerializerOptions { WriteIndented = true };

        /// <summary>
        /// Create schema on empty database or validate migrations if schema exists.
        /// </summary>
        /// <typeparam name="T">The concrete database context.</typeparam>
        /// <param name="app">The <see cref="IApplicationBuilder"/> object.</param>
        /// <param name="terminateOnException">If true when the exeption eccures the application will be terminated.</param>
        /// <param name="sleepBeforeTerminate">If true when <paramref name="terminateOnException"/> the main thread will sleep before terminate.</param>
        /// <param name="terminationSleepMiliseconds">The sleep interval when <paramref name="terminateOnException"/> is true and <paramref name="sleepBeforeTerminate"/> is true.</param>
        public static void CreateDbSchemaOnFirstRun<T>(this IApplicationBuilder app,
            bool terminateOnException = true,
            bool sleepBeforeTerminate = true,
            int terminationSleepMiliseconds = 10000)
                where T : DbContext
        {
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var services = scope.ServiceProvider;
                var factory = services.GetRequiredService<ILoggerFactory>();
                var logger = factory.CreateLogger("DbInitializer");
                bool exceptionOccured = false;
                try
                {
                    var context = services.GetRequiredService<T>();

                    if (HasTables(context))
                        CheckMigrationsHistory(context);
                    else
                        InitializeSchema(context);
                }
                catch (DbSchemaValidationException e)
                {
                    logger.LogCritical(e, "An error occurred during validation the DB schema.");
                    exceptionOccured = true;
                }
                catch (Exception e)
                {
                    logger.LogCritical(e, "An error occurred on creating the DB schema.");
                    exceptionOccured = true;
                }
                if (exceptionOccured && terminateOnException)
                {
                    if (sleepBeforeTerminate)
                        Thread.Sleep(terminationSleepMiliseconds);
                    Environment.Exit(1);
                }
            }
        }

        static void InitializeSchema(DbContext context)
        {
            var migrator = context.Database.GetService<IMigrator>();
            migrator.Migrate();
        }

        static bool HasTables(DbContext context)
        {
            var creatorContext = context.Database.GetService<IRelationalDatabaseCreator>();
            return creatorContext.HasTables();
        }

        static void CheckMigrationsHistory(DbContext context)
        {
            var appliedMigrations = context.Database.GetAppliedMigrations().OrderBy(x => x);
            var migrations = context.Database.GetMigrations().OrderBy(x => x);
            var diffMigrations = migrations.Except(appliedMigrations);

            if (diffMigrations.Any())
                throw new DbSchemaValidationException("Error during Database schema validation. " +
                    $"There are difference between applied migrations and service migrations.{Environment.NewLine}" +
                    $"Applied migrations: {JsonSerializer.Serialize(appliedMigrations, _intendedSerializerOptions)}{Environment.NewLine}" +
                    $"Service migrations: {JsonSerializer.Serialize(migrations, _intendedSerializerOptions)}{Environment.NewLine}" +
                    $"Difference: {JsonSerializer.Serialize(diffMigrations, _intendedSerializerOptions)}");
        }

        public class DbSchemaValidationException : Exception
        {
            /// <summary>Initializes a new instance of the <see cref="DbSchemaValidationException" /> class with a specified error message.</summary>
            /// <param name="message">The message that describes the error.</param>
            public DbSchemaValidationException(string message) : base(message)
            {

            }
        }
    }
}