using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using MySql.Data;
using MySql.Data.MySqlClient;
using Nini.Config;
using Nini;

namespace WorldCupBot
{
    class Data
    {
        private MySqlConnection SQL;
        private IConfigSource config;

        private static class DataStrings
        {

            public const string CONECTION = "SERVER={0}; DATABASE={1}; UID={2}; PASSWORD={3}";
            public const string SELECT_TEAM = "SELECT * FROM `teams` WHERE `id` = @id";
            public const string SEARCH_TEAM = "SELECT * FROM `teams` WHERE `code` = UCASE(@query) OR UCASE(`name`) LIKE UCASE(@query) LIMIT 1;";
            public const string SELECT_NEXT_GAME = "SELECT * FROM `schedule` WHERE `dateTime` > @now ORDER BY `dateTime` ASC";
            public const string SELECT_NEXT_TEAM_GAME = "SELECT * FROM `schedule` WHERE `dateTime` > @now AND (`teamA`=@team OR `teamB`=@team) ORDER BY `dateTime` LIMIT 1";
            public const string SELECT_GAME_BY_DATE = "SELECT * FROM `schedule` WHERE `dateTime` < @tonight AND `dateTime` > @morning";
            public const string UPDATE_SCORE = "UPDATE `schedule` SET `teamA_score`=@scoreA, `teamB_score`=@scoreB WHERE `id`=@id";
            public const string SELECT_GAME = "SELECT * FROM `schedule` WHERE `id`=@id";
        }

        public Data()
        {
            this.config = new IniConfigSource("data.conf");

            string server = config.Configs["data"].GetString("server");
            string database = config.Configs["data"].GetString("database");
            string uid = config.Configs["data"].GetString("uid");
            string password = config.Configs["data"].GetString("password");

            this.SQL = SQL = new MySqlConnection
                (string.Format(DataStrings.CONECTION, server, database, uid, password));

            Open();
        }

        ~Data()
        {
            Close();
        }

        private void Open()
        {
            SQL.Open();
        }

        private void Close()
        {
            SQL.Close();
        }

        /// <summary>
        /// Pulls games for a specific date/day
        /// </summary>
        /// <param name="date">The date to search</param>
        /// <returns>Array of Games</returns>
        public Game[] GetGamesByDate(DateTime date)
        {
            List<Game> games = new List<Game>();

            using (var c = MakeTextCommand(DataStrings.SELECT_GAME_BY_DATE))
            {
                DateTime tonight = new DateTime(date.Year, date.Month, date.Day, 23, 59, 59);
                DateTime morning = new DateTime(date.Year, date.Month, date.Day, 00, 00, 01);

                c.Parameters.AddWithValue("@tonight", tonight.Ticks);
                c.Parameters.AddWithValue("@morning", morning.Ticks);

                using (var d = ExecuteData(c))
                {
                    if (d.Rows.Count > 0)
                    {
                        foreach (DataRow r in d.Rows)
                        {
                            games.Add(MakeGame(r));
                        }
                    }
                }

            }

            return games.ToArray();
        }

        /// <summary>
        /// Get a Team by the team ID
        /// </summary>
        /// <param name="id">The team ID</param>
        /// <returns>The team object</returns>
        public Team GetTeamByID(int id)
        {
            Team team = null;
            
            using (var c = MakeTextCommand(DataStrings.SELECT_TEAM))
            {
                c.Parameters.AddWithValue("@id", id);

                using (var d = ExecuteData(c))
                {
                    if (d.Rows.Count > 0)
                    {
                        var r = d.Rows[0];
                        team = MakeTeam(r);
                    }
                }                
            }

            return team;
        }

        /// <summary>
        /// Get the next scheduled game(s)
        /// </summary>
        /// <returns>The next scheduled game(s), or an empty array if none exist</returns>
        public Game[] GetNextGames()
        {
            List<Game> g = new List<Game>();

            using (var c = MakeTextCommand(DataStrings.SELECT_NEXT_GAME))
            {
                c.Parameters.AddWithValue("@now", DateTime.UtcNow.Ticks);

                using (var d = ExecuteData(c))
                {
                    if (d.Rows.Count > 0)
                    {
                        long nextGameTime = d.Rows[0].Field<Int64>("dateTime");
                        
                        foreach (DataRow r in d.Rows)
                        {
                            if (r.Field<Int64>("dateTime") == nextGameTime)
                            {
                                g.Add(MakeGame(r));
                            }
                        }

                    }
                }
            }

            return g.ToArray();
        }

        /// <summary>
        /// Search for a team by name
        /// </summary>
        /// <param name="query">The query string to search</param>
        /// <returns>Team object if succesfull, or null if not</returns>
        public Team SearchTeam(string query)
        {
            Team t = null;

            using (var c = MakeTextCommand(DataStrings.SEARCH_TEAM))
            {
                c.Parameters.AddWithValue("@query", query.Replace(' ', '%'));

                using (var d = ExecuteData(c))
                {
                    if (d.Rows.Count > 0)
                    {
                        var r = d.Rows[0];
                        t = this.MakeTeam(r);
                    }
                }
            }

            return t;
        }

        /// <summary>
        /// Get the next scheduled game for a specific team
        /// </summary>
        /// <param name="teamName">The team name to search within</param>
        /// <returns>The next game for the given team</returns>
        public Game[] GetNextTeamGame(string teamName)
        {
            List<Game> g = new List<Game>();

            Team t = SearchTeam(teamName);
            
            if (t != null)
            {
                using (var c = MakeTextCommand(DataStrings.SELECT_NEXT_TEAM_GAME))
                {
                    c.Parameters.AddWithValue("@now", DateTime.UtcNow.Ticks);
                    c.Parameters.AddWithValue("@team", t.ID);

                    using (var d = ExecuteData(c))
                    {
                        if (d.Rows.Count > 0)
                        {
                            var r = d.Rows[0];
                            g.Add(MakeGame(r));
                        }
                    }
                }
            }
            return g.ToArray();
        }

        /// <summary>
        /// Get a game by ID
        /// </summary>
        /// <param name="id">The game ID</param>
        /// <returns>The matching game is succesful, null if not</returns>
        public Game GetGame(int id)
        {
            Game g = null;
            using (var c = MakeTextCommand(DataStrings.SELECT_GAME))
            {
                c.Parameters.AddWithValue("@id", id);
                using (var dt = ExecuteData(c))
                {
                    if (dt.Rows.Count > 0)
                    {
                        g = MakeGame(dt.Rows[0]);
                    }
                }
            }
            return g;
        }

        /// <summary>
        /// Update the score of a game
        /// </summary>
        /// <param name="id">The game ID</param>
        /// <param name="scoreA">The home team score</param>
        /// <param name="scoreB">The away team score</param>
        public void UpdateScore(int id, int scoreA, int scoreB)
        {
            using (var c = MakeTextCommand(DataStrings.UPDATE_SCORE))
            {
                Game g = GetGame(id);

                c.Parameters.AddWithValue("@id", id);
                c.Parameters.AddWithValue("@scoreA", scoreA);
                c.Parameters.AddWithValue("@scoreB", scoreB);

                if (g != null)
                {
                    if (g.HasScore)
                    {
                        if (g.TeamAScore != scoreA || g.TeamBScore != scoreB)
                        {
                            c.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        c.ExecuteNonQuery();
                    }
                }
                
            }
        }

        /// <summary>
        /// Produce a Game object from the MySQL data row
        /// </summary>
        private Game MakeGame(DataRow r)
        {
            Team teamA = this.GetTeamByID(Convert.ToInt32(r["teamA"].ToString()));
            int teamAscore = Convert.ToInt32(r["teamA_score"]);
            int teamBscore = Convert.ToInt32(r["teamB_score"]);
            Team teamB = this.GetTeamByID(Convert.ToInt32(r["teamB"].ToString()));

            DateTime dateTime = new DateTime(Convert.ToInt64(r["dateTime"].ToString()));

            Game g = new Game(teamA, teamAscore, teamB, teamBscore, dateTime, r["venue"].ToString());

            return g;
        }

        /// <summary>
        /// Make a team object
        /// </summary>
        private Team MakeTeam(DataRow r)
        {
            Team t = new Team(Convert.ToInt32(r["id"]), r["name"].ToString(), r["code"].ToString());
            return t;
        }

        /// <summary>
        /// Make a text command
        /// </summary>
        private MySqlCommand MakeTextCommand(string text)
        {
            MySqlCommand command = this.SQL.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = text;
            return command;
        }

        /// <summary>
        /// Execute some command and return a data table
        /// </summary>
        private DataTable ExecuteData(MySqlCommand c)
        {
            DataTable dt = new DataTable();
            dt.Load(c.ExecuteReader());
            return dt;
        }

    }


    public class Team
    {
        public int ID { get; private set; }
        public string Name { get; private set; }
        public string Code { get; private set; }

        public Team(int id, string name, string code) 
        {
            this.ID = id;
            this.Name = name;
            this.Code = code;
        }
    }

    public class Game
    {
        public Team TeamA { get; private set; }
        public Team TeamB { get; private set; }
        public int TeamAScore { get; private set; }
        public int TeamBScore { get; private set; }
        public bool HasScore { get; private set; }
        public DateTime DateTime { get; private set; }
        public String Venue { get; private set; }

        public Game(Team teamA, int teamAscore, Team teamB, int teamBscore, DateTime dateTime, String venue)
        {
            this.TeamA = teamA;
            this.TeamB = teamB;
            this.DateTime = dateTime;
            this.Venue = venue;
            this.TeamAScore = teamAscore;
            this.TeamBScore = teamBscore;
            
            if (teamAscore == -1 || teamBscore == -1)
            {
                this.HasScore = false;
            }
            else
            {
                this.HasScore = true;
            }
        }

    }
}
