// <auto-generated/>
#pragma warning disable
using Microsoft.AspNetCore.Routing;
using System;
using System.Linq;
using Wolverine.Http;

namespace Internal.Generated.WolverineHandlers
{
    // START: POST_authenticated
    public class POST_authenticated : Wolverine.Http.HttpHandler
    {
        private readonly Wolverine.Http.WolverineHttpOptions _options;

        public POST_authenticated(Wolverine.Http.WolverineHttpOptions options) : base(options)
        {
            _options = options;
        }



        public override async System.Threading.Tasks.Task Handle(Microsoft.AspNetCore.Http.HttpContext httpContext)
        {
            var authenticatedEndpoint = new WolverineWebApi.AuthenticatedEndpoint();
            var fakeAuthenticationMiddleware = new WolverineWebApi.FakeAuthenticationMiddleware();
            var (request, jsonContinue) = await ReadJsonAsync<WolverineWebApi.AuthenticatedRequest>(httpContext);
            if (jsonContinue == Wolverine.HandlerContinuation.Stop) return;
            var result = WolverineWebApi.FakeAuthenticationMiddleware.Before(request);
            if (!(result is Wolverine.Http.WolverineContinue))
            {
                await result.ExecuteAsync(httpContext).ConfigureAwait(false);
                return;
            }

            var result_of_Get = authenticatedEndpoint.Get(request);
            await WriteString(httpContext, result_of_Get);
        }

    }

    // END: POST_authenticated
    
    
}

