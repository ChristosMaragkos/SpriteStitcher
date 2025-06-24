namespace SpriteStitcher;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("SpriteStitcher - Create Sprite Atlases from a directory of images");
        Console.WriteLine("---------------------------------------------------");
        Console.WriteLine("This tool can stitch together images from a specified directory into a single sprite atlas.");
        Console.WriteLine("Then the relevant metadata is saved to a JSON file and, along with the atlas,");
        Console.WriteLine("is placed in a separate folder in your directory for easy access.");
        Console.WriteLine("Alternatively, you can also unstitch an atlas back into individual images.");
        Console.WriteLine("---------------------------------------------------");
        Console.WriteLine("Usage: <your-sprite-folder-path> [--stitch | --unstitch] [--padding <amount (Default: 2)>]");
        Console.WriteLine("Options:");
        Console.WriteLine("  --stitch       : Stitch images into a sprite atlas");
        Console.WriteLine("  --unstitch     : Unstitch a sprite atlas into individual images");
        Console.WriteLine("  --padding <n>  : Set padding between images in the atlas (default: 2)");
        Console.WriteLine("---------------------------------------------------");
        
        var (inputDirectory, operation, padding) = ParseArguments(args);
        
        var outputDirectory = Path.Combine(inputDirectory, "SpriteStitcher");

        switch (operation)
        {
            case "--stitch":
                AtlasStitcher.StitchAtlas(inputDirectory, outputDirectory, padding);
                break;
            case "--unstitch":
                AtlasStitcher.UnstitchAtlas(inputDirectory, outputDirectory);
                break;
            default:
                Console.WriteLine("No valid operation specified. Please use --stitch or --unstitch.");
                break;
        }
    }

    static (string inputDirectory, string operation, int padding) ParseArguments(string[] args)
    {
        
        while (true)
        {
            string[] inputArgs;

            if (args.Length == 0)
            {
                Console.WriteLine("Please enter a command.");
                var inputLine = Console.ReadLine()?.Trim() ?? "";
                inputArgs = inputLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            }
            else
            {
                inputArgs = args;
            }

            if (inputArgs.Length == 0)
            {
                Console.WriteLine("No arguments provided. Please specify a directory and operation.");
                args = Array.Empty<string>();
                continue;
            }
            
            var inputDirectory = inputArgs[0].Trim('"');
            var operation = "";
            var padding = 2; // Default padding

            var invalidArgs = false;

            if (inputArgs[0].Equals("help", StringComparison.OrdinalIgnoreCase) ||
                inputArgs[0].Equals("--help", StringComparison.OrdinalIgnoreCase) ||
                inputArgs[0].Equals("-h", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("---------------------------------------------------");
                Console.WriteLine("Usage: <your-sprite-folder-path> [--stitch | --unstitch] [--padding <amount (Default: 2)>] [--recursive]");
                Console.WriteLine("Options:");
                Console.WriteLine("  --stitch       : Stitch images into a sprite atlas");
                Console.WriteLine("  --unstitch     : Unstitch a sprite atlas into individual images");
                Console.WriteLine("  --padding <n>  : Set padding between images in the atlas (default: 2)");
                Console.WriteLine("  --recursive    : Recursively search for images in subdirectories");
                Console.WriteLine("---------------------------------------------------");
                args = Array.Empty<string>();
                continue;
            }

            if (!Directory.Exists(inputDirectory))
            {
                Console.WriteLine("The specified directory does not exist: " + inputDirectory);
                args = Array.Empty<string>();
                continue;
            }

            if (!(inputArgs.Contains("--stitch") || inputArgs.Contains("--unstitch")) 
                                                 || (inputArgs.Contains("--stitch") && inputArgs.Contains("--unstitch")))
            {
                Console.WriteLine("You must specify either --stitch or --unstitch.");
                args = Array.Empty<string>();
                continue;
            }

            for (var i = 1; i < inputArgs.Length; i++)
            {
                switch (inputArgs[i].ToLower())
                {
                    case "--stitch":
                        operation = "--stitch";
                        break;
                    case "--unstitch":
                        operation = "--unstitch";
                        break;
                    case "--padding":
                        if (i + 1 < inputArgs.Length && int.TryParse(inputArgs[i + 1], out var parsedPadding))
                        {
                            padding = parsedPadding;
                            i++; // Skip the next argument since it's the value for padding
                        }
                        else
                        {
                            Console.WriteLine("Invalid padding value. Please provide a valid integer.");
                            invalidArgs = true;
                        }
                        break;
                    default:
                        Console.WriteLine("Invalid argument: " + inputArgs[i]);
                        invalidArgs = true;
                        break;
                }
            }

            if (invalidArgs)
            {
                Console.WriteLine("Use --help for usage.");
                args = Array.Empty<string>(); // reset for next loop
                continue;
            }
            
            // Extra validation per operation
            bool validInputFound;

            switch (operation)
            {
                case "stitch":
                {
                    var pngFiles = Directory.GetFiles(inputDirectory, "*.png", SearchOption.TopDirectoryOnly)
                        .Where(path => !path.Contains("SpriteStitcher")); // avoid previous output
                    validInputFound = pngFiles.Any();

                    if (!validInputFound)
                    {
                        Console.WriteLine("No .png files found to stitch in the specified folder.");
                        args = Array.Empty<string>();
                        continue;
                    }

                    break;
                }
                case "unstitch":
                {
                    var atlasPath = Path.Combine(inputDirectory, "atlas.png");
                    var jsonPath = Path.Combine(inputDirectory, "atlas.json");

                    validInputFound = File.Exists(atlasPath) && File.Exists(jsonPath);

                    if (validInputFound) return (inputDirectory, operation, padding);
                    Console.WriteLine("No sprite atlas found in the specified folder.");
                    args = Array.Empty<string>();
                    continue;
                }
            }


            return (inputDirectory, operation, padding);
        }
        
    }
}