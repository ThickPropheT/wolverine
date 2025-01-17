using System.Data.Common;
using JasperFx.Core.Exceptions;
using Wolverine.Runtime;
using Wolverine.Runtime.Agents;

namespace Wolverine.RDBMS.Polling;

internal class DatabaseOperationBatch : IAgentCommand
{
    private readonly IMessageDatabase _database;
    private readonly IDatabaseOperation[] _operations;

    public DatabaseOperationBatch(IMessageDatabase database, IDatabaseOperation[] operations)
    {
        _database = database;
        _operations = operations;
    }

    public async IAsyncEnumerable<object> ExecuteAsync(IWolverineRuntime runtime, CancellationToken cancellationToken)
    {
        var builder = _database.ToCommandBuilder();
        foreach (var operation in _operations)
        {
            operation.ConfigureCommand(builder);
        }

        var cmd = builder.Compile();
        var conn = cmd.Connection;
        await conn!.OpenAsync(cancellationToken);

        var tx = await conn.BeginTransactionAsync(cancellationToken);
        cmd.Transaction = tx;

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        var exceptions = new List<Exception>();
        await ApplyCallbacksAsync(_operations, reader, exceptions, cancellationToken);
        await reader.CloseAsync();

        await tx.CommitAsync(cancellationToken);

        foreach (var command in _operations.SelectMany(x => x.PostProcessingCommands()))
        {
            yield return command;
        }

        await conn.CloseAsync();
    }
    
    public static async Task ApplyCallbacksAsync(IReadOnlyList<IDatabaseOperation> operations, DbDataReader reader,
        IList<Exception> exceptions,
        CancellationToken token)
    {
        var first = operations.First();

        if (!(first is IDoNotReturnData))
        {
            await first.ReadResultsAsync(reader, exceptions, token).ConfigureAwait(false);
            try
            {
                await reader.NextResultAsync(token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                if (first is IExceptionTransform t && t.TryTransform(e, out var transformed))
                {
                    throw transformed;
                }

                throw;
            }
        }

        foreach (var operation in operations.Skip(1))
        {
            if (operation is IDoNotReturnData)
            {
                continue;
            }

            await operation.ReadResultsAsync(reader, exceptions, token).ConfigureAwait(false);
            try
            {
                await reader.NextResultAsync(token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                if (operation is IExceptionTransform t && t.TryTransform(e, out var transformed))
                {
                    throw transformed;
                }

                throw;
            }
        }
        
        
    }

}