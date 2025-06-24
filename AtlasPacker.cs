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
            int bestX = -1, bestY = int.MaxValue;
            
            for (var i = 0; i < skyline.Count; i++)
            {
                var (startX, startY) = skyline[i];
                var w = image.Width + padding;
                var h = image.Height + padding;

                if (startX + w > maxAtlasWidth)
                    continue;
                
                var maxY = startY;
                var j = i + 1;
                var endX = startX + w;
                while (j < skyline.Count && skyline[j].X < endX)
                {
                    maxY = Math.Max(maxY, skyline[j].Y);
                    j++;
                }

                if (maxY + h >= bestY) continue;
                bestX = startX;
                bestY = maxY;
            }

            if (bestX == -1)
                throw new Exception($"Could not pack sprite {name}, too wide for atlas.");

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