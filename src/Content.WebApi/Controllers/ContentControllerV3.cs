﻿// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Content.Commands;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Content.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ContentControllerV3 : ControllerBase
    {
        [HttpGet]
        [Route("/v3/manifest")]
        public async Task GetManifest([FromServices] HttpGetCdnManifestCommand command)
        {
            await command.ExecuteAsync(HttpContext, ContentTypes.ManifestV3);
        }

        [HttpGet]
        [Route("/v3/appconfig/{id}")]
        public async Task GetAppConfig(string id, [FromServices] HttpGetCdnImmutableNonExpiringContentCommand command)
        {
            await command.ExecuteAsync(HttpContext, ContentTypes.AppConfigV2, id);
        }

        [HttpGet]
        [Route("/v3/riskcalculationparameters/{id}")]
        public async Task GetRiskCalculationParameters(string id, [FromServices] HttpGetCdnImmutableNonExpiringContentCommand command)
        {
            await command.ExecuteAsync(HttpContext, ContentTypes.RiskCalculationParametersV2, id);
        }

        [HttpGet]
        [Route("/v3/exposurekeyset/{id}")]
        public async Task GetExposureKeySet(string id, [FromServices] HttpGetCdnEksCommand command)
        {
            await command.ExecuteAsync(HttpContext, ContentTypes.ExposureKeySetV2, id);
        }

        [HttpGet]
        [Route("/v3/resourcebundle/{id}")]
        public async Task GetResourceBundleAsync(string id, [FromServices] HttpGetCdnImmutableNonExpiringContentCommand command)
        {
            await command.ExecuteAsync(HttpContext, ContentTypes.ResourceBundleV3, id);
        }
    }
}
