#!/usr/bin/env bash
# SPDX-License-Identifier: AGPL-3.0-or-later
#
# Starts RabbitMQ with a user of its own for every Homeport service, each
# allowed only what the service does (ADR-0014), and the administrator from
# RABBITMQ_DEFAULT_USER and RABBITMQ_DEFAULT_PASS, which only the broker's
# own health check uses.
#
# It runs as the container's entrypoint: it writes the users into a
# definitions file that RabbitMQ imports while it boots, so they exist before
# the first connection is accepted, and then hands over to the image's own
# entrypoint. It runs on every start, so a changed password takes effect with
# the next one. Against a broker of your own, create the same users and
# permissions once with rabbitmqctl.
#
# The exchange and queue names are those of UsageMessaging in
# src/Shared/Homeport.Shared.Internal; Wolverine's dead-letter queue is the
# one metering moves a message to that it could not record.
set -euo pipefail

definitions=/var/lib/rabbitmq/homeport-definitions.json
config=/etc/rabbitmq/conf.d/90-homeport.conf

# A JSON string: backslashes and quotes escaped. Control characters are
# refused rather than escaped; no password needs them.
json_string() {
  local value="$1"
  if [[ "${value}" =~ [[:cntrl:]] ]]; then
    echo "init-homeport.sh: a value contains a control character" >&2
    exit 1
  fi
  value="${value//\\/\\\\}"
  value="${value//\"/\\\"}"
  printf '"%s"' "${value}"
}

user() {
  local name="$1" password="$2" tags="$3"
  printf '{"name":%s,"password":%s,"tags":[%s]}' "$(json_string "${name}")" "$(json_string "${password}")" "${tags}"
}

permission() {
  local name="$1" configure="$2" write="$3" read="$4"
  printf '{"user":%s,"vhost":"/","configure":"%s","write":"%s","read":"%s"}' "$(json_string "${name}")" "${configure}" "${write}" "${read}"
}

admin="${RABBITMQ_DEFAULT_USER:?}"

# Regular expressions in JSON: a literal dot is written \\. here.
cat > "${definitions}" <<JSON
{
  "vhosts": [{"name": "/"}],
  "users": [
    $(user "${admin}" "${RABBITMQ_DEFAULT_PASS:?}" '"administrator"'),
    $(user gateway "${GATEWAY_RABBITMQ_PASSWORD:?}" ''),
    $(user metering "${METERING_RABBITMQ_PASSWORD:?}" '')
  ],
  "permissions": [
    $(permission "${admin}" '.*' '.*' '.*'),
    $(permission gateway '^homeport\\.usage$' '^homeport\\.usage$' '^$'),
    $(permission metering \
      '^(homeport\\.usage|homeport\\.metering\\.usage|wolverine-dead-letter-queue)$' \
      '^(homeport\\.metering\\.usage|wolverine-dead-letter-queue)$' \
      '^(homeport\\.usage|homeport\\.metering\\.usage|wolverine-dead-letter-queue)$')
  ]
}
JSON
chown rabbitmq:rabbitmq "${definitions}"
chmod 600 "${definitions}"

cat > "${config}" <<CONF
definitions.import_backend = local_filesystem
definitions.local.path = ${definitions}
CONF

exec docker-entrypoint.sh "$@"
