# The public site

MediaPilot has a public page that presents the product and offers the installer:

    https://ckrattoz.github.io/MediaPilot/

It is a static page with no build step: the `site/` folder of this repository
**is** the site. GitHub Pages publishes it from the `main` branch, from the
`/site` folder.

## How it is published

| Setting | Value |
| --- | --- |
| Source | Deploy from a branch |
| Branch | `main` |
| Folder | `/site` |
| Address | `https://ckrattoz.github.io/MediaPilot/` |

Every push to `main` republishes the page. There is nothing to compile: the
HTML carries its own stylesheet and the images live in `site/assets/`.

The page follows the identity of the product: the palette and the mark come
from `src/MediaPilot.UI/Resources/Brand`. A change of the brand has to be
followed by a change of the page.

The rule of one long lived branch applies here too: the site lives in `main`
and not in a `gh-pages` branch, so the page and the product never drift apart.

## The download contract

The button of the page points to an address that does not change between
releases:

    https://github.com/ckrattoz/MediaPilot/releases/latest/download/MediaPilot-x64.msi

Two rules keep it alive:

1. The asset of the latest release is named **`MediaPilot-x64.msi`**, without
   the version inside. The address of `releases/latest/download/` resolves the
   name inside the latest release, so a name that carries the version breaks
   with the next release.
2. The release that the page advertises is always the latest one. A release
   marked as a prerelease does not become `latest`, and a release published
   without the asset leaves the address without a file behind it.

## The hash of the page

The page publishes the SHA-256 of the installer so that anybody can check the
file before installing it. **The hash has to be updated with every release**:
the MSI is built again and its bytes change even when the version does not.

```powershell
Get-FileHash artifacts\packages\MediaPilot-x64.msi -Algorithm SHA256
```

The value belongs in two places of `site/index.html`: the text of the `code`
element and the attribute `data-copy` of the button, both inside the section
**Descarga verificada**.

## What a release has to carry

| Asset | Content |
| --- | --- |
| `MediaPilot-x64.msi` | The installer |
| `SHA256SUMS.txt` | The hash of the installer, in the format of `sha256sum` |

The notes of the release are the section of `CHANGELOG.md` of the version.

`gh` is not installed in the development environment, so the release is
published with the REST API of GitHub and the credential that Git Credential
Manager keeps for `github.com`:

```powershell
$headers = @{
  Authorization = "Bearer $token"
  Accept        = 'application/vnd.github+json'
  'X-GitHub-Api-Version' = '2022-11-28'
}

$release = Invoke-RestMethod -Method Post `
  -Uri 'https://api.github.com/repos/ckrattoz/MediaPilot/releases' `
  -Headers $headers `
  -Body (@{ tag_name = 'v0.6.1'; name = 'MediaPilot 0.6.1'; body = $notes;
            draft = $false; prerelease = $false } | ConvertTo-Json)

Invoke-WebRequest -Method Post `
  -Uri "https://uploads.github.com/repos/ckrattoz/MediaPilot/releases/$($release.id)/assets?name=MediaPilot-x64.msi" `
  -Headers $headers -InFile 'artifacts\packages\MediaPilot-0.6.1-x64.msi' `
  -ContentType 'application/octet-stream'
```

The token never reaches the repository and never appears in a log.

## The version on the page

The version of the product appears in three places of `site/index.html`: the
eyebrow of the cover, the line of the footer and the title of the section of
the hash. All three have to be moved with every release.

## The signature of the installer

The installer is not signed with a certificate of purchase, so SmartScreen
marks it in the first execution. The page says so, and explains how to go
ahead, instead of leaving the visitor in front of an unexplained warning.

Signing the installer is the pending step to remove that warning. When it
happens, the notice of the page has to go with it.
