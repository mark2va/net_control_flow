using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NetControlFlow.Config;
using NetControlFlow.Logging;

namespace NetControlFlow.AI
{
    public class OllamaAssistant
    {
        private readonly OllamaConfig _config;
        private readonly HttpClient _httpClient;

        public OllamaAssistant(OllamaConfig config)
        {
            _config = config;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds) };
        }

        public async Task<bool> HealthCheckAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_config.Endpoint}/api/tags");
                var isHealthy = response.IsSuccessStatusCode;

                var variables = new Dictionary<string, object>
                {
                    { "Endpoint", _config.Endpoint },
                    { "Status", isHealthy ? "Healthy" : "Unhealthy" }
                };
                LogManager.LogOperation("Ollama Health Check", variables);

                return isHealthy;
            }
            catch (Exception ex)
            {
                LogManager.LogError($"Ollama health check failed: {_config.Endpoint}", ex);
                return false;
            }
        }

        public async Task<string> AnalyzeCodeAsync(string code, string analysisType)
        {
            try
            {
                var prompt = BuildPrompt(code, analysisType);
                var response = await QueryOllamaAsync(prompt);

                var variables = new Dictionary<string, object>
                {
                    { "Model", _config.Model },
                    { "AnalysisType", analysisType },
                    { "CodeLength", code.Length },
                    { "ResponseLength", response.Length }
                };
                LogManager.LogOperation("Code Analysis", variables);

                return response;
            }
            catch (Exception ex)
            {
                LogManager.LogError($"Error analyzing code with Ollama", ex);
                throw;
            }
        }

        public async Task<string> SuggestNameAsync(string code, string context)
        {
            try
            {
                var prompt = $@"Based on the following code:
{code}

Context: {context}

Suggest a meaningful name for this code element. Respond with only the name, no explanation.";

                return await QueryOllamaAsync(prompt);
            }
            catch (Exception ex)
            {
                LogManager.LogError($"Error suggesting name with Ollama", ex);
                throw;
            }
        }

        private async Task<string> QueryOllamaAsync(string prompt)
        {
            var request = new
            {
                model = _config.Model,
                prompt = prompt,
                stream = false
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(request),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                $"{_config.Endpoint}/api/generate",
                content);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<dynamic>(json);

            return result["response"]?.ToString() ?? "";
        }

        private string BuildPrompt(string code, string analysisType)
        {
            return analysisType switch
            {
                "deobfuscation" => $@"Analyze the following obfuscated .NET IL code and suggest deobfuscation steps:
{code}

Provide specific techniques to reverse the obfuscation.",

                "control_flow" => $@"Analyze this control flow graph and identify the obfuscation pattern:
{code}

Is this code flattened, virtualized, or otherwise obfuscated?",

                "naming" => $@"Suggest meaningful names for variables, methods, or classes based on this code context:
{code}",

                _ => code
            };
        }
    }
}