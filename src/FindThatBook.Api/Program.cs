using System.Text;
using FindThatBook.Api;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using FindThatBook.Api.Auditing;
using FindThatBook.Api.ErrorHandling;
using FindThatBook.Application.Abstractions;
using FindThatBook.Application.Services;
using FindThatBook.Infrastructure;

const string clientCorsPolicy = "Client";

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
// Cloudflare sends location headers as UTF-8 (e.g. "Zürich"); Kestrel's default would reject or garble them.
builder.WebHost.ConfigureKestrel(kestrel => kestrel.RequestHeaderEncodingSelector = _ => Encoding.UTF8);

// snake_case JSON everywhere (see ApiJsonConventions): controllers, minimal endpoints, ProblemDetails, validation error keys.
builder.Services
    .AddControllers(options => options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider(ApiJsonConventions.NamingPolicy)))
    .AddJsonOptions(options => ApiJsonConventions.Apply(options.JsonSerializerOptions));
builder.Services.ConfigureHttpJsonOptions(options => ApiJsonConventions.Apply(options.SerializerOptions));
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = ApiJsonConventions.RenameTraceId);
builder.Services.AddExceptionHandler<UpstreamServiceFailedExceptionHandler>();
builder.Services.AddCors(options => options.AddPolicy(clientCorsPolicy, policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
          .AllowAnyHeader()
          .AllowAnyMethod()));
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<ClientContextResolver>();
builder.Services.AddSingleton<SearchAuditRecorder>();
builder.Services.AddScoped<CandidateExplainer>();
builder.Services.AddScoped<IBookSearchService, BookSearchService>();

WebApplication app = builder.Build();

if (!app.Services.GetRequiredService<IIntegrationStatus>().IsQueryExtractionConfigured)
{
    FindThatBook.Api.StartupLog.GeminiNotConfigured(app.Logger);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseCors(clientCorsPolicy);

// In the container the built React client lives in wwwroot; in local dev Vite serves it instead.
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

// Unknown API routes return 404; every other unknown path falls through to the React app.
app.MapFallback("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("index.html");

app.Run();

/// <summary>Exposed for WebApplicationFactory-based integration tests.</summary>
public partial class Program;
