using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Coaching.Application.Common;
using Coaching.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Coaching.Api.Auth;

internal static class AuthenticationSetup
{
    public static IServiceCollection AddJwtAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = JwtRegisteredClaimNames.Sub,
                    RoleClaimType = JwtTokenGenerator.RoleClaimType,
                };
            });

        services.AddAuthorizationBuilder()
            // @BR-f00-codebase-001
            .AddPolicy(Policies.AdminOnly, p => p.RequireAuthenticatedUser().RequireRole(Roles.Admin))
            // @BR-f00-codebase-002
            .AddPolicy(Policies.StudentOnly, p => p.RequireAuthenticatedUser().RequireRole(Roles.Student));

        return services;
    }
}
