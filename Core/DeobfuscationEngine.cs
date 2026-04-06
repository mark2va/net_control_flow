using System;
using System.IO;
using System.Threading.Tasks;
using dnlib.DotNet;
using NetControlFlow.Config;
using NetControlFlow.Models;
using NetControlFlow.Logging;
using NetControlFlow.Detection;
using NetControlFlow.Deobfuscators;

namespace NetControlFlow.Core
{
    public class DeobfuscationEngine
    {
        private readonly DeobfuscatorConfig _config;
        private readonly ObfuscatorDetector _detector;
        private readonly BaseDeobfuscator[] _deobfuscators;

        public DeobfuscationEngine(DeobfuscatorConfig config)
        {
            _config = config;
            _detector = new ObfuscatorDetector();
            _deobfuscators = InitializeDeobfuscators();
        }

        private BaseDeobfuscator[] InitializeDeobfuscators()
        {
            return new BaseDeobfuscator[]
            {
                new ConfuserExDeobfuscator(),
                // TODO: Add other deobfuscators as they are implemented
                // new SmartAssemblyDeobfuscator(),
                // new DotfuscatorDeobfuscator(),
                // new EazfuscatorDeobfuscator(),
                // new ILProtectorDeobfuscator()
            };
        }

        public async Task<DeobfuscationResult> ProcessAssemblyAsync(string inputPath)
        {
            var result = new DeobfuscationResult
            {
                OriginalPath = inputPath,
                OutputPath = Path.Combine(_config.OutputPath, Path.GetFileName(inputPath))
            };

            try
            {
                LogManager.LogInfo($"Loading assembly: {inputPath}");
                
                // Load module
                var module = ModuleDefMD.Load(inputPath);
                
                // Detect obfuscator
                var detection = _detector.Detect(module);
                LogManager.LogInfo($"Detected: {detection.Obfuscator} (confidence: {detection.Confidence:P2})");
                
                result.Statistics["DetectedObfuscator"] = detection.Obfuscator.ToString();
                result.Statistics["Confidence"] = detection.Confidence;
                
                // Select appropriate deobfuscator
                var deobfuscator = SelectDeobfuscator(detection.Obfuscator);
                if (deobfuscator == null)
                {
                    result.Warnings.Add($"No deobfuscator available for {detection.Obfuscator}");
                    LogManager.LogWarning($"No deobfuscator found for {detection.Obfuscator}");
                }
                else
                {
                    // Run deobfuscation
                    LogManager.LogInfo($"Running {deobfuscator.Name}...");
                    var deobResult = await deobfuscator.DeobfuscateAsync(module, _config);
                    
                    result.MethodsProcessed = deobResult.MethodsProcessed;
                    result.IssuesFixed = deobResult.IssuesFixed;
                    result.Warnings.AddRange(deobResult.Warnings);
                    result.Errors.AddRange(deobResult.Errors);
                    
                    // Save output
                    if (result.Success)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(result.OutputPath));
                        module.Write(result.OutputPath);
                        LogManager.LogInfo($"Saved to: {result.OutputPath}");
                    }
                }
                
                // Optional: AI analysis
                if (_config.EnableAIAnalysis)
                {
                    // TODO: Implement AI analysis
                    LogManager.LogInfo("AI analysis not yet implemented");
                }
                
                module.Dispose();
            }
            catch (Exception ex)
            {
                result.Errors.Add(ex.Message);
                LogManager.LogError($"Error processing {inputPath}", ex);
            }
            
            return result;
        }

        public async Task<DeobfuscationResult[]> ProcessDirectoryAsync(string directory)
        {
            var files = Directory.GetFiles(directory, "*.dll", SearchOption.AllDirectories);
            files = files.Concat(Directory.GetFiles(directory, "*.exe", SearchOption.AllDirectories)).ToArray();
            
            var results = new List<DeobfuscationResult>();
            foreach (var file in files)
            {
                var result = await ProcessAssemblyAsync(file);
                results.Add(result);
            }
            return results.ToArray();
        }

        private BaseDeobfuscator? SelectDeobfuscator(ObfuscatorType type)
        {
            foreach (var deob in _deobfuscators)
            {
                if (deob.SupportedObfuscators.Contains(type))
                    return deob;
            }
            return null;
        }
    }
}
