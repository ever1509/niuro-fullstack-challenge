using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Niuro.Loans.Application.Decisions.Rules;
using Niuro.Loans.Application.Events;
using Niuro.Loans.Application.Persistence;
using Niuro.Loans.Infrastructure.Blacklist;
using Niuro.Loans.Infrastructure.ExternalService;
using Niuro.Loans.Infrastructure.Outbox;
using Niuro.Loans.Infrastructure.Persistence;
using Niuro.Loans.Infrastructure.Persistence.Repositories;

namespace Niuro.Loans.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Binds every port the Application layer declares to a concrete adapter. Swapping the
    /// database or the delivery mechanism is a change to this method and nothing else.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("LoansDatabase")
            ?? throw new InvalidOperationException("Connection string 'LoansDatabase' is not configured.");

        services.AddDbContext<LoansDbContext>(options => options.UseSqlite(connectionString));

        // The clock is a dependency like any other. TryAdd so a test host can substitute it.
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ILoanApplicationRepository, LoanApplicationRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEventPublisher, OutboxEventPublisher>();
        services.AddScoped<IBlacklistedSsnRegistry, BlacklistedSsnRegistry>();

        services.AddOutboxDelivery(configuration);

        return services;
    }

    private static void AddOutboxDelivery(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));

        services.AddHttpClient<IExternalLoanService, ExternalLoanServiceClient>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<OutboxOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + '/');
            client.Timeout = options.RequestTimeout;
        });

        var outboxOptions = configuration.GetSection(OutboxOptions.SectionName).Get<OutboxOptions>() ?? new OutboxOptions();

        if (outboxOptions.Enabled)
        {
            services.AddHostedService<OutboxDispatcher>();
        }
    }
}
