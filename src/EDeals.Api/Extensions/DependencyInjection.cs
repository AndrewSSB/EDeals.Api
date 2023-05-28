using EDeals.Api.GatewayServices;
using EDeals.Api.Middlewares;
using EDeals.Api.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace EDeals.Api.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection ConfigureSettings(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<JwtSettings>(configuration.GetSection(nameof(JwtSettings)));
            services.Configure<RestServiceSettings>(configuration.GetSection(nameof(RestServiceSettings)));

            return services;
        }

        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddHttpClient();
            services.AddSingleton<IGatewayService, GatewayService>();

            return services;
        }

        public static IApplicationBuilder AddMiddlewares(this IApplicationBuilder builder)
        {
            builder.UseMiddleware<GatewayMiddleware>();

            return builder;
        }

        public static IServiceCollection ConfigureSwagger(this IServiceCollection services, JwtSettings settings)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
            })
               .AddJwtBearer(options =>
               {
                   options.SaveToken = true;
                   options.RequireHttpsMetadata = false;
                   options.TokenValidationParameters = new TokenValidationParameters()
                   {
                       ValidateIssuer = false,
                       ValidateAudience = false,
                       IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(settings.Secret))
                   };
               });

            services.AddSwaggerGen(opt =>
            {
                opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Enter JWT",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "bearer"
                });
                opt.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type=ReferenceType.SecurityScheme,
                                Id="Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            return services;
        }

        public static IServiceCollection ConfigureRolesAndPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(opt =>
            {
                opt.AddPolicy("User", policy => policy.RequireRole("User").RequireAuthenticatedUser().Build());
                opt.AddPolicy("Admin", policy => policy.RequireRole("Admin").RequireAuthenticatedUser().Build());
                opt.AddPolicy("SuperAdmin", policy => policy.RequireRole("SuperAdmin").RequireAuthenticatedUser().Build());
            });

            return services;
        }
    }
}
