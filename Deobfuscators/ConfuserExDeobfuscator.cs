using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using NetControlFlow.Logging;
using NetControlFlow.Utilities;
using NetControlFlow.Models;
using NetControlFlow.Config;

namespace NetControlFlow.Deobfuscators
{
    public class ConfuserExDeobfuscator : BaseDeobfuscator
    {
        private StringDecryptor _stringDecryptor;
        private ResourceExtractor _resourceExtractor;
        private ControlFlowUnflattener _cfUnflattener;

        public ConfuserExDeobfuscator()
        {
            Name = "ConfuserEx";
            SupportedObfuscators.Add(ObfuscatorType.ConfuserEx);
        }

        protected override void Deobfuscate(ModuleDefMD module, DeobfuscatorConfig config, DeobfuscationResultInternal result)
        {
            try
            {
                LogManager.LogInfo($"Starting {Name} deobfuscation");
                
                _stringDecryptor = new StringDecryptor(module);
                _resourceExtractor = new ResourceExtractor(module);
                _cfUnflattener = new ControlFlowUnflattener(module);

                // Run base cleaning first
                base.Deobfuscate(module, config, result);

                // Stage 2: Decrypt strings
                if (config.EnableStringDecryption)
                {
                    DecryptStrings(module, result);
                }

                // Stage 3: Unflatten control flow
                if (config.EnableControlFlowUnflattening)
                {
                    UnflattenControlFlow(module, result);
                }

                // Stage 4: Extract and decrypt resources
                if (config.EnableResourceExtraction)
                {
                    ExtractResources(module, result);
                }

                // Stage 5: Remove anti-debug/anti-decompile
                RemoveAntiDebugging(module, result);

                LogManager.LogInfo($"{Name} deobfuscation completed");
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Error during {Name} deobfuscation: {ex.Message}");
                LogManager.LogError($"Error during {Name} deobfuscation", ex);
                throw;
            }
        }

        private void DecryptStrings(ModuleDefMD module, DeobfuscationResultInternal result)
        {
            try
            {
                int decryptedCount = 0;
                foreach (var type in module.Types)
                {
                    foreach (var method in type.Methods)
                    {
                        if (method.Body == null || !method.Body.HasInstructions)
                            continue;

                        var instructions = method.Body.Instructions;
                        for (int i = 0; i < instructions.Count; i++)
                        {
                            if (instructions[i].OpCode == OpCodes.Call)
                            {
                                if (instructions[i].Operand is MethodDef calledMethod)
                                {
                                    if (IsStringDecryptionMethod(calledMethod))
                                    {
                                        // Attempt to decrypt
                                        var decrypted = _stringDecryptor.DecryptString(method, i);
                                        if (decrypted != null)
                                        {
                                            // Replace with ldstr
                                            instructions[i].OpCode = OpCodes.Ldstr;
                                            instructions[i].Operand = decrypted;
                                            decryptedCount++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                result.IssuesFixed += decryptedCount;
                result.MethodsProcessed += decryptedCount > 0 ? 1 : 0;
                LogManager.LogInfo($"Decrypted {decryptedCount} strings in {Name}");
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"Error decrypting strings: {ex.Message}");
                LogManager.LogError($"Error decrypting strings in {Name}", ex);
            }
        }

        private void UnflattenControlFlow(ModuleDefMD module, DeobfuscationResultInternal result)
        {
            try
            {
                int unflattenedCount = 0;
                foreach (var type in module.Types)
                {
                    foreach (var method in type.Methods)
                    {
                        if (method.Body == null)
                            continue;

                        if (_cfUnflattener.IsFlattenedMethod(method))
                        {
                            _cfUnflattener.Unflatten(method);
                            unflattenedCount++;
                        }
                    }
                }

                result.IssuesFixed += unflattenedCount;
                result.MethodsProcessed += unflattenedCount;
                LogManager.LogInfo($"Unflattened {unflattenedCount} methods in {Name}");
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"Error unflattening control flow: {ex.Message}");
                LogManager.LogError($"Error unflattening control flow in {Name}", ex);
            }
        }

        private void ExtractResources(ModuleDefMD module, DeobfuscationResultInternal result)
        {
            try
            {
                var extractedCount = _resourceExtractor.ExtractAll();
                result.IssuesFixed += extractedCount;
                LogManager.LogInfo($"Extracted {extractedCount} resources from {Name}");
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"Error extracting resources: {ex.Message}");
                LogManager.LogError($"Error extracting resources from {Name}", ex);
            }
        }

        private void RemoveAntiDebugging(ModuleDefMD module, DeobfuscationResultInternal result)
        {
            try
            {
                int removedCount = 0;
                foreach (var type in module.Types)
                {
                    foreach (var method in type.Methods)
                    {
                        if (method.Body == null || !method.Body.HasInstructions)
                            continue;

                        var instructions = method.Body.Instructions;
                        var toRemove = new List<Instruction>();

                        for (int i = 0; i < instructions.Count; i++)
                        {
                            // Remove debugger.break()
                            if (instructions[i].OpCode.Name == "call")
                            {
                                if (instructions[i].Operand is MethodDef mdef &&
                                    mdef.Name == "Break" &&
                                    mdef.DeclaringType?.Name == "Debugger")
                                {
                                    toRemove.Add(instructions[i]);
                                    removedCount++;
                                }
                            }

                            // Remove int3 instructions
                            if (instructions[i].OpCode.Name == "int3")
                            {
                                toRemove.Add(instructions[i]);
                                removedCount++;
                            }
                        }

                        foreach (var instr in toRemove)
                            instructions.Remove(instr);
                    }
                }

                result.IssuesFixed += removedCount;
                LogManager.LogInfo($"Removed {removedCount} anti-debug instructions");
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"Error removing anti-debug code: {ex.Message}");
                LogManager.LogError($"Error removing anti-debug code", ex);
            }
        }

        private bool IsStringDecryptionMethod(MethodDef method)
        {
            if (method == null || method.Body == null)
                return false;

            // ConfuserEx string decryption methods typically have:
            // - Parameters: int/uint, int/uint (for key/index)
            // - Return type: string
            // - Complex IL with XOR operations

            if (method.ReturnType.FullName != "System.String")
                return false;

            var parameters = method.Parameters;
            if (parameters.Count < 1 || parameters.Count > 3)
                return false;

            var instrs = method.Body.Instructions;
            var hasXorOps = instrs.Any(i => i.OpCode.Name.Contains("xor"));
            var hasLdsfld = instrs.Any(i => i.OpCode == OpCodes.Ldsfld);

            return hasXorOps || hasLdsfld;
        }
    }
}