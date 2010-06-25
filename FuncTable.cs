using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WorldCupBot
{
    class FuncTable : IFunction
    {
        public event FunctionOutput FunctionOutputHandler;

        private const string URL_TABLES
            = "http://cdnedge.bbc.co.uk/sport/hi/english/static/football/statistics/competition/700/ais_table.json";

        private char[] Tables
            = "ABCDEFGH".ToCharArray();

        [Trigger('.', "group", "Get a group table")]
        public void GetGroupTable(IrcTrigger trigger)
        {
            if(trigger.Arguments.Any())
            {
                char group = trigger.Message.ToUpper()[0];

                JObject jsonObject = JObject.Parse(new System.Net.WebClient().DownloadString(URL_TABLES));

                JArray tables = (JArray)jsonObject["footballTables"]["table"];

                foreach (var table in tables)
                {

                    int tableID = table["round"]["number"].Value<int>()-1;
                    
                    if (Tables[tableID]== group)
                    {
                        string[] lines = FormatGroupTable(Tables[tableID], (JObject)table).Split(new string[]{Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries);
                        
                        foreach(string line in lines) 
                        {
                            FunctionOutputHandler(Meebey.SmartIrc4net.SendType.Message, trigger.Channel, line);
                        }
                    }                

                }
            }
        }

        private string FormatGroupTable(char group, JObject obj)
        {
            StringBuilder groupTable = new StringBuilder();

            groupTable.AppendLine(Colour.BOLD + "Group " + group);

            string teamFormat = "{0}. {1} (W:{2} L:{3} D:{4} PTS:{5})";

            foreach (var team in obj["rows"]["row"])
            {
                groupTable.AppendLine(string.Format(teamFormat, team["rank"].Value<string>(),
                    team["team"]["name"].Value<string>(), team["won"].Value<string>(),
                    team["lost"].Value<string>(), team["drawn"].Value<string>(), team["points"].Value<string>()));
            }

            return groupTable.ToString();
        }

    }
}
