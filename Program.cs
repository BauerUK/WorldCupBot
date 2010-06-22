#define DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Meebey.SmartIrc4net;
using Nini.Config;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Reflection;

namespace WorldCupBot
{
    class Program
    {
        // general config items
        private static IConfigSource conf;
        private static IrcClient irc;

        // the IRC functions
        // TODO: this should be handed off to some dynamic loading
        private static List<IFunction> functions;
        
        static void Main(string[] args)
        {
            // load the configuration information first
            // since other things will rely on it.
            try
            {
                conf = new IniConfigSource(Path.Combine(Environment.CurrentDirectory, "bot.conf"));
            }
            catch (IOException ioe)
            {
                FatalError(ioe.Message);
            }
            catch (Exception ex)
            {
                FatalError(ex.Message);
            }

            // initialise our functions (the IRC commands).
            // let them set up and initialise before we connect
            functions = new List<IFunction>();
            InitFunctions();

            // and begin to handle their outputs now
            foreach (var f in functions)
            {
                f.FunctionOutputHandler += new FunctionOutput(f_FunctionOutputHandler);
            }

            // load the irc stuff
            irc = new IrcClient();

            // and handle some irc events
            irc.OnRawMessage += new IrcEventHandler(irc_OnRawMessage);
            irc.OnConnected += new EventHandler(irc_OnConnected);

            // listen for commands in the console
            // (but we don't do anything with it yet)
            new Thread(new ThreadStart(ReadLine)).Start();

            // now set up IRC and connect
            try
            {
                irc.SendDelay = 200;
                irc.ActiveChannelSyncing = true;
                irc.Encoding = System.Text.Encoding.UTF8;
                irc.AutoNickHandling = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("IRC Initialisation error: ");
                FatalError(ex.Message);
            }

            try
            {
                string network = (string)GetConf("bot", "network");
                int port = Convert.ToInt32(GetConf("bot", "port"));
                irc.Connect(network, port);
            }
            catch (ConnectionException e)
            {
                Console.WriteLine("Connection error: ");
                FatalError(e.Message);
            }

            string nick = "", realName = "", ident = "";

            try
            {
                nick = (string)GetConf("bot", "nick");
                realName = (string)GetConf("bot", "real_name");
                ident = (string)GetConf("bot", "ident");
            }
            catch (Exception e)
            {
                Console.WriteLine("Configuration error.");
                FatalError(e.Message);
            }

            try
            {
                irc.Login(nick, realName, 0, ident);
                irc.Listen(); // hang here until disconnect
                irc.Disconnect();
            }
            catch (ConnectionException ce)
            {
                Console.WriteLine("Login error: ");
                FatalError(ce.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unkonwn error: ");
                Console.Write(e);
                FatalError(e.Message);
            }    
        }


        static void f_FunctionOutputHandler(SendType type, string destination, string message)
        {
            irc.SendMessage(type, destination, message);
        }

        static void InitFunctions()
        {
            // functions go here
            functions.Add(new FuncSchedule());
        }

        static void irc_OnConnected(object sender, EventArgs e)
        {
            // identify
            irc.SendMessage(SendType.Message, "NickServ", "IDENTIFY " + (string)GetConf("bot", "password"), Priority.High);

            string[] chans = ((string)GetConf("bot", "chans")).Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string chan in chans)
            {
                irc.RfcJoin(chan);
            }
        }

        
        static void irc_OnRawMessage(object sender, IrcEventArgs e)
        {
            // for debugging purposes only
            System.Console.WriteLine(e.Data.RawMessage);

            // make sure we only respond to channel or query messages
            if (e.Data.Type == ReceiveType.ChannelMessage || e.Data.Type == ReceiveType.QueryMessage)
            {
                foreach (var f in functions)
                {

                    // basically, check all of the functions to see 
                    // if anything is matched by the incoming message
                    // this shit is all defined as attributes of methods
                    // in the function obejcts.
                    // if we have a match call invoke the method.
                    // we deal with the response later (asynchronously)

                    // this is probably expensive, but it'll do for now

                    Type t = f.GetType();

                    foreach (MethodInfo method in t.GetMethods())
                    {
                        object[] attributes = method.GetCustomAttributes(true);

                        var triggerAttributes = from attribute in attributes
                                                where attribute.GetType() == typeof(Trigger)
                                                select attribute;

                        foreach (Trigger triggerAttribute in triggerAttributes)
                        {
                            IrcTrigger ircTrigger = null;

                            if (IrcTrigger.Load(@"^\" + triggerAttribute.TriggerChar + triggerAttribute.TriggerValue, e, out ircTrigger))
                            {
                                method.Invoke(f, new object[]{ircTrigger});
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Shortcut to retreive bot config elements
        /// </summary>
        /// <param name="config">the ini config group</param>
        /// <param name="key">the key</param>
        /// <returns>the value</returns>
        private static object GetConf(string config, string key) {
            return conf.Configs[config].Get(key);
        }

        // shitty method of logging
        private static void FatalError(string message)
        {
            Console.WriteLine(message);

            //TODO: Log this message -- it is probably pretty important

            Environment.Exit(0);
        }

        // this does nothing right now, probably should handle basic commands
        // for general bot control.
        // if not, it should be handled (securely) via IRC
        static void ReadLine()
        {
            while (true)
            {
                Console.ReadLine();
            }
        }
    }
}
