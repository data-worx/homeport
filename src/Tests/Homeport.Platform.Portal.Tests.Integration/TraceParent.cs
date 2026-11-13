// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Portal.Tests.Integration;

/// <summary>A W3C trace context a request carries, so a test knows the trace id the portal shows.</summary>
internal static class TraceParent
{
    internal const string Header = "traceparent";

    internal const string TraceId = "4bf92f3577b34da6a3ce929d0e0e4736";

    internal const string Value = "00-" + TraceId + "-00f067aa0ba902b7-01";
}
