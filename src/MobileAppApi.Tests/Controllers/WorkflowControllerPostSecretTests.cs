// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NCrunch.Framework;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Core;
using NL.Rijksoverheid.ExposureNotification.BackEnd.MobileAppApi.Workflow.Entities;
using NL.Rijksoverheid.ExposureNotification.BackEnd.MobileAppApi.Workflow.EntityFramework;
using NL.Rijksoverheid.ExposureNotification.BackEnd.TestFramework;
using Xunit;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.MobileAppApi.Tests.Controllers
{
    [Collection(nameof(WorkflowControllerPostSecretTests))]
    [ExclusivelyUses(nameof(WorkflowControllerPostSecretTests))]
    public abstract class WorkflowControllerPostSecretTests : WebApplicationFactory<Startup>, IDisposable
    {
        private readonly WebApplicationFactory<Startup> _Factory;
        private readonly FakeNumberGen _FakeNumbers = new FakeNumberGen();
        private readonly IDbProvider<WorkflowDbContext> _WorkflowDbProvider;

        protected WorkflowControllerPostSecretTests(IDbProvider<WorkflowDbContext> workflowDbProvider)
        {
            _WorkflowDbProvider = workflowDbProvider;
            _Factory = WithWebHostBuilder(
                builder =>
                {
                    builder.ConfigureTestServices(services =>
                    {
                        services.AddScoped(sp => _WorkflowDbProvider.CreateNewWithTx());
                        services.Replace(new ServiceDescriptor(typeof(IRandomNumberGenerator), _FakeNumbers));
                        services.AddTransient<DecoyTimeAggregatorAttribute>();
                    });

                    builder.ConfigureAppConfiguration((ctx, config) =>
                    {
                        config.AddInMemoryCollection(new Dictionary<string, string>
                        {
                            ["Workflow:ResponsePadding:ByteCount:Min"] = "8",
                            ["Workflow:ResponsePadding:ByteCount:Max"] = "64"
                        });
                    });
                });
        }

        private class FakeNumberGen: IRandomNumberGenerator
        {
            public int Value { get; set; } = 10;

            public int Next(int min, int max) => Value;

            public byte[] NextByteArray(int length)
            {
                var buffer = new byte[length];
                buffer[0] = (byte)Value;
                return buffer;
            }
        }

        void IDisposable.Dispose()
        {
            base.Dispose();
        }

        [Fact]
        [ExclusivelyUses("WorkflowControllerPostSecretTests")]
        public async Task PostSecretTest_EmptyDb()
        {
            // Arrange
            var client = _Factory.CreateClient();

            _FakeNumbers.Value = 1;

            // Act
            var result = await client.PostAsync("v1/register", null);

            // Assert
            var items = await _WorkflowDbProvider.CreateNew().KeyReleaseWorkflowStates.ToListAsync();
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal(1, items.Count);
        }

        private TekReleaseWorkflowStateEntity Create(int value)
        {
            var e1 = new TekReleaseWorkflowStateEntity
            {
                BucketId = new byte[32],
                ConfirmationKey = new byte[32],
                LabConfirmationId = "1"
            };

            e1.ConfirmationKey[0] = (byte) value;
            e1.BucketId[0] = (byte)value;
            e1.LabConfirmationId = $"{value}{value}{value}{value}{value}{value}";

            return e1;
        }

        [Fact]
        [ExclusivelyUses("WorkflowControllerPostSecretTests")]
        public async Task PostSecretTest_5RetriesAndBang()
        {
            using var dbContext = _WorkflowDbProvider.CreateNew();
            dbContext.KeyReleaseWorkflowStates.AddRange(Enumerable.Range(1, 5).Select(Create));
            dbContext.SaveChanges();
            // Arrange
            var client = _Factory.CreateClient();
            _FakeNumbers.Value = 1;

            // Act
            var result = await client.PostAsync("v1/register", null);

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal("{\"labConfirmationId\":null,\"bucketId\":null,\"confirmationKey\":null,\"validity\":-1}", await result.Content.ReadAsStringAsync());
        }

        [Fact]
        [ExclusivelyUses("WorkflowControllerPostSecretTests")]
        public async Task PostSecret_MissThe5Existing()
        {
            using var dbContext = _WorkflowDbProvider.CreateNew();
            dbContext.KeyReleaseWorkflowStates.AddRange(Enumerable.Range(1, 5).Select(Create));
            dbContext.SaveChanges();
            
            _FakeNumbers.Value = 6;
            // Arrange
            var client = _Factory.CreateClient();

            // Act
            var result = await client.PostAsync("v1/register", null);

            // Assert
            var items = await dbContext.KeyReleaseWorkflowStates.ToListAsync();
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal(6, items.Count);
        }

        [Fact]
        public async Task Register_has_padding()
        {
            // Arrange
            var client = _Factory.CreateClient();

            // Act
            var result = await client.PostAsync("v1/register", null);

            // Assert
            Assert.True(result.Headers.Contains("padding"));
        }
    }
}