using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using NetControlFlow.Logging;

namespace NetControlFlow.Utilities
{
    public class ResourceExtractor
    {
        private readonly ModuleDef _module;
        private readonly string _outputDir;

        public ResourceExtractor(ModuleDef module, string outputDir = "./extracted_resources")
        {
            _module = module;
            _outputDir = outputDir;

            if (!Directory.Exists(_outputDir))
                Directory.CreateDirectory(_outputDir);
        }

        public int ExtractAll()
        {
            int extractedCount = 0;

            try
            {
                // Extract embedded resources
                extractedCount += ExtractEmbeddedResources();

                // Extract embedded assemblies
                extractedCount += ExtractEmbeddedAssemblies();

                LogManager.LogOperation($"Extracted {extractedCount} resources total");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"Error extracting resources", ex);
            }

            return extractedCount;
        }

        private int ExtractEmbeddedResources()
        {
            int count = 0;

            try
            {
                foreach (var resource in _module.Resources.OfType<EmbeddedResource>())
                {
                    try
                    {
                        var data = resource.GetResourceData();
                        var filePath = Path.Combine(_outputDir, SanitizeFileName(resource.Name));

                        // Detect if it's compressed
                        if (IsGzipCompressed(data))
                        {
                            var decompressed = DecompressGzip(data);
                            File.WriteAllBytes(filePath + ".decompressed", decompressed);
                            count++;
                        }
                        else
                        {
                            File.WriteAllBytes(filePath, data);
                            count++;
                        }

                        LogManager.LogDebug($"Extracted resource: {resource.Name}");
                    }
                    catch (Exception ex)
                    {
                        LogManager.LogDebug($"Error extracting resource {resource.Name}", 
                            new Dictionary<string, object> { { "Error", ex.Message } });
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"Error extracting embedded resources", ex);
            }

            return count;
        }

        private int ExtractEmbeddedAssemblies()
        {
            int count = 0;

            try
            {
                // Look for embedded .NET assemblies in resources
                foreach (var resource in _module.Resources.OfType<EmbeddedResource>())
                {
                    try
                    {
                        var data = resource.GetResourceData();

                        // Check for .NET assembly signature (MZ header + PE signature)
                        if (IsValidAssembly(data))
                        {
                            var filePath = Path.Combine(_outputDir, 
                                SanitizeFileName(resource.Name) + ".dll");

                            File.WriteAllBytes(filePath, data);
                            count++;

                            LogManager.LogDebug($"Extracted embedded assembly: {resource.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogManager.LogDebug($"Error processing resource {resource.Name}", 
                            new Dictionary<string, object> { { "Error", ex.Message } });
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"Error extracting embedded assemblies", ex);
            }

            return count;
        }

        private bool IsValidAssembly(byte[] data)
        {
            if (data.Length < 64)
                return false;

            // Check for MZ header
            if (data[0] != 0x4D || data[1] != 0x5A)
                return false;

            // Check for PE signature at offset specified in header
            if (data.Length >= 64)
            {
                int peOffset = BitConverter.ToInt32(data, 0x3C);
                if (peOffset > 0 && peOffset < data.Length - 4)
                {
                    return data[peOffset] == 0x50 && data[peOffset + 1] == 0x45;
                }
            }

            return false;
        }

        private bool IsGzipCompressed(byte[] data)
        {
            if (data.Length < 2)
                return false;

            // Check for gzip magic number
            return data[0] == 0x1F && data[1] == 0x8B;
        }

        private byte[] DecompressGzip(byte[] data)
        {
            using (var input = new MemoryStream(data))
            using (var gzip = new GZipStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                gzip.CopyTo(output);
                return output.ToArray();
            }
        }

        private string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return new string(name
                .Replace(".", "_")
                .Where(c => !invalid.Contains(c))
                .ToArray());
        }
    }
}