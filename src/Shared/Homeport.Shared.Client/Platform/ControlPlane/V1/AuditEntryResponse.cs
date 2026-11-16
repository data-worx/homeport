// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Platform.ControlPlane.V1;

/// <summary>What an operator did, as it was recorded at the time.</summary>
/// <param name="Id">The entry's id.</param>
/// <param name="OccurredAt">When it was done, in UTC.</param>
/// <param name="OperatorSubject">The operator's subject at the identity provider.</param>
/// <param name="OperatorName">The operator's display name at the time.</param>
/// <param name="Action">What was done, such as <c>tenant.created</c>.</param>
/// <param name="Target">What it was done to, such as <c>tenants/{id}</c>.</param>
/// <param name="Summary">What was done, in one English sentence.</param>
public sealed record AuditEntryResponse(Guid Id, DateTime OccurredAt, string OperatorSubject, string OperatorName, string Action, string Target, string Summary);
