// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.Workflow;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Components.EfDatabase.Entities
{
    public class TekEntity
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public TekReleaseWorkflowStateEntity Owner { get; set; }
        
        [MinLength(UniversalConstants.DailyKeyDataLength), MaxLength(UniversalConstants.DailyKeyDataLength)]
        public byte[] KeyData { get; set; } = new byte[UniversalConstants.DailyKeyDataLength];
        public int RollingStartNumber { get; set; }
        public int RollingPeriod { get; set; }

        public PublishingState PublishingState { get; set; }
       
        public DateTime PublishAfter { get; set; }
    }
}