﻿// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.EfDatabase;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.EfDatabase.Contexts;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.Services;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Components.Workflow.Expiry
{
    public class RemoveExpiredWorkflowsCommand
    {
        private readonly Func<WorkflowDbContext> _DbContextProvider;
        private readonly ILogger<RemoveExpiredWorkflowsCommand> _Logger;
        private readonly IUtcDateTimeProvider _Dtp;
        private RemoveExpiredWorkflowsResult _Result;
        private readonly IWorkflowConfig _Config;

        public RemoveExpiredWorkflowsCommand(Func<WorkflowDbContext> dbContext, ILogger<RemoveExpiredWorkflowsCommand> logger, IUtcDateTimeProvider dtp, IWorkflowConfig config)
        {
            _DbContextProvider = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _Dtp = dtp ?? throw new ArgumentNullException(nameof(dtp));
            _Config = config ?? throw new ArgumentNullException(nameof(config));
        }


        private void ReadStats(WorkflowStats stats, WorkflowDbContext dbc)
        {
            stats.Count = dbc.KeyReleaseWorkflowStates.Count();

            stats.Expired = dbc.KeyReleaseWorkflowStates.Count(x => x.ValidUntil < _Dtp.Snapshot);
            stats.Unauthorised = dbc.KeyReleaseWorkflowStates.Count(x => x.ValidUntil < _Dtp.Snapshot && x.LabConfirmationId != null && x.AuthorisedByCaregiver == null && x.DateOfSymptomsOnset == null);
            stats.Authorised = dbc.KeyReleaseWorkflowStates.Count(x => x.ValidUntil < _Dtp.Snapshot && x.LabConfirmationId == null && x.AuthorisedByCaregiver != null && x.DateOfSymptomsOnset != null);

            stats.AuthorisedAndFullyPublished = dbc.KeyReleaseWorkflowStates.Count(x => x.ValidUntil < _Dtp.Snapshot
                                                                                        && x.AuthorisedByCaregiver != null
                                                                                        && x.DateOfSymptomsOnset != null
                                                                                        && x.LabConfirmationId == null
                                                                                        && x.Teks.Count(y => y.PublishingState == PublishingState.Unpublished) == 0);

            stats.TekCount = dbc.TemporaryExposureKeys.Count();
            stats.TekPublished = dbc.TemporaryExposureKeys.Count(x => x.PublishingState == PublishingState.Published);
            stats.TekUnpublished = dbc.TemporaryExposureKeys.Count(x => x.PublishingState == PublishingState.Unpublished);
        }

        private void Log(WorkflowStats stats, string message)
        {
            var sb = new StringBuilder(message);
            sb.AppendLine();
            sb.AppendLine($"{nameof(WorkflowStats.Count)}:{stats.Count}");
            sb.AppendLine($"{nameof(WorkflowStats.Expired)}:{stats.Expired}");
            sb.AppendLine("of which:");
            sb.AppendLine($"   Unauthorised:{stats.Unauthorised}");
            sb.AppendLine($"   Authorised:{stats.Authorised}");
            sb.AppendLine("    of which:");
            sb.AppendLine($"      FullyPublished:{stats.AuthorisedAndFullyPublished}");
            sb.AppendLine();
            sb.AppendLine($"{nameof(WorkflowStats.TekCount)}:{stats.TekPublished}");
            sb.AppendLine("of which:");
            sb.AppendLine($"   Published:{stats.TekPublished}");
            sb.AppendLine($"   Unpublished:{stats.TekUnpublished}");

            _Logger.LogInformation(sb.ToString());
        }


        /// <summary>
        /// NB Do not delete workflows where the keys have not yet been published.
        /// NB Posting more TEKs into a workflow is blocked by validation/filtering and does not rely on this deletion
        /// 1. After 0430Z, run the EKS Engine
        /// 2. Run this deletion
        /// This kills the ones already published and the TEKs that will be published AFTER 0400Z.
        /// Cascading delete kills the TEKs.
        /// </summary>
        public RemoveExpiredWorkflowsResult Execute()
        {
            if (_Result != null)
                throw new InvalidOperationException("Object already used.");


            _Result = new RemoveExpiredWorkflowsResult();
            _Result.DeletionsOn = _Config.CleanupDeletesData;

            _Logger.LogInformation("Begin Workflow cleanup.");
            _Logger.LogInformation("Workflow cleanup complete.");

            using (var dbc = _DbContextProvider())
            {
                using (var tx = dbc.BeginTransaction())
                {
                    ReadStats(_Result.Before, dbc);
                    Log(_Result.Before, "Workflow stats before cleanup:");

                    if (!_Result.DeletionsOn)
                    {
                        _Logger.LogInformation("No Workflows deleted - Deletions switched off");
                        return _Result;
                    }

                    _Result.UnauthorisedKilled = dbc.Database.ExecuteSqlInterpolated($"WITH WalkingDead As ( SELECT Id FROM [TekReleaseWorkflowState] As [w] WHERE [ValidUntil] < {_Dtp.Snapshot} AND [AuthorisedByCaregiver] IS NULL AND [DateOfSymptomsOnset] IS NULL AND [LabConfirmationId] IS NOT NULL) DELETE WalkingDead");
                    _Logger.LogInformation("Workflows deleted - Unauthorised:{unauthorised}", _Result.UnauthorisedKilled);
                    _Result.AuthorisedAndFullyPublishedKilled = dbc.Database.ExecuteSqlInterpolated($"WITH WalkingDead As ( SELECT Id FROM [TekReleaseWorkflowState] As [w] WHERE [ValidUntil] < {_Dtp.Snapshot} AND [AuthorisedByCaregiver] IS NOT NULL AND [DateOfSymptomsOnset] IS NOT NULL AND [LabConfirmationId] IS NULL AND (SELECT COUNT(*) FROM [TemporaryExposureKeys] AS [t] WHERE [w].[Id] = [t].[OwnerId] AND [t].[PublishingState] = 0) = 0) DELETE WalkingDead");
                    _Logger.LogInformation("Workflows deleted - FullyPublished:{fullyPublished}", _Result.AuthorisedAndFullyPublishedKilled);
                    tx.Commit();
                }

                using (dbc.BeginTransaction())
                    ReadStats(_Result.After, dbc);

                Log(_Result.Before, "Workflow stats after cleanup:");
                return _Result;
            }
        }
    }
}