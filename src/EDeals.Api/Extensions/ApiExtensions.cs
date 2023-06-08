namespace EDeals.Api.Extensions
{
    internal static class ApiExtensions
    {
        public static void AddApplicationSettings(WebApplicationBuilder builder)
        {
            builder.Configuration.AddJsonFile("appsettings.json", false, true);
            builder.Configuration.AddJsonFile("appsettings.Local.json", true, true);
            builder.Configuration.AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", false, true);
            builder.Configuration.AddEnvironmentVariables();
        }
    }
}
