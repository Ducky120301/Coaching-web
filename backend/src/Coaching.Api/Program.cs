using Coaching.Api.Auth;
using Coaching.Api.Common;
using Coaching.Api.Controllers;
using Coaching.Application;
using Coaching.Application.Abstractions;
using Coaching.Infrastructure;
using Coaching.Infrastructure.Emailing;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

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

// @ADR-f01-p1-tai-khoan-hoc-vien-007: chốt 2 — /api/test/emails chỉ tồn tại khi Email:Provider=Capture
var captureEmails = EmailProviders.IsCapture(builder.Configuration);
builder.Services.AddControllers()
    .ConfigureApplicationPartManager(parts =>
    {
        if (!captureEmails)
            parts.FeatureProviders.Add(new ExcludeControllersFeatureProvider(typeof(TestEmailsController)));
    })
    .ConfigureApiBehaviorOptions(options =>
    // Lỗi model-binding tự động của [ApiController] cũng phải là ProblemDetails có `code` + tiêu đề tiếng Việt, giữ `errors`.
    options.InvalidModelStateResponseFactory = context =>
    {
        var problem = new ValidationProblemDetails(context.ModelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Dữ liệu gửi lên không hợp lệ.",
            Type = "request.invalid",
        };
        problem.Extensions["code"] = "request.invalid";
        return new ObjectResult(problem) { StatusCode = StatusCodes.Status400BadRequest };
    });
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
