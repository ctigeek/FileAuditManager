using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http.Routing;

namespace FileAuditManager.Controllers.Registration
{
    public class LowercaseRouteConstraint : IHttpRouteConstraint
    {
        public bool Match(HttpRequestMessage request, IHttpRoute route, string parameterName, IDictionary<string, object> values, HttpRouteDirection routeDirection)
        {
            var path = request.RequestUri.AbsolutePath;
            return path.Equals(path.ToLowerInvariant(), StringComparison.InvariantCulture);
        }
    }
}