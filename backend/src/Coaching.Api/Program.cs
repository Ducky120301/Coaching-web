using Coaching.Api.Auth;
using Coaching.Api.Common;
using Coaching.Application;
using Coaching.Application.Abstractions;
using Coaching.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddJwtAuth(builder.Configuration);
builder.Services.AddCoachingRateLimiting(builder.Configuration);
// @ADR-f00-codebase-004: mặc định lấy hop cuối X-Forwarded-For (ForwardLimit = 1; nginx compose nối IP thật vào cuối).
// Giả định triển khai: API CHỈ nhận request qua proxy — gọi thẳng API thì client tự đặt được XFF.
// Chỉnh được bằng cấu hình (vd. Render có thể thêm proxy): ForwardedHeaders:ForwardLimit, ForwardedHeaders:KnownProxies:N, ForwardedHeaders:KnownNetworks:N (CIDR).
var forwarded = builder.Configuration.GetSection("ForwardedHeaders");
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = forwarded.GetValue("ForwardLimit", 1);
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
    foreach (var proxy in forwarded.GetSection("KnownProxies").Get<string[]>() ?? [])
        options.KnownProxies.Add(System.Net.IPAddress.Parse(proxy));
    foreach (var network in forwarded.GetSection("KnownNetworks").Get<string[]>() ?? [])
        options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
});

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Configuration.GetValue("Database:SeedOnStartup", true))
    await app.Services.InitializeDatabaseAsync();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

await app.RunAsync();

/// <summary>Cho WebApplicationFactory trong integration test.</summary>
public partial class Program;
