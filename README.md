# C# File Collector and Token Estimator

This project is a C# console application designed to process .NET solution files, collect specific file types, and estimate the number of tokens in the processed content. It's particularly useful for preparing codebases for analysis by AI models like GPT or Claude.

## Features

- Scans a specified .NET solution folder for project files
- Collects content from .cs, .cshtml, and .razor files
- Ignores test projects and migration folders
- Removes comments and unnecessary code elements
- Estimates the number of tokens in the processed content
- Generates separate output files for each project and file type
- Creates a summary file with the total token count

## Prerequisites

- .NET 8.0 SDK 
- A .NET solution to process

## Configuration

The application uses an `appsettings.json` file for configuration. Create this file in the same directory as the executable with the following content:

```json
{
  "SolutionFolder": "C:\\path\\to\\your\\solution\\folder"
}
```

Replace the path with the actual path to your .NET solution folder.

## Usage

1. Compile the program using your preferred C# compiler or IDE.
2. Ensure the `appsettings.json` file is in the same directory as the compiled executable.
3. Run the executable.

The program will process the specified solution and generate the following files:

- `{ProjectName}.{FileType}.txt`: Contains the processed content of each file type for each project.
- `{SolutionName}.{TotalTokens}_Tokens.txt`: An empty file indicating the total estimated token count.

## Output

The program will output progress information to the console, including:

- Names of created output files
- Any errors encountered during processing

## Token Estimation

The program uses a simple estimation method of 0.3 tokens per character. This is a rough approximation and may not exactly match the tokenization of specific AI models.

## Limitations

- The token estimation is approximate and may not precisely match the tokenization of specific AI models.
- The program assumes a standard .NET solution structure.
- Large solutions with many files may take some time to process.

## Customization

You can modify the `TOKENS_PER_CHAR` constant in the code to adjust the token estimation ratio if needed.
