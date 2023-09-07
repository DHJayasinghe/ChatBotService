using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Azure.Identity;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using System;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using System.Linq;

namespace CustomerRegistration
{
    public class UserManagementFunction
    {
        private readonly CosmosClient _cosmosClient;
        private readonly ClientSecretCredential _clientCredentials;

        public UserManagementFunction(CosmosClient cosmosClient, ClientSecretCredential clientCredentials)
        {
            _cosmosClient = cosmosClient;
            _clientCredentials = clientCredentials;
        }

        [FunctionName("Register")]
        public async Task<ActionResult> Register([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "user")] UserRequest req, ILogger log)
        {
            using var client = new GraphServiceClient(_clientCredentials);

            var requestBody = new Microsoft.Graph.Models.User
            {
                AccountEnabled = true,
                DisplayName = req.DisplayName,
                MailNickname = req.MailNickname,
                UserPrincipalName = req.UserPrincipalName,
                PasswordProfile = new PasswordProfile
                {
                    ForceChangePasswordNextSignIn = true,
                    Password = "xWwvJ]6NMw+bWH-d",
                },
            };
            try
            {
                var result = await client.Users.PostAsync(requestBody);
                var userId = result.Id; // save this ID for update

                return new OkObjectResult(userId);
            }
            catch (ODataError ex)
            {
                if (ex.ResponseStatusCode == (int)System.Net.HttpStatusCode.BadRequest)
                    return new BadRequestObjectResult(ex.Error.Message);
                throw;
            }
        }


        [FunctionName("Update")]
        public async Task<ActionResult> Update([HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "user")] User user, ILogger log)
        {
            using var client = new GraphServiceClient(_clientCredentials);

            var userContainer = _cosmosClient.GetContainer("interviewappdb", "User");
            var userCompanyContainer = _cosmosClient.GetContainer("interviewappdb", "Usercompanyassoc");

            var currentUser = await GetUserAsync(userContainer, user);
            var companyLinks = await GetCompanyLinksAsync(userCompanyContainer, user);

            // update fields with new data
            currentUser.firstName = user.firstName;
            currentUser.lastName = user.lastName;

            companyLinks.ForEach(link => link.Active = user.active);

            string mailNickName = user.emailAddress.Replace("@", ".");
            var requestBody = new Microsoft.Graph.Models.User
            {
                AccountEnabled = user.active,
                DisplayName = $"{user.firstName} {user.lastName}",
                MailNickname = mailNickName,
                UserPrincipalName = $"{mailNickName}@iamdhanukagmail.onmicrosoft.com",
            };
            try
            {
                var result = client.Users[user.id].PatchAsync(requestBody);

                await userContainer.UpsertItemAsync(currentUser, new PartitionKey(currentUser.id));
                var updateLinksTasks = companyLinks.Select(companyLink => userCompanyContainer.UpsertItemAsync(companyLink, new PartitionKey(companyLink.RecruiterId)));
                await Task.WhenAll(updateLinksTasks);
            }
            catch (ODataError ex)
            {
                if (ex.ResponseStatusCode == (int)System.Net.HttpStatusCode.BadRequest)
                    return new BadRequestObjectResult(ex.Error.Message);
                throw;
            }


            return new OkResult();
        }

        private async Task<List<Usercompanyassoc>> GetCompanyLinksAsync(Container userContainer, User user)
        {
            QueryDefinition queryDefinition = new QueryDefinition("SELECT * FROM c WHERE c.recruiterid = @recruiterId")
               .WithParameter("@recruiterId", user.id);

            using var feedIterator = userContainer.GetItemQueryIterator<Usercompanyassoc>(queryDefinition);

            var companyLinks = new List<Usercompanyassoc>();
            while (feedIterator.HasMoreResults)
            {
                var response = await feedIterator.ReadNextAsync();
                foreach (var link in response) companyLinks.Add(link);
            }
            return companyLinks;
        }

        private async Task<User> GetUserAsync(Container userContainer, User user)
        {
            QueryDefinition queryDefinition = new QueryDefinition("SELECT * FROM c WHERE c.id = @recruiterId")
               .WithParameter("@recruiterId", user.id);

            using var feedIterator = userContainer.GetItemQueryIterator<User>(queryDefinition);

            var users = new List<User>();
            while (feedIterator.HasMoreResults)
            {
                var response = await feedIterator.ReadNextAsync();
                foreach (var u in response) users.Add(u);
            }
            return users.FirstOrDefault();
        }
    }
}

public record UserRequest
{
    public string DisplayName { get; init; }
    public string MailNickname { get; init; }
    public string UserPrincipalName { get; init; }
}

public record UserUpdateRequest : UserRequest
{
    public Guid Id { get; init; }
}