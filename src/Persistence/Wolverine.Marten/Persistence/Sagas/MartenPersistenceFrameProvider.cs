﻿using JasperFx.CodeGeneration.Frames;
using JasperFx.CodeGeneration.Model;
using JasperFx.Core.Reflection;
using Lamar;
using Marten;
using Marten.Events;
using Wolverine.Configuration;
using Wolverine.Marten.Codegen;
using Wolverine.Persistence;
using Wolverine.Persistence.Sagas;

namespace Wolverine.Marten.Persistence.Sagas;

internal class MartenPersistenceFrameProvider : IPersistenceFrameProvider
{
    public bool CanPersist(Type entityType, IContainer container, out Type persistenceService)
    {
        persistenceService = typeof(IDocumentSession);
        return true;
    }

    public Type DetermineSagaIdType(Type sagaType, IContainer container)
    {
        var store = container.GetInstance<IDocumentStore>();
        return store.Options.FindOrResolveDocumentType(sagaType).IdType;
    }

    public void ApplyTransactionSupport(IChain chain, IContainer container)
    {
        if (!chain.Middleware.OfType<TransactionalFrame>().Any())
        {
            chain.Middleware.Add(new TransactionalFrame());
        }
    }

    public bool CanApply(IChain chain, IContainer container)
    {
        if (chain is SagaChain)
        {
            return true;
        }

        return 
               chain.ServiceDependencies(container, new []{typeof(IDocumentSession), typeof(IQuerySession)}).Any(x => x == typeof(IDocumentSession) || x.Closes(typeof(IEventStream<>)));
    }

    public Frame DetermineLoadFrame(IContainer container, Type sagaType, Variable sagaId)
    {
        return new LoadDocumentFrame(sagaType, sagaId);
    }

    public Frame DetermineInsertFrame(Variable saga, IContainer container)
    {
        return new DocumentSessionOperationFrame(saga, nameof(IDocumentSession.Insert));
    }

    public Frame CommitUnitOfWorkFrame(Variable saga, IContainer container)
    {
        var call = MethodCall.For<IDocumentSession>(x => x.SaveChangesAsync(default));
        call.CommentText = "Commit all pending changes";
        return call;
    }

    public Frame DetermineUpdateFrame(Variable saga, IContainer container)
    {
        return new DocumentSessionOperationFrame(saga, nameof(IDocumentSession.Update));
    }

    public Frame DetermineDeleteFrame(Variable sagaId, Variable saga, IContainer container)
    {
        return new DocumentSessionOperationFrame(saga, nameof(IDocumentSession.Delete));
    }
}