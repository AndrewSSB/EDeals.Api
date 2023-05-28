using EDeals.Api.Attributes;
using EDeals.Api.GatewayServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Threading;

namespace EDeals.Api.Middlewares
{
    public class GatewayMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly HashSet<string> _availableMicroservices;

        public GatewayMiddleware(RequestDelegate next)
        {
            _next = next;
            _availableMicroservices = Enum.GetNames<EDealsMicroserviceTypes>().Select(x => x.ToLower()).ToHashSet();
        }

        public async Task InvokeAsync(HttpContext context, ILogger<GatewayMiddleware> logger, IAuthorizationService authorizationService, IGatewayService gatewayService)
        {
            using var cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);

            if (!context.Request.Path.HasValue)
            {
                await _next(context);
                return;
            }

            var microserviceName = GetMicroserviceNameFromContextPath(context);
            if (microserviceName == null)
            {
                await _next(context);
                return;
            }

            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                var authenticationResult = await context.AuthenticateAsync();
                if (authenticationResult.Succeeded)
                {
                    context.User = authenticationResult.Principal;
                }
            }

            var endpoint = context.GetEndpoint();

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
            return;
        }

        private string? GetMicroserviceNameFromContextPath(HttpContext context)
        {
            var path = context.Request.Path.Value!.AsSpan().TrimStart('/');
            var slashIndex = path.IndexOf('/');
            var pathName = path[..(slashIndex == -1 ? 0 : slashIndex)].ToString().ToLower();
            var microserviceName = _availableMicroservices.Contains(pathName) ? pathName : null;

            return microserviceName;
        }

        private async Task<bool> PassesDefaultAuthentication(HttpContext context, IAuthorizationService authorizationService, CancellationToken cancellationToken)
        {
            // Enforce authentication
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                await context.ChallengeAsync();
                return false;
            }

            // Authorize user with a default policy
            var authorizationResult = await authorizationService.AuthorizeAsync(context.User, "User");
            if (!authorizationResult.Succeeded)
            {
                await context.ForbidAsync();
                return false;
            }
         
            return true;
        }

        private static async Task<bool> PassesEndpointAuthentication(HttpContext context, Endpoint endpoint, IAuthorizationService authorizationService)
        {
            var authorizeAttributes = endpoint.Metadata.OfType<AuthorizeAttribute>().ToList();
            var allowAnonymousAttributes = endpoint.Metadata.OfType<AllowAnonymousAttribute>().ToList();

            if (allowAnonymousAttributes.Count > 0)
            {
                return true;
            }

            if (authorizeAttributes.Count > 0 && (!context.User.Identity?.IsAuthenticated ?? true))
            {
                await context.ChallengeAsync();
                return false;
            }

            foreach (var authorizeAttribute in authorizeAttributes)
            {
                if (!IsUserInValidRole(authorizeAttribute.Roles, context))
                {
                    await context.ForbidAsync();
                    return false;
                }

                if (!await IsUserMeetingPolicy(authorizeAttribute.Policy, context, authorizationService))
                {
                    await context.ForbidAsync();
                    return false;
                }
            }

            return true;
        }

        private static bool IsUserInValidRole(string? roles, HttpContext context)
        {
            if (string.IsNullOrEmpty(roles)) return true;

            return roles
                .Split(',')
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .Any(role => context.User.IsInRole(role));
        }

        private static async Task<bool> IsUserMeetingPolicy(string? policy, HttpContext context, IAuthorizationService authorizationService)
        {
            if (string.IsNullOrWhiteSpace(policy))
            {
                return true;
            }

            var authorizationResult = await authorizationService.AuthorizeAsync(context.User, policy);
            return authorizationResult.Succeeded;
        }
    }
}
