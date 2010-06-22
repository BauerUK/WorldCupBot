using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WorldCupBot
{
    public delegate void FunctionOutput(Meebey.SmartIrc4net.SendType type, string channel, string message);

    public interface IFunction
    {
        event FunctionOutput FunctionOutputHandler;
    }

    public class Trigger : Attribute 
    {
        public string TriggerValue { get; private set; }
        public char TriggerChar { get; private set; }
        public string TriggerHelp { get; private set; }

        public Trigger(char c, string val, string help)
        {
            this.TriggerChar = c;
            this.TriggerValue = val;
            this.TriggerHelp = help;
        }
    }
    
}
