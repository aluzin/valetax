using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Valetax.Application;
using Valetax.Application.Abstractions;
using Valetax.Application.Telemetry;
using Valetax.Api.Middleware;
using Valetax.Api.Swagger;
using Valetax.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var authenticationSection = builder.Configuration.GetRequiredSection("Authentication");
var openTelemetrySection = builder.Configuration.GetSection("OpenTelemetry");
var serviceName = openTelemetrySection["ServiceName"] ?? builder.Environment.ApplicationName;
var otlpEndpoint = openTelemetrySection["OtlpEndpoint"];

builder.Host.UseSerilog((context, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console()
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithProcessId()
        .Enrich.WithThreadId();
});

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter(ApplicationMetrics.MeterName)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddPrometheusExporter();
    })
    .WithTracing(tracing =>
    {
        tracing
            .AddSource(ApplicationTracing.ActivitySourceName)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation(options =>
            {
                options.FilterHttpRequestMessage = request => !string.Equals(
                    request.RequestUri?.Host,
                    "loki",
                    StringComparison.OrdinalIgnoreCase);
            });

        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            tracing.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
        }
    });

builder.Services.AddOptions<AppAuthenticationOptions>()
    .Bind(authenticationSection)
    .Validate(options => !string.IsNullOrWhiteSpace(options.RememberMeCode), "Authentication:RememberMeCode is not configured.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Jwt.Issuer), "Authentication:Jwt:Issuer is not configured.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Jwt.Audience), "Authentication:Jwt:Audience is not configured.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Jwt.SigningKey), "Authentication:Jwt:SigningKey is not configured.")
    .ValidateOnStart();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<AppAuthenticationOptions>>((options, authenticationOptions) =>
    {
        var auth = authenticationOptions.Value;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = auth.Jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = auth.Jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(auth.Jwt.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter a valid JWT bearer token."
    });
    options.OperationFilter<AuthorizeOperationFilter>();
});
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();
await app.Services.InitializeInfrastructureAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", () => Results.Redirect("/swagger"));
}

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapControllers();
app.MapPrometheusScrapingEndpoint();

app.Run();
