using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using System.Linq;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Graph.Models;
using Microsoft.Graph;
using Azure.Identity;
using Newtonsoft.Json;

namespace CustomerRegistration;

public class CompanyManagementFunction
{
    private readonly CosmosClient _cosmosClient;
    private readonly ClientSecretCredential _clientCredentials;

    public CompanyManagementFunction(CosmosClient cosmosClient, ClientSecretCredential clientCredentials)
    {
        _cosmosClient = cosmosClient;
        _clientCredentials = clientCredentials;
    }

    [FunctionName("CompanyManagementFunction")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "company")] Company company,
        ILogger log)
    {
        company.id = Guid.NewGuid().ToString();

        var companyContainer = _cosmosClient.GetContainer("interviewappdb", "Company");
        var userContainer = _cosmosClient.GetContainer("interviewappdb", "User");
        var userCompanyContainer = _cosmosClient.GetContainer("interviewappdb", "Usercompanyassoc");

        await RegisterCompanyAsync(company, companyContainer);

        var userRegistrationTasks = company.listUser.Select(user => SaveUserAsync(user, userContainer, log));
        await Task.WhenAll(userRegistrationTasks);

        var successfullyRegisteredUsers = company.listUser.Where(user => !string.IsNullOrEmpty(user.id)).ToList();

        var linkToCompanyTasks = successfullyRegisteredUsers.Select(user => LinkUserToCompanyAsync(user, company, userCompanyContainer));
        await Task.WhenAll(linkToCompanyTasks);

        return new OkResult();
    }

    private static async Task RegisterCompanyAsync(Company company, Container companyContainer)
    {
        await companyContainer.CreateItemAsync(company, new PartitionKey(company.id));
    }

    private static async Task LinkUserToCompanyAsync(User user, Company company, Container userCompanyContainer)
    {
        await userCompanyContainer.CreateItemAsync(new Usercompanyassoc
        {
            Id = Guid.NewGuid().ToString(),
            CompanyId = company.id,
            RecruiterId = user.id,
            Active = true
        }, new PartitionKey(company.id));
    }

    private async Task<bool> SaveUserAsync(User user, Container clientContainer, ILogger logger)
    {
        using var client = new GraphServiceClient(_clientCredentials);

        var requestBody = new Microsoft.Graph.Models.User
        {
            AccountEnabled = true,
            DisplayName = user.firstName + " " + user.lastName,
            MailNickname = user.firstName,
            UserPrincipalName = user.firstName + "@iamdhanukagmail.onmicrosoft.com",
            PasswordProfile = new PasswordProfile
            {
                ForceChangePasswordNextSignIn = true,
                Password = "xWwvJ]6NMw+bWH-d",
            },
        };
        try
        {
            var result = await client.Users.PostAsync(requestBody);
            user.id = result.Id;

            var response = await clientContainer.CreateItemAsync(user, new PartitionKey(user.id));
            return true;
        }
        catch (ODataError ex)
        {
            if (ex.ResponseStatusCode == (int)System.Net.HttpStatusCode.BadRequest)
            {
                logger.LogWarning(ex.Error.Message);
            }

            return false;
        }
    }
}

public record CompanyRequest
{
    public string Name { get; set; }
}

public class Usercompanyassoc
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("recruiterid")]
    public string RecruiterId { get; set; }

    [JsonProperty("companyid")]
    public string CompanyId { get; set; }

    [JsonProperty("active")]
    public bool Active { get; set; }
}