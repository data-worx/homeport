// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Gateway.Infrastructure.ControlPlane;

/// <summary>The control plane could not be reached or answered with an error.</summary>
public sealed class ControlPlaneUnavailableException : Exception
{
    /// <summary>Creates the exception.</summary>
    public ControlPlaneUnavailableException()
    {
    }

    /// <summary>Creates the exception with a message.</summary>
    public ControlPlaneUnavailableException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and the failure that caused it.</summary>
    public ControlPlaneUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
