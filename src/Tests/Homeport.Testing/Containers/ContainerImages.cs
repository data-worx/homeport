// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Testing.Containers;

/// <summary>
/// The third-party images the tests start, each with the <c>tag@digest</c>
/// the AppHost pins for it, so the tests run against the image an operator
/// runs. A digest update in the AppHost is copied here in the same pull
/// request.
/// </summary>
public static class ContainerImages
{
    public const string Postgres = "postgres:18.7-alpine@sha256:0115267ab5a509022df9319c77be365cef335adb71556c75f60f5360ee247e71";

    public const string RabbitMq = "rabbitmq:4.4-alpine@sha256:7b77c3d3a996c7fcca1ca8bf53ce1557ff43c13b8ca2c132df6031781b209ef2";

    public const string Valkey = "valkey/valkey:9.2-alpine@sha256:c8e2d17498838af5d91e12eb7c11220b3434a7b7131a333e3af52ad5e0b80882";
}
