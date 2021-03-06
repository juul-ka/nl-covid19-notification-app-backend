﻿// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Core.AspNet;
using NL.Rijksoverheid.ExposureNotification.BackEnd.MobileAppApi.Commands.DecoyKeys;
using NL.Rijksoverheid.ExposureNotification.BackEnd.MobileAppApi.Commands.RegisterSecret;
using NL.Rijksoverheid.ExposureNotification.BackEnd.MobileAppApi.Commands.SendTeks;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.MobileAppApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WorkflowController : ControllerBase
    {
        private readonly RegisterSecretLoggingExtensions _LoggerRegisterSecret;
        private readonly PostKeysLoggingExtensions _LoggerPostKeys;
        private readonly DecoyKeysLoggingExtensions _LoggerDecoyKeys;

        public WorkflowController(RegisterSecretLoggingExtensions loggerRegisterSecret, PostKeysLoggingExtensions loggerPostKeys, DecoyKeysLoggingExtensions LoggerDecoyKeys)
        {
            _LoggerRegisterSecret = loggerRegisterSecret ?? throw new ArgumentNullException(nameof(loggerRegisterSecret));
            _LoggerPostKeys = loggerPostKeys ?? throw new ArgumentNullException(nameof(loggerPostKeys));
            _LoggerDecoyKeys = LoggerDecoyKeys ?? throw new ArgumentNullException(nameof(LoggerDecoyKeys));
        }

        [ResponsePaddingFilterFactory]
        [SuppressErrorFactory]
        [DecoyTimeAggregatorAttributeFactory]
        [HttpPost]
        [Route(EndPointNames.MobileAppApi.ReleaseTeks)]
        public async Task<IActionResult> PostWorkflow([FromQuery] byte[] sig, [FromServices] HttpPostReleaseTeksCommand2 command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            _LoggerPostKeys.WriteStartPostKeys();
            return await command.ExecuteAsync(sig, Request);
        }

        [ResponsePaddingFilterFactory]
        [SuppressErrorFactory]
        [DecoyTimeAggregatorAttributeFactory]
        [HttpPost]
        [Route(EndPointNames.MobileAppApi.Register)]
        public async Task<IActionResult> PostSecret([FromServices]HttpPostRegisterSecret command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            _LoggerRegisterSecret.WriteStartSecret();
            return await command.ExecuteAsync();
        }

        [ResponsePaddingFilterFactory]
        [SuppressErrorFactory]
        [DecoyTimeGeneratorAttributeFactory]
        [HttpPost]
        [Route(EndPointNames.MobileAppApi.RandomNoise)]
        public async Task<IActionResult> StopKeys()
        {
            _LoggerDecoyKeys.WriteStartDecoy();
            return new OkResult();
        }

        [HttpGet]
        [Route("/")]
        public IActionResult AssemblyDump([FromServices] IWebHostEnvironment env) => new DumpAssembliesToPlainText().Execute(env.IsDevelopment()); //ncrunch: no coverage
    }
}
