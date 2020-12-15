﻿// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using NCrunch.Framework;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.EfDatabase;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.EfDatabase.Contexts;
using NL.Rijksoverheid.ExposureNotification.BackEnd.TestFramework;
using Xunit;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Components.Tests.ExposureKeySetsEngine
{
    [Trait("db", "ss")]
    public class WfToEksEksBatchJobMk3TestsSqlServer : WfToEks_EksBatchJobMk3Tests
    {
        private const string DbNamePrefix = nameof(WfToEks_EksBatchJobMk3Tests) + "_";

        public WfToEksEksBatchJobMk3TestsSqlServer() : base(
            new SqlServerDbProvider<WorkflowDbContext>(DbNamePrefix + "W"),
            new SqlServerDbProvider<DkSourceDbContext>(DbNamePrefix + "D"),
            new SqlServerDbProvider<EksPublishingJobDbContext>(DbNamePrefix + "P"),
            new SqlServerDbProvider<ContentDbContext>(DbNamePrefix + "C"),
            new SqlServerWrappedEfExtensions())
        { }
    }
}