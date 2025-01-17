using Weasel.Core;
using Wolverine.Persistence.Durability;

namespace Wolverine.RDBMS.Durability;

[Obsolete("Goes away when durability agent becomes just an agent")]
public class MoveReplayableErrorMessagesToIncoming : IDurabilityAction
{
    public string Description => "Moving Replayable Error Envelopes from DeadLetterTable to IncomingTable";

    public Task ExecuteAsync(IMessageDatabase database, IDurabilityAgent agent,
        IDurableStorageSession session)
    {
        return session.WithinTransactionAsync(() =>
            MoveReplayableErrorMessagesToIncomingAsync(session, database));
    }

    public Task MoveReplayableErrorMessagesToIncomingAsync(IDurableStorageSession session,
        IMessageDatabase wolverineDatabase)
    {
        if (session.Transaction == null)
        {
            throw new InvalidOperationException("No current transaction");
        }

        var insertIntoIncomingSql = $@"
insert into {wolverineDatabase.SchemaName}.{DatabaseConstants.IncomingTable} ({DatabaseConstants.IncomingFields}) 
select {DatabaseConstants.Body}, {DatabaseConstants.Id}, '{EnvelopeStatus.Incoming}', 0, null, 0, {DatabaseConstants.MessageType}, {DatabaseConstants.ReceivedAt}
from {wolverineDatabase.SchemaName}.{DatabaseConstants.DeadLetterTable} where {DatabaseConstants.Replayable} = @replayable";

        var removeFromDeadLetterSql =
            $"; delete from {wolverineDatabase.SchemaName}.{DatabaseConstants.DeadLetterTable} where {DatabaseConstants.Replayable} = @replayable";

        var removeFromDeadLetterSqlAndInsertIntoIncomingSql = $"{insertIntoIncomingSql}; {removeFromDeadLetterSql}";

        return session.CreateCommand(removeFromDeadLetterSqlAndInsertIntoIncomingSql)
            .With("replayable", true)
            .ExecuteNonQueryAsync(session.Cancellation);
    }
}