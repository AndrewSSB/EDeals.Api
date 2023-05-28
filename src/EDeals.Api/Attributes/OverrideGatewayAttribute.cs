namespace EDeals.Api.Attributes
{
    /// <summary>
    /// Marker attribute. If it is present on a controller or on an action
    /// then the Gateway Middleware will execute the action's body
    /// instead of executing the middleware's redirect logic
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class OverrideGatewayAttribute : Attribute
    {
    }
}
