using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Meebey.SmartIrc4net;
using System.Data;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net;

namespace WorldCupBot
{
    /// <summary>
    /// IRC Function:
    ///     World Cup Schedule
    ///     
    /// </summary>
    class FuncSchedule : IFunction
    {
        public event FunctionOutput FunctionOutputHandler;

        private Data d;
        private System.Timers.Timer update;
        private const string URL_JSON_SCORES = "http://cdnedge.bbc.co.uk/sport/hi/english/static/football/statistics/competition/700/live_scores_summary.json";
        
        public FuncSchedule()
        {
            const int SECOND = 1000;
            const int MINUTE = SECOND * 60;
            
            this.d = new Data();
            update = new System.Timers.Timer(MINUTE * 1);
            update.Elapsed += new System.Timers.ElapsedEventHandler(update_Elapsed);
            update.Start();
            CheckScores();
        }

        private void update_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            CheckScores();
        }

        private void CheckScores()
        {
            using (var wc = new WebClient())
            {
                string json = wc.DownloadString(URL_JSON_SCORES);

                JObject o = JObject.Parse(json);

                JArray allMatches = (JArray)o["competition"][0]["match"];

                var liveMatches = from match in allMatches
                                  where (string)match["status"] != "FIXTURE"
                                  select match;

                foreach (var liveMatch in liveMatches)
                {
                    var homeTeam = liveMatch["homeTeam"];
                    int scoreA = homeTeam["score"].Value<int>();

                    var awayTeam = liveMatch["awayTeam"];
                    int scoreB = awayTeam["score"].Value<int>();

                    string broadcastUrl = (string)liveMatch["broadcastOnline"];
                    
                    int gameID = int.Parse(broadcastUrl.Substring(broadcastUrl.Length - 2,2));

                    Game g = d.GetGame(gameID);

                    if (g != null)
                    {

                        if (g.TeamAScore != scoreA || g.TeamBScore != scoreB)
                        {
                            // TODO: announce
                        }

                        this.d.UpdateScore(gameID, scoreA, scoreB);
                    }

                    
                }

            }
        }

        [Trigger('.', "n(ext)?g(ame)?", 
            "<b>.nextgame|nextg|ngame|ng [<u>TEAM</u>]</b><br>Returns: how long until the next game. If a <u>TEAM</u> is given, how long until the next game for that team.")]
        public void Next(IrcTrigger trigger)
        {
            string responseFormat = "Next Game: {0} vs. {1} - Kick off {2}";

            string response = null;

            Game g = null;
            
            if (!string.IsNullOrEmpty(trigger.Message))
            {
                g = d.GetNextGame(trigger.Message);
            }
            else
            {
                g = d.GetNextGame();
            }

            if (g != null)
            {
                response = string.Format(responseFormat, g.TeamA.Name, g.TeamB.Name, Helpers.Time.ReadableDifference(g.DateTime));
            }
            else
            {
                response = "No games found.";
            }

            if (FunctionOutputHandler != null && response != null)
            {
                FunctionOutputHandler(SendType.Message, trigger.Channel, response);
            }
        }

        [Trigger('.',"games", "<b>.games</b><br>Lists all of the games being played on a given date.")]
        public void Today(IrcTrigger trigger)
        {
            string responseFormat = "Games for {0}: {1}";
            string response = null;

            DateTime searchFor = new DateTime();

            bool success = false;

            
            if (trigger.Message != string.Empty)
            {
                try
                {
                    string message = trigger.Message.ToLower().Replace("tomorrow", "in 1 day");
                    var d = Helpers.Time.ParseDate(message);
                    searchFor = d.Dates.Last();
                    success = true;
                }
                catch
                {
                    success = false;
                }                
            }
            else
            {
                searchFor = DateTime.UtcNow;
                success = true;
            }

            if (success)
            {
                var games = d.GetGamesByDate(searchFor);

                if (games.Any())
                {
                    
                    List<string> gameList = games.Select(g =>
                                string.Concat("[", Colour.BOLD, g.DateTime.AddHours(2).ToShortTimeString(), Colour.NORMAL, "] ", g.TeamA.Name,
                                (g.HasScore ? (string.Concat(" ", g.TeamAScore, " - ", g.TeamBScore , " ") ) : " v "), g.TeamB.Name)).ToList();

                    string lastGame = gameList[gameList.Count - 1];

                    gameList[gameList.Count - 1] = "and " + lastGame;

                    response = string.Format(responseFormat, searchFor.ToShortDateString(),
                        string.Join("; ", gameList));
                }
                else
                {
                    response = "No games for " + searchFor.ToShortDateString();
                }
            }
            else
            {
                response = "Unable to find games for that date. Did you enter a real date?";
            }
             
            if (FunctionOutputHandler != null)
            {
                FunctionOutputHandler(SendType.Message, trigger.Channel, response);
            }
        }
               

    }

    
}
