// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.ControlPlane.Application.Tenants;

/// <summary>The operator who makes a change, as the caller token names them (ADR-0020).</summary>
/// <param name="Subject">The operator's subject at the identity provider.</param>
/// <param name="Name">The operator's display name.</param>
public sealed record Operator(string Subject, string Name);
