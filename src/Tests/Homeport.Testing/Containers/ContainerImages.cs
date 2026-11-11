// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Testing.Containers;

/// <summary>
/// The third-party images the tests start, pinned as <c>tag@digest</c>, so
/// every test project runs against the same image.
/// </summary>
public static class ContainerImages
{
    public const string Postgres = "postgres:18.3@sha256:7e32e9833a6fb1c92c32552794cb6ed569d51b445a54907d35fc112ef39684db";

    public const string RabbitMq = "rabbitmq:4.3@sha256:8144dac709541439da6106d7421079132171b7f0a48b7a7b1f4bd39b22abdb75";

    public const string Valkey = "valkey/valkey:9.1@sha256:418652cfb58ef879d4978c33553735d7147016032d5aefaa14c828e611eb9dfd";
}
