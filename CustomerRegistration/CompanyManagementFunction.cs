using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace CustomerRegistration;

public class CompanyManagementFunction
{
    private readonly string _connectionString;

    public CompanyManagementFunction(IConfiguration configuration) => _connectionString = configuration.GetConnectionString("CosmosDB");

    [FunctionName("CompanyManagementFunction")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "company")] CompanyRequest req,
        ILogger log)
    {
        var companyId = Guid.NewGuid();
        var companyDatabaseName = $"{req.Name}_{companyId}";
        CosmosClient client = new CosmosClient(_connectionString);

        var database = await client.CreateDatabaseAsync(companyDatabaseName);
        await CreateContainersAsync(database.Database, new List<string> { "Client", "ClientChat", "ClientSchedule", "Company", "Interviewing", "InterviewsChat", "InterviewsSchedule", "Job", "Questionairres" });

        return new OkObjectResult(companyDatabaseName);
    }

    private static async Task CreateContainersAsync(Database database, IEnumerable<string> containers)
    {
        var tasks = containers.Select(container =>
        {
            return database.DefineContainer(name: container, partitionKeyPath: $"/id").CreateAsync();
        });
        await Task.WhenAll(tasks);
    }
}

public record CompanyRequest
{
    public string Name { get; set; }
}