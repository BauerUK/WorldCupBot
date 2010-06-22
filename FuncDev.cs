using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WorldCupBot
{
    class FuncDev : IFunction
    {
        public event FunctionOutput FunctionOutputHandler;

        [Trigger('.', "dev",
            "Various developer info")]
        private void Dev(IrcTrigger trigger)
        {
            if (trigger.Arguments.Any())
            {
                switch (trigger.Arguments[0].ToLower())
                {
                    case "about":
                        Output(trigger.Channel, "Zakumi is written in C#/.NET from the ground up and is based on the Meebey SmartIRC4Net framework");
                        break;

                    case "issue":
                        Output(trigger.Channel, "Report/track issues at http://github.com/BauerUK/WorldCupBot/issues or in #worldcup-dev");
                        break;

                    case "wiki":
                        Output(trigger.Channel, "WorldCupBot wiki: http://wiki.github.com/BauerUK/WorldCupBot/");
                        break;

                    case "git":
                        Output(trigger.Channel, "git repo: git@github.com:BauerUK/WorldCupBot.git");
                        break;

                    case "github":
                        Output(trigger.Channel, "WorldCupBot github page: http://github.com/BauerUK/WorldCupBot");
                        break;
                }
            }
            else
            {
                Output(trigger.Channel, "Join #worldcup-dev for developer information/bugs/feature reports, etc.");
            }
        }

        private void Output(string channel, string message)
        {
            if (FunctionOutputHandler != null)
            {
                FunctionOutputHandler(Meebey.SmartIrc4net.SendType.Message, channel, message);
            }
        }
    }
}
