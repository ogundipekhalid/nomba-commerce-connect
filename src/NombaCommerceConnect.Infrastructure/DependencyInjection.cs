using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NombaCommerceConnect.Application.Interfaces;
using NombaCommerceConnect.Application.Nomba;
using NombaCommerceConnect.Infrastructure.Payments.Nomba;
using NombaCommerceConnect.Infrastructure.Persistence;
using NombaCommerceConnect.Infrastructure.Persistence.Repositories;

namespace NombaCommerceConnect.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default") ?? "Data Source=nombacommerceconnect.db";
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

        services.AddScoped<IVendorRepository, VendorRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        services.Configure<NombaOptions>(configuration.GetSection(NombaOptions.SectionName));
        var nombaOptions = configuration.GetSection(NombaOptions.SectionName).Get<NombaOptions>() ?? new NombaOptions();

        services.AddScoped<INombaWebhookSignatureVerifier, NombaWebhookSignatureVerifier>();

        if (nombaOptions.UseFakeClient)
        {
            services.AddScoped<INombaClient, FakeNombaClient>();
        }
        else
        {
            // AddHttpClient<TInterface, TImplementation> registers TInterface in DI
            // backed by a typed HttpClient - no separate AddScoped needed.
            services.AddHttpClient<INombaAuthTokenProvider, NombaAuthTokenProvider>((sp, client) =>
            {
                client.BaseAddress = new Uri(nombaOptions.BaseUrl);
            });
            services.AddHttpClient<INombaClient, NombaClient>((sp, client) =>
            {
                client.BaseAddress = new Uri(nombaOptions.BaseUrl);
            });
        }

        return services;
    }
}
