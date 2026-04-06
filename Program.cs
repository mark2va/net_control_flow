using System;
using System.IO;
using System.Threading.Tasks;
using NetControlFlow.Config;
using NetControlFlow.Core;
using NetControlFlow.Logging;

namespace NetControlFlow
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
                Console.WriteLine("║     .NET Control Flow Deobfuscator v1.0 (dnlib Edition)   ║");
                Console.WriteLine("║                Based on de4dot-cex                         ║");
                Console.WriteLine("╚═══════════════════════════════════════════════════════════╝\n");

                // Load or create config
                var configPath = args.Length > 0 ? args[0] : "./config.json";
                DeobfuscatorConfig config;

                if (File.Exists(configPath))
                {
                    config = DeobfuscatorConfig.LoadFromFile(configPath);
                    Console.WriteLine($"[*] Loaded config from: {configPath}\n");
                }
                else
                {
                    config = DeobfuscatorConfig.CreateDefault();
                    config.SaveToFile(configPath);
                    Console.WriteLine($"[*] Created default config: {configPath}");
                    Console.WriteLine("[!] Please configure the settings and run again.\n");
                    return;
                }

                // Initialize logging
                LogManager.Initialize(config.Logging);
                Console.WriteLine($"[+] Logging initialized to: {config.Logging.LogPath}\n");

                // Create engine
                var engine = new DeobfuscationEngine(config);

                // Process input
                if (Directory.Exists(config.InputPath))
                {
                    Console.WriteLine($"[*] Processing directory: {config.InputPath}");
                    var results = await engine.ProcessDirectoryAsync(config.InputPath);
                    PrintResults(results);
                }
                else if (File.Exists(config.InputPath))
                {
                    Console.WriteLine($"[*] Processing file: {config.InputPath}");
                    var result = await engine.ProcessAssemblyAsync(config.InputPath);
                    PrintResults(new[] { result });
                }
                else
                {
                    Console.WriteLine($"[!] Input path not found: {config.InputPath}");
                    return;
                }

                Console.WriteLine("\n[+] Processing complete!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[!] Fatal error: {ex.Message}");
                LogManager.LogError("Fatal error", ex);
                Environment.Exit(1);
            }
        }

        static void PrintResults(DeobfuscationResult[] results)
        {
            Console.WriteLine("\n╔═══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    RESULTS SUMMARY                         ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════╝\n");

            foreach (var result in results)
            {
                Console.WriteLine($"File:              {result.OriginalPath}");
                Console.WriteLine($"Output:            {result.OutputPath}");
                Console.WriteLine($"Methods Processed: {result.MethodsProcessed}");
                Console.WriteLine($"Issues Fixed:      {result.IssuesFixed}");
                
                if (result.Warnings.Count > 0)
                {
                    Console.WriteLine($"Warnings:          {result.Warnings.Count}");
                    foreach (var warning in result.Warnings)
                        Console.WriteLine($"  - {warning}");
                }

                if (result.Errors.Count > 0)
                {
                    Console.WriteLine($"Errors:            {result.Errors.Count}");
                    foreach (var error in result.Errors)
                        Console.WriteLine($"  - {error}");
                }

                Console.WriteLine();
            }
        }
    }
}