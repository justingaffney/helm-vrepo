# helm-vrepo
`helm-vrepo` is a Helm plugin for building virtual Helm repositories by aggregating charts from multiple upstream repositories (HTTP, OCI, or manual sources), and optionally filtering the chart versions using configurable policies.

## Use Cases
- **Version governance**
    - Apply consistent version policies across charts, for example excluding prerelease versions
    - Use SemVer ranges and regex for advanced chart version filtering
- **Chart whitelisting**
    - Consolidate charts from multiple repositories into a single unified repository index
    - Simplify whitelisting source Helm repositories in GitOps systems such as [Argo CD](https://argo-cd.readthedocs.io/en/stable/user-guide/projects/#managing-projects) or [Flux](https://fluxcd.io/flux/guides/helmreleases/#helm-repository) (Flux support is [limited](#mixed-mode-repository-indexes-are-explicitly-not-supported-in-flux))
- **Development workflows**
  - Test new chart versions by creating multiple virtual repositories with different version policies. For example a testing repository could be created that allows prerelease versions, but is limited to a small set of charts and versions

## Installation
> [!IMPORTANT]
> Requires Helm 4.3.0+. Earlier Helm versions are not supported, plugin installations will fail.

> [!WARNING]
> Helm currently has one [bug](https://github.com/helm/helm/issues/32541) (with a [PR](https://github.com/helm/helm/pull/32548) open to fix it) affecting all 4.x versions, that prevents installation of plugins from multi-architecture OCI images. So (at the time of writing this) the multi-architecture `helm-vrepo` plugin's OCI image cannot be installed on any Helm 4 versions, and the install command `helm plugin install oci://...` documented below will fail. 
>
> The current workaround is to directly install the plugin's single-architecture OCI image that matches the architecture of the system where the plugin is being installed (can be found in the `OS / Arch` tab in https://github.com/justingaffney/helm-vrepo/pkgs/container/helm-vrepo%2Fhelm-vrepo-plugin).

### Set up GPG public key for signature verification
> Helm verifies plugins by default during installation. Verification can be disabled by including `--verify=false` in the installation command, and this step can be skipped (_**not recommended**_).

#### Import the GPG public key
```bash
curl -SsL https://github.com/justingaffney/helm-vrepo/raw/refs/heads/main/public-key.asc | gpg --import
```

#### (*Optional)* Verify the fingerprint
> Expected fingerprint: `E6E5 F0F4 D783 63B0 3009  2C03 91EC EDE2 3C1F 5E78`
```bash
gpg --list-keys --with-fingerprint 91ECEDE23C1F5E78
```

### Install
```bash
helm plugin install oci://ghcr.io/justingaffney/helm-vrepo/helm-vrepo-plugin:latest
```

> [!IMPORTANT]
> By default Helm looks for the GPG keyring file at `~/.gnupg/pubring.kbx`, but on Windows the GPG keyring file is at `%APPDATA%/gnupg/pubring.kbx`. The correct file path must be specified using the `--keyring` option.

### Uninstall
```bash
helm plugin uninstall helm-vrepo
```

### Update
Just uninstall and then reinstall with the new version.

### (*Alternative*) Install as a standalone CLI tool
The Helm plugin binary is just a regular CLI application, and can alternatively be downloaded directly from [GitHub Releases](../../releases) and run as a standalone CLI tool (using `helm-vrepo` instead of `helm vrepo`).

## Quick Start

### 1. Initialize a Configuration File
Create a new virtual repository configuration:
```bash
helm vrepo init -o vrepo.yaml
```
This generates a configuration file with commented examples.

### 2. Configure Your Virtual Repository
Edit `vrepo.yaml` to specify:
- **Upstream repositories**: List of HTTP or OCI repositories with the specific charts you want to include
  - **Version policies**: Apply version filtering at the global, repository, or individual level
- **Manual charts**: Locally-hosted or custom charts to include

### 3. Generate the Repository Index
Generate a virtual repository index from your configuration:
```bash
helm vrepo gen -c vrepo.yaml -o index.yaml
```
This creates an `index.yaml` file that Helm can use as a repository.

### 4. Use Your Virtual Repository
Add the virtual repository to Helm (must be accessible via HTTP or local file):
```bash
helm repo add my-virtual-repository file:///path/to/index.yaml
helm repo update
```

Now you can search and install charts from your virtual repository:
```bash
helm search repo my-virtual-repository
helm install my-release my-virtual-repository/some-chart
```

## Configuration

### Virtual Repository Configuration (`vrepo.yaml`)
The configuration file defines:
- **`upstream`**: Source repositories and charts to aggregate
  - **`versionPolicy`**: Global version filtering applied to all charts across all repositories
  - **`repositories`**: List of upstream repositories (HTTP or OCI)
    - **`url`**: Repository URL (`https://`, `http://`, or `oci://`)
    - **`charts`**: Dictionary of chart names to include (chart name as key)
    - **`versionPolicy`**: Optional version filtering for all charts in this repository (overrides global policy)
      - **`exclude.prerelease`**: Exclude prerelease versions (e.g., `1.0.0-alpha`)
      - **`exclude.invalidSemVer`**: Exclude versions that don't follow semantic versioning
      - **`exclude.versions`**: List of version patterns to exclude
    - **Per-chart policy**: Each chart can have its own `versionPolicy` that overrides repository and global policies
- **`manualRepository`**: Manually-curated charts to include
  - **`entries`**: Dictionary of chart names to their versions (Helm repository index format)
    - Each entry contains chart metadata: name, version, description, URLs, etc.

### Version Policy Hierarchy
Policies are applied in this order (most specific wins):
1. **Global policy** (`upstream.versionPolicy`) - Applied to all charts
2. **Repository policy** (`upstream.repositories[].versionPolicy`) - Applies to all charts in a repository
3. **Chart policy** (`upstream.repositories[].charts[chartName].versionPolicy`) - Applies to a specific chart

### Version Patterns
Version patterns support three formats:
- **Specific version**: `1.2.3`
- **SemVer range**: `<2.0.0`, `^1.2.3`, `1.0.0 - 2.3.4`, `~1.2.8`
- **Regular expression**: `^(?!deprecated-).*$` (for complex matching or non-SemVer versions)

### Example Configuration
```yaml
# yaml-language-server: $schema=https://github.com/justingaffney/helm-vrepo/releases/latest/download/config-schema.json
apiVersion: v1alpha1

upstream:
  versionPolicy:
    exclude:
      invalidSemVer: true
      prerelease: true

  repositories:
    - url: https://charts.example.com
      charts:
        my-chart: {}
        another-chart: {}

    - url: oci://registry.example.com/charts
      versionPolicy:
        exclude:
          prerelease: false
      charts:
        oci-chart:
          versionPolicy:
            exclude:
              versions:
                - "<1.5.0"
                - "^(?!stable-).*$"

manualRepository:
  entries:
    custom-chart:
      - name: custom-chart
        version: "1.0.0"
        appVersion: "v1.0.0"
        description: "A custom chart"
        urls:
          - "https://example.com/charts/custom-chart-1.0.0.tar.gz"
```

## Commands & common flags
Common flags used by commands:
- `--config`, `-c` : Path to the configuration YAML file. Required for commands that read a config
- `--output`, `-o` : Path to write generated output to. Use `-` to write to stdout
- `--overwrite` : Allow overwriting an existing output file

Common commands:
- `helm vrepo config initialize --output <output-file>`  (alias: `helm vrepo init`)
  - Initialize a new virtual repository configuration file with example content.
  - Example: `helm vrepo init -o vrepo.yaml`

- `helm vrepo index generate --config <config-file> --output <output-file>`  (alias: `helm vrepo gen`)
  - Generate a Helm repository `index.yaml` from the configuration.
  - Example: `helm vrepo gen -c vrepo.yaml -o index.yaml`

- `helm vrepo config validate --config <config-file>`
  - Validate your configuration file for correctness and schema compliance.

- `helm vrepo config schema --output <output-file>`
  - Emit the JSON Schema for configuration. Use `--api-version` to restrict to a specific API version.
  - Example: `helm vrepo config schema -o schema.json -v v1alpha1`

- `helm vrepo config schema list`
  - List supported configuration API versions.

## Limitations

### Authenticated upstream repositories are not supported
`helm-vrepo` currently only supports unauthenticated upstream repositories. Generating a virtual repository index will fail if any configured upstream repositories require authentication.

### Support for "mixed-mode" repository indexes is limited
While the Helm CLI can handle entries in the `index.yaml` file using `oci://` chart URLs, instead of the traditional `https://` URLs for `.tgz` files, this behavior is not officially "supported" or defined in Helm's `index.yaml` schema. However some popular chart repositories rely heavily on this behavior (for example [Bitnami](https://charts.bitnami.com/bitnami/index.yaml)), so in order to not break backward compatibility "mixed-mode" repository indexes will likely continue to work until at least the next major release. Other tools with Helm integration can also handle "mixed-mode" repository indexes, such as [Argo CD](https://argo-cd.readthedocs.io/en/latest/user-guide/helm/).

#### "Mixed-mode" repository indexes are explicitly not supported in Flux
One notable exception is Flux which does not handle "mixed-mode" repository indexes, and according to comments on [this GitHub issue](https://github.com/fluxcd/flux2/issues/5100) it appears it will likely never be able to handle them. `helm-vrepo` virtual repositories do still work in Flux, they just cannot include OCI charts (i.e. must be "single-mode").

However Flux has [deep integration with Helm](https://fluxcd.io/flux/use-cases/helm) that makes this plugin mostly redundant anyway.

### OCI repositories are now the default Helm chart distribution method
From Helm 4 onwards, OCI registries are documented as the default method for distributing Helm charts, instead of traditional HTTP repositories, and the ecosystem is being strongly encouraged to migrate from HTTP repositories to OCI repositories. There is also speculation that HTTP repository support will be removed entirely in the next major version (Helm 5).

So while the [use cases](#use-cases) for virtual repositories are still valid, and cannot be addressed using OCI repositories alone, the mechanism used by `helm-vrepo` to implement virtual repositories (HTTP repository indexes) may not be supported by Helm in a future major release.

## Copyright and License
Copyright © 2025-2026 Justin Gaffney

Licensed under the [Apache License, Version 2.0](LICENSE)
