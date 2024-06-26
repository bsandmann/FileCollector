using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text;

class Program
{
    const double TOKENS_PER_CHAR = 0.3;
    static List<string> ignoreFolders;

    static void Main(string[] args)
    {
        string appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
        if (!File.Exists(appSettingsPath))
        {
            Console.WriteLine("appsettings.json file not found in the current directory.");
            return;
        }

        string jsonString = File.ReadAllText(appSettingsPath);
        var jsonDocument = JsonDocument.Parse(jsonString);
        var root = jsonDocument.RootElement;

        string solutionFolder = root.GetProperty("SolutionFolder").GetString();
        if (string.IsNullOrEmpty(solutionFolder))
        {
            Console.WriteLine("SolutionFolder not specified in appsettings.json");
            return;
        }

        ignoreFolders = root.TryGetProperty("IgnoreFolders", out var ignoreFoldersElement)
            ? ignoreFoldersElement.EnumerateArray().Select(e => e.GetString()).ToList()
            : new List<string>();

        int totalTokens = ProcessSolution(solutionFolder);

        // Create token count file
        string solutionName = Path.GetFileNameWithoutExtension(Directory.GetFiles(solutionFolder, "*.sln").FirstOrDefault() ?? "Solution");
        string tokenFileName = $"{solutionName}.{totalTokens}_Tokens.txt";
        File.WriteAllText(tokenFileName, string.Empty);
        Console.WriteLine($"Created token count file: {tokenFileName}");
    }

    static int ProcessSolution(string solutionFolder)
    {
        int totalTokens = 0;
        var projectFiles = Directory.GetFiles(solutionFolder, "*.csproj", SearchOption.AllDirectories);

        foreach (var projectFile in projectFiles)
        {
            string projectName = Path.GetFileNameWithoutExtension(projectFile);
            
            if (projectName.Contains("Test", StringComparison.OrdinalIgnoreCase))
                continue;

            string projectFolder = Path.GetDirectoryName(projectFile);

            totalTokens += ProcessProject(solutionFolder, projectName, projectFolder);
        }

        return totalTokens;
    }

    static int ProcessProject(string solutionFolder, string projectName, string projectFolder)
    {
        int projectTokens = 0;
        var fileTypes = new[] { "cs", "cshtml", "razor" };

        foreach (var fileType in fileTypes)
        {
            var files = Directory.GetFiles(projectFolder, $"*.{fileType}", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\Migrations\\", StringComparison.OrdinalIgnoreCase) &&
                            !Path.GetFileName(f).Contains("Assembly", StringComparison.OrdinalIgnoreCase) &&
                            !ShouldIgnoreFile(solutionFolder, f));

            if (!files.Any()) continue;

            string outputFileName = $"{projectName}.{fileType}.txt";
            using (StreamWriter writer = new StreamWriter(outputFileName))
            {
                foreach (var file in files)
                {
                    writer.WriteLine($"--- File: {Path.GetFileName(file)}");
                    writer.WriteLine();

                    var content = File.ReadAllText(file);
                    content = RemoveComments(content);
                    var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                    bool insideNamespace = false;
                    StringBuilder processedContent = new StringBuilder();

                    foreach (var line in lines)
                    {
                        string trimmedLine = line.Trim();
                        if (string.IsNullOrWhiteSpace(trimmedLine)) continue;
                        if (trimmedLine.StartsWith("using ")) continue;
                        if (trimmedLine.StartsWith("namespace "))
                        {
                            insideNamespace = true;
                            continue;
                        }
                        if (insideNamespace && trimmedLine == "{") continue;
                        
                        writer.WriteLine(line);
                        processedContent.AppendLine(line);
                    }

                    writer.WriteLine();
                    writer.WriteLine("--- End of file");
                    writer.WriteLine();

                    projectTokens += EstimateTokens(processedContent.ToString());
                }
            }

            Console.WriteLine($"Created {outputFileName}");
        }

        return projectTokens;
    }

    static bool ShouldIgnoreFile(string solutionFolder, string filePath)
    {
        string relativePath = Path.GetRelativePath(solutionFolder, filePath);
        return ignoreFolders.Any(folder => relativePath.StartsWith(folder, StringComparison.OrdinalIgnoreCase));
    }

    static string RemoveComments(string content)
    {
        // Remove single-line comments
        content = Regex.Replace(content, @"//.*$", "", RegexOptions.Multiline);

        // Remove multi-line comments (including XML documentation comments)
        content = Regex.Replace(content, @"/\*.*?\*/|///.*$", "", RegexOptions.Singleline | RegexOptions.Multiline);

        return content;
    }

    static int EstimateTokens(string content)
    {
        return (int)Math.Ceiling(content.Length * TOKENS_PER_CHAR);
    }
}