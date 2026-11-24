# SPDX-License-Identifier: AGPL-3.0-or-later
#
# The KoSIT validator with the XRechnung configuration, the reference the
# e-invoicing module's verdicts are compared with. A test oracle only: it is
# built by KositValidatorContainer while the tests run, never published,
# never shipped. The configuration bundles the EN 16931 schematron under
# EUPL-1.2, so it is downloaded here from a fixed release, checked against
# its checksum, and never committed.
FROM eclipse-temurin:21-jre-alpine@sha256:5d7689f47d670aca766b9503a70bccc2d73b4eb9221ae8f714ae958313c4ce70

ARG VALIDATOR_VERSION=1.6.3
ARG VALIDATOR_SHA256=799e64befca97d4080e03608c80b85dd5a5ecc5f4ae4f35d1116ec2855b9a7c9
ARG CONFIGURATION_TAG=v2026-08-31
ARG CONFIGURATION_FILE=xrechnung-3.0.2-validator-configuration-2026-08-31.zip
ARG CONFIGURATION_SHA256=2530cd107c414511c5d0462ec10f886910395abfca820db82e83d70bf01221a8

WORKDIR /opt/kosit

RUN wget -q -O validator.jar "https://github.com/itplr-kosit/validator/releases/download/v${VALIDATOR_VERSION}/validator-${VALIDATOR_VERSION}-standalone.jar" \
 && echo "${VALIDATOR_SHA256}  validator.jar" | sha256sum -c - \
 && wget -q -O configuration.zip "https://github.com/itplr-kosit/validator-configuration-xrechnung/releases/download/${CONFIGURATION_TAG}/${CONFIGURATION_FILE}" \
 && echo "${CONFIGURATION_SHA256}  configuration.zip" | sha256sum -c - \
 && mkdir configuration \
 && unzip -q configuration.zip -d configuration \
 && rm configuration.zip

USER 10001

EXPOSE 8080

ENTRYPOINT ["java", "-jar", "/opt/kosit/validator.jar", "-s", "/opt/kosit/configuration/scenarios.xml", "-r", "/opt/kosit/configuration", "-D", "-H", "0.0.0.0", "-P", "8080"]
