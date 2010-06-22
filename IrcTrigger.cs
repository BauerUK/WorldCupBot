using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Meebey.SmartIrc4net;
using System.Text.RegularExpressions;

namespace WorldCupBot
{
    public class IrcTrigger
    {
        public string Channel { get; private set; }
        public string Nick { get; private set; }
        public string Host { get; private set; }
        public string Message { get; private set; }
        public string Trigger { get; private set; }
        public string[] Arguments { get; private set; }
        
        private static string triggerFormat = @"^(?<trigger>{0})(?: )(?<args>.+)|(?<trigger>{0})$";

        public static bool Load(string trigger, IrcEventArgs e, out IrcTrigger ircTrigger)
        {
            ircTrigger = new IrcTrigger();
            
            Regex regex = new Regex(string.Format(triggerFormat, trigger), RegexOptions.IgnoreCase);
            
            Match m = regex.Match(e.Data.Message);
            
            if(m.Success)
            {
                ircTrigger.Channel = e.Data.Channel;
                ircTrigger.Nick = e.Data.Nick;
                ircTrigger.Host = e.Data.Host;
                ircTrigger.Message = m.Groups["args"].Value;
                ircTrigger.Arguments = m.Groups["args"].Value.Split(new char[] { ' ' });
                ircTrigger.Trigger = m.Groups["trigger"].Value;
                return true;
            }

            return false;
        }


    }
}
