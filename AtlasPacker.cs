using System.Diagnostics.CodeAnalysis;
using System.Drawing;

namespace SpriteStitcher;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public static class AtlasPacker
{
    public static Dictionary<string, AtlasHandler.SpriteRect> PackSprites(
        List<(string Name, Bitmap Image)> images, int padding, int maxAtlasWidth,
        out int atlasWidth, out int atlasHeight)
    {
        var positions = new Dictionary<string, AtlasHandler.SpriteRect>();
        var skyline = new List<(int X, int Y)> { (0, 0) };
        atlasWidth = 0;
        atlasHeight = 0;

        foreach (var (name, image) in images)
        {
            var wWithPad = image.Width + padding;
            var hWithPad = image.Height + padding;

            if (wWithPad > maxAtlasWidth)
                throw new Exception(
                    $"Sprite {name} (width + padding {wWithPad}) exceeds max atlas width {maxAtlasWidth}.");

            int bestX = -1, bestY = int.MaxValue;

            for (var i = 0; i < skyline.Count; i++)
            {
                var (startX, startY) = skyline[i];

                if (startX + wWithPad > maxAtlasWidth)
                    continue;

                var maxY = startY;
                var j = i + 1;
                var endX = startX + wWithPad;
                while (j < skyline.Count && skyline[j].X < endX)
                {
                    maxY = Math.Max(maxY, skyline[j].Y);
                    j++;
                }

                if (maxY + hWithPad >= bestY) continue;
                bestX = startX;
                bestY = maxY;
            }

            // Start a new row if no fit found
            if (bestX == -1)
            {
                bestX = 0;
                bestY = atlasHeight; // place at the current bottom
                skyline.Clear();
                skyline.Add((0, bestY));
            }

            positions[name] = new AtlasHandler.SpriteRect
            {
                X = bestX + padding / 2,
                Y = bestY + padding / 2,
                Width = image.Width,
                Height = image.Height
            };

            var insertX = bestX;
            var insertY = bestY + image.Height + padding;
            var insertEnd = bestX + image.Width + padding;

            skyline.Add((insertX, insertY));
            skyline = skyline
                .Where(s => s.X < insertX || s.X > insertEnd)
                .Append((insertEnd, bestY))
                .OrderBy(s => s.Item1)
                .ToList();

            atlasWidth = Math.Max(atlasWidth, bestX + image.Width + padding);
            atlasHeight = Math.Max(atlasHeight, bestY + image.Height + padding);
        }

        return positions;
    }
}