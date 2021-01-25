﻿// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using Microsoft.Extensions.Logging;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Content.Commands;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Core;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Crypto.Certificates;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Crypto.Signing;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Domain;
using NL.Rijksoverheid.ExposureNotification.BackEnd.EksEngine.Commands;
using NL.Rijksoverheid.ExposureNotification.BackEnd.EksEngine.Commands.FormatV1;
using Xunit;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.EksEngine.Tests.ExposureKeySets
{
    public class ExposureKeySetBuilderTests
    {
        private class FakeEksHeaderInfoConfig : IEksHeaderInfoConfig
        {
            public string AppBundleId => "nl.rijksoverheid.en";
            public string VerificationKeyId => "ServerNL";
            public string VerificationKeyVersion => "v1";
        }

        //y = 4.3416x + 715.24
        [Theory]
        [InlineData(500, 123)]
        [InlineData(1000, 123)]
        [InlineData(2000, 123)]
        [InlineData(3000, 123)]
        [InlineData(10000, 123)]
        public void Build(int keyCount, int seed)
        {
            //Arrange
            var lf = new LoggerFactory();
            var certProviderLogger = new EmbeddedCertProviderLoggingExtensions(lf.CreateLogger<EmbeddedCertProviderLoggingExtensions>());
            var eksBuilderV1Logger = new EksBuilderV1LoggingExtensions(lf.CreateLogger<EksBuilderV1LoggingExtensions>());
            var dtp = new StandardUtcDateTimeProvider();
            
            var sut = new EksBuilderV1(
                new FakeEksHeaderInfoConfig(),
                new EcdSaSigner(
                    new EmbeddedResourceCertificateProvider(
                        new HardCodedCertificateLocationConfig("TestECDSA.p12", ""),
                        certProviderLogger)),
                new CmsSignerEnhanced(
                    new EmbeddedResourceCertificateProvider(
                        new HardCodedCertificateLocationConfig("TestRSA.p12", "Covid-19!"),
                        certProviderLogger),
                    new EmbeddedResourcesCertificateChainProvider(
                        new HardCodedCertificateLocationConfig("Resources.StaatDerNLChain-Expires2020-08-28.p7b", "")),
                    dtp
                    ),
                dtp,
                new GeneratedProtobufEksContentFormatter(),
                eksBuilderV1Logger
                );

            //Act
            var result = sut.BuildAsync(GetRandomKeys(keyCount, seed)).GetAwaiter().GetResult();
            Trace.WriteLine($"{keyCount} keys = {result.Length} bytes.");
            
            //Assert
            Assert.True(result.Length > 0);

            using (var fs = new FileStream("EKS.zip", FileMode.Create, FileAccess.Write))
            {
                fs.Write(result, 0, result.Length);
            }
        }

        [Fact]
        public void EksBuilderV1WithDummy_NLSigHasDummyText()
        {
            //Arrange
            var KeyCount = 500;
            var lf = new LoggerFactory();
            var certProviderLogger = new EmbeddedCertProviderLoggingExtensions(lf.CreateLogger<EmbeddedCertProviderLoggingExtensions>());
            var eksBuilderV1Logger = new EksBuilderV1LoggingExtensions(lf.CreateLogger<EksBuilderV1LoggingExtensions>());
            var dtp = new StandardUtcDateTimeProvider();
            var dummySigner = new DummyCmsSigner();

            var sut = new EksBuilderV1(
                new FakeEksHeaderInfoConfig(),
                new EcdSaSigner(
                    new EmbeddedResourceCertificateProvider(
                        new HardCodedCertificateLocationConfig("TestECDSA.p12", ""),
                        certProviderLogger)
                    ),
                dummySigner,
                dtp,
                new GeneratedProtobufEksContentFormatter(),
                eksBuilderV1Logger
                );

            //Act
            var result = sut.BuildAsync(GetRandomKeys(KeyCount, 123)).GetAwaiter().GetResult();

            //Assert
            using var zipFileInMemory = new MemoryStream();
            zipFileInMemory.Write(result, 0, result.Length);
            using (var zipFileContent = new ZipArchive(zipFileInMemory, ZipArchiveMode.Read, false))
            {
                var NlSignature = zipFileContent.ReadEntry(ZippedContentEntryNames.NLSignature);
                Assert.NotNull(NlSignature);
                Assert.Equal(NlSignature, dummySigner.DummyContent);
            }
        }

        private TemporaryExposureKeyArgs[] GetRandomKeys(int workflowCount, int seed)
        {
            var random = new Random(seed);
            var workflowKeyValidatorConfig = new DefaultTekValidatorConfig();
            var workflowValidatorConfig = new DefaultTekListValidationConfig();

            var result = new List<TemporaryExposureKeyArgs>(workflowCount * workflowValidatorConfig.TemporaryExposureKeyCountMax);
            var keyBuffer = new byte[workflowKeyValidatorConfig.KeyDataLength];

            for (var i = 0; i < workflowCount; i++)
            {
                var keyCount = 1 + random.Next(workflowValidatorConfig.TemporaryExposureKeyCountMax - 1);
                var keys = new List<TemporaryExposureKeyArgs>(keyCount);

                for (var j = 0; j < keyCount; j++)
                {
                    random.NextBytes(keyBuffer);
                    keys.Add(new TemporaryExposureKeyArgs
                    {
                        KeyData = keyBuffer,
                        RollingStartNumber = workflowKeyValidatorConfig.RollingPeriodMin + j,
                        RollingPeriod = 11,
                        TransmissionRiskLevel = TransmissionRiskLevel.Medium
                    });
                }
                result.AddRange(keys);

            }

            return result.ToArray();
        }
    }
}