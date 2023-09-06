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
        string tenantId = "69380036-9b93-4add-a715-9e8921d06841";
        string clientId = "97c063f7-39b4-469d-9861-0daf9c08adf2";
        string clientSecret = "nrs8Q~VcMNNZHrGHQEJbol8cdOpnbCxs9AALIb9h";

        [FunctionName("Register")]
        public async Task<ActionResult> Register([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "user")] UserRequest req, ILogger log)
        {
            var clientSecretCredentials = new ClientSecretCredential(tenantId, clientId, clientSecret);
            GraphServiceClient client = new GraphServiceClient(clientSecretCredentials);

            //7cbb0f77-2f62-4f47-9e11-8552b69a5658
            var requestBody = new User
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
            }
            catch (ODataError ex)
            {
                if (ex.ResponseStatusCode == (int)System.Net.HttpStatusCode.BadRequest)
                    return new BadRequestObjectResult(ex.Error.Message);
                throw;
            }


            return new OkResult();
        }


        [FunctionName("Update")]
        public ActionResult Update([HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "user")] UserUpdateRequest req, ILogger log)
        {
            var clientSecretCredentials = new ClientSecretCredential(tenantId, clientId, clientSecret);
            GraphServiceClient client = new GraphServiceClient(clientSecretCredentials);

            var requestBody = new User
            {
                AccountEnabled = true,
                DisplayName = req.DisplayName,
                MailNickname = req.MailNickname,
                UserPrincipalName = req.UserPrincipalName,
            };
            try
            {
                var result = client.Users[req.Id.ToString()].ToPatchRequestInformation(requestBody);
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
    public Guid Id { get; set; }
}