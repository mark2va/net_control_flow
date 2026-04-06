using Newtonsoft.Json;
using System;
using System.IO;

namespace NetControlFlow.Config
{
    public class DeobfuscatorConfig
    {
        [JsonProperty("input_path")]
        public string InputPath { get; set; }

        [JsonProperty("output_path")]
        public string OutputPath { get; set; }

        [JsonProperty("recursive")]
        public bool Recursive { get; set; } = false;

        [JsonProperty("detect_only")]
        public bool DetectOnly { get; set; } = false;

        [JsonProperty("clean_strings_only")]
        public bool CleanStringsOnly { get; set; } = false;

        [JsonProperty("preserve_original")]
        public bool PreserveOriginal { get; set; } = true;

        [JsonProperty("logging")]
        public LoggingConfig Logging { get; set; } = new LoggingConfig();

        [JsonProperty("ollama")]
        public OllamaConfig Ollama { get; set; } = new OllamaConfig();

        [JsonProperty("deobfuscation")]
        public DeobfuscationOptions Deobfuscation { get; set; } = new DeobfuscationOptions();

        public static DeobfuscatorConfig LoadFromFile(string configPath)
        {
            if (!File.Exists(configPath))
                throw new FileNotFoundException($"Config file not found: {configPath}");

            var json = File.ReadAllText(configPath);
            return JsonConvert.DeserializeObject<DeobfuscatorConfig>(json) 
                ?? throw new InvalidOperationException("Failed to parse config");
        }

        public static DeobfuscatorConfig CreateDefault()
        {
            return new DeobfuscatorConfig
            {
                InputPath = "./input",
                OutputPath = "./output",
                Logging = LoggingConfig.CreateDefault(),
                Ollama = OllamaConfig.CreateDefault()
            };
        }

        public void SaveToFile(string configPath)
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(configPath, json);
        }
    }

    public class LoggingConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("log_path")]
        public string LogPath { get; set; } = "./logs";

        [JsonProperty("log_level")]
        public string LogLevel { get; set; } = "Information";

        [JsonProperty("include_variables")]
        public bool IncludeVariables { get; set; } = true;

        [JsonProperty("log_format")]
        public string LogFormat { get; set; } = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level}] {Message}{NewLine}{Exception}";

        public static LoggingConfig CreateDefault()
        {
            return new LoggingConfig
            {
                LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs")
            };
        }
    }

    public class OllamaConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = false;

        [JsonProperty("endpoint")]
        public string Endpoint { get; set; } = "http://localhost:11434";

        [JsonProperty("model")]
        public string Model { get; set; } = "llama2";

        [JsonProperty("timeout_seconds")]
        public int TimeoutSeconds { get; set; } = 30;

        [JsonProperty("use_for_naming")]
        public bool UseForNaming { get; set; } = false;

        [JsonProperty("use_for_analysis")]
        public bool UseForAnalysis { get; set; } = false;

        public static OllamaConfig CreateDefault()
        {
            return new OllamaConfig();
        }
    }

    public class DeobfuscationOptions
    {
        [JsonProperty("unflat_control_flow")]
        public bool UnflatControlFlow { get; set; } = true;

        [JsonProperty("restore_names")]
        public bool RestoreNames { get; set; } = true;

        [JsonProperty("remove_anti_decompile")]
        public bool RemoveAntiDecompile { get; set; } = true;

        [JsonProperty("devirtualize_methods")]
        public bool DevirtualizeMethods { get; set; } = false;

        [JsonProperty("decrypt_resources")]
        public bool DecryptResources { get; set; } = true;

        [JsonProperty("clean_metadata")]
        public bool CleanMetadata { get; set; } = true;

        [JsonProperty("remove_bad_attributes")]
        public bool RemoveBadAttributes { get; set; } = true;

        [JsonProperty("fix_method_signatures")]
        public bool FixMethodSignatures { get; set; } = true;

        [JsonProperty("supported_obfuscators")]
        public string[] SupportedObfuscators { get; set; } = 
        {
            "ConfuserEx",
            "SmartAssembly",
            "Dotfuscator",
            "Eazfuscator",
            "ILProtector"
        };
    }
}
