// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text;
using Homeport.Shared.Client.Platform.Metering.V1;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Homeport.Platform.Metering.Api.Export;

/// <summary>
/// Answers a request for <see cref="UsageApi.CsvMediaType"/> with
/// <see cref="UsageCsv"/>. Writes nothing but a <see cref="UsageResponse"/>.
/// </summary>
internal sealed class UsageCsvOutputFormatter : TextOutputFormatter
{
    /// <summary>Supports the CSV media type, in UTF-8 without a byte order mark.</summary>
    public UsageCsvOutputFormatter()
    {
        SupportedMediaTypes.Add(UsageApi.CsvMediaType);
        SupportedEncodings.Add(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    /// <summary>Writes the usage as CSV.</summary>
    public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
    {
        var usage = (UsageResponse)context.Object!;
        await context.HttpContext.Response.WriteAsync(UsageCsv.Write(usage), selectedEncoding, context.HttpContext.RequestAborted);
    }

    /// <summary>Writes the usage response only.</summary>
    protected override bool CanWriteType(Type? type) => type == typeof(UsageResponse);
}
