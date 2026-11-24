// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Model;

/// <summary>Names the business term (BT) or business group (BG) of EN 16931 a member of the model holds.</summary>
/// <param name="id">The identifier of the term or group, such as <c>BT-1</c> or <c>BG-4</c>.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class BusinessTermAttribute(string id) : Attribute
{
    /// <summary>The identifier of the term or group, such as <c>BT-1</c> or <c>BG-4</c>.</summary>
    public string Id { get; } = id;
}
