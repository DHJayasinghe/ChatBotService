using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Azure.Identity;
using Microsoft.Graph.Models;

namespace CustomerRegistration
{
    public class CustomerRegisterFunction
    {
        string tenantId = "69380036-9b93-4add-a715-9e8921d06841";
        string clientId = "97c063f7-39b4-469d-9861-0daf9c08adf2";
        string clientSecret = "nrs8Q~VcMNNZHrGHQEJbol8cdOpnbCxs9AALIb9h";

        [FunctionName("Register")]
        public async Task<ActionResult> Register([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "Register")] HttpRequest req, ILogger log)
        {
            var clientSecretCredentials = new ClientSecretCredential(tenantId, clientId, clientSecret);
            GraphServiceClient client = new GraphServiceClient(clientSecretCredentials);

            var requestBody = new User
            {
                AccountEnabled = true,
                DisplayName = "Adele Vance",
                MailNickname = "AdeleV",
                UserPrincipalName = "AdeleV@iamdhanukagmail.onmicrosoft.com",
                PasswordProfile = new PasswordProfile
                {
                    ForceChangePasswordNextSignIn = true,
                    Password = "xWwvJ]6NMw+bWH-d",
                },
            };
            var result = await client.Users.PostAsync(requestBody);

            return new OkResult();
        }
    }
}