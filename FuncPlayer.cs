﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HtmlAgilityPack;
using System.Net;

namespace WorldCupBot
{
    class FuncPlayer : IFunction
    {
        public event FunctionOutput FunctionOutputHandler;

        [Trigger('.', "player", "Gets a player details")]
        public void GetPlayer(IrcTrigger trigger)
        {

            string outputFormat = "{0} (#{1} for {2}) - Goals: {3} Fouls: {4} (" + 
            Colour.MakeColour(Colour.RED, Colour.RED) + "R" + Colour.NORMAL + ":{5} - " + 
            Colour.MakeColour(Colour.YELLOW, Colour.YELLOW) + "Y" + Colour.NORMAL + ":{6}) " + 
            "Shots [on target]: {7} [{8}] " + 
            "Passes [% complete]: {9} [{10}]";

            if (trigger.Arguments.Any())
            {
                Player p = searchPlayer(trigger.Message);

                if (p != null)
                {
                    Output(trigger.Channel, string.Format(outputFormat, p.Name, p.KitNumber, p.Team, p.Goals, p.Fouls,
                            p.Reds, p.Yellows, p.Shots, p.ShotsOnTarget, p.Passes, p.PassCompletion));

                }
                else
                {
                    Output(trigger.Channel, string.Format("No player by the name of {0} found", trigger.Message));
                }
            }
            else
            {
                Output(trigger.Channel, "No player specified");
            }
            
        }

        private void Output(string chan, string message)
        {
            if (FunctionOutputHandler != null)
            {
                FunctionOutputHandler(Meebey.SmartIrc4net.SendType.Message, chan, message);
            }
        }

        private Player searchPlayer(string query)
        {
            Player p = null;

            string url = @"http://www.fifa.com/worldcup/players/index.htmx?pn=" + query.Replace(" ", "+");

            string htmlResults = download(url);

            if (!htmlResults.Contains("No data available for specified filters"))
            {
                HtmlDocument results = new HtmlDocument();
                results.LoadHtml(htmlResults);

                string rel = "http://fifa.com" +
                    results.DocumentNode.SelectSingleNode("//ul[@class=\"featuredPl\"][1]/li[1]/div[@class=\"bgPlMedium\"]/a[1]").Attributes["href"].Value;

                rel = rel.Replace("/worldcup", "/worldcup/statistics");

                HtmlDocument playerDoc = new HtmlDocument();
                playerDoc.LoadHtml(download(rel));

                return Player.MakePlayer(playerDoc);
            }

            return p;
        }

        private string download(string url)
        {
            return new WebClient().DownloadString(url);
        }
    }

    class Player
    {
        public string Name { get; private set; }
        public string Team { get; private set; }
        public int KitNumber { get; private set; }
        public int Goals { get; private set; }
        public int Fouls { get; private set; }
        public int Yellows { get; private set; }
        public int Reds { get; private set; }
        public int Shots { get; private set; }
        public int ShotsOnTarget { get; private set; }
        public int Passes { get; private set; }
        public string PassCompletion { get; private set; }

        public static Player MakePlayer(HtmlDocument fifaDocument)
        {
            Player p = new Player();

            var mainStats = fifaDocument.DocumentNode.SelectNodes("//p[@class=\"val\"]");

            int goals = 0;
            int fouls = 0;
            int shots = 0;
            int passes = 0;
            int kitNumber = 0;

            int.TryParse(mainStats[0].InnerText, out goals);
            int.TryParse(mainStats[1].InnerText, out fouls);
            int.TryParse(mainStats[2].InnerText, out shots);
            int.TryParse(mainStats[3].InnerText, out passes);

            p.Goals = goals;
            p.Fouls = fouls;
            p.Shots = shots;
            p.Passes = passes;

            var playerInfo = fifaDocument.DocumentNode.SelectNodes("//a[@class=\"firstColor\"]");

            int.TryParse(playerInfo[0].InnerText, out kitNumber);

            p.KitNumber = kitNumber;

            p.Team = playerInfo[2].InnerText;
            p.Name = playerInfo[1].InnerText;

            var labels = fifaDocument.DocumentNode.SelectNodes("//div[@class=\"label\"]");

            Dictionary<string, string> stats = new Dictionary<string, string>();

            foreach (var label in labels)
            {
                stats.Add(label.InnerText.ToUpper(), label.NextSibling.InnerText);
            }


            int yellows = 0;
            int reds = 0;
            int shotsOnTarget = 0;
            string passCompetion = "0%";

            int.TryParse(stats["YELLOW CARDS"], out yellows);
            int.TryParse(stats["RED CARDS"], out reds);
            int.TryParse(stats["SHOTS (ON TARGET)"], out shotsOnTarget);

            passCompetion = stats["PASSES COMPLETION RATE"];

            p.Yellows = yellows;
            p.Reds = reds;
            p.ShotsOnTarget = shotsOnTarget;
            p.PassCompletion = passCompetion.Trim();

            return p;
        }

    }
}
