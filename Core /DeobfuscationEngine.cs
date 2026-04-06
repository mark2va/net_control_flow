using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NetControlFlow.AI;
using NetControlFlow.Analysis;
using NetControlFlow.Config;
using NetControlFlow.Detection;
using NetControlFlow.Logging;
using NetControlFlow.Models;

namespace NetControlFlow.Core
{
    public class DeobfuscationEngine
    {
        private readonly DeobfuscatorConfig _config;
        private readonly OllamaAssistant _ollamaAssistant;
        private readonly ControlFlowAnalyzer _cfAnalyzer;
        private readonly ObfuscatorDetector _detector;

        public DeobfuscationEngine(DeobfuscatorConfig config)
        {
            _config = config;
            _ollamaAssistant = config.Ollama.Enabled ? 
                new OllamaAssistant(config.Ollama) : null;
            _cfAnalyzer = new ControlFlowAnalyzer();
            _detector = new ObfuscatorDetector();
        }

        public async Task<DeobfuscationResult> ProcessAssemblyAsync(string assemblyPath)
        {
            var result = new DeobfuscationResult { OriginalPath = assemblyPath };

            try
            {
                LogManager.LogOperation($"Processing assembly: {assemblyPath}");

                var module = ModuleDefMD.Load(assemblyPath);
                
                // Detect obfuscator
                if (_config.DetectOnly)
                {
                    var detection = _detector.DetectObfuscator(module);
                    LogManager.LogSuccess($"Detected: {detection.ObfuscatorName} (Confidence: {detection.Confidence}%)");
                    return result;
                }

                // Process all types
                foreach (var type in module.Types)
                {
                    ProcessType(type, result);
                }

                // Save cleaned assembly
                var outputPath = GenerateOutputPath(assemblyPath);
                module.Write(outputPath);
                result.OutputPath = outputPath;

                LogManager.LogSuccess($"Assembly processed. Output: {outputPath}");
            }
            catch (Exception ex)
            {
                result.Errors.Add(ex.Message);
                LogManager.LogError($"Error processing assembly: {assemblyPath}", ex);
            }

            return result;
        }

        private void ProcessType(TypeDef type, DeobfuscationResult result)
        {
            foreach (var method in type.Methods)
            {
                if (method.Body == null || !method.Body.HasInstructions)
                    continue;

                try
                {
                    ProcessMethod(method, result);
                    result.MethodsProcessed++;
                }
                catch (Exception ex)
                {
                    result.Warnings.Add($"Error processing {method.FullName}: {ex.Message}");
                    LogManager.LogError($"Error processing method: {method.FullName}", ex);
                }
            }

            // Process nested types recursively
            foreach (var nestedType in type.NestedTypes)
            {
                ProcessType(nestedType, result);
            }
        }

        private void ProcessMethod(MethodDef method, DeobfuscationResult result)
        {
            // Analyze control flow
            var cfg = _cfAnalyzer.AnalyzeMethod(method);

            // Remove dead code
            if (_config.Deobfuscation.RemoveAntiDecompile)
            {
                RemoveAntiDecompilationCode(method);
                result.IssuesFixed++;
            }

            // Fix method signatures
            if (_config.Deobfuscation.FixMethodSignatures)
            {
                FixMethodSignature(method);
            }

            // Use Ollama for analysis if enabled
            if (_config.Ollama.Enabled && _config.Ollama.UseForAnalysis)
            {
                _ = AnalyzeWithOllamaAsync(method, cfg);
            }
        }

        private void RemoveAntiDecompilationCode(MethodDef method)
        {
            var instructions = method.Body.Instructions;
            var toRemove = new List<Instruction>();

            foreach (var instr in instructions)
            {
                // Remove int3 (debugger break)
                if (instr.OpCode.Name == "int3")
                    toRemove.Add(instr);

                // Remove invalid operations
                if (instr.OpCode.Name.Contains("invalid"))
                    toRemove.Add(instr);
            }

            foreach (var instr in toRemove)
            {
                instructions.Remove(instr);
            }

            if (toRemove.Count > 0)
            {
                LogManager.LogOperation($"Removed {toRemove.Count} anti-decompilation instructions from {method.Name}");
            }
        }

        private void FixMethodSignature(MethodDef method)
        {
            try
            {
                // Validate and fix corrupted signatures
                if (method.ReturnType == null)
                {
                    method.ReturnType = method.Module.CorLibTypes.Void;
                }

                foreach (var param in method.Parameters)
                {
                    if (param.Type == null)
                    {
                        param.Type = method.Module.CorLibTypes.Object;
                    }
                }

                LogManager.LogOperation($"Fixed signature for method: {method.Name}");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"Error fixing signature for {method.Name}", ex);
            }
        }

        private async Task AnalyzeWithOllamaAsync(MethodDef method, ControlFlowGraph cfg)
        {
            try
            {
                if (!await _ollamaAssistant.HealthCheckAsync())
                    return;

                var code = string.Join("\n", 
                    method.Body.Instructions.Select(i => $"{i.Offset:X4}: {i.OpCode} {i.Operand}"));

                var analysis = await _ollamaAssistant.AnalyzeCodeAsync(code, "control_flow");
                LogManager.LogAnalysis(method.Name, "OllamaAnalysis", 
                    new Dictionary<string, object> { { "Analysis", analysis } });
            }
            catch (Exception ex)
            {
                LogManager.LogDebug($"Ollama analysis failed for {method.Name}", 
                    new Dictionary<string, object> { { "Error", ex.Message } });
            }
        }

        private string GenerateOutputPath(string inputPath)
        {
            var directory = Path.GetDirectoryName(inputPath);
            var filename = Path.GetFileNameWithoutExtension(inputPath);
            var extension = Path.GetExtension(inputPath);

            if (_config.PreserveOriginal)
                return Path.Combine(directory ?? ".", $"{filename}-cleaned{extension}");

            return inputPath;
        }

        public async Task<List<DeobfuscationResult>> ProcessDirectoryAsync(string directoryPath)
        {
            var results = new List<DeobfuscationResult>();

            try
            {
                var searchPattern = _config.Recursive ? 
                    SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

                var assemblies = Directory.GetFiles(
                    directoryPath,
                    "*.exe",
                    searchPattern).Concat(
                    Directory.GetFiles(directoryPath, "*.dll", searchPattern)
                );

                foreach (var assemblyPath in assemblies)
                {
                    var result = await ProcessAssemblyAsync(assemblyPath);
                    results.Add(result);
                }

                LogManager.LogSuccess($"Batch processing complete. Processed {results.Count} assemblies.");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"Error processing directory: {directoryPath}", ex);
            }

            return results;
        }
    }
}
