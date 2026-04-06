using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using NetControlFlow.Logging;

namespace NetControlFlow.Detection
{
    public class ObfuscatorDetector
    {
        public DetectionResult DetectObfuscator(ModuleDef module)
        {
            var result = new DetectionResult { Module = module.Name };

            // Check for known obfuscator attributes and signatures
            CheckConfuserEx(module, result);
            CheckSmartAssembly(module, result);
            CheckDotfuscator(module, result);
            CheckEazfuscator(module, result);
            CheckILProtector(module, result);

            var variables = new Dictionary<string, object>
            {
                { "Module", module.Name },
                { "DetectedObfuscator", result.ObfuscatorName ?? "Unknown" },
                { "Confidence", result.Confidence }
            };
            LogManager.LogAnalysis("Obfuscator", "Detection", variables);

            return result;
        }

        private void CheckConfuserEx(ModuleDef module, DetectionResult result)
        {
            var confuserAttribute = module.Assembly?.CustomAttributes
                .FirstOrDefault(a => a.TypeFullName.Contains("ConfuserAttribute"));

            if (confuserAttribute != null)
            {
                result.ObfuscatorName = "ConfuserEx";
                result.Confidence = 100;
                result.Features.Add("ConfuserAttribute detected");
                return;
            }

            // Check for ConfuserEx specific patterns
            var hasResourceEncryption = module.Resources.Any(r => r.Name.Contains(".Confuser"));
            var hasStringEncryption = module.Types.Any(t => 
                t.Methods.Any(m => m.Body?.Instructions.Any(i => 
                    i.OpCode.Name.Contains("call")) ?? false));

            if (hasResourceEncryption)
            {
                result.ObfuscatorName = "ConfuserEx";
                result.Confidence = 85;
                result.Features.Add("Encrypted resources detected");
            }
        }

        private void CheckSmartAssembly(ModuleDef module, DetectionResult result)
        {
            var smartAttribute = module.Assembly?.CustomAttributes
                .FirstOrDefault(a => a.TypeFullName.Contains("SmartAssembly"));

            if (smartAttribute != null)
            {
                result.ObfuscatorName = "SmartAssembly";
                result.Confidence = 100;
                result.Features.Add("SmartAssembly attribute detected");
            }
        }

        private void CheckDotfuscator(ModuleDef module, DetectionResult result)
        {
            var dotfuscatorAttribute = module.Assembly?.CustomAttributes
                .FirstOrDefault(a => a.TypeFullName.Contains("Dotfuscator"));

            if (dotfuscatorAttribute != null)
            {
                result.ObfuscatorName = "Dotfuscator";
                result.Confidence = 100;
                result.Features.Add("Dotfuscator attribute detected");
            }
        }

        private void CheckEazfuscator(ModuleDef module, DetectionResult result)
        {
            var eazAttribute = module.Assembly?.CustomAttributes
                .FirstOrDefault(a => a.TypeFullName.Contains("Eazfuscator"));

            if (eazAttribute != null)
            {
                result.ObfuscatorName = "Eazfuscator.NET";
                result.Confidence = 100;
                result.Features.Add("Eazfuscator attribute detected");
            }
        }

        private void CheckILProtector(ModuleDef module, DetectionResult result)
        {
            var ilpAttribute = module.Assembly?.CustomAttributes
                .FirstOrDefault(a => a.TypeFullName.Contains("ILProtector"));

            if (ilpAttribute != null)
            {
                result.ObfuscatorName = "ILProtector";
                result.Confidence = 100;
                result.Features.Add("ILProtector attribute detected");
            }
        }
    }

    public class DetectionResult
    {
        public string Module { get; set; }
        public string ObfuscatorName { get; set; }
        public int Confidence { get; set; }
        public List<string> Features { get; set; } = new List<string>();
    }
}
