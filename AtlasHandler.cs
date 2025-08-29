using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpriteStitcher;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public static class AtlasHandler
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // New Code with CommandLineParser
    public static void StitchAtlas(string inputDir, string outputDir, int padding, int maxWidth, string atlasName,
        string customMetaPointer, out bool didJob)
    {
        if (!Directory.Exists(inputDir))
        {
            Console.WriteLine($"Input directory '{inputDir}' does not exist.");
            didJob = false;
            return;
        }

        var imagePaths = Directory.GetFiles(inputDir, "*.png");
        if (imagePaths.Length == 0)
        {
            Console.WriteLine("No PNG images found in the specified directory.");
            didJob = false;
            return;
        }

        if (!atlasName.EndsWith(".png"))
        {
            atlasName += ".png";
            Console.WriteLine("Atlas name must end with .png. Automatically appending .png to the name.");
        }

        var atlasPath = Path.Combine(outputDir, atlasName);

        var atlasNameInMeta = string.IsNullOrWhiteSpace(customMetaPointer)
            ? atlasPath
            : customMetaPointer;

        Console.WriteLine(
            $"Stitching images from {inputDir} into an atlas with padding {padding} and max width {maxWidth}...");

        var images = new List<(string Name, Bitmap Image)>();

        foreach (var path in imagePaths)
        {
            try
            {
                var bmp = new Bitmap(path);
                var name = Path.GetFileName(path);
                images.Add((name, bmp));
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error loading image {path}: {e.Message}");
                throw;
            }
        }

        var spriteMap = AtlasPacker.PackSprites(images, padding, maxWidth,
            out var atlasWidth, out var atlasHeight);

        SavePng(outputDir, atlasWidth, atlasHeight, images, spriteMap, atlasPath);

        SaveJson(outputDir, atlasPath, atlasNameInMeta, spriteMap);

        didJob = true;
    }

    private static void SavePng(string outputDir, int atlasWidth, int atlasHeight,
        List<(string Name, Bitmap Image)> images, Dictionary<string, SpriteRect> spriteMap,
        string atlasPath)
    {
        using var atlas = new Bitmap(atlasWidth, atlasHeight, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(atlas);

        g.Clear(Color.Transparent);

        foreach (var (name, image) in images)
        {
            var rect = spriteMap[name];
            g.DrawImage(image, rect.X, rect.Y, rect.Width, rect.Height);
        }

        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        atlas.Save(atlasPath);
        Console.WriteLine($"[✓] Saved atlas to: {atlasPath}");
    }

    private static void SaveJson(string outputDir, string atlasPath, string atlasNameInMeta,
        Dictionary<string, SpriteRect> spriteMap)
    {
        var jsonPath = Path.Combine(outputDir, atlasPath.Replace(".png", ".json"));
        var metadata = new
        {
            image = atlasNameInMeta,
            sprites = spriteMap
        };

        var json = JsonSerializer.Serialize(metadata, JsonSerializerOptions);
        File.WriteAllText(jsonPath, json);
        Console.WriteLine($"[✓] Saved metadata to: {jsonPath}");
    }

    // Legacy Code
    public static void StitchAtlas(string inputDirectory, string outputDirectory, int padding, out bool didJob,
        string atlasName)
    {
        if (!atlasName.EndsWith(".png"))
        {
            atlasName += ".png";
            Console.WriteLine("Atlas name must end with .png. Automatically appending .png to the name.");
        }

        Console.WriteLine($"Stitching images from {inputDirectory} into an atlas with padding {padding}...");

        if (Directory.GetFiles(inputDirectory).Length == 0)
        {
            Console.WriteLine(
                "No images found in the specified directory. Please ensure the directory contains PNG images.");
            didJob = false;
            return;
        }

        var imagePaths = Directory.GetFiles(inputDirectory, "*.png");
        var images = new List<(string Name, Bitmap Image)>();

        foreach (var path in imagePaths)
        {
            try
            {
                var bmp = new Bitmap(path);
                var name = Path.GetFileName(path);
                images.Add((name, bmp));
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error loading image {path}: {e.Message}");
                throw;
            }
        }

        const int maxAtlasWidth = 4096;
        var spriteMap =
            AtlasPacker.PackSprites(images, padding, maxAtlasWidth, out var atlasWidth, out var atlasHeight);

        using var atlas = new Bitmap(atlasWidth, atlasHeight, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(atlas);
        g.Clear(Color.Transparent);

        foreach (var (name, image) in images)
        {
            var rect = spriteMap[name];
            g.DrawImage(image, rect.X, rect.Y, rect.Width, rect.Height);
        }

        if (!Directory.Exists(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        var atlasPath = Path.Combine(outputDirectory, atlasName);
        atlas.Save(atlasPath);
        Console.WriteLine($"[✓] Saved atlas to: {atlasPath}");

        var jsonPath = Path.Combine(outputDirectory, atlasName.Replace(".png", ".json"));
        var metadata = new AtlasMetadata
        {
            Image = atlasPath,
            sprites = spriteMap
        };

        var json = JsonSerializer.Serialize(metadata, JsonSerializerOptions);

        File.WriteAllText(jsonPath, json);
        Console.WriteLine($"[✓] Saved metadata to: {jsonPath}");
        didJob = true;
    }

    public static void UnstitchAtlas(string atlasLocation, out bool didJob)
    {
        if (!File.Exists(atlasLocation))
        {
            Console.WriteLine($"Atlas file '{atlasLocation}' does not exist.");
            didJob = false;
            return;
        }

        var jsonLocation = atlasLocation.Replace(".png", ".json");
        if (!File.Exists(jsonLocation))
        {
            Console.WriteLine($"Metadata file '{jsonLocation}' does not exist.");
            didJob = false;
            return;
        }

        Console.WriteLine($"Unstitching atlas {atlasLocation} into individual images...");

        var jsonText = File.ReadAllText(jsonLocation);
        var metadata = JsonSerializer.Deserialize<AtlasMetadata>(jsonText);

        if (metadata?.sprites is null)
        {
            Console.WriteLine($"Failed to parse atlas metadata. Please ensure {jsonLocation} is valid.");
            didJob = false;
            return;
        }

        if (metadata.sprites.Count == 0)
        {
            Console.WriteLine($"Metadata contains 0 sprites. Aborting.\nPath: {jsonLocation}\nContents:\n{jsonText}");
            didJob = false;
            return;
        }

        using var atlas = new Bitmap(atlasLocation);
        var extractedDirectory = Path.Combine(Path.GetDirectoryName(atlasLocation) ?? ".",
            $"unstitched-{Path.GetFileNameWithoutExtension(atlasLocation)}");

        Directory.CreateDirectory(extractedDirectory);

        Console.WriteLine($"Found {metadata.sprites.Count} sprites to extract...");

        foreach (var (name, rectData) in metadata.sprites)
        {
            var rect = rectData.ToRectangle();
            using var sprite = atlas.Clone(rect, atlas.PixelFormat);
            var outputPath = Path.Combine(extractedDirectory, name);
            try
            {
                sprite.Save(outputPath, ImageFormat.Png);
                Console.WriteLine($"[✓] Extracted {name} -> {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[✗] Failed to extract {name}: {ex.Message}");
            }
        }

        Console.WriteLine($"Finished unstitching. Sprites saved to: {extractedDirectory}");

        if (Program.QueryYesNo("Do you want to delete the original atlas and metadata files? (y/n)"))
        {
            try
            {
                File.Delete(atlasLocation);
                File.Delete(jsonLocation);
                Console.WriteLine("Deleted original atlas and metadata files.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete original files: {ex.Message}");
            }
        }

        didJob = true;
    }


    private class AtlasMetadata
    {
        public string Image { get; init; } = "";

        // ReSharper disable once InconsistentNaming
        public Dictionary<string, SpriteRect> sprites { get; init; } = new();
    }

    public class SpriteRect
    {
        [JsonPropertyName("x")] public int X { get; init; }

        [JsonPropertyName("y")] public int Y { get; init; }

        [JsonPropertyName("width")] public int Width { get; init; }

        [JsonPropertyName("height")] public int Height { get; init; }

        public Rectangle ToRectangle() => new(X, Y, Width, Height);
    }
}