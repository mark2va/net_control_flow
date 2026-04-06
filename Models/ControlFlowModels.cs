using dnlib.DotNet.Emit;
using System.Collections.Generic;

namespace NetControlFlow.Models
{
    public class ControlFlowGraph
    {
        public List<BasicBlock> Blocks { get; set; } = new List<BasicBlock>();
        public List<ControlFlowPattern> Patterns { get; set; } = new List<ControlFlowPattern>();
    }

    public class BasicBlock
    {
        public int Id { get; set; }
        public int StartOffset { get; set; }
        public List<Instruction> Instructions { get; set; } = new List<Instruction>();
        public List<BasicBlock> Successors { get; set; } = new List<BasicBlock>();
        public List<BasicBlock> Predecessors { get; set; } = new List<BasicBlock>();
        public BlockType Type { get; set; } = BlockType.Normal;
    }

    public enum BlockType
    {
        Normal,
        Condition,
        Exception,
        Loop,
        Return,
        Throw
    }

    public enum ControlFlowPattern
    {
        FlattenedSwitch,
        OpaquePredicates,
        DeadCode,
        Virtualized,
        Encrypted,
        JunkCode
    }

    public class DeobfuscationResult
    {
        public string OriginalPath { get; set; }
        public string OutputPath { get; set; }
        public int MethodsProcessed { get; set; }
        public int IssuesFixed { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
    }
}