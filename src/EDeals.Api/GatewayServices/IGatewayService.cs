namespace EDeals.Api.GatewayServices
{
    public interface IGatewayService
    {
        Task ForwardRequest(HttpContext context, EDealsMicroserviceTypes type, CancellationToken cancellationToken);
    }
}
