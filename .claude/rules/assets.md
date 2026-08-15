---
paths:
  - "*.png"
  - "*.ico"
  - "*.svg"
  - "**/*.png"
  - "**/*.jpg"
  - "**/*.jpeg"
  - "**/*.svg"
  - "**/*.ico"
---

# Images and packaged assets

**An image the maintainer supplies ships byte for byte.** Not resized, not recoloured, not
cropped, not composited with anything, and not redrawn from what you can see of it.

If it does not work — over the format's size limit, wrong format, unreadable at the 128 px a
nuget.org listing renders, wrong aspect — **say so and stop**. Changing it is the
maintainer's call, not yours.

Check the file rather than describing it: format, dimensions, weight, transparency, and how
much of the canvas it fills. `file`, `identify` and reading the header all beat looking at
it.

This is written down because it was got wrong: three variants were composited out of one
supplied mark, and every one was worse than the file it started from.

The repository ships one such asset today: `icon.png`, the package icon.
