# The public site

MediaPilot has a public page that presents the product and offers the installer:

    https://ckrattoz.github.io/MediaPilot/

It is a static page with no build step: the `site/` folder of this repository
**is** the site. GitHub Pages publishes it from the `main` branch, from the
`/site` folder.

## How it is published

| Setting | Value |
| --- | --- |
| Source | GitHub Actions (`build_type: workflow`) |
| Workflow | `.github/workflows/pages.yml` |
| Content | The `site` folder of the `main` branch |
| Address | `https://ckrattoz.github.io/MediaPilot/` |
| Environment | `github-pages` |

The workflow uploads the `site` folder as the artifact of the deployment and
publishes it. Every push to `main` that touches the folder republishes the
page, and the workflow can also be run by hand.

There is nothing to compile: the HTML carries its own stylesheet and the images
live in `site/assets/`.

The REST API of GitHub only accepts the root of the repository or `/docs` as
the source of a site published from a branch, and the site of this repository
lives in its own folder, so the publication goes through the workflow. That
also keeps the rule of one long lived branch: there is no `gh-pages` branch, and
the page and the product never drift apart.

The page follows the identity of the product: the palette and the mark come
from `src/MediaPilot.UI/Resources/Brand`. A change of the brand has to be
followed by a change of the page.

## The themes of the page

The page has a light theme and a dark theme, the same way the application
offers both, and the visitor chooses between three options: follow the theme of
the system, light and dark.

| Piece | Where |
| --- | --- |
| The palette | The variables of `:root` and of `html[data-theme="dark"]` |
| The switch | The `theme-switch` button of the navigation and its menu |
| The choice | `localStorage`, under the key `mediapilot-theme`, with `auto`, `light` or `dark` |
| The captures | `assets/*.png` and `assets/*-light.png` |

Every color of the page comes from a variable, so the two themes never drift
apart. A color that is written down in a rule does not belong there.

Two attributes of the `html` element carry the state of the theme:

| Attribute | Meaning |
| --- | --- |
| `data-theme` | The theme that is on screen: `light` or `dark` |
| `data-theme-choice` | The choice of the visitor: `auto`, `light` or `dark` |

The palette only knows two themes, so `auto` is resolved before the first paint:
the script of the `head` reads the choice, asks the system with
`matchMedia('(prefers-color-scheme: dark)')` and writes `data-theme` with the
answer. That is why the palette never needs a third copy of the variables, and
why the page never shows the wrong theme while it loads.

`auto` is the default one, and the page keeps following the theme of the system
while it is open: the listener of the media query applies the change when the
system switches, but only when the visitor never picked a theme by hand.

The captures of the product are real shots of the application, and the
application takes them in both themes. The switch swaps the `src` of the images
with the one of the theme that is on screen, so a capture of the light theme
never appears in the dark page, or the other way around. A new capture has to
come in the two themes.

## The colors of the page

The page has two colors of the brand and they do not have the same job:

| Color | Job |
| --- | --- |
| The orange of the mark | The actions: the download, the code, the shortcuts |
| The lime of the mark | The claims that can be checked |

The lime is used with restraint: the dot of the version, the tick of the
privileges that are not needed, the icons of the two promises and the digest of
the installer. It never replaces the orange, and it never spreads over a whole
section. A page with the lime everywhere stops being a page of the product.

Both shades come from the palette of the application: the lime of the mark is
`MediaPilot.Brush.BrandLime`, and the dark one is the shade of the dark theme
while the light one is the shade of the light theme.

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
