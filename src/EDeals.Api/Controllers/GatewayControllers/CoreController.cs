using EDeals.Api.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EDeals.Api.Controllers.GatewayControllers
{
    [Route("[controller]")]
    [ApiController]
    // In order to override the authorization used by the gateway middleware for a forwarded request
    // add an empty method (public void is fine) here with the correct method and route attribute (e.g. [HttpPost("api/v1/auth/endpoint")] )
    // and set the desired authorization attributes (e.g. [Authorize] or [Authorize("policy")] or [AllowAnonymous] etc.)
    public class CoreController : ControllerBase
    {
        [AllowAnonymous]
        [HttpPost("api/authentication/login")]
        public void Login() { }
        
        [AllowAnonymous]
        [HttpPost("api/authentication/register")]
        public void Register() { }
    }
}
