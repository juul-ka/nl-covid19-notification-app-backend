﻿// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System;
using Microsoft.Extensions.Logging;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.Logging;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Components.Workflow.DecoyKeys
{
	public class DecoyKeysLoggingExtensions
	{
        private const string Name = "Decoykeys(PostSecret)";
		private const int Base = LoggingCodex.Decoy;

		private const int Start = Base;

		private readonly ILogger _Logger;

		public DecoyKeysLoggingExtensions(ILogger<DecoyKeysLoggingExtensions> logger)
		{
			_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public void WriteStartDecoy()
		{
			_Logger.LogInformation("[{name}/{id}] POST triggered.",
				Name, Start);
		}
	}
}