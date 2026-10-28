// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace Homeport.Platform.Gateway.Infrastructure.Signing;

/// <summary>Reads the P-256 keys of <see cref="SigningOptions"/> from PEM.</summary>
internal static class SigningKeyPem
{
    /// <summary>Reads a P-256 private key; <see langword="false"/> for anything else.</summary>
    internal static bool TryReadPrivate(string pem, [NotNullWhen(true)] out ECDsa? key)
    {
        return TryRead(pem, expectPrivate: true, out key);
    }

    /// <summary>
    /// Reads a P-256 public key; <see langword="false"/> for anything else,
    /// a private key included, which never belongs where only the public one
    /// is asked for.
    /// </summary>
    internal static bool TryReadPublic(string pem, [NotNullWhen(true)] out ECDsa? key)
    {
        return TryRead(pem, expectPrivate: false, out key);
    }

    private static bool TryRead(string pem, bool expectPrivate, [NotNullWhen(true)] out ECDsa? key)
    {
        var candidate = ECDsa.Create();
        try
        {
            candidate.ImportFromPem(pem);
            if (IsP256(candidate) && HasPrivateKey(candidate) == expectPrivate)
            {
                key = candidate;
                return true;
            }
        }
        catch (Exception exception) when (exception is ArgumentException or CryptographicException)
        {
            // Not a PEM the runtime can read: reported as false.
        }

        candidate.Dispose();
        key = null;
        return false;
    }

    private static bool IsP256(ECDsa key)
    {
        return key.ExportParameters(includePrivateParameters: false).Curve.Oid.Value
            == ECCurve.NamedCurves.nistP256.Oid.Value;
    }

    private static bool HasPrivateKey(ECDsa key)
    {
        try
        {
            key.ExportParameters(includePrivateParameters: true);
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }
}
