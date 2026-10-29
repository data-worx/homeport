// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Shared.Contracts;
using Microsoft.AspNetCore.Http;

namespace Homeport.Sdk.Tests.Integration.Controllers;

/// <summary>The problem types of the test module, declared the way a module declares its own.</summary>
internal static class TestProblems
{
    internal const string NotFoundCode = "thing.not-found";

    internal static readonly ProblemType Conflict = new(
        new Uri("https://things.example/problems/conflict"),
        "The thing already exists.",
        StatusCodes.Status409Conflict);

    internal static readonly ProblemType NotFound = new(
        new Uri("https://things.example/problems/not-found"),
        "The thing does not exist.",
        StatusCodes.Status404NotFound);

    internal static ProblemType For(Error error)
    {
        return error.Code switch
        {
            NotFoundCode => NotFound,
            _ => HomeportProblems.Internal,
        };
    }
}
