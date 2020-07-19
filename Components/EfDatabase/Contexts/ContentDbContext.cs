﻿// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System;
using Microsoft.EntityFrameworkCore;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.ExposureKeySets;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.ExposureKeySetsEngine;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.Manifest;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.ContentLoading;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Components.EfDatabase.Contexts
{
    public class PublishingJobDbContext : DbContext
    {
        public PublishingJobDbContext(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<EksCreateJobInputEntity> EksInput { get; set; }
        public DbSet<EksCreateJobOutputEntity> EksOutput { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (modelBuilder == null) throw new ArgumentNullException(nameof(modelBuilder));
            modelBuilder.HasDefaultSchema("dbo");
            modelBuilder.ApplyConfiguration(new Configuration.Content.EksCreateJobInput());
            modelBuilder.ApplyConfiguration(new Configuration.Content.EksCreateJobOutput());
            modelBuilder.ApplyConfiguration(new Configuration.Content.ExposureKeySetContent());
            modelBuilder.ApplyConfiguration(new Configuration.Content.Manifest());
            modelBuilder.ApplyConfiguration(new Configuration.Content.GenericContentConfig());
        }
    }

    public class ContentDbContext : DbContext
    {
        public ContentDbContext(DbContextOptions options)
            : base(options)
        {
        }

        //TODO MOVE
        public DbSet<EksCreateJobInputEntity> EksInput { get; set; }
        //TODO MOVE
        public DbSet<EksCreateJobOutputEntity> EksOutput { get; set; }
        public DbSet<ManifestEntity> ManifestContent { get; set; }
        public DbSet<ExposureKeySetContentEntity> ExposureKeySetContent { get; set; }
        public DbSet<GenericContentEntity> GenericContent { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (modelBuilder == null) throw new ArgumentNullException(nameof(modelBuilder));
            modelBuilder.HasDefaultSchema("dbo");
            modelBuilder.ApplyConfiguration(new Configuration.Content.EksCreateJobInput());
            modelBuilder.ApplyConfiguration(new Configuration.Content.EksCreateJobOutput());
            modelBuilder.ApplyConfiguration(new Configuration.Content.ExposureKeySetContent());
            modelBuilder.ApplyConfiguration(new Configuration.Content.Manifest());
            modelBuilder.ApplyConfiguration(new Configuration.Content.GenericContentConfig());
        }
    }
}
