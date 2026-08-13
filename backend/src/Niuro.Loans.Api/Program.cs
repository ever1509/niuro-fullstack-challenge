using Niuro.Loans.Api;
using Niuro.Loans.Application.Decisions;
using Niuro.Loans.Application.Decisions.Rules;
using Niuro.Loans.Application.LoanApplications;
using Niuro.Loans.Infrastructure;
using Niuro.Loans.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

const string FrontendCorsPolicy = "frontend";

builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("LoansDatabase")
    ?? throw new InvalidOperationException("Connection string 'LoansDatabase' is not configured."));

// The use case and the rule engine. Every IDenyRule registered here is picked up by the
// engine automatically, so a new rule is exactly one new line below and one new class.
builder.Services.AddScoped<IDenyRule, UnservedStateRule>();
builder.Services.AddScoped<IDenyRule, BlacklistedSsnRule>();
builder.Services.AddScoped<LoanDecisionEngine>();
builder.Services.AddScoped<SubmitLoanApplicationHandler>();

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

builder.Services.AddCors(options => options.AddPolicy(
    FrontendCorsPolicy,
    policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

await DatabaseInitializer.InitializeAsync(app.Services);

app.UseExceptionHandler();
app.UseCors(FrontendCorsPolicy);
app.MapControllers();

app.Run();

// Exposed so the integration tests can boot this exact application rather than a stand-in.
public partial class Program;
