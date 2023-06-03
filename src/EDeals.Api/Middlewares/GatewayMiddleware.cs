using EDeals.Api.Attributes;
using EDeals.Api.GatewayServices;
using EDeals.Api.RedisServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using System.Threading;

namespace EDeals.Api.Middlewares
{
    public class GatewayMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly HashSet<string> _availableMicroservices;

        // TODO: remove comment to add redis
        //private readonly IJWTRevocationService _jwtRevocationService;
        
        public GatewayMiddleware(RequestDelegate next)//, IJWTRevocationService jwtRevocationService)
        {
            _next = next;
            _availableMicroservices = Enum.GetNames<EDealsMicroserviceTypes>().Select(x => x.ToLower()).ToHashSet();
            //_jwtRevocationService = jwtRevocationService;
        }

        public async Task InvokeAsync(HttpContext context, 
            ILogger<GatewayMiddleware> logger, 
            IAuthorizationService authorizationService, 
            IGatewayService gatewayService)
        {
            using var cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);

            if (!context.Request.Path.HasValue)
            {
                await _next(context);
                return;
            }
            
            // Check if we are supporting that microservice for automatic forwarding
            var microserviceName = GetMicroserviceNameFromContextPath(context);
            if (microserviceName == null)
            {
                await _next(context);
                return;
            }
            
            // Authenticate user
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                var authenticationResult = await context.AuthenticateAsync();
                if (authenticationResult.Succeeded)
                {
                    context.User = authenticationResult.Principal;
                }
            }

            var endpoint = context.GetEndpoint();

            // Check if any [OverrideGateway] is present on the endpoint,
            // and if yes, execute the endpoint itself instead of automatically forwarding the request
            if (endpoint != null)
            {
                var overrideGatewayAttributes = endpoint.Metadata.OfType<OverrideGatewayAttribute>().ToList();
                if (overrideGatewayAttributes.Count > 0)
                {
                    await _next(context);
                    return;
                }
            }

            if (context.Request.Method.ToUpper() != "OPTIONS")
            {
                if (endpoint != null && !await PassesEndpointAuthentication(context, endpoint, authorizationService))
                {
                    return;
                }
                else if (endpoint == null && !await PassesDefaultAuthentication(context, authorizationService, cancellationToken.Token))
                {
                    return;
                }
            }

            // Forward request to the specific microservice
            context.Request.Path = context.Request.Path.Value[$"/{microserviceName}".Length..];
            logger.LogInformation("Forwarding request for path {path}", context.Request.Path.Value);
            await gatewayService.ForwardRequest(context, Enum.Parse<EDealsMicroserviceTypes>(microserviceName, true), cancellationToken.Token);
            logger.LogInformation("{protocol} {method} {path} responded with {code}", context.Request.Protocol.Split('/').FirstOrDefault(), context.Request.Method, context.Request.Path.Value, context.Response.StatusCode);

            // TODO: Enable this
            //await RevokeTokenIfNeeded(context.Request.Method, context.Request.Path, context.Request.Headers.Authorization);

            return;
        }

        private async Task<bool> PassesDefaultAuthentication(HttpContext context, IAuthorizationService authorizationService, CancellationToken cancellationToken)
        {
            // Enforce authentication
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                await context.ChallengeAsync();
                return false;
            }

            // Check for blacklisted tokens
            var token = context.Request.Headers.Authorization;
            // TODO: Remove comment to add redis
            //if (await _jwtRevocationService.IsTokenRevoked(token))
            //{
            //    await context.ChallengeAsync();
            //    return false;
            //}

            // Authorize user with a default policy
            var authorizationResult = await authorizationService.AuthorizeAsync(context.User, "User");
            if (!authorizationResult.Succeeded)
            {
                await context.ChallengeAsync();
                return false;
            }
         
            return true;
        }

        private async Task<bool> PassesEndpointAuthentication(HttpContext context, Endpoint endpoint, IAuthorizationService authorizationService)
        {
            // Use what the endpoint has defined for authorization
            var authorizeAttributes = endpoint.Metadata.OfType<AuthorizeAttribute>().ToList();
            var allowAnonymousAttributes = endpoint.Metadata.OfType<AllowAnonymousAttribute>().ToList();

            // Skip authorization if any AllowAnonymous attribute is present
            if (allowAnonymousAttributes.Count > 0)
            {
                return true;
            }

            // Check for blacklisted tokens
            var token = context.Request.Headers.Authorization;
            // TODO: Remove comment to add redis
            //if (await _jwtRevocationService.IsTokenRevoked(token))
            //{
            //    await context.ChallengeAsync();
            //    return false;
            //}

            // Enforce authentication
            if (authorizeAttributes.Count > 0 && (!context.User.Identity?.IsAuthenticated ?? true))
            {
                await context.ChallengeAsync();
                return false;
            }
            
            // Handle authorization attributes
            foreach (var authorizeAttribute in authorizeAttributes)
            {
                // Role validation
                if (!IsUserInValidRole(authorizeAttribute.Roles, context))
                {
                    await context.ForbidAsync();
                    return false;
                }

                // Policy validation
                if (!await IsUserMeetingPolicy(authorizeAttribute.Policy, context, authorizationService))
                {
                    await context.ForbidAsync();
                    return false;
                }
            }

            return true;
        }

        private async Task<bool> IsUserMeetingPolicy(string? policy, HttpContext context, IAuthorizationService authorizationService)
        {
            if (string.IsNullOrWhiteSpace(policy))
            {
                return true;
            }

            var authorizationResult = await authorizationService.AuthorizeAsync(context.User, policy);
            return authorizationResult.Succeeded;
        }

        private async Task RevokeTokenIfNeeded(string method, string path, string? token = null)
        {
            // TODO: Remove comment to add redis
            //if (HttpMethods.IsDelete(method) && path.Contains("/api/user/account"))
            //{
            //    await _jwtRevocationService.RevokeToken(token);
            //}

            //if (HttpMethods.IsPost(method) && path.Contains("/api/authentication/logout"))
            //{
            //    await _jwtRevocationService.RevokeToken(token);
            //}
        }

        private bool IsUserInValidRole(string? roles, HttpContext context)
        {
            if (string.IsNullOrEmpty(roles)) return true;

            return roles
                .Split(',')
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .Any(role => context.User.IsInRole(role));
        }

        private string? GetMicroserviceNameFromContextPath(HttpContext context)
        {
            var path = context.Request.Path.Value!.AsSpan().TrimStart('/');
            var slashIndex = path.IndexOf('/');
            var pathName = path[..(slashIndex == -1 ? 0 : slashIndex)].ToString().ToLower();
            var microserviceName = _availableMicroservices.Contains(pathName) ? pathName : null;

            return microserviceName;
        }
    }
}
