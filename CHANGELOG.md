# Changelog

All notable changes to this project are documented here.

The format is based on [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Homeport starts locally with its infrastructure — PostgreSQL, RabbitMQ,
  Valkey and Keycloak — either through the .NET Aspire AppHost or through a
  Docker Compose file generated from it. Every third-party image is pinned by
  digest. See [running Homeport locally](docs/operators/running-locally.md).
