using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Azure.Identity;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;

namespace CustomerRegistration
{
    public class CustomerRegisterFunction
    {
        string tenantId = "69380036-9b93-4add-a715-9e8921d06841";
        string clientId = "97c063f7-39b4-469d-9861-0daf9c08adf2";
        string clientSecret = "nrs8Q~VcMNNZHrGHQEJbol8cdOpnbCxs9AALIb9h";

        [FunctionName("Register")]
        public async Task<ActionResult> Register([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "Register")] UserRequest req, ILogger log)
        {
            var clientSecretCredentials = new ClientSecretCredential(tenantId, clientId, clientSecret);
            GraphServiceClient client = new GraphServiceClient(clientSecretCredentials);


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