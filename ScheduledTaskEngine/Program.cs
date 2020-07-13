﻿// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.Logging;
using Serilog;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.ScheduledTaskEngine
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
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            // Build configuration
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile($"appsettings.{environmentName}.json", true, true)
                .Build();

            // add the framework services
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            try
            {
                Log.Information("Starting scheduled task engine.");
                await serviceProvider.GetService<App>().Run(args);
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
            services.AddSingleton(Configuration);
            services.AddTransient<App>();
        }
    }
}
