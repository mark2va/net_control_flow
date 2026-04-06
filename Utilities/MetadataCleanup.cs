using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using NetControlFlow.Logging;

namespace NetControlFlow.Utilities
{
    public class MetadataCleanup
    {
        private readonly ModuleDef _module;

        public MetadataCleanup(ModuleDef module)
        {
            _module = module;
        }

        public void CleanAll()
        {
            try
            {
                RemoveObfuscatorAttributes();
                RemoveSuspiciousAttributes();
                FixTypeNames();
                FixMethodNames();
                RemoveEmptyNamespaces();
                LogManager.LogSuccess("Metadata cleanup completed");
            }
            catch (Exception ex)
            {
                LogManager.LogError("Error during metadata cleanup", ex);
            }
        }

        private void RemoveObfuscatorAttributes()
        {
            try
            {
                var toRemove = new List<CustomAttribute>();
                var obfuscatorKeywords = new[] 
                { 
                    "Confuser", "SmartAssembly", "Dotfuscator", "Eazfuscator", 
                    "ILProtector", "Obfuscated", "SuppressIldasm", "DeobfuscatedBy"
                };

                foreach (var attr in _module.Assembly.CustomAttributes)
                {
                    if (obfuscatorKeywords.Any(kw => attr.TypeFullName.Contains(kw)))
                        toRemove.Add(attr);
                }

                foreach (var attr in toRemove)
                    _module.Assembly.CustomAttributes.Remove(attr);

                if (toRemove.Count > 0)
                    LogManager.LogOperation($"Removed {toRemove.Count} obfuscator attributes");
            }
            catch (Exception ex)
            {
                LogManager.LogError("Error removing obfuscator attributes", ex);
            }
        }

        private void RemoveSuspiciousAttributes()
        {
            try
            {
                var suspiciousPatterns = new[] 
                { 
                    "DynamicallyInvoked", "DebuggerHidden", "DebuggerNonUserCode"
                };

                int removedCount = 0;

                foreach (var type in _module.GetTypes())
                {
                    var typeAttrsToRemove = new List<CustomAttribute>();
                    foreach (var attr in type.CustomAttributes)
                    {
                        if (suspiciousPatterns.Any(p => attr.TypeFullName.Contains(p)))
                            typeAttrsToRemove.Add(attr);
                    }

                    foreach (var attr in typeAttrsToRemove)
                    {
                        type.CustomAttributes.Remove(attr);
                        removedCount++;
                    }

                    // Check methods
                    foreach (var method in type.Methods)
                    {
                        var methodAttrsToRemove = new List<CustomAttribute>();
                        foreach (var attr in method.CustomAttributes)
                        {
                            if (suspiciousPatterns.Any(p => attr.TypeFullName.Contains(p)))
                                methodAttrsToRemove.Add(attr);
                        }

                        foreach (var attr in methodAttrsToRemove)
                        {
                            method.CustomAttributes.Remove(attr);
                            removedCount++;
                        }
                    }
                }

                if (removedCount > 0)
                    LogManager.LogOperation($"Removed {removedCount} suspicious attributes");
            }
            catch (Exception ex)
            {
                LogManager.LogError("Error removing suspicious attributes", ex);
            }
        }

        private void FixTypeNames()
        {
            try
            {
                int fixedCount = 0;

                foreach (var type in _module.GetTypes())
                {
                    if (IsSuspiciousName(type.Name))
                    {
                        type.Name = $"Type_{fixedCount:X4}";
                        fixedCount++;
                    }
                }

                if (fixedCount > 0)
                    LogManager.LogOperation($"Fixed {fixedCount} suspicious type names");
            }
            catch (Exception ex)
            {
                LogManager.LogError("Error fixing type names", ex);
            }
        }

        private void FixMethodNames()
        {
            try
            {
                int fixedCount = 0;

                foreach (var type in _module.GetTypes())
                {
                    foreach (var method in type.Methods)
                    {
                        if (IsSuspiciousName(method.Name) && !method.IsSpecialName)
                        {
                            method.Name = $"Method_{fixedCount:X4}";
                            fixedCount++;
                        }
                    }
                }

                if (fixedCount > 0)
                    LogManager.LogOperation($"Fixed {fixedCount} suspicious method names");
            }
            catch (Exception ex)
            {
                LogManager.LogError("Error fixing method names", ex);
            }
        }

        private void RemoveEmptyNamespaces()
        {
            try
            {
                int cleanedCount = 0;

                foreach (var type in _module.GetTypes())
                {
                    if (string.IsNullOrWhiteSpace(type.Namespace) ||
                        type.Namespace.Any(c => char.IsControl(c)))
                    {
                        type.Namespace = "Deobfuscated";
                        cleanedCount++;
                    }
                }

                if (cleanedCount > 0)
                    LogManager.LogOperation($"Cleaned {cleanedCount} empty/suspicious namespaces");
            }
            catch (Exception ex)
            {
                LogManager.LogError("Error removing empty namespaces", ex);
            }
        }

        private bool IsSuspiciousName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return true;

            // Check for non-printable characters
            if (name.Any(c => char.IsControl(c) && c != '\0'))
                return true;

            // Check for Unicode characters that look like obfuscation
            if (name.Any(c => c > 0x3000 && c < 0xA000))
                return true;

            // Check for mostly numbers/symbols
            var alphaCount = name.Count(char.IsLetterOrDigit);
            if (alphaCount == 0)
                return true;

            return false;
        }
    }
}