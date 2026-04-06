using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using NetControlFlow.Logging;

namespace NetControlFlow.Utilities
{
    public class ControlFlowUnflattener
    {
        private readonly ModuleDefMD _module;

        public ControlFlowUnflattener(ModuleDefMD module)
        {
            _module = module;
        }

        public bool IsFlattenedMethod(MethodDef method)
        {
            if (method == null || !method.HasBody || method.Body.Instructions.Count < 50)
                return false;

            // Look for state variable patterns
            var instrs = method.Body.Instructions;
            
            // Check for large switch statements (common in flattened CF)
            int switchCount = 0;
            foreach (var instr in instrs)
            {
                if (instr.OpCode == OpCodes.Switch)
                    switchCount++;
            }

            // Check for state field access
            bool hasStateAccess = instrs.Any(i => 
                i.OpCode == OpCodes.Ldfld || 
                i.OpCode == OpCodes.Stfld);

            // Check for goto-like branching
            int branchCount = 0;
            foreach (var instr in instrs)
            {
                if (instr.OpCode.Code >= Code.Br && instr.OpCode.Code <= Code.Leave)
                    branchCount++;
            }

            // Heuristic: many branches + switch or state access = likely flattened
            return (branchCount > 30 && (switchCount > 0 || hasStateAccess));
        }

        public void Unflatten(MethodDef method)
        {
            try
            {
                if (!IsFlattenedMethod(method))
                    return;

                LogManager.LogInfo($"Attempting to unflatten method: {method.FullName}");
                
                // Basic simplification - remove dead blocks and optimize
                SimplifyControlFlow(method);
                
                LogManager.LogInfo($"Unflattened method: {method.FullName}");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"Error unflattening method {method.FullName}", ex);
            }
        }

        private void SimplifyControlFlow(MethodDef method)
        {
            if (!method.HasBody) return;

            var body = method.Body;
            
            // Remove unreachable instructions
            RemoveDeadInstructions(body);
            
            // Optimize branches
            OptimizeBranches(body);
            
            // Update exception handlers if needed
            UpdateExceptionHandlers(body);
        }

        private void RemoveDeadInstructions(MethodBody body)
        {
            var reachable = new HashSet<Instruction>();
            var worklist = new Stack<Instruction>();
            
            if (body.Instructions.Count > 0)
                worklist.Push(body.Instructions[0]);

            while (worklist.Count > 0)
            {
                var instr = worklist.Pop();
                if (reachable.Contains(instr)) continue;
                
                reachable.Add(instr);
                
                // Follow branches
                if (instr.Operand is Instruction target)
                {
                    worklist.Push(target);
                }
                else if (instr.Operand is Instruction[] targets)
                {
                    foreach (var t in targets)
                        worklist.Push(t);
                }
                
                // Next instruction (if not unconditional branch)
                if (instr.OpCode.Code != Code.Br && instr.OpCode.Code != Code.Ret)
                {
                    var next = GetNextInstruction(body, instr);
                    if (next != null)
                        worklist.Push(next);
                }
            }

            // Remove unreachable
            var toRemove = body.Instructions.Where(i => !reachable.Contains(i)).ToList();
            foreach (var instr in toRemove)
            {
                body.Instructions.Remove(instr);
            }
        }

        private void OptimizeBranches(MethodBody body)
        {
            // Convert long branches to short where possible
            foreach (var instr in body.Instructions)
            {
                if (instr.Operand is Instruction target)
                {
                    var offset = target.Offset - instr.Offset;
                    if (Math.Abs(offset) < 127)
                    {
                        // Could use short form
                        instr.OpCode = GetShortForm(instr.OpCode);
                    }
                }
            }
        }

        private void UpdateExceptionHandlers(MethodBody body)
        {
            // Clean up exception handlers that reference removed instructions
            var validInstrs = new HashSet<Instruction>(body.Instructions);
            
            var badHandlers = body.ExceptionHandlers
                .Where(h => !validInstrs.Contains(h.TryStart) ||
                           !validInstrs.Contains(h.HandlerStart) ||
                           (h.FilterStart != null && !validInstrs.Contains(h.FilterStart)))
                .ToList();

            foreach (var handler in badHandlers)
            {
                body.ExceptionHandlers.Remove(handler);
            }
        }

        private Instruction? GetNextInstruction(MethodBody body, Instruction current)
        {
            var idx = body.Instructions.IndexOf(current);
            if (idx >= 0 && idx < body.Instructions.Count - 1)
                return body.Instructions[idx + 1];
            return null;
        }

        private OpCode GetShortForm(OpCode opCode)
        {
            return opCode.Code switch
            {
                Code.Br_S or Code.Br => OpCodes.Br_S,
                Code.Brfalse_S or Code.Brfalse => OpCodes.Brfalse_S,
                Code.Brtrue_S or Code.Brtrue => OpCodes.Brtrue_S,
                Code.Beq_S or Code.Beq => OpCodes.Beq_S,
                Code.Bne_Un_S or Code.Bne_Un => OpCodes.Bne_Un_S,
                Code.Blt_S or Code.Blt => OpCodes.Blt_S,
                Code.Blt_Un_S or Code.Blt_Un => OpCodes.Blt_Un_S,
                Code.Bgt_S or Code.Bgt => OpCodes.Bgt_S,
                Code.Bgt_Un_S or Code.Bgt_Un => OpCodes.Bgt_Un_S,
                Code.Ble_S or Code.Ble => OpCodes.Ble_S,
                Code.Ble_Un_S or Code.Ble_Un => OpCodes.Ble_Un_S,
                Code.Bge_S or Code.Bge => OpCodes.Bge_S,
                Code.Bge_Un_S or Code.Bge_Un => OpCodes.Bge_Un_S,
                Code.Leave_S or Code.Leave => OpCodes.Leave_S,
                _ => opCode
            };
        }
    }
}
