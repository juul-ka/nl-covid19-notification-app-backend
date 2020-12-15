﻿// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System;
using System.Diagnostics;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.WebApi;
using Xunit;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Components.Tests.WebApi
{
    public class AssemblyStuffTests
    {
        [Fact]
        public void GetCustomAttributeTest()
        {
            Trace.Write(string.Join(Environment.NewLine, GetType().Assembly.Dump()));
        }
    }
}