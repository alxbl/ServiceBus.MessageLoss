using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Microsoft.Azure.WebJobs;
using Microsoft.ServiceBus.Messaging;

namespace WebJob
{
    public class Functions
    {
        private static readonly Client Client = new Client();
        public static async Task ProcessQueueMessage([ServiceBusTrigger(Constants.TopicName, Constants.Subscription)] BrokeredMessage m, TextWriter log, CancellationToken t)
        {
            try
            {
                // 1. Read a message.
                var body = m.GetBody<int>();
                var msg = body.ToString();

                // 2. Write the blob.
                await Client.WriteAsync(msg, msg, t);
                await log.WriteLineAsync($"Store> {msg}");
            }
            catch (OperationCanceledException e)
            {
                log.WriteLine("The operation was canceled!" + e);
                throw; // rethrow.
            }
        }
    }
}
