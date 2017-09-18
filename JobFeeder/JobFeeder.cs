using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Common;
using Microsoft.ServiceBus.Messaging;

namespace JobFeeder
{
    public class JobFeeder
    {
        // The number of blobs to create.
        private const int BlobCount = 100000;
        // The size of a batch when pushing messages to the topic.
        private const int BatchSize = 300; // Don't put 0.

        /// <summary>
        /// Feeds messages to `Webjob`'s topic and allows to validate/clean the storage.
        /// 
        /// Usage:
        /// 
        /// JobFeeder.exe [clean|validate]
        /// 
        /// clean: Will wipe `Constants.StorageContainer` before pushing `BlobCount` messages to `Constants.TopicName`.
        /// validate: Will pull all the blobs from `StorageContainer` and check that the expected number of blobs is present. No pushing happens.
        /// 
        /// No arguments: Will only push messages to the topic without cleaning the storage container.
        /// </summary>
        public static void Main(string[] args)
        {
            CancellationTokenSource s = new CancellationTokenSource();
            Console.Write("Establishing connection... ");

            var client = new Client();
            var sender = client.Sender;
            Console.WriteLine("ok");

            client.EnsureTopicExists().Wait(s.Token);

            if (args.Length > 0 && args.Contains("clean"))
            {
                Console.Write("Wiping storage... ");
                client.WipeStorageAsync(s.Token).Wait(s.Token); // Hey, we are an example program, so we block ~0 without feeling bad.
                Console.WriteLine("ok");
            }

            if (args.Length > 0 && args.Contains("validate"))
            {
                Console.Write("Validating... ");
                var results = client.ListAsync(s.Token).Result.Select(int.Parse).OrderBy(i => i).ToList();
                Console.WriteLine(results.Count != BlobCount ? "failed!" : "success");

                var expected = new List<int>();
                for(int i = 1; i <= BlobCount; ++i) expected.Add(i);
                var missing = expected.Except(results).ToList();
                Console.WriteLine($"{missing.Count} blobs are missing");
                Console.WriteLine(string.Join(", ", missing));
            }
            else
            {
                Console.Write($"Enqueuing {BlobCount} messages... ");

                var batches = BlobCount / BatchSize;
                var sent = 0;
                for (var i = 0; i <= batches; ++i)
                {
                    if (sent >= BlobCount) break;

                    var batch = new List<int>();
                    for (var j = 1; j <= BatchSize && sent < BlobCount; ++j)
                    {
                        var n = BatchSize * i + j;
                        batch.Add(n);
                        sent++;
                    }
                    sender.SendBatch(batch.Select(n => new BrokeredMessage(n)));
                }
                Console.WriteLine("ok");
            }
            Console.ReadKey();
        }
    }
}
