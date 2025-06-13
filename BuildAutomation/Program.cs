using System.Text;
using CliWrap;
using Serilog;
using Utilities.Logging;

namespace BuildAutomation;

/// <summary>
/// Automates building exporter containers for all architectures and pushing them to Docker Hub
/// <br/> Requirements: .NET SDK installed and Docker Desktop running.
/// <br/>
/// <br/> If anyone else wants to use this and push your own images - change the image names below to point to your repository instead of mine (lukassoo) and set pushImages to true 
/// </summary>
internal static class Program
{
    static ILogger log = null!;

    static List<string> tagNames = ["armv7", "armv8", "latest"];
    static List<string> projectNames = ["Shelly3EmExporter", "ShellyPlugExporter", "ShellyPlus1PmExporter", "ShellyPlusPlugExporter", "ShellyPro3EmExporter", "ShellyProEmExporter", "ShellyPlusPmMiniExporter", "ShellyEmExporter"];
    
    // static List<string> tagNames = ["development"];
    // static List<string> projectNames = ["ShellyProEmExporter"];

    const bool pushImages = false;
    
    static Dictionary<string, string> imageNames = new()
    {
        {"Shelly3EmExporter", "lukassoo/shelly-3em-exporter"},
        {"ShellyPlugExporter", "lukassoo/shelly-plug-exporter"},
        {"ShellyPlus1PmExporter", "lukassoo/shelly-plus-1pm-exporter"},
        {"ShellyPlusPlugExporter", "lukassoo/shelly-plus-plug-exporter"},
        {"ShellyPro3EmExporter", "lukassoo/shelly-pro-3em-exporter"},
        {"ShellyProEmExporter", "lukassoo/shelly-pro-em-exporter"},
        {"ShellyPlusPmMiniExporter", "lukassoo/shelly-plus-pm-mini-exporter"},
        {"ShellyEmExporter", "lukassoo/shelly-em-exporter"}
    };
    
    static Dictionary<string, string> baseImagePostfixes = new()
    {
        {"armv7", "-arm32v7"},
        {"armv8", "-arm64v8"},
        {"latest", ""},
        {"development", ""}
    };
    
    static Dictionary<string, string> targetPlatforms = new()
    {
        {"armv7", "linux/arm/v7"},
        {"armv8", "linux/arm64"},
        {"latest", "linux/amd64"},
        {"development", "linux/amd64"}
    };
    
    static async Task Main(string[] args)
    {
        LogSystem.Init(false, "Information");
        log = Log.ForContext(typeof(Program));
        
        log.Information("---------- Starting ----------");

        DirectoryInfo? solutionDirectory = TryGetSolutionDirectoryInfo();

        if (solutionDirectory == null)
        {
            log.Error("Failed to get solution directory");
            goto end;
        }

        DirectoryInfo[] solutionDirectories = solutionDirectory.GetDirectories();

        foreach (DirectoryInfo projectDirectory in solutionDirectories)
        {
            if (!projectNames.Contains(projectDirectory.Name))
            {
                continue;
            }
         
            string projectName = projectDirectory.Name;
            
            if (!await PublishProject(projectDirectory))
            {
                log.Error("Failed to publish project: {projectName}", projectName);
                continue;
            }

            if (!await BuildImages(projectDirectory, pushImages))
            {
                log.Error("Failed to build project images: {projectName}", projectName);
            }
        }
        
        end:
        log.Information("---------- Ending ------------");
        await Log.CloseAndFlushAsync();
    }


    static async Task<bool> PublishProject(DirectoryInfo projectDirectory)
    {
        string projectName = projectDirectory.Name;
        
        Command command = Cli.Wrap("dotnet")
            .WithArguments(["publish", projectDirectory.FullName + "/" + projectDirectory.Name + ".csproj"]);

        try
        {
            CommandResult result = await command.ExecuteAsync();

            if (!result.IsSuccess)
            {
                log.Error("Publishing unsuccessful: {projectName}", projectName);
            }
            else
            {
                log.Information("Published: {projectName}", projectName);
            }
            
            return result.IsSuccess;
        }
        catch (Exception exception)
        {
            log.Error(exception, "Failed to publish project: {projectName}", projectName);
            return false;
        }
    }

    static async Task<bool> BuildImages(DirectoryInfo projectDirectory, bool push = false)
    {
        string projectName = projectDirectory.Name;
        string dockerFilePath = projectDirectory.FullName + "/Dockerfile";

        if (!File.Exists(dockerFilePath))
        {
            log.Error("Dockerfile not found in project: {projectName}", projectName);
            return false;
        }

        string baseDockerFile = await File.ReadAllTextAsync(dockerFilePath);
        
        
        foreach (string tagName in tagNames)
        {
            string baseImagePostfix = baseImagePostfixes[tagName];
            
            StringBuilder dockerfileStringBuilder = new(baseDockerFile);
            dockerfileStringBuilder.Insert(48, baseImagePostfix);
            
            string finalDockerfile = dockerfileStringBuilder.ToString();
            string imageName = imageNames[projectName];
            string targetPlatform = targetPlatforms[tagName];
            string fullImageName = imageName + ":" + tagName;

            Command command = Cli.Wrap("docker")
                .WithArguments(["build", "--platform", targetPlatform, "-f-", "-t", fullImageName, "."])
                .WithWorkingDirectory(projectDirectory.FullName)
                .WithStandardInputPipe(PipeSource.FromString(finalDockerfile));

            try
            {
                CommandResult result = await command.ExecuteAsync();

                if (!result.IsSuccess)
                {
                    log.Error("Image building unsuccessful: {projectName}", projectName);
                    return false;
                }

                log.Information("Built image: {imageName}", fullImageName);

                if (push)
                {
                    Command pushCommand = Cli.Wrap("docker").WithArguments(["image", "push", fullImageName]);
                    
                    CommandResult pushResult = await pushCommand.ExecuteAsync();

                    if (!pushResult.IsSuccess)
                    {
                        log.Error("Push unsuccessful: {imageName}", fullImageName);
                    }
                    else
                    {
                        log.Information("Pushed image: {imageName}", fullImageName);
                    }
                }
            }
            catch (Exception exception)
            {
                log.Error(exception, "Failed to build image: {projectName}", projectName);
            }
        }

        return true;
    }
    
    static DirectoryInfo? TryGetSolutionDirectoryInfo(string? currentPath = null)
    {
        DirectoryInfo? directory = new(currentPath ?? Directory.GetCurrentDirectory());
        
        while (directory != null && directory.GetFiles("*.sln").Length == 0)
        {
            directory = directory.Parent;
        }
        
        return directory;
    }
}