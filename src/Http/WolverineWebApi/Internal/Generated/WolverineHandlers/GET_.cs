// <auto-generated/>
#pragma warning disable
using Microsoft.AspNetCore.Routing;
using System;
using System.Linq;
using Wolverine.Http;

namespace Internal.Generated.WolverineHandlers
{
    // START: GET_
    public class GET_ : Wolverine.Http.HttpHandler
    {
        private readonly Wolverine.Http.WolverineHttpOptions _options;

        public GET_(Wolverine.Http.WolverineHttpOptions options) : base(options)
        {
            _options = options;
        }



        public override System.Threading.Tasks.Task Handle(Microsoft.AspNetCore.Http.HttpContext httpContext)
        {
            var homeEndpoint = new WolverineWebApi.HomeEndpoint();
            var result = homeEndpoint.Index();
            return result.ExecuteAsync(httpContext);
        }

    }

    // END: GET_
    
    
}

