using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using NetControlFlow.Logging;

namespace NetControlFlow.Utilities
{
    public class StringDecryptor
    {
        private readonly ModuleDef _module;
        private readonly Dictionary<string, string> _decryptCache;

        public StringDecryptor(ModuleDef module)
        {
            _module = module;
            _decryptCache = new Dictionary<string, string>();
        }

        public string DecryptString(MethodDef method, int callInstructionIndex)
        {
            try
            {
                var instructions = method.Body.Instructions;
                if (callInstructionIndex < 1 || callInstructionIndex >= instructions.Count)
                    return null;

                var callInstr = instructions[callInstructionIndex];
                if (callInstr.OpCode != OpCodes.Call)
                    return null;

                var calledMethod = callInstr.Operand as MethodDef;
                if (calledMethod == null)
                    return null;

                // Look backwards for the arguments
                var args = new List<object>();
                int idx = callInstructionIndex - 1;
                int argCount = calledMethod.Parameters.Count;

                while (idx >= 0 && args.Count < argCount)
                {
                    var instr = instructions[idx];

                    if (instr.OpCode == OpCodes.Ldc_I4)
                        args.Insert(0, (int)instr.Operand);
                    else if (instr.OpCode == OpCodes.Ldc_I4_S)
                        args.Insert(0, (int)instr.Operand);
                    else if (instr.OpCode == OpCodes.Ldstr)
                        args.Insert(0, (string)instr.Operand);
                    else if (instr.OpCode == OpCodes.Ldsfld)
                    {
                        // Try to resolve the field value
                        var field = instr.Operand as FieldDef;
                        if (field?.InitialValue != null)
                            args.Insert(0, field.InitialValue);
                    }

                    idx--;
                }

                if (args.Count < argCount)
                    return null;

                // Try to decrypt
                return ExecuteDecryption(calledMethod, args.ToArray());
            }
            catch (Exception ex)
            {
                LogManager.LogDebug($"Error decrypting string", 
                    new Dictionary<string, object> { { "Method", method?.Name }, { "Error", ex.Message } });
                return null;
            }
        }

        private string ExecuteDecryption(MethodDef method, object[] args)
        {
            try
            {
                // Create a simple execution context for common decryption patterns
                // This is a simplified version - real implementation would need IL emulation

                var cacheKey = $"{method.FullName}:{string.Join(",", args)}";
                if (_decryptCache.TryGetValue(cacheKey, out var cached))
                    return cached;

                // Pattern matching for common ConfuserEx decryption
                var result = TrySimpleXorDecrypt(args);
                
                if (result != null)
                {
                    _decryptCache[cacheKey] = result;
                    return result;
                }

                return null;
            }
            catch (Exception ex)
            {
                LogManager.LogDebug($"Decryption execution failed", 
                    new Dictionary<string, object> { { "Error", ex.Message } });
                return null;
            }
        }

        private string TrySimpleXorDecrypt(object[] args)
        {
            try
            {
                if (args.Length < 2)
                    return null;

                if (args[0] is string encryptedStr && args[1] is int key)
                {
                    var result = new string(encryptedStr.Select(c => 
                        (char)(c ^ key)).ToArray());
                    
                    // Basic validity check
                    if (result.Length > 0 && !char.IsControl(result[0]))
                        return result;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public void ClearCache()
        {
            _decryptCache.Clear();
            LogManager.LogDebug("String decryption cache cleared");
        }
    }
}