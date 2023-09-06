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

namespace CustomerRegistration
{
    public class UserManagementFunction
    {
        private readonly ClientSecretCredential _clientCredentials;
        public UserManagementFunction(ClientSecretCredential clientCredentials) => _clientCredentials = clientCredentials;

        [FunctionName("Register")]
        public async Task<ActionResult> Register([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "user")] UserRequest req, ILogger log)
        {
            using var client = new GraphServiceClient(_clientCredentials);

            //7cbb0f77-2f62-4f47-9e11-8552b69a5658
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
        public ActionResult Update([HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "user")] UserUpdateRequest req, ILogger log)
        {
            using var client = new GraphServiceClient(_clientCredentials);

            var requestBody = new Microsoft.Graph.Models.User
            {
                AccountEnabled = true,
                DisplayName = req.DisplayName,
                MailNickname = req.MailNickname,
                UserPrincipalName = req.UserPrincipalName,
            };
            try
            {
                var result = client.Users[req.Id.ToString()].PatchAsync(requestBody);
            }
            catch (ODataError ex)
            {
                if (ex.ResponseStatusCode == (int)System.Net.HttpStatusCode.BadRequest)
                    return new BadRequestObjectResult(ex.Error.Message);
                throw;
            }


            return new OkResult();
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