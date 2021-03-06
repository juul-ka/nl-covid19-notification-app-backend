﻿// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Content.Commands;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Content.Commands.EntityFramework;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Core;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Crypto.Certificates;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Crypto.Signing;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Domain;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Manifest.Commands;
using NL.Rijksoverheid.ExposureNotification.BackEnd.TestFramework;
using Xunit;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.ManifestEngine.Tests
{
    public abstract class ManifestEngineTests : IDisposable
    {
        private readonly IDbProvider<ContentDbContext> _ContentDbProvider;

        private readonly LoggerFactory _Lf;

        private readonly NlContentResignExistingV1ContentCommand _Resign;
        private readonly Mock<IContentSigner> _NlSigner;

        public ManifestEngineTests(IDbProvider<ContentDbContext> contentDbProvider)
        {
            _Lf = new LoggerFactory();

            _ContentDbProvider = contentDbProvider ?? throw new ArgumentException();

            _NlSigner = new Mock<IContentSigner>(MockBehavior.Loose);
            _NlSigner.Setup(x => x.GetSignature(new byte[0])).Returns(new byte[] { 2 });
            
            _Resign = new NlContentResignExistingV1ContentCommand(
                new NlContentResignCommand(_ContentDbProvider.CreateNew, _NlSigner.Object, new ResignerLoggingExtensions(_Lf.CreateLogger<ResignerLoggingExtensions>())));
        }

        private ManifestUpdateCommand CreateManifestJob()
        {
            var eksConfig = new Mock<IEksConfig>(MockBehavior.Strict);
            eksConfig.Setup(x => x.LifetimeDays).Returns(14);

            var _Dtp = new StandardUtcDateTimeProvider();

            var jsonSerializer = new StandardJsonSerializer();

            return new ManifestUpdateCommand(
                new ManifestBuilder(_ContentDbProvider.CreateNew(), eksConfig.Object, _Dtp),
                new ManifestBuilderV3(_ContentDbProvider.CreateNew(), eksConfig.Object, _Dtp),
                new ManifestBuilderV4(_ContentDbProvider.CreateNew(), eksConfig.Object, _Dtp),
                _ContentDbProvider.CreateNew,
                new ManifestUpdateCommandLoggingExtensions(_Lf.CreateLogger<ManifestUpdateCommandLoggingExtensions>()),
                _Dtp,
                jsonSerializer,
                new StandardContentEntityFormatter(new ZippedSignedContentFormatter(_NlSigner.Object), new Sha256HexPublishingIdService(), jsonSerializer),
                () => new StandardContentEntityFormatter(new ZippedSignedContentFormatter(_NlSigner.Object), new Sha256HexPublishingIdService(), jsonSerializer)
            );
        }

        [Fact]
        public async Task EmptySystem()
        {
            await CreateManifestJob().ExecuteV1Async();
            await _Resign.ExecuteAsync();

            Assert.Equal(1, _ContentDbProvider.CreateNew().Content.Count(x => x.Type == ContentTypes.ManifestV2));
            Assert.Equal(1, _ContentDbProvider.CreateNew().Content.Count(x => x.Type == ContentTypes.Manifest));
            Assert.Equal(2, _ContentDbProvider.CreateNew().Content.Count());
        }

        [Fact]
        public async Task RunTwice()
        {
            await CreateManifestJob().ExecuteV1Async();
            await _Resign.ExecuteAsync();
            await CreateManifestJob().ExecuteV1Async();
            //await _Resign.ExecuteAsync();

            Assert.Equal(1, _ContentDbProvider.CreateNew().Content.Count(x => x.Type == ContentTypes.ManifestV2));
            Assert.Equal(1, _ContentDbProvider.CreateNew().Content.Count(x => x.Type == ContentTypes.Manifest));
        }

        public void Dispose() 
        {
            _ContentDbProvider.Dispose();
        }
    }
}