using Microsoft.Extensions.Logging;
using Wolverine.Configuration;
using Wolverine.Runtime;
using Wolverine.Transports;
using Wolverine.Transports.Sending;

namespace Wolverine.RDBMS.Transport;

internal class DatabaseControlEndpoint : Endpoint
{
    public Guid NodeId { get; }
    private readonly DatabaseControlTransport _parent;
    private readonly System.Threading.Timer _timer;

    public DatabaseControlEndpoint(DatabaseControlTransport parent, Guid nodeId) : base(new Uri($"{DatabaseControlTransport.ProtocolName}://{nodeId}"), EndpointRole.System)
    {
        NodeId = nodeId;
        _parent = parent;
        Mode = EndpointMode.BufferedInMemory;
        ExecutionOptions.MaxDegreeOfParallelism = 1;
        ExecutionOptions.EnsureOrdered = true;
    }

    public override ValueTask<IListener> BuildListenerAsync(IWolverineRuntime runtime, IReceiver receiver)
    {
        return new ValueTask<IListener>(new DatabaseControlListener(_parent, this, receiver, runtime.LoggerFactory.CreateLogger<DatabaseControlListener>(), runtime.Options.Durability.Cancellation));
    }

    protected override ISender CreateSender(IWolverineRuntime runtime)
    {
        return new DatabaseControlSender(this, _parent, runtime.LoggerFactory.CreateLogger<DatabaseControlSender>(), runtime.Options.Durability.Cancellation);
    }
}