using CommandLine;

namespace SpriteStitcher;

class Program
{

    [Verb("stitch", true, HelpText = "Stitch all PNG images in the specified directory into a sprite atlas.")]
    private class StitchOptions
    {
        [Value(0,
            MetaName = "input-directory",
            Required = true,
            HelpText = "Directory containing PNG images to stitch.")]
        public string InputDirectory { get; set; } = string.Empty;
        
        [Option('p',
            "padding",
            Required = false,
            HelpText = "Padding between images in the atlas, in pixels (default: 2).",
            Default = 2)]
        public int Padding { get; set; } = 2;
        
        [Option('m',
            "max-atlas-width",
            Required = false,
            HelpText = "Maximum width of the atlas, in pixels (default: 4096).",
            Default = 4096)]
        public int MaxAtlasWidth { get; set; } = 4096;
        
        [Option('n', "atlas-name", Required = false, HelpText = "Name for the output atlas file (default: atlas.png).")]
        public string AtlasName { get; set; } = "atlas.png";
        
        public string OutputDirectory => Path.Combine(InputDirectory, "stitched");

        [Option('c', "custom-metadata-pointer", 
            Default = null,
            Required = false, 
            HelpText = "Configure the path the metadata will point to. " + 
                       "Useful for game engines, like \"res://\" for Godot. " +
                       "If omitted, defaults to the absolute path of the atlas file.")]
        public string? CustomAtlasPathInMetadata { get; set; }
    }

    [Verb("unstitch",
        HelpText =
            "Read the atlas.png and atlas.json in the specified directory and unstitch them into individual images.")]
    private class UnstitchOptions
    {
        [Value(0, MetaName = "input-directory", Required = true, HelpText = "The absolute path to the atlas PNG file.")]
        public string InputDirectory { get; set; } = string.Empty;
    }
    
    private static void Main()
    {
        Console.WriteLine("SpriteStitcher - Create Sprite Atlases from a directory of images");
        Console.WriteLine("---------------------------------------------------");
        Console.WriteLine("This tool can stitch together images from a specified directory into a single sprite atlas.");
        Console.WriteLine("Then the relevant metadata is saved to a JSON file and, along with the atlas,");
        Console.WriteLine("is placed in a separate folder in your directory for easy access.");
        Console.WriteLine("Alternatively, you can also unstitch an atlas back into individual images.");

        ParseLoop();

        Console.WriteLine("Thank you for using SpriteStitcher! Press any key to exit.");
        Console.ReadKey();
    }

    private static void ParseLoop()
    {
        var didJob = false;


        while (!didJob)
        {
            Console.WriteLine("---------------------------------------------------");
            Console.WriteLine("For help, type 'help' or use the --help flag.");
            Console.WriteLine("---------------------------------------------------");
            var parserResult = Parser.Default.ParseArguments<StitchOptions, UnstitchOptions>
                (Console.ReadLine()!.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        
            parserResult
                .WithParsed<StitchOptions>(opts =>
                {
                    AtlasHandler.StitchAtlas(opts.InputDirectory,
                        opts.OutputDirectory,
                        opts.Padding,
                        opts.MaxAtlasWidth,
                        opts.AtlasName,
                        opts.CustomAtlasPathInMetadata!, // Suppressing null. It is handled in StitchAtlas.
                        out didJob
                    );
                })
                .WithParsed<UnstitchOptions>(opts =>
                {
                    AtlasHandler.UnstitchAtlas(opts.InputDirectory, out didJob);
                });

            if (!didJob && !QueryYesNo("Would you like to try again?"))
            {
                break;
            }
        }
    }

    public static bool QueryYesNo(string question)
    {
        while (true)
        {
            Console.WriteLine(question + " (y/n)");
            var response = Console.ReadKey();
            Console.WriteLine();
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (response.Key)
            {
                case ConsoleKey.Y:
                {
                    return true;
                }
                case ConsoleKey.N:
                {
                    return false;
                }
                default:
                {
                    Console.WriteLine("Please enter 'y' or 'n'.");
                    break;
                }
            }
        }
    }
}