﻿using System;
using Microsoft.Extensions.Logging;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Components.Logging.GetCdnContent
{
	public static class LoggingExtensionsGetCdnContent
	{
        private const string Name = "HttpGetCdnContent";
        private const int First = LoggingCodex.GetCdnContent;

        private const int InvalidType = First;
        private const int InvalidId = First + 1;
        private const int HeaderMissing = First + 2;
        private const int NotFound = First + 3;
        private const int EtagFound = First + 4;

		public static void WriteInvalidType(this ILogger logger, string cdnId)
		{
			if (logger == null) throw new ArgumentNullException(nameof(logger));

			logger.LogError("[{name}/{id}] Invalid generic content type - {Id}.", Name, InvalidType,
				cdnId);
		}

		public static void WriteInvalidId(this ILogger logger, string cdnId)
		{
			if (logger == null) throw new ArgumentNullException(nameof(logger));

			logger.LogError("[{name}/{id}] Invalid content id - {Id}.",
				Name, InvalidId,
				cdnId);
		}

		public static void WriteHeaderMissing(this ILogger logger)
		{
			if (logger == null) throw new ArgumentNullException(nameof(logger));

			logger.LogError("[{name}/{id}] Required request header missing - if-none-match.",
				Name, HeaderMissing
				);
		}

		public static void WriteNotFound(this ILogger logger, string cdnId)
		{
			if (logger == null) throw new ArgumentNullException(nameof(logger));

			logger.LogError("[{name}/{id}] Content not found - {Id}.",
				Name, NotFound,
				cdnId);
		}

		public static void WriteEtagFound(this ILogger logger, string cdnId)
		{
			if (logger == null) throw new ArgumentNullException(nameof(logger));

			logger.LogWarning("[{name}/{id}] Matching etag found, responding with 304 - {Id}.",
				Name, EtagFound,
				cdnId);
		}
	}
}
