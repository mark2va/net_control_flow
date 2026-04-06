# .NET Control Flow Deobfuscator

A comprehensive .NET assembly deobfuscator built with **dnlib**, featuring control flow analysis, pattern detection, and Ollama AI integration for advanced analysis.

## Features

### ✅ Core Capabilities

- **Control Flow Graph Analysis** - Reconstruct and analyze method CFGs
- **Obfuscator Detection** - Identify ConfuserEx, SmartAssembly, Dotfuscator, Eazfuscator, ILProtector
- **String Decryption** - Decrypt encrypted strings with caching
- **Control Flow Unflattening** - Restore linear code from switch-based flattened flow
- **Resource Extraction** - Extract and decompress embedded resources and assemblies
- **Metadata Cleaning** - Remove obfuscator signatures and suspicious attributes
- **Anti-Debug Removal** - Strip debugger.break() and int3 instructions
- **Batch Processing** - Process directories recursively
- **Comprehensive Logging** - All operations logged with variable tracking
- **Ollama AI Integration** - Optional local AI analysis for complex patterns

### 📊 Supported Obfuscators

- ConfuserEx (full support)
- SmartAssembly (detection + basic cleanup)
- Dotfuscator (detection + basic cleanup)
- Eazfuscator.NET (detection + basic cleanup)
- ILProtector (detection + basic cleanup)

## Requirements

- Windows 10+ or compatible OS
- .NET Framework 4.7.2+
- Visual Studio 2019+ (for compilation)
- Optional: Ollama (for AI features)

## Installation

### Clone Repository
```bash
git clone https://github.com/mark2va/net_control_flow.git
cd net_control_flow