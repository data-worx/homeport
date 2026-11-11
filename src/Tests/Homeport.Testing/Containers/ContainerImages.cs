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
    public const string Postgres = "postgres:18.6-alpine@sha256:123b17998ccaf47e53b502e8c6a1e79afd8f579c6f3eec83c563da026f957395";

    public const string RabbitMq = "rabbitmq:4.3-alpine@sha256:68fd0566c1c5a5aef2d7b3ad55af8be503a767c1ff8646d7088c8c7946270cf9";

    public const string Valkey = "valkey/valkey:9.1-alpine@sha256:bf09d58e96d6b4524a15396b65b242bbe18beebbcc15a04d8ee75ddac62fae0b";
}
