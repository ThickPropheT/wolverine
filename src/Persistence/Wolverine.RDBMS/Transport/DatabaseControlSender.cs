using JasperFx.Core;
using Microsoft.Extensions.Logging;
using Weasel.Core;
using Wolverine.Runtime.Serialization;
using Wolverine.Transports.Sending;
using Wolverine.Util.Dataflow;

namespace Wolverine.RDBMS.Transport;

internal class DatabaseControlSender : ISender, IAsyncDisposable
{
    private readonly DatabaseControlEndpoint _endpoint;
    private readonly DatabaseControlTransport _transport;
    private readonly RetryBlock<Envelope> _retryBlock;

    public DatabaseControlSender(DatabaseControlEndpoint endpoint, DatabaseControlTransport transport, ILogger logger, CancellationToken cancellationToken)
    {
        _endpoint = endpoint;
        _transport = transport;
        Destination = endpoint.Uri;

        _retryBlock = new RetryBlock<Envelope>(sendMessage, logger, cancellationToken);
    }

    public bool SupportsNativeScheduledSend { get; } = false;
    public Uri Destination { get; }
    public async Task<bool> PingAsync()
    {
        try
        {
            await using var conn = _transport.Database.CreateConnection();
            await conn.OpenAsync();
            await conn.CloseAsync();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async ValueTask SendAsync(Envelope envelope)
    {
        // TODO -- make this be configurable
        envelope.DeliverWithin = 10.Seconds();

        await _retryBlock.PostAsync(envelope);
    }

    private async Task sendMessage(Envelope envelope, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return;
        
        await using var conn = _transport.Database.CreateConnection();
        

        try
        {
            await conn.OpenAsync(cancellationToken);
            
            await conn.CreateCommand(
                    $"insert into {_transport.TableName} (id, message_type, node_id, body, expires) values (@id, @messagetype, @node, @body, @expires)")
                .With("id", envelope.Id)
                .With("messagetype", envelope.MessageType)
                .With("node", _endpoint.NodeId)
                .With("body", EnvelopeSerializer.Serialize(envelope))
                .With("expires", DateTimeOffset.UtcNow.AddSeconds(30)).ExecuteNonQueryAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                throw;
            }
        }

        await conn.CloseAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _retryBlock.DrainAsync();
        _retryBlock.Dispose();
    }
}