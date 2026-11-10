using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(FCG.Payments.IntegrationTests.Infrastructure.PaymentsApiFactory))]

namespace FCG.Payments.IntegrationTests.Infrastructure;

public class PaymentsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const decimal MaxApprovedAmount = 100m;

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
        _ = Server;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DbConnection", _postgres.GetConnectionString());
        builder.UseSetting("Payments:MaxApprovedAmount", MaxApprovedAmount.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.ConfigureTestServices(services => services.AddMassTransitTestHarness());
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
