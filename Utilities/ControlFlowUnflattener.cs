// ControlFlowUnflattener.cs

using System;
using System.Collections.Generic;

namespace Utilities
{
    public class ControlFlowUnflattener
    {
        private Dictionary<int, Action> switchCases;

        public ControlFlowUnflattener()
        {
            switchCases = new Dictionary<int, Action>();
        }

        public void AddCase(int caseNumber, Action action)
        {
            switchCases[caseNumber] = action;
        }

        public void ExecuteCase(int caseNumber)
        {
            if (switchCases.ContainsKey(caseNumber))
            {
                switchCases[caseNumber].Invoke();
            }
            else
            {
                throw new ArgumentException($"Case {caseNumber} does not exist.");
            }
        }
    }
}