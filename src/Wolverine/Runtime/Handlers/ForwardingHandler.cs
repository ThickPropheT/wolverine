﻿using System.Threading;
using System.Threading.Tasks;
using JasperFx.Core.Reflection;

namespace Wolverine.Runtime.Handlers;

internal class ForwardingHandler<T, TDestination> : MessageHandler where T : IForwardsTo<TDestination>
{
    private readonly HandlerGraph _graph;
    private readonly Lazy<MessageHandler> _inner;

    public ForwardingHandler(HandlerGraph graph)
    {
        _graph = graph;
        Chain = new HandlerChain(typeof(T), graph);
        
        _inner = new Lazy<MessageHandler>(() => graph.HandlerFor(typeof(TDestination))!);
    }

    public override Task HandleAsync(MessageContext context, CancellationToken cancellation)
    {
        var innerMessage = context.Envelope!.Message!.As<T>();
        context.Envelope.Message = innerMessage.Transform();

        return _inner.Value.HandleAsync(context, cancellation);
    }
}