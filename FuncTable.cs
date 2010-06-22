using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WorldCupBot
{
    class FuncTable
    {
        private const string URL_TABLES
            = "http://cdnedge.bbc.co.uk/sport/hi/english/static/football/statistics/competition/700/ais_table.json";

        private void UpdateGroupTables()
        {
            JObject jsonObject = JObject.Parse(new System.Net.WebClient().DownloadString(URL_TABLES));

            JArray tables = (JArray)jsonObject["footballTables"]["table"];

            foreach (var table in tables)
            {
                string tableID = table["round"]["stage"]["number"].Value<string>();
                var teams = table["rows"]["row"];

                foreach (var team in teams)
                {
                    var teamDetails = team["team"];
                }

            }

        }

        public void GetGroupTable()
        {

        }

    }
}
