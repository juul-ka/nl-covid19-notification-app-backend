﻿// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.EfDatabase;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.EfDatabase.Contexts;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.Logging;
using Serilog;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.ContentPusherEngine
{
    internal class Program
    {
        public Program()
        {
            AppDomain.CurrentDomain.UnhandledException += AppDomainExceptinHandler;
        }

        private void AppDomainExceptinHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Fatal(e.ExceptionObject.ToString());
        }

        public static IConfigurationRoot Configuration { get; private set; }

        private static async Task Main(string[] args)
        {
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            // Build configuration
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile($"appsettings.{environmentName}.json", true, true)
                .Build();

            // Add the framework services
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            try
            {
                Log.Information("Starting CDN Pusher Engine.");
                var pushIt = serviceProvider.GetService<PusherTask>();
                await pushIt.PushIt();
            }
            finally
            {
                Log.Information("Exiting.");
                Log.CloseAndFlush();
            }
        }

        public static void ConfigureServices(IServiceCollection services)
        {
            ComponentsContainerHelper.RegisterDefaultServices(services);

            services.AddSeriLog(Configuration);
            services.AddSingleton<IConfiguration>(Configuration);
            services.AddSingleton<PusherTask>();
            services.AddSingleton<IDataApiUrls>(new DataApiUrls(Configuration, "DataApi"));
            services.AddSingleton<IReceiverConfig>(new ReceiverConfig(Configuration, "Receiver"));

            
            var certificateHack = Configuration.GetValue("CertificateHack", false);
            if (certificateHack)
            {
                Log.Warning("Unproven hack for self-signed certificates is enabled.");
                ServicePointManager.ServerCertificateValidationCallback += (_, __, ___, ____) =>
                    true;
            }
        }
    }
}
