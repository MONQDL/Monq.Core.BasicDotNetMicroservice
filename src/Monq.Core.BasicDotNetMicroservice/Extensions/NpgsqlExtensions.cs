using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Monq.Core.BasicDotNetMicroservice.Extensions;

/// <summary>
/// Extension methods to work with Npgsql (EF provider for PostgreSQL).
/// </summary>
public static class NpgsqlExtensions
{
    /// <summary>
    /// Import data in binary format.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="dbSet"><see cref="DbSet{TEntity}"/>.</param>
    /// <param name="entities">Entity list.</param>
    /// <param name="columns">Column names.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="selectors">
    /// <para>Value selectors.</para>
    /// <para>Must <b>strictly</b> correspond to the order in <paramref name="columns"/>.</para>
    /// <para>Selector returns property value along with the corresponding database column type <see cref="NpgsqlDbType"/>.</para>
    /// </param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public static async Task BinaryImport<T>(
        this DbSet<T> dbSet,
        IEnumerable<T> entities,
        IEnumerable<string> columns,
        CancellationToken cancellationToken = default,
        params Func<T, (object?, NpgsqlDbType)>[] selectors) where T : class
    {
        if (selectors.Length == 0)
            throw new ArgumentException("Selectors are not defined.", nameof(selectors));

        var entityType = dbSet.EntityType;
        var table = entityType.GetTableMappings().First().Table;

        var currentDbContext = dbSet.GetService<ICurrentDbContext>();

        // No using here.
        var connection = currentDbContext.Context.Database.GetDbConnection();
        if (connection is not NpgsqlConnection npgsqlConnection)
            throw new NotSupportedException($"Connection is not of type {nameof(NpgsqlConnection)}.");

        var columnQuery = string.Join(", ", columns.Select(x => $"\"{x}\""));

        if (npgsqlConnection.State != ConnectionState.Open)
            await npgsqlConnection.OpenAsync(cancellationToken);
        using var copyStream = await npgsqlConnection.BeginBinaryImportAsync(
            @$"COPY ""{table.Name}"" ({columnQuery}) FROM STDIN BINARY", cancellationToken);
        foreach (var entity in entities)
        {
            await copyStream.StartRowAsync(cancellationToken);
            foreach (var selector in selectors)
            {
                var (propVal, dbType) = selector(entity);
                await copyStream.WriteAsync(propVal, dbType, cancellationToken);
            }
        }
        await copyStream.CompleteAsync(cancellationToken);
    }

    /// <summary>
    /// Insert data as one array, ignoring insertion conflicts.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="dbSet"><see cref="DbSet{TEntity}"/>.</param>
    /// <param name="columns">Column names.</param>
    /// <param name="values">
    /// <para>Data to insert: one array for each column in <paramref name="columns"/>.</para>
    /// <para>Must <b>strictly</b> correspond to the order in <paramref name="columns"/>.</para>
    /// <para>Must be the same length.</para>
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    public static async Task InsertFromUnnest<T>(
        this DbSet<T> dbSet,
        IEnumerable<string> columns,
        object[] values,
        CancellationToken cancellationToken = default) where T : class
    {
        var entityType = dbSet.EntityType;
        var table = entityType.GetTableMappings().First().Table;

        var currentDbContext = dbSet.GetService<ICurrentDbContext>();
        var database = currentDbContext.Context.Database;

        var columnQuery = string.Join(", ", columns.Select(x => $"\"{x}\""));
        var unnestBody = string.Join(',', Enumerable.Range(0, columns.Count()).Select(x => $"{{{x}}}"));
        var sql = @$"
            INSERT INTO ""{table.Name}"" ({columnQuery})
            SELECT * FROM unnest({unnestBody})
            ON CONFLICT DO NOTHING;
            ";
        var query = FormattableStringFactory.Create(sql, values);

#if NET7_0_OR_GREATER
        await database.ExecuteSqlAsync(query, cancellationToken);
#else
        await database.ExecuteSqlInterpolatedAsync(query, cancellationToken);
#endif
    }
}
