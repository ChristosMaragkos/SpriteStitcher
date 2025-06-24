using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpriteStitcher;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public static class AtlasHandler
{
    
    public static void StitchAtlas(string inputDirectory, string outputDirectory, int padding, out bool didJob, string atlasName)
{
    if (!atlasName.EndsWith(".png"))
    {
        Console.WriteLine("Atlas name must end with .png");
        didJob = false;
        return;
    }

    Console.WriteLine($"Stitching images from {inputDirectory} into an atlas with padding {padding}...");

    if (Directory.GetFiles(inputDirectory).Length == 0)
    {
        Console.WriteLine("No images found in the specified directory. Please ensure the directory contains PNG images.");
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
    var spriteMap = AtlasPacker.PackSprites(images, padding, maxAtlasWidth, out var atlasWidth, out var atlasHeight);

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
    var metadata = new
    {
        image = atlasPath,
        sprites = spriteMap
    };

    var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });

    File.WriteAllText(jsonPath, json);
    Console.WriteLine($"[✓] Saved metadata to: {jsonPath}");
    didJob = true;
}

    public static void UnstitchAtlas(string inputDirectory, string atlasName, out bool didJob)
    {
        
        if (!atlasName.EndsWith(".png"))
        {
            Console.WriteLine("Atlas name must end with .png");
            didJob = false;
            return;
        }
        
        Console.WriteLine($"Unstitching atlas {atlasName} from {inputDirectory} into individual images...");
        
        var jsonPath = Path.Combine(inputDirectory, atlasName.Replace(".png", ".json"));

        if (!File.Exists(jsonPath))
        {
            Console.WriteLine($"Atlas {atlasName} metadata file not found. Please ensure {atlasName.Replace(".png", ".json")} exists in the output directory.");
            didJob = false;
            return;
        }
        
        var jsonText = File.ReadAllText(jsonPath);
        var metadata = JsonSerializer.Deserialize<AtlasMetadata>(jsonText);

        if (metadata?.sprites is null)
        {
            Console.WriteLine($"Failed to parse atlas metadata. Please ensure {atlasName.Replace(".png", ".json")} is valid.");
            didJob = false;
            return;
        }

        if (metadata.sprites.Count == 0)
        {
            Console.WriteLine($"Found {metadata.sprites.Count} sprites in JSON. Is the JSON file valid?");
            didJob = false;
            return;
        }
        
        var atlasImagePath = Path.Combine(inputDirectory, atlasName);

        if (!File.Exists(atlasImagePath))
        {
            Console.WriteLine($"Atlas image {atlasName} not found. Please ensure it exists in the output directory.");
            didJob = false;
            return;
        }
        
        using var atlas = new Bitmap(atlasImagePath);
        
        var extractedDirectory = Path.Combine(inputDirectory, "unstitched");
        Directory.CreateDirectory(extractedDirectory);

        foreach (var (name, rectData) in metadata.sprites)
        {
            var rect = rectData.ToRectangle();
            using var sprite = atlas.Clone(rect, atlas.PixelFormat);
            var outputPath = Path.Combine(extractedDirectory, name);
            try {
                sprite.Save(outputPath, ImageFormat.Png);
                Console.WriteLine($"[✓] Extracted {name} to {outputPath}");
            } catch (Exception ex) {
                Console.WriteLine($"[✗] Failed to extract {name}: {ex.Message}");
            }
        }
        var answered = false;
        
        var shouldDelete = false;

        while (!answered)
        {
            Console.WriteLine(
                $"[✓] Unstitching complete. Sprites saved to: {extractedDirectory}. Delete the original atlas and metadata? (y/n)");
            var response = Console.ReadKey();
            Console.WriteLine(); // New line after key press
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (response.Key)
            {
                case ConsoleKey.Y:
                    answered = true;
                    shouldDelete = true;
                    break;
                case ConsoleKey.N:
                    Console.WriteLine($"[✓] Kept original atlas and metadata files.");
                    answered = true;
                    break;
                default:
                    Console.WriteLine("Invalid input. Delete the original atlas and metadata? (y/n).");
                    break;
            }
        }
        
        if (shouldDelete)
        {
            try
            {
                atlas.Dispose();
                File.Delete(atlasImagePath);
                File.Delete(jsonPath);
                Console.WriteLine($"[✓] Deleted original atlas and metadata files.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[✗] Failed to delete original files: {ex.Message}");
            }
        }

        didJob = true;
        
    }

    
    public class AtlasMetadata
    {
        public string Image { get; init; } = "";
        // ReSharper disable once InconsistentNaming
        public Dictionary<string, SpriteRect> sprites { get; init; } = new();
    }

    public class SpriteRect
    {
        [JsonPropertyName("x")]
        public int X { get; init; }
        
        [JsonPropertyName("y")]
        public int Y { get; init; }
        
        [JsonPropertyName("width")]
        public int Width { get; init; }
        
        [JsonPropertyName("height")]
        public int Height { get; init; }

        public Rectangle ToRectangle() => new(X, Y, Width, Height);
    }
}