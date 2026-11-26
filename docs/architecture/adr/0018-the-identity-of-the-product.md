# ADR 0018 - The identity of the product is one vector master, kept in code

- **Status:** Accepted
- **Date:** 2026-11-25
- **Decides:** What the mark of MediaPilot is, where it lives and how it reaches
  every surface of the product.
- **Extends:** [ADR 0009](0009-msi-installer-wix.md), which decided how the MSI
  is built, and [ADR 0002](0002-explicit-theme-resolution.md), which decided
  how the palette of the interface is resolved.

## Context

MediaPilot had no identity of its own. Every surface that a user can see
belonged to somebody else:

- The **executable carried the default icon of the SDK**, and the icon in the
  notification area was a placeholder drawn for the first version of the
  project.
- The **accent of the interface was the blue of the Fluent theme**, a color
  that has nothing to do with the product.
- The **installer showed the artwork of the WixUI extension**, a wizard that
  every Windows program has, with the images of another product.
- The **about window was a list of text** with no mark at all.

The original AutoHotkey project had the same problem and solved it with a
text logo, drawn in the script. A text logo is the wrong answer for a
native application: it does not scale, it does not have a shape of its own
at 16 pixels, and it cannot be the icon of an executable.

## Considered alternatives

| Option | Pros | Cons |
| --- | --- | --- |
| **Keep a design file (Figma, Illustrator) as the master** | The designer works with the tools of the trade; the file is the source of truth | A binary file in the repository that no code can verify; the product would need an asset pipeline to turn it into icons; an external dependency that the build cannot check |
| Keep the master as SVG and let the build rasterize it | One file, readable in any text editor | WPF does not read SVG; a rasterizer is a dependency of the build, and the result is not reproducible without pinning its version |
| Generate the icons at build time, every time | The files are never stale | The ordinary build would depend on the tool and on WPF itself to produce the icons of the executable; a broken generator would break the build of the product |
| Edit the PNG by hand, one size at a time | Nothing to build | The sizes drift apart: the icon of the tray at 16 pixels stops looking like the icon of the executable at 256 |
| **Keep the master as code, and version the generated files** | The master is reviewed like the rest of the code; the generated files are reproducible and the build does not depend on the tool | The generated files have to be committed, and the master has to be regenerated when it changes |

## Decision

**The master of the mark is code, and the files it produces are versioned.**

### The mark

The mark is a **rounded tile in the style of Windows 11 with a wave of three
bars**, and the central bar is a **triangle that points to the right**. The
triangle keeps the association with the playback without repeating the
loose generic triangle that the original project used.

- The tile is a rounded square, 256 units of side, with a radius of 0.225
  of its side.
- The wave is two rounded bars and the triangle. The palette is the
  **graphite tile with the orange bars and the lime triangle**, and it was
  chosen with a contact sheet that showed the four candidate schemes at the
  real sizes, on the real backgrounds.

**There are two shapes, not one.** A single geometry does not survive 16
pixels: the gaps between the bars fall below a pixel and the tip of the
triangle fades away. The `Regular` shape is used from 32 pixels up and the
`Bold` shape, with thicker bars, wider gaps and a truncated tip, from 24
pixels down.

### Where the master lives

- The master is the project `tools/MediaPilot.Brand`, and it uses the vector
  API of WPF (`RectangleGeometry`, `RenderTargetBitmap`). It is **part of
  the solution**, so it does not rot, and it is **not a dependency of the
  product**: the application does not reference it and it does not ship.
- The tool writes the icons of the notification area, the icon of the
  executable, the resources that the windows show and the images of the
  installer.
- The **generated files are versioned**. The ordinary build of MediaPilot
  never runs the tool: it consumes the files, like any other asset.

### The palette

- The **orange of the mark is the accent of the application**. On a dark
  surface it is the same orange, with dark text on top of it. On a light
  surface it is a darker shade of the same hue (`#C25500`), because the
  pure orange does not reach the contrast that the text of a button needs.
- The **lime of the mark is the color of the level of the volume** and of
  the secondary accents. It is darkened on the light surface for the same
  reason, and it is never the accent of a control.
- The **theme of the Fluent library is not touched**: only the palette of
  MediaPilot changes, so the controls keep the look of Windows 11.

### The states of the icon

The icon of the notification area keeps the mechanism of the original
project, which is the only one that works: **two files, one for the active
state and one for the dimmed state**, and the service swaps between them.
The active one is the mark in color, and the inactive one is the same mark
with every tone grayed.

### The images of the installer

- The control `Bitmap` of Windows Installer **does not accept PNG**: the
  images of the wizard are BMP.
- The size is the one of the control of the extension: **370x234** for the
  image of the welcome dialog and **370x44** for the band of the dialogs.
  The extension draws them at 493x312 and 493x58 and lets the control scale
  them; drawing them at the size of the control avoids the scaling.
- The control **does not blend the image**, so the alpha channel of the
  files is forced to opaque.
- The extension lets every image of the wizard be replaced: the value of
  each one is the name of the binary of the package that holds it. The two
  binaries of MediaPilot take the place of the ones of the extension.

### The icon of the windows

The windows show the **32 pixel PNG** and not the ICO. WPF builds the icon
of the window from the **first frame** of the ICO, and the first frame is
the smallest one, so the icon of the title bar would be a 16 pixel image
stretched to the size of the taskbar. The system does not have that
problem: it picks the frame that fits the size it needs, which is why the
ICO is the right format for the executable and for the notification area.

## Consequences

- **Positive:** the mark of the application is the same in every surface,
  and it was checked at the real sizes before it was adopted.
- **Positive:** the master is reviewed like the rest of the code, and the
  result is reproducible: running the tool again produces the same files.
- **Positive:** the product does not depend on anything that is not the
  .NET runtime, and neither does its build.
- **Positive:** the images of the installer are the ones of the product and
  not the ones of the extension.
- **Negative:** the generated files are committed, so a change of the master
  is a change in two places: the code of the tool and the files it wrote.
  The review of the diff shows the files that changed, which is the price
  of not having an asset pipeline.
- **Negative:** the shape of the mark is defined in numbers, and not in a
  design tool. It is precise, but it is not a drawing.
- **Accepted:** there is no word in the mark. The name of the product is
  written next to it, in the interface and in the installer, and a mark
  that carries a word is not a mark.

## Measurements

- The ICO of the notification area holds seven frames (16, 20, 24, 32, 48,
  64 and 256). The 256 is a PNG, and the rest are DIB of 32 bits per pixel.
- The generated icons were compared **byte by byte** against the PNG
  masters: the maximum difference is **1 per channel**, which is the
  rounding of the floating point of the rasterizer.
- The two defects of the writer of the ICO were found by that comparison:
  the **red and blue channels were written swapped** (the format of the ICO
  is BGRA) and the **origin and the destination were the same buffer**, so
  the rows were corrupted when the image was turned upside down. Both are
  covered by the comparison against the masters.
- The contrast of the palette was measured with the formulas of WCAG 2.1:
  the orange of the light surface reaches **4.34** against the surface and
  **4.57** against the text, and the orange of the dark surface reaches
  **6.92**. The first shade of the lime only reached **2.71** against the
  light surface, so it was darkened to `#6B8E0C`, which reaches **3.63**.
