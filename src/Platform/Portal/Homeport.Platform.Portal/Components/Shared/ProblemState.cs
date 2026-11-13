// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Shared.Client.Problems;

namespace Homeport.Platform.Portal.Components.Shared;

/// <summary>
/// The one way a page shows a problem of the admin API (ADR-0020): the
/// entries of <c>errors</c> at the form fields the page names, and everything
/// else - <c>title</c>, <c>detail</c>, the other entries and the trace id -
/// in a <see cref="ProblemAlert"/>.
/// </summary>
/// <param name="fields">The field paths the page shows at its form fields, such as <c>name</c>.</param>
public sealed class ProblemState(params string[] fields)
{
    private const string Separator = " ";

    /// <summary>The problem shown, if any.</summary>
    public ProblemResponse? Problem { get; private set; }

    /// <summary>
    /// Whether the problem needs the alert: anything but errors that all sit
    /// at a form field of the page.
    /// </summary>
    public bool ShowsAlert => Problem is not null && (Problem.Errors is not { Count: > 0 } || OtherErrors.Count > 0);

    /// <summary>The errors whose field the page does not show.</summary>
    public IReadOnlyList<KeyValuePair<string, string[]>> OtherErrors =>
        Problem?.Errors?.Where(error => !fields.Contains(error.Key, StringComparer.Ordinal)).ToList() ?? [];

    /// <summary>Shows <paramref name="problem"/>.</summary>
    public void Show(ProblemResponse problem) => Problem = problem;

    /// <summary>Shows nothing.</summary>
    public void Clear() => Problem = null;

    /// <summary>Whether the field <paramref name="field"/> has an error.</summary>
    public bool HasError(string field) => ErrorOf(field) is not null;

    /// <summary>The messages of the field <paramref name="field"/>, or <see langword="null"/>.</summary>
    public string? ErrorOf(string field)
    {
        return Problem?.Errors?.TryGetValue(field, out var messages) == true
            ? string.Join(Separator, messages)
            : null;
    }
}
