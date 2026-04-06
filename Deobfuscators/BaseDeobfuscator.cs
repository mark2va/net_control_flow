using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NetControlFlow.Logging;
using NetControlFlow.Models;
using NetControlFlow.Config;

namespace NetControlFlow.Deobfuscators
{
    public class DeobfuscationResultInternal
    {
        public int MethodsProcessed { get; set; }
        public int IssuesFixed { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
    }

    public abstract class BaseDeobfuscator
    {
        public string Name { get; protected set; } = "Unknown";
        public List<ObfuscatorType> SupportedObfuscators { get; protected set; } = new List<ObfuscatorType>();

        public virtual async Task<DeobfuscationResultInternal> DeobfuscateAsync(ModuleDefMD module, DeobfuscatorConfig config)
        {
            var result = new DeobfuscationResultInternal();
            
            try
            {
                LogManager.LogInfo($"Starting {Name} deobfuscation...");
                
                // Run synchronous deobfuscation in task
                await Task.Run(() =>
                {
                    Deobfuscate(module, config, result);
                });
                
                LogManager.LogInfo($"{Name} deobfuscation completed. Methods: {result.MethodsProcessed}, Issues Fixed: {result.IssuesFixed}");
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Deobfuscation error: {ex.Message}");
                LogManager.LogError($"Error in {Name}", ex);
            }
            
            return result;
        }

        protected virtual void Deobfuscate(ModuleDefMD module, DeobfuscatorConfig config, DeobfuscationResultInternal result)
        {
            // Clean attributes
            if (config.EnableMetadataCleanup)
            {
                CleanAttributes(module, result);
            }

            // Clean namespaces
            CleanNamespaces(module, result);

            // String decryption
            if (config.EnableStringDecryption)
            {
                DecryptStrings(module, config, result);
            }

            // Control flow unflattening
            if (config.EnableControlFlowUnflattening)
            {
                UnflattenControlFlow(module, config, result);
            }

            // Resource extraction
            if (config.EnableResourceExtraction)
            {
                ExtractResources(module, config, result);
            }
        }

        protected virtual void CleanAttributes(ModuleDefMD module, DeobfuscationResultInternal result)
        {
            try
            {
                var toRemove = new List<CustomAttribute>();
                foreach (var attr in module.Assembly.CustomAttributes)
                {
                    if (attr.TypeFullName.Contains("SuppressIldasm") ||
                        attr.TypeFullName.Contains("ObfuscatedByAttribute") ||
                        attr.TypeFullName.Contains("Confuser"))
                    {
                        toRemove.Add(attr);
                    }
                }

                foreach (var attr in toRemove)
                    module.Assembly.CustomAttributes.Remove(attr);

                if (toRemove.Count > 0)
                {
                    result.IssuesFixed += toRemove.Count;
                    LogManager.LogInfo($"Removed {toRemove.Count} obfuscator attributes");
                }
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"Error cleaning attributes: {ex.Message}");
                LogManager.LogError($"Error cleaning attributes in {Name}", ex);
            }
        }

        protected virtual void CleanNamespaces(ModuleDefMD module, DeobfuscationResultInternal result)
        {
            try
            {
                int cleaned = 0;
                foreach (var type in module.Types)
                {
                    if (string.IsNullOrWhiteSpace(type.Namespace) ||
                        type.Namespace.Contains("\x00") ||
                        type.Namespace.Contains("＜") ||
                        (type.Namespace.Length > 0 && Char.IsControl(type.Namespace[0])))
                    {
                        type.Namespace = "Deobfuscated";
                        cleaned++;
                    }
                }

                if (cleaned > 0)
                {
                    result.IssuesFixed += cleaned;
                    LogManager.LogInfo($"Cleaned {cleaned} suspicious namespaces");
                }
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"Error cleaning namespaces: {ex.Message}");
                LogManager.LogError($"Error cleaning namespaces in {Name}", ex);
            }
        }

        protected virtual void DecryptStrings(ModuleDefMD module, DeobfuscatorConfig config, DeobfuscationResultInternal result)
        {
            // To be overridden by specific deobfuscators
        }

        protected virtual void UnflattenControlFlow(ModuleDefMD module, DeobfuscatorConfig config, DeobfuscationResultInternal result)
        {
            // To be overridden by specific deobfuscators
        }

        protected virtual void ExtractResources(ModuleDefMD module, DeobfuscatorConfig config, DeobfuscationResultInternal result)
        {
            // To be overridden by specific deobfuscators
        }
    }
}