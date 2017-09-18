# Restarting a WebJob that writes to Blob Storage in a `Function` can lead to data loss.

This is the sample application that helps reproduce the issue mentioned in the github issue.
You will need [ServiceBusExplorer][1] to monitor when the topic is empty.

[1]: https://blogs.msdn.microsoft.com/paolos/2015/03/02/service-bus-explorer-2-6-now-available/

## Setup

1. Create a resource group in Azure.
1. Create a storage account with the settings mentioned in the issue. (`storage`) 

    - Performance = `Standard`
    - Secure Transfer = `Disabled`
    - Replication = `LRS`

1. Create a service bus (`servicebus`)
1. Create an app service plan (`serviceplan`)

    - Size: `S0` or `S1`
    - Instances: `1`

1. Go to `Common/Constants.cs` and enter your connection strings. This will handle the `App.config` for you.
1. Configure ServiceBusExplorer with the `servicebus` connection string to monitor the topics.
1. Build the solution, it should work, if not, let me know.
1. Setup a deployment profile for the `DemoWebJob` project to `serviceplan` which you created in 
   the previous step.
1. Publish `DemoWebJob` and stop the WebJob until ready to reproduce.
1. Go to `JobFeeder/JobFeeder.cs` and change the `BlobCount` and `BatchSize` to desired values.
1. Run `JobFeeder.exe`, it will create the containers, topics, and push `BlobCount` messages.
1. In ServiceBusExplorer, Validate that all messages are in the topic's subscription

## Repro

1. Now that everything is setup, start the webjob in the Azure Portal (or automate it with Powershell if you wish.)
1. Watch for the topic messages to begin processing.
1. Once they do, wait a little and change one of the following:
   - Application Settings of the Web App running the WebJob
   - `serviceplan` scale (S1 -> S2 -> S3 -> S2 -> S1, etc.)
   - `serviceplan` instance (1..10)
   - Stop, Start, or Restart the webjob.
1. Repeat the previous step multiple times, each time should yield a few more lost messages.
1. After 4-5 restarts, feel free to let the topic drain.

> It might help to scale out and up if you're going to process a large number of messages.

> It's easier to reproduce the more messages you send, and the more times you restart the web job.

I usually scale up to S3 (10 instances) and process 100K messages. With around 5-6 restarts, 
I'll get anywhere between 2000 and 5000 blobs missing.


## Validate

1. Run `JobFeeder.exe validate` to validate the data once the topic message count reaches 0.

> You might want to pipe the output to a file for easier parsing.

The tool will print the blob names (an integer) that were not found in `storageaccount`.


## Repeating the test run

If you want to run the test again, run `JobFeeder.exe clean`, which will wipe the storage container.

You might need to run clean a second time, as Azure can take a little while to delete the container, 
and you'll get `409 Conflict` when trying to recreate the container. Wait a few minutes for the container
to delete properly, then run `JobFeeder.exe` again to refill the queue. 

> If `BlobCount` is small, or if you are running on a large instance count, you might want to stop 
> the webjob while refilling the topic so that you have time to switch the the portal to restart the webjob.
