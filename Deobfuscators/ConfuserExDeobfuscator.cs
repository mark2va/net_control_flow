using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using NetControlFlow.Logging;
using NetControlFlow.Utilities;

namespace NetControlFlow.Deobfuscators
{
    public class ConfuserExDeobfuscator : BaseDeobfuscator
    {
        private StringDecryptor _stringDecryptor;
        private ResourceExtractor _resourceExtractor;
        private ControlFlowUnflattener _cfUnflattener;

        public ConfuserExDeobfuscator(ModuleDef module) : base(module)
        {
            Name = "ConfuserEx";
            _stringDecryptor = new StringDecryptor(module);
            _resourceExtractor = new ResourceExtractor(module);
            _cfUnflattener = new ControlFlowUnflattener(module);
        }

        public override void Deobfuscate()
        {
            try
            {
                LogManager.LogOperation($"Starting {Name} deobfuscation");

                // Stage 1: Clean metadata
                CleanAttributes();
                CleanNamespaces();

                // Stage 2: Decrypt strings
                DecryptStrings();

                // Stage 3: Unflatten control flow
                UnflattenControlFlow();

                // Stage 4: Extract and decrypt resources
                ExtractResources();

                // Stage 5: Remove anti-debug/anti-decompile
                RemoveAntiDebugging();

                LogManager.LogSuccess($"{Name} deobfuscation completed");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"Error during {Name} deobfuscation", ex);
                throw;
            }
        }

        private void DecryptStrings()
        {
            try
            {
                int decryptedCount = 0;
                foreach (var type in Module.Types)
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

                LogManager.LogOperation($"Decrypted {decryptedCount} strings in {Name}");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"Error decrypting strings in {Name}", ex);
            }
        }

        private void UnflattenControlFlow()
        {
            try
            {
                int unflattenedCount = 0;
                foreach (var type in Module.Types)
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

                LogManager.LogOperation($"Unflattened {unflattenedCount} methods in {Name}");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"Error unflattening control flow in {Name}", ex);
            }
        }

        private void ExtractResources()
        {
            try
            {
                var extractedCount = _resourceExtractor.ExtractAll();
                LogManager.LogOperation($"Extracted {extractedCount} resources from {Name}");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"Error extracting resources from {Name}", ex);
            }
        }

        private void RemoveAntiDebugging()
        {
            try
            {
                int removedCount = 0;
                foreach (var type in Module.Types)
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

                LogManager.LogOperation($"Removed {removedCount} anti-debug instructions");
            }
            catch (Exception ex)
            {
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