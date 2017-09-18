using System;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.ServiceBus;

namespace Common
{
    public class Client
    {
        public CloudBlobClient Storage { get; set; }
        public MessageSender Sender { get; set; }
        public MessagingFactory Factory { get; set; }
        public NamespaceManager Manager { get; set; }


        public Client()
        {
            // Create the service bus message sender.
            Factory = MessagingFactory.CreateFromConnectionString(Constants.ServiceBus);
            Manager = NamespaceManager.CreateFromConnectionString(Constants.ServiceBus);
            Sender = Factory.CreateMessageSender(Constants.TopicName);

            // Connect to blob storage.
            var account = CloudStorageAccount.Parse(Constants.StorageAccount);
            Storage = account.CreateCloudBlobClient();
        }

        // Creates the Service Bus Topic that mimics the production settings.
        public async Task EnsureTopicExists()
        {
            if (!await Manager.TopicExistsAsync(Constants.TopicName))
            {
                Console.WriteLine($"Topic {Constants.TopicName} does not exist. It will be created.");

                // Create the topic that `JobFeeder` will push to.
                await Manager.CreateTopicAsync(new TopicDescription(Constants.TopicName)
                {
                    EnableBatchedOperations = true,
                    EnablePartitioning = true,
                });

                // Create the subscription that `WebJob` will subscribe to.
                await Manager.CreateSubscriptionAsync(new SubscriptionDescription(Constants.TopicName, Constants.Subscription)
                {
                    EnableBatchedOperations = true,
                    MaxDeliveryCount = 10
                });
            }

            // Create the blob container if it doesn't exist so that the tests run fine.
            var cref = GetContainer();
            await cref.CreateIfNotExistsAsync();
        }

        /// <summary>
        /// Drop and re-create the container.
        /// </summary>
        public async Task WipeStorageAsync(CancellationToken t)
        {
            var cref = GetContainer();
            await cref.DeleteIfExistsAsync(t);
            // This might throw when running `JobFeeder clean`. Just re-run... it happens because Azure isn't done deleting the container yet.
            var ncref = GetContainer();
            await ncref.CreateIfNotExistsAsync(t);
        }

        public async Task WriteAsync(string blob, string jason, CancellationToken t)
        {
            var cref = GetContainer();
            var b = cref.GetBlockBlobReference(blob);

            if (Constants.SynchronousBlobStorage)
            {
                b.UploadText(jason);
                await Task.CompletedTask;
            }
            else
            {
                await b.UploadTextAsync(jason, t);
            }
        }

        public Task<string> ReadAsync(string blob, CancellationToken t)
        {
            var cref = GetContainer();
            var b = cref.GetBlockBlobReference(blob);
            return Constants.SynchronousBlobStorage ? Task.FromResult(b.DownloadText()) : b.DownloadTextAsync(t);
        }

        /// <summary>
        /// List all the blobs inside the container.
        /// </summary>
        public async Task<IEnumerable<string>> ListAsync(CancellationToken t)
        {
            var cref = GetContainer();
            var blobs = new List<string>();
            BlobResultSegment segment = null;
            do
            {
                segment = Constants.SynchronousBlobStorage 
                    ? await cref.ListBlobsSegmentedAsync(segment?.ContinuationToken ?? new BlobContinuationToken(), t)
                    : cref.ListBlobsSegmented(segment?.ContinuationToken ?? new BlobContinuationToken());
                var res = segment.Results;
                blobs.AddRange(res.Select(b => (b as ICloudBlob)?.Name).Where(n => !string.IsNullOrWhiteSpace(n)));
            } while (segment.ContinuationToken != null);
            return blobs;
        }

        private CloudBlobContainer GetContainer() => Storage.GetContainerReference(Constants.StorageContainer);
    }
}
