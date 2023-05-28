using EDeals.Api.Extensions;
using EDeals.Api.Settings;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddRouting(options => options.LowercaseUrls = true);

    // Add json / env files
    ApiExtensions.AddApplicationSettings(builder);

    // Configure builder services
    builder.Services.ConfigureSettings(builder.Configuration);

    var jwtSettings = builder.Configuration.GetSection(nameof(JwtSettings)).Get<JwtSettings>();
    builder.Services.ConfigureSwagger(jwtSettings!);

    builder.Services.ConfigureRolesAndPolicies();

    builder.Services.AddServices();

    // Add Serilog
    builder.Host.UseSerilog((context, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration));

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.AddMiddlewares();

    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex) when (ex is not OperationCanceledException && !ex.GetType().Name.Contains("StopTheHostException") && !ex.GetType().Name.Contains("HostAbortedException"))
{
    Log.Fatal(ex, "Unhandled exception in Program");
}
finally
{
    Log.Information("App shut down complete");
    Log.CloseAndFlush();
}