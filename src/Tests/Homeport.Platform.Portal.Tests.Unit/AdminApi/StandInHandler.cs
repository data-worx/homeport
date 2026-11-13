// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Portal.Tests.Unit.AdminApi;

/// <summary>
/// Answers every request with what <see cref="Respond"/> returns, or throws
/// what <see cref="Failure"/> holds, and records the request once answered.
/// </summary>
internal sealed class StandInHandler : HttpMessageHandler
{
    private readonly List<(HttpRequestMessage Request, string? Body)> received = [];

    /// <summary>The answer to a request.</summary>
    internal Func<HttpRequestMessage, HttpResponseMessage> Respond { get; set; } = _ => new HttpResponseMessage(System.Net.HttpStatusCode.OK);

    /// <summary>When set, every request fails with it, as an unreachable server does.</summary>
    internal Exception? Failure { get; set; }

    /// <summary>The requests answered so far, with their bodies.</summary>
    internal IReadOnlyList<(HttpRequestMessage Request, string? Body)> Received => received;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (Failure is not null)
        {
            throw Failure;
        }

        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        var response = Respond(request);
        received.Add((request, body));
        return response;
    }
}
