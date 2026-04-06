using System;
using Newtonsoft.Json;
using System.IO;
using NetControlFlow.Logging;

namespace NetControlFlow.Config
{
    public class DeobfuscatorConfig
    {
        public string InputPath { get; set; } = "./input";
        public string OutputPath { get; set; } = "./output";
        public LoggingConfig Logging { get; set; } = new LoggingConfig();
        public bool EnableStringDecryption { get; set; } = true;
        public bool EnableControlFlowUnflattening { get; set; } = true;
        public bool EnableResourceExtraction { get; set; } = true;
        public bool EnableMetadataCleanup { get; set; } = true;
        public bool EnableAIAnalysis { get; set; } = false;
        public string AIModel { get; set; } = "llama2";
        public string[] EnabledDeobfuscators { get; set; } = new[] { "ConfuserEx", "SmartAssembly", "Dotfuscator", "Eazfuscator", "ILProtector" };

        public static DeobfuscatorConfig LoadFromFile(string path)
        {
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<DeobfuscatorConfig>(json) ?? CreateDefault();
        }

        public void SaveToFile(string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        public static DeobfuscatorConfig CreateDefault()
        {
            return new DeobfuscatorConfig();
        }
    }
}
