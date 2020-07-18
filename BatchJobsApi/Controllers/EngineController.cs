﻿// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.DevOps;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.Manifest;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.BatchJobsApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    //[Authorize]
    public class EngineController : ControllerBase
    {
        private readonly ILogger _Logger;

        public EngineController(ILogger<EngineController> logger)
        {
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Generate new ExposureKeySets.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("/v1/eksengineexecute")]
        public async Task<IActionResult> ExposureKeySets([FromQuery] bool useAllKeys, [FromServices] HttpPostGenerateExposureKeySetsCommand command)
        {
            _Logger.LogInformation("EKS Engine triggered.");
            return await command.Execute(useAllKeys);
        }

        /// <summary>
        /// Generate new ExposureKeySets.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("/v1/manifestengineexecute")]
        public async Task<IActionResult> Manifest([FromServices] ManifestBatchJob command)
        {
            _Logger.LogInformation("Manifest Engine triggered.");
            await command.Execute();
            return new OkResult();
        }
    }
}
