using System;
using System.Collections.Generic;
using dnlib.DotNet;
using NetControlFlow.Models;

namespace NetControlFlow.Detection
{
    public class ObfuscatorDetector
    {
        public DetectionResult Detect(ModuleDefMD module)
        {
            var result = new DetectionResult
            {
                Obfuscator = ObfuscatorType.Unknown,
                Confidence = 0.0,
                DetectedFeatures = new List<string>()
            };

            // Check for ConfuserEx signatures
            if (CheckConfuserEx(module, result))
            {
                result.Obfuscator = ObfuscatorType.ConfuserEx;
                return result;
            }

            // Check for SmartAssembly
            if (CheckSmartAssembly(module, result))
            {
                result.Obfuscator = ObfuscatorType.SmartAssembly;
                return result;
            }

            // Check for Dotfuscator
            if (CheckDotfuscator(module, result))
            {
                result.Obfuscator = ObfuscatorType.Dotfuscator;
                return result;
            }

            // Check for Eazfuscator
            if (CheckEazfuscator(module, result))
            {
                result.Obfuscator = ObfuscatorType.Eazfuscator;
                return result;
            }

            // Check for ILProtector
            if (CheckILProtector(module, result))
            {
                result.Obfuscator = ObfuscatorType.ILProtector;
                return result;
            }

            return result;
        }

        private bool CheckConfuserEx(ModuleDefMD module, DetectionResult result)
        {
            int score = 0;

            // Check for ConfuserEx runtime helper types
            foreach (var type in module.Types)
            {
                if (type.Name.StartsWith("<") && type.Name.Contains("Confuser"))
                {
                    score += 20;
                    result.DetectedFeatures.Add($"ConfuserEx type: {type.Name}");
                }

                // Check for specific ConfuserEx protection markers
                if (type.Namespace == "Confuser.Core" || type.Namespace?.Contains("Confuser") == true)
                {
                    score += 30;
                    result.DetectedFeatures.Add($"ConfuserEx namespace: {type.Namespace}");
                }
            }

            // Check for renamed types with invalid identifiers (common in ConfuserEx)
            int invalidIdentCount = 0;
            foreach (var type in module.Types)
            {
                if (!IsValidIdentifier(type.Name) && type.Name.Length <= 5)
                    invalidIdentCount++;
            }

            if (invalidIdentCount > module.Types.Count * 0.7)
            {
                score += 25;
                result.DetectedFeatures.Add($"High ratio of obfuscated type names: {invalidIdentCount}/{module.Types.Count}");
            }

            // Check for control flow obfuscation patterns
            int cflowMethods = 0;
            foreach (var type in module.Types)
            {
                foreach (var method in type.Methods)
                {
                    if (method.HasBody && method.Body.Instructions.Count > 100)
                    {
                        // Check for excessive branching (control flow obfuscation)
                        int branchCount = 0;
                        foreach (var instr in method.Body.Instructions)
                        {
                            if (instr.OpCode.Code >= Code.Br && instr.OpCode.Code <= Code.Leave)
                                branchCount++;
                        }

                        if (branchCount > 50)
                            cflowMethods++;
                    }
                }
            }

            if (cflowMethods > 10)
            {
                score += 25;
                result.DetectedFeatures.Add($"Control flow obfuscation detected in {cflowMethods} methods");
            }

            if (score >= 50)
            {
                result.Confidence = Math.Min(1.0, score / 100.0);
                return true;
            }

            return false;
        }

        private bool CheckSmartAssembly(ModuleDefMD module, DetectionResult result)
        {
            // Check for SmartAssembly attributes
            foreach (var asmRef in module.AssemblyRefs)
            {
                if (asmRef.Name.Contains("SmartAssembly"))
                {
                    result.Confidence = 0.95;
                    result.DetectedFeatures.Add($"SmartAssembly reference: {asmRef.Name}");
                    return true;
                }
            }

            return false;
        }

        private bool CheckDotfuscator(ModuleDefMD module, DetectionResult result)
        {
            // Check for Dotfuscator-specific patterns
            foreach (var type in module.Types)
            {
                if (type.Name.Contains("a#") || type.Name.StartsWith("zzz"))
                {
                    result.Confidence = 0.85;
                    result.DetectedFeatures.Add($"Dotfuscator pattern: {type.Name}");
                    return true;
                }
            }

            return false;
        }

        private bool CheckEazfuscator(ModuleDefMD module, DetectionResult result)
        {
            // Check for Eazfuscator signatures
            foreach (var type in module.Types)
            {
                if (type.Namespace == "Eazfuscator.NET" || type.Name.Contains("Eazfuscator"))
                {
                    result.Confidence = 0.95;
                    result.DetectedFeatures.Add($"Eazfuscator type: {type.FullName}");
                    return true;
                }
            }

            return false;
        }

        private bool CheckILProtector(ModuleDefMD module, DetectionResult result)
        {
            // Check for ILProtector patterns
            foreach (var type in module.Types)
            {
                if (type.Name == "ProtectMe" || type.Name.Contains("Wise"))
                {
                    result.Confidence = 0.9;
                    result.DetectedFeatures.Add($"ILProtector type: {type.Name}");
                    return true;
                }
            }

            return false;
        }

        private bool IsValidIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;

            // Basic check for valid C# identifier
            if (!char.IsLetter(name[0]) && name[0] != '_')
                return false;

            for (int i = 1; i < name.Length; i++)
            {
                char c = name[i];
                if (!char.IsLetterOrDigit(c) && c != '_')
                    return false;
            }

            return true;
        }
    }
}
