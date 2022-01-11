﻿namespace Swan.Data.Extensions;

/// <summary>
/// Provides extension methods for <see cref="DbConnection"/> objects.
/// </summary>
public static partial class ConnectionExtensions
{
    /// <summary>
    /// Retrieves the <see cref="DbProvider"/> associated with the given connection.
    /// </summary>
    /// <param name="connection">The connection to extract the provider from.</param>
    /// <returns>The associated DB provider.</returns>
    public static DbProvider Provider(this DbConnection connection) => connection is null
        ? throw new ArgumentNullException(nameof(connection))
        : DbProvider.FromConnection(connection);

    public static async Task<IReadOnlyList<string>> TableNames(this DbConnection connection)
    {
        if (connection is null)
            throw new ArgumentNullException(nameof(connection));

        await connection.EnsureIsValidAsync().ConfigureAwait(false);

        var tables = new List<string>();
        var dt = connection.GetSchema("Tables");
        foreach (DataRow row in dt.Rows)
        {
            string tablename = (string)row[2];
            tables.Add(tablename);
        }
        return tables;
    }

    /// <summary>
    /// Acquires a connected table context that can be used to inspect the associated
    /// table schema and issue CRUD commands. Once the schema is obtained, it is cached
    /// and reused whenever the table context is re-acquired. Caching keys are
    /// computed based on the connection string, provider type and table name and schema.
    /// </summary>
    /// <param name="connection">The associated connection.</param>
    /// <param name="tableName">The associated table name.</param>
    /// <param name="schema">The optional schema.</param>
    /// <returns>A connected table context.</returns>
    public static ITableContext Table(this DbConnection connection, string tableName, string? schema = default) =>
        new TableContext(connection, tableName, schema);

    /// <summary>
    /// Acquires a typed, connected table context that can be used to inspect the associated
    /// table schema and issue CRUD commands. Once the schema is obtained, it is cached
    /// and reused whenever the table context is re-acquired. Caching keys are
    /// computed based on the connection string, provider type and table name and schema.
    /// </summary>
    /// <param name="connection">The associated connection.</param>
    /// <param name="tableName">The associated table name.</param>
    /// <param name="schema">The optional schema.</param>
    /// <returns>A connected table context.</returns>
    public static ITableContext<T> Table<T>(this DbConnection connection, string tableName, string? schema = default)
        where T : class =>
        new TableContext<T>(connection, tableName, schema);

    /// <summary>
    /// Ensures the connection state is open and that the <see cref="DbConnection.Database"/> property has been set.
    /// </summary>
    /// <param name="connection">The connection to check for validity.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An awaitable task.</returns>
    public static async Task EnsureIsValidAsync(this DbConnection connection, CancellationToken ct = default)
    {
        if (connection is null)
            throw new ArgumentNullException(nameof(connection));

        if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(ct).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(connection.Database))
            throw new InvalidOperationException($"{nameof(connection)}.{nameof(connection.Database)} must be set.");
    }

    /// <summary>
    /// Ensures the connection state is open and that the <see cref="DbConnection.Database"/> property has been set.
    /// </summary>
    /// <param name="connection">The connection to check for validity.</param>
    public static void EnsureIsValid(this DbConnection connection)
    {
        if (connection is null)
            throw new ArgumentNullException(nameof(connection));

        if (connection.State != ConnectionState.Open)
            connection.Open();

        if (string.IsNullOrWhiteSpace(connection.Database))
            throw new InvalidOperationException($"{nameof(connection)}.{nameof(connection.Database)} must be set.");
    }

    /// <summary>
    /// Starts a fluent command definition using a <see cref="CommandSource"/>.
    /// When done, use the <see cref="CommandSource.EndCommandText"/> method call
    /// to extract the action <see cref="DbCommand"/>.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="initialText">The optional, initial command text to start building upon.</param>
    /// <returns>A fluent command definition.</returns>
    public static CommandSource BeginCommandText(this DbConnection connection, string? initialText = default) => connection is null
        ? throw new ArgumentNullException(nameof(connection))
        : new(connection, initialText);
}
