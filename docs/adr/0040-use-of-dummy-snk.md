# ADR-0040: Use of Dummy Strong Name Key in Development

## Status

Accepted

## Date

2026-08-13

## Context

For `EricksonLopez.Pagination` (and all its extensions) to be adopted in highly regulated enterprise environments, it must support Strong Naming. This allows assemblies to be installed in the Global Assembly Cache (GAC) and referenced by other strongly named enterprise projects.

However, publicly committing a real private key pair (`.snk` with private key) into the source control of an open-source repository defeats the cryptographic purpose of strong naming, as any party could compile arbitrary code signed with that key.

## Decision

We have decided to check in a public/dummy key (`DummyDevelopmentKey.snk`) into version control to allow local contributors to build and test the codebase as strongly named.

During the official release pipeline (`.github/workflows/publish.yml`), the dummy key is overwritten and replaced with the production private key extracted from repository secrets (`SIGNING_KEY_BASE64`):

```yaml
- name: Inject Strong Name Key
  run: echo "${{ secrets.SIGNING_KEY_BASE64 }}" | base64 -d > DummyDevelopmentKey.snk
```

The central build configuration file (`Directory.Build.props`) conditionally applies assembly signing only if the key file is present:

```xml
<SignAssembly Condition="Exists('$(MSBuildThisFileDirectory)DummyDevelopmentKey.snk')">true</SignAssembly>
<AssemblyOriginatorKeyFile Condition="Exists('$(MSBuildThisFileDirectory)DummyDevelopmentKey.snk')">$(MSBuildThisFileDirectory)DummyDevelopmentKey.snk</AssemblyOriginatorKeyFile>
```

## Consequences

### Positive
- **Enterprise Compatibility**: The official NuGet packages are signed with a secure, inaccessible private key.
- **Developer Experience**: Local developers and contributors do not need to manage private keys. Local builds still execute and test all features requiring strong naming.
- **Security**: The production signing secret `SIGNING_KEY_BASE64` remains strictly confined to GitHub Actions CI infrastructure.

### Negative
- If the signing key secret in GitHub Actions is rotated or replaced, published packages will use a new key, causing a breaking change (different `PublicKeyToken`) for strongly named consumers.
