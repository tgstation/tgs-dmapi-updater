# TGS DMAPI Updater Action

This action creates pull requests to the source repository to update the TGS DMAPI to the latest version.

## Usage

```yml
jobs:
  update-dmapi:
    runs-on: ubuntu-latest
    name: Updates the TGS DMAPI
    steps:
    - uses: tgstation/tgs-dmapi-updater@v1
      with:
        header-path: 'code/__DEFINES/tgs.dm'
        library-path: 'code/modules/tgs'
```

## Inputs

### `header-path`

**Required** The path to the `tgs.dm` file.

### `library-path`

**Required** The path to the `tgs` library directory.

### `target-branch`

The branch to update. Defaults to `master`.

### `github-token`

The GitHub token to use to push the changes and create the pull request. Defaults to the GITHUB_TOKEN secret.

## Outputs

### `release-notes`

The raw markdown of the DMAPI release notes.
