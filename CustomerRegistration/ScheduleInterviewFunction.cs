using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using System.Collections.Generic;

namespace CustomerRegistration
{
    public class ScheduleInterviewFunction
    {
        private readonly ServiceBusClient _serviceBusClient;
        public ScheduleInterviewFunction(ServiceBusClient serviceBusClient) => _serviceBusClient = serviceBusClient;

        [FunctionName("ScheduleInterviewFunction")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var interview = new Interview
            {
                Id = Guid.NewGuid(),
                Name = "My Interview",
                ScheduledTime = DateTime.UtcNow.AddHours(1)
            };

            DateTimeOffset scheduledTimeToSend = interview.ScheduledTime.AddMinutes(-20);
            await using var queue = _serviceBusClient.CreateSender("notification-queue");
            await queue.ScheduleMessageAsync(new ServiceBusMessage(JsonConvert.SerializeObject(new InterviewScheduledEvent
            {
                Id = interview.Id
            }))
            {
                ContentType = "application/json",
            }, DateTimeOffset.UtcNow.AddMinutes(1));

            return new OkResult();
        }

        [FunctionName("negotiate")]
        public static SignalRConnectionInfo Negotiate(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "notifications/negotiate")] HttpRequest req,
            [SignalRConnectionInfo(HubName = "serverless")] SignalRConnectionInfo connectionInfo) => connectionInfo;

        [FunctionName("SendNotificationFunction")]
        public async Task SendNotificationFunction(
            [ServiceBusTrigger("notification-queue", Connection = "ServiceBus")] InterviewScheduledEvent @event,
            [SignalR(HubName = "serverless")] IAsyncCollector<SignalRMessage> signalRMessages,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request. {@event}", @event);


            // get notification from cosmosdb
            // sending notification via email

            var userId = "userid_from_cosmosdb_entity";

            await SendPushNotificationAsync(signalRMessages, "notificationtitle", new List<string>() { userId });
            await SendEmailNotificationAsync();
        }

        private static async Task SendPushNotificationAsync(IAsyncCollector<SignalRMessage> signalRMessages, string title, IEnumerable<string> receiverIds)
        {
            var notifications = new List<Task>();
            foreach (var receiverId in receiverIds)
            {
                notifications.Add(signalRMessages.AddAsync(new SignalRMessage
                {
                    Target = receiverId,
                    Arguments = new[] { title }
                }));
            }

            await Task.WhenAll(notifications);
        }
        private static async Task SendEmailNotificationAsync()
        {

            // logic to send email notification comes here
        }
    }
}
