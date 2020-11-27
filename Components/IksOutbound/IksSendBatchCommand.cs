﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Eu.Interop;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.EfDatabase.Contexts;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.EfDatabase.Entities;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Components.IksOutbound
{
    // TODO: this class (together with HttpPostIksCommand) will be refactored soon!
    public class IksSendBatchCommand
    {
        private readonly Func<HttpPostIksCommand> _IksSendCommandFactory;
        private readonly Func<IksOutDbContext> _IksOutboundDbContextFactory;
        private List<int> _Todo;
        private IIksSigner _Signer;
        private IBatchTagProvider _BatchTagProvider;
        private readonly List<IksSendResult> _Results = new List<IksSendResult>();
        private HttpPostIksResult _LastResult;
        private ILogger<IksSendBatchCommand> _Logger;

        public IksSendBatchCommand(Func<IksOutDbContext> iksOutboundDbContextFactory, Func<HttpPostIksCommand> iksSendCommandFactory, IIksSigner signer, IBatchTagProvider batchTagProvider, ILogger<IksSendBatchCommand> logger)
        {
            _IksSendCommandFactory = iksSendCommandFactory ?? throw new ArgumentNullException(nameof(iksSendCommandFactory));
            _IksOutboundDbContextFactory = iksOutboundDbContextFactory ?? throw new ArgumentNullException(nameof(iksOutboundDbContextFactory));
            _Signer = signer ?? throw new ArgumentNullException(nameof(signer));
            _BatchTagProvider = batchTagProvider ?? throw new ArgumentNullException(nameof(batchTagProvider));
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IksSendBatchResult> ExecuteAsync()
        {
            using (var dbc = _IksOutboundDbContextFactory())
            {
                _Todo = dbc.Iks
                    .Where(x => !x.Sent)
                    .Select(x => x.Id)
                    .ToList();
            }

            for (var i = 0; i < _Todo.Count && (_LastResult == null || _LastResult.HttpResponseCode == HttpStatusCode.Created); i++)
            {
                await ProcessOne(_Todo[i]);
            }

            return new IksSendBatchResult 
            { 
                Found = _Todo.Count,
                Sent = _Results.ToArray()
            };
        }

        private byte[] SignDks(IksOutEntity item)
        {
            // Unpack
            var parser = new Google.Protobuf.MessageParser<DiagnosisKeyBatch>(()=> new DiagnosisKeyBatch());
            var batch = parser.ParseFrom(item.Content);

            if (batch == null)
            {
                throw new Exception("TODO Something went wrong");
            }

            var efgsSerializer = new EfgsDiagnosisKeyBatchSerializer();

            return _Signer.GetSignature(efgsSerializer.Serialize(batch));
        }

        private async Task ProcessOne(int i)
        {
            using var dbc = _IksOutboundDbContextFactory();
            var item = await dbc.Iks.SingleAsync(x => x.Id == i);
            
            var args = new IksSendCommandArgs
            {
                BatchTag = _BatchTagProvider.Create(item.Content),
                Content = item.Content,
                Signature = SignDks(item),
            };

            await SendOne(args);

            var result = new IksSendResult
            {
                Exception = _LastResult.Exception,
                StatusCode = _LastResult?.HttpResponseCode
            };

            _Results.Add(result);
            
            // Note: EFGS returns Created on successful upload, not OK.
            item.Sent = _LastResult?.HttpResponseCode == HttpStatusCode.Created;

            // TODO: Implement a state machine for batches; this is useful around error cases.
            // * Re-try for selected states.
            // * For data errors, end state with invalid (initially).
            // * Allow for manual fixing of data errors with a special retry state?
            //

            if (item.Sent)
                await dbc.SaveChangesAsync();
        }

        /// <summary>
        /// Pass/Fail
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private async Task SendOne(IksSendCommandArgs args)
        {
            // NOTE: no retry here
            var sender = _IksSendCommandFactory();
            var result = await sender.ExecuteAsync(args);

            _LastResult = result;

            // TODO: handle the return types
            if (result != null)
            {
                switch (result.HttpResponseCode)
                {
                    case HttpStatusCode.OK:
                    case HttpStatusCode.Created:
                        _Logger.LogInformation("EFGS: Success");
                        return;
                    case HttpStatusCode.BadRequest:
                        _Logger.LogError("EFGS: Invalid request (either errors in the data or an invalid signature)");
                        break;
                    case HttpStatusCode.Forbidden:
                        _Logger.LogError("EFGS: Invalid/missing certificates");
                        break;
                    case HttpStatusCode.NotAcceptable:
                        _Logger.LogError("EFGS:  Data format or content is not valid.");
                        break;
                    case HttpStatusCode.RequestEntityTooLarge:
                        _Logger.LogError("EFGS: Data already exist");
                        break;
                    case HttpStatusCode.InternalServerError:
                        _Logger.LogError("EFGS: Not able to write data. Retry please.");
                        break;
                    default:
                        _Logger.LogError("Unknown error: {httpResponseCode}", result.HttpResponseCode);
                        break;
                }
            }

            // TODO for Production Quality Code:
            //
            // Handle the error codes like this:
            //
            // Auto-retry: InternalServerError, ANY undefined error
            // Fix config then retry: BadRequest, Forbidden
            // Fix file then retry: NotAcceptable
            // Skip: NotAcceptable
            //
            // Also: consider splitting this file up into a class which makes the calls, and a class
            // which handles the workflow described above.
            //
            // The table IksOut will gain the fields: State, RetryCount, Retry flag
            // 
            // Code modified to include anything tagged with the Retry flag again.
            //
            // We must also define a State enumeration with a logical set of states as per the error handling.
            // State diagram is helpful here (TODO: Ryan)
            //
            // Basically we must be able to manually trigger retries for any data errors and configuration errors, have automatic retry for
            // transient errors. Ideally driven by some kind of portal, but at first it will be DB tinkering.
            //
            // For states:
            //
            // States: New, Failed, Sent (ended successfully), Skipped (ended unsuccessfully)
            // Failed states (combined Efgs and our own errors):
            //    EfgsInvalidSignature, EfgsInvalidCertificate, EfgsInvalidContent, EfgsDuplicateContent, EfgsUnavailable, EfgsUndefined
            //    UnableToConnect (when we can't connect to efgs), Unknown (catch-all for any other errors)
            //
            // I think that it's cleaner to split the states into State and FailedState; the latter being more detailed states for failures.

        }
    }
}