using System;
using System.Configuration;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Common;

namespace DemoWebjob
{
    public class WebJob
    {
        public static void Main()
        {
            var config = new JobHostConfiguration();

            // Just to avoid having to configure everything in App.config.
            var cfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            cfg.ConnectionStrings.ConnectionStrings.Add(new ConnectionStringSettings("AzureWebJobsStorage", Constants.StorageAccount));
            cfg.ConnectionStrings.ConnectionStrings.Add(new ConnectionStringSettings("AzureWebJobsDashboard", Constants.StorageAccount));
            cfg.Save();
            ConfigurationManager.RefreshSection("connectionStrings");

            config.UseServiceBus(new ServiceBusConfiguration
            {
                ConnectionString = Constants.ServiceBus,
                MessageOptions = new OnMessageOptions { AutoComplete = true, MaxConcurrentCalls = 32, AutoRenewTimeout = TimeSpan.FromSeconds(120) },
                PrefetchCount = 64
            });

            // if (config.IsDevelopment) config.UseDevelopmentSettings(); // Pretend we're in production.
            var host = new JobHost(config);
            host.RunAndBlock();
        }
    }
}
