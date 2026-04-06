using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using NetControlFlow.Logging;
using NetControlFlow.Models;

namespace NetControlFlow.Analysis
{
    public class ControlFlowAnalyzer
    {
        public ControlFlowGraph AnalyzeMethod(MethodDef method)
        {
            try
            {
                var cfg = new ControlFlowGraph();
                var body = method.Body;

                if (body == null || body.Instructions.Count == 0)
                    return cfg;

                // Create basic blocks
                var blocks = CreateBasicBlocks(body);
                cfg.Blocks = blocks;

                // Analyze edges
                AnalyzeEdges(blocks, body);

                // Detect patterns
                DetectControlFlowPatterns(cfg, method);

                var variables = new Dictionary<string, object>
                {
                    { "Method", method.FullName },
                    { "BlockCount", blocks.Count },
                    { "InstructionCount", body.Instructions.Count }
                };
                LogManager.LogAnalysis(method.Name, "ControlFlow", variables);

                return cfg;
            }
            catch (Exception ex)
            {
                LogManager.LogError($"Error analyzing method: {method.FullName}", ex);
                throw;
            }
        }

        private List<BasicBlock> CreateBasicBlocks(CilBody body)
        {
            var blocks = new List<BasicBlock>();
            var instructions = body.Instructions;
            var blockStarts = new HashSet<int> { 0 };

            // Find all branch targets and exception handler boundaries
            foreach (var instr in instructions)
            {
                if (instr.OpCode.FlowControl == FlowControl.Branch ||
                    instr.OpCode.FlowControl == FlowControl.Cond_Branch ||
                    instr.OpCode.FlowControl == FlowControl.Call)
                {
                    if (instr.Operand is Instruction target)
                        blockStarts.Add(target.Offset);

                    var nextIdx = instructions.IndexOf(instr) + 1;
                    if (nextIdx < instructions.Count)
                        blockStarts.Add(instructions[nextIdx].Offset);
                }
            }

            // Add exception handler boundaries
            foreach (var handler in body.ExceptionHandlers)
            {
                if (handler.TryStart != null)
                    blockStarts.Add(handler.TryStart.Offset);
                if (handler.HandlerStart != null)
                    blockStarts.Add(handler.HandlerStart.Offset);
            }

            var sortedOffsets = blockStarts.OrderBy(o => o).ToList();

            // Create blocks
            for (int i = 0; i < sortedOffsets.Count; i++)
            {
                var startOffset = sortedOffsets[i];
                var endOffset = i + 1 < sortedOffsets.Count ? sortedOffsets[i + 1] : int.MaxValue;

                var blockInstructions = instructions
                    .Where(instr => instr.Offset >= startOffset && instr.Offset < endOffset)
                    .ToList();

                if (blockInstructions.Count > 0)
                {
                    var block = new BasicBlock
                    {
                        Id = blocks.Count,
                        StartOffset = startOffset,
                        Instructions = blockInstructions
                    };
                    blocks.Add(block);
                }
            }

            return blocks;
        }

        private void AnalyzeEdges(List<BasicBlock> blocks, CilBody body)
        {
            foreach (var block in blocks)
            {
                if (block.Instructions.Count == 0)
                    continue;

                var lastInstr = block.Instructions.Last();
                var flowControl = lastInstr.OpCode.FlowControl;

                if (flowControl == FlowControl.Branch || flowControl == FlowControl.Cond_Branch)
                {
                    if (lastInstr.Operand is Instruction target)
                    {
                        var targetBlock = blocks.FirstOrDefault(b => b.StartOffset == target.Offset);
                        if (targetBlock != null)
                            block.Successors.Add(targetBlock);
                    }
                }

                if (flowControl != FlowControl.Return && 
                    flowControl != FlowControl.Branch &&
                    flowControl != FlowControl.Throw)
                {
                    var nextInstr = body.Instructions.FirstOrDefault(i => i.Offset > lastInstr.Offset);
                    if (nextInstr != null)
                    {
                        var nextBlock = blocks.FirstOrDefault(b => b.StartOffset == nextInstr.Offset);
                        if (nextBlock != null)
                            block.Successors.Add(nextBlock);
                    }
                }
            }
        }

        private void DetectControlFlowPatterns(ControlFlowGraph cfg, MethodDef method)
        {
            // Detect flattened switch patterns
            if (IsLikelyFlattenedSwitch(cfg))
            {
                cfg.Patterns.Add(ControlFlowPattern.FlattenedSwitch);
                LogManager.LogInfo($"Detected FlattenedSwitch pattern in {method.Name}");
            }

            // Detect opaque predicates
            if (HasOpaquePredicates(cfg))
            {
                cfg.Patterns.Add(ControlFlowPattern.OpaquePredicates);
                LogManager.LogInfo($"Detected OpaquePredicates pattern in {method.Name}");
            }

            // Detect dead code
            if (HasDeadCode(cfg))
            {
                cfg.Patterns.Add(ControlFlowPattern.DeadCode);
                LogManager.LogInfo($"Detected DeadCode pattern in {method.Name}");
            }
        }

        private bool IsLikelyFlattenedSwitch(ControlFlowGraph cfg)
        {
            // Check for many blocks with similar structure (switch flattening signature)
            return cfg.Blocks.Count > 10 && 
                   cfg.Blocks.Count(b => b.Successors.Count >= 2) > 5;
        }

        private bool HasOpaquePredicates(ControlFlowGraph cfg)
        {
            // Check for branches with constant conditions
            return cfg.Blocks.Any(b => 
            {
                var lastInstr = b.Instructions.LastOrDefault();
                return lastInstr?.OpCode == OpCodes.Brfalse || 
                       lastInstr?.OpCode == OpCodes.Brtrue;
            });
        }

        private bool HasDeadCode(ControlFlowGraph cfg)
        {
            // Check for unreachable blocks
            var reachable = new HashSet<BasicBlock>();
            TraverseReachable(cfg.Blocks.FirstOrDefault(), reachable);
            return reachable.Count < cfg.Blocks.Count;
        }

        private void TraverseReachable(BasicBlock block, HashSet<BasicBlock> visited)
        {
            if (block == null || visited.Contains(block))
                return;

            visited.Add(block);
            foreach (var successor in block.Successors)
                TraverseReachable(successor, visited);
        }
    }
}
