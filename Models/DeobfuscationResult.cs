using System;
using System.Collections.Generic;

namespace NetControlFlow.Models
{
    public class DeobfuscationResult
    {
        public string OriginalPath { get; set; } = "";
        public string OutputPath { get; set; } = "";
        public int MethodsProcessed { get; set; }
        public int IssuesFixed { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        public Dictionary<string, object> Statistics { get; set; } = new Dictionary<string, object>();
        public bool Success => Errors.Count == 0;
    }

    public enum ObfuscatorType
    {
        Unknown,
        ConfuserEx,
        SmartAssembly,
        Dotfuscator,
        Eazfuscator,
        ILProtector,
        Obfuscar,
        CryptoObfuscator,
        .NETReactor,
        AgileDotNet,
        CodeVeil,
        Armadillo,
        Themida,
        WinLicense
    }

    public class DetectionResult
    {
        public ObfuscatorType Obfuscator { get; set; }
        public double Confidence { get; set; }
        public List<string> DetectedFeatures { get; set; } = new List<string>();
        public string Version { get; set; } = "";
    }
}
