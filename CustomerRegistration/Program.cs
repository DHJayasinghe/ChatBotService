using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(CustomerRegistration.Program))]
namespace CustomerRegistration;

public class Program : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.GetContext().Configuration;

        services.AddSingleton(new ClientSecretCredential(configuration["AzureAD:TenantId"], configuration["AzureAD:ClientId"], configuration["AzureAD:ClientSecret"]));
        services.AddSingleton(new CosmosClient(configuration.GetConnectionString("CosmosDB")));
    }
}