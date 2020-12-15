﻿// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.EfDatabase.Contexts;
using NL.Rijksoverheid.ExposureNotification.BackEnd.TestFramework;
using Xunit;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Components.Tests.Interop
{
    [Trait("db", "ss")]
    public class IksEngineTestsSqlserver : IksEngineTest
    {
        private const string Prefix = nameof(IksEngineTest) + "_";

        public IksEngineTestsSqlserver() : base(
            new SqlServerDbProvider<WorkflowDbContext>(Prefix+"W"),
            new SqlServerDbProvider<IksInDbContext>(Prefix + "II"),
            new SqlServerDbProvider<DkSourceDbContext>(Prefix + "D"),
            new SqlServerDbProvider<IksPublishingJobDbContext>(Prefix + "P"),
            new SqlServerDbProvider<IksOutDbContext>(Prefix + "IO"),
            new SqliteWrappedEfExtensions()
        )
        { }
    }
}