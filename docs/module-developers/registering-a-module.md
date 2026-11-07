# Registering a module

A module becomes reachable through the gateway by registering at the registry.
With the SDK, `Homeport.Sdk`, that is configuration: `AddHomeportModule`
registers the instance once it has started and keeps it registered. A module
in another language implements the same protocol; it is specified in the
[module contract](../reference/module-contract/README.md#registration-and-heartbeat).

## Configure it

| Setting | Environment variable | Meaning |
|---|---|---|
| `Homeport:Registry:Url` | `Homeport__Registry__Url` | The registry, such as `http://registry` |
| `Homeport:Registry:Key` | `Homeport__Registry__Key` | The module's registration key, a secret from the operator |
| `Homeport:Registry:InstanceAddress` | `Homeport__Registry__InstanceAddress` | Where the gateway reaches this instance, such as `http://contracts-1:8080` |

Without `Url`, the module does not register and logs a warning; the gateway
never routes to it. That is how a module runs on its own, in a test for
example. With `Url` but without `Key` or an absolute `InstanceAddress`, the
module refuses to start.

In the AppHost, give the module a key parameter and its own endpoint as
address. Echo is the reference, in `src/Aspire/Homeport.Aspire.AppHost/Program.cs`:

```csharp
var contracts = builder.AddProject<Projects.Homeport_Modules_Contracts_Api>("contracts")
    .WithEnvironment("Homeport__Registry__Url", "http://registry")
    .WithEnvironment("Homeport__Registry__Key", contractsRegistrationKey)
    .WithReference(registry);
contracts.WithEnvironment("Homeport__Registry__InstanceAddress", contracts.GetEndpoint("http"));
```

The registry needs the same key under the module id; see
[the registry](../operators/registry.md#registration-keys).

## What happens

1. **Registration.** Once the module serves requests, it sends its manifest
   and its address to the registry. Each instance chooses an id of its own
   at start, so instances of one module never replace each other.
2. **Heartbeat.** Every 10 s the instance renews its registration. An
   instance without a heartbeat for 30 s is dropped, and the gateway stops
   routing to it at its next poll, at most 5 s later.
3. **Registering again.** When the registry answers a heartbeat with `404` —
   it was down for longer than 30 s, or Valkey lost the instance — the
   instance registers again on its own.

The registry checks the manifest against the module contract, not the SDK.
When it refuses the registration, the module logs the reason at Error and
stops with exit code 1, because it would run without ever being routed to:

| Answer | Reason | Log names |
|---|---|---|
| `401` | Unknown module id, or a wrong key | the status |
| `422` | The manifest breaks the module contract | every field and its message, such as `contractVersion: Contract version 99 is not supported; supported versions: 1.` |

A registry that cannot be reached is not a refusal. The module logs a
warning and tries again every 10 s.

## Several instances

Start as many instances as you need, each with its own `InstanceAddress`.
The gateway sends each request to one of the instances that declare its
route. Instances that register the same address, such as compose replicas
behind one service name, count as one: the network in front of them
distributes the requests, not the gateway.

An instance that stops without a final word keeps receiving requests until
it is dropped, up to 35 s. The gateway answers those with `502`
[`module-unreachable`](../reference/problems/module-unreachable.md).

## Rolling update

Roll out a new version instance by instance. The routes follow the instances
that declare them:

- A route both versions declare goes to every instance of either version.
  Where they declare it with different attributes — scope, meter, quantity —
  those of the instance with the highest `version` apply.
- A route only the new version declares goes only to its instances.
- A route only the old version declares disappears once its last instance
  is dropped.

No step needs the gateway or the registry to be touched.

## Two major versions side by side

A breaking change is a new major version under the same module id: `/v2/…`
next to `/v1/…`. Either deploy one build that declares both, or keep the old
build running for `/v1/…` next to the new one declaring `/v2/…`. Each route
goes only to the instances that declare it, so callers move to `/v2/…` when
they are ready, and `/v1/…` disappears with the last instance that declares
it.
