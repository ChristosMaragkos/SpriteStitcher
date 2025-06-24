# SpriteStitcher

**SpriteStitcher** is a lightweight and simple command-line tool that stitches multiple PNG images into a single sprite atlas, or unpacks a sprite atlas back into its individual images.

It's written in C# (.NET 8), portable, and built with game developers and pixel artists in mind.

---

## Features

- Stitch PNG images into an optimized atlas using skyline-packing.
- Unstitch existing atlases back into individual images.
- JSON metadata for use in engines or your own tools.

---

## Usage

### Stitch

```sh
SpriteStitcher.exe <folder> --stitch <name> [--padding <n - default 2>]
```
**Stitch** takes every `.png` file within the specified directory (but not subdirectories!) 
and combines them all into a single `.png` image, with the specified padding of n pixels between each image.

### Example:
```sh
SpriteStitcher.exe C:\MySprites --stitch "my_atlas.png" --padding 4
```
This command will:
- Create a subdirectory called `stitched` within `C:\MySprites` to store the atlas
- Stitch all `.png` files together into an atlas called `my_atlas.png`, with a padding of 4 pixels between each sprite.
- Generate the relevant metadata within `my_atlas.json`.

### Unstitch
```sh
SpriteStitcher.exe <folder> --unstitch <atlas.png>
```
**Unstitch** takes in a PNG of an atlas and, using its metadata, separates it into its original images.

### Example:
```sh
SpriteStitcher.exe D:\MySprites\stitched --unstitch my_atlas.png
```
This command will:
- Create the subdirectory `unstitched`.
- Parse `my_atlas.json` to process each image within the atlas
- Extract all images to `unstitched`
- Ask to delete the atlas and metadata

## JSON Format
Each atlas is accompanied by a .json file in the following format:
```json
{
  "image": "atlas.png",
  "sprites": {
    "sprite1.png": { "x": 0, "y": 0, "width": 32, "height": 32 },
    ...
  }
}
```
## Build
You do not need to build SpriteStitcher directly from the source. Just go to [Releases](https://github.com/WhiteTowerGames/SpriteStitcher/releases) and download the latest version!

## About
I made SpriteStitcher as part of my effort to familiarize myself with C#. That being said, it also fills a large gap within my workflow and I'd love to see it flourish. So, **Pull Requests, forks, and anything in between is welcome!**
If you'd like SpriteStitcher to have a certain feature please do not hesitate to open up an issue or a PR!

***Credits: White Tower Games / Christos Maragkos: Programming and Testing***

## Notes
- (Currently) assumes non-rotated, top-left anchored sprites.
- Atlas max width is set to 4096.
- Only `.png` images are supported.
