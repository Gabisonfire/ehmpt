using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SQLite;
using System.IO;

namespace EHMProgressTracker
{
    public class dbHelper
    {

        public static SQLiteConnection sqlc;
        public static string DEFAULT_DB = utils.Cfg("default_db");

        // Only possible attributes
        public static readonly string[] playerAttributes = {
                "Checking", "Deflections", "Deking", "Faceoffs", "Hitting", "Off_The_Puck", "Passing",
                "Pokecheck", "Positioning", "Slapshot", "Stickhandling", "Wristshot", "Aggression", "Anticipation",
                "Bravery", "Creativity", "Determination", "Flair", "Influence", "Teamwork", "Work_Rate",
                "Acceleration", "Agility", "Balance", "Speed", "Stamina", "Strength", "Ingame_Date"
            };

        public static readonly string[] goalieAttributes = {
            "Blocker", "Glove", "Passing", "Pokecheck", "Positioning", "Rebound_Control", "Recovery", "Reflexes", "Stickhandling",
            "Aggression", "Anticipation", "Bravery", "Creativity", "Determination", "Flair", "Influence", "Teamwork", "Work_Rate",
            "Acceleration", "Agility", "Balance", "Speed", "Stamina", "Strength", "Ingame_Date"
        };

        public static readonly string[] commonAttributes = {
            "First_Name", "Last_Name", "BirthDate"
        };

        // Search available dbs
        public static Dictionary<string, string> SearchDbFiles()
        {
            try
            {
                Dictionary<string, string> res = new Dictionary<string, string>();
                string[] files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.sqlite");
                if (files == null || files.Length == 0)
                {
                    SQLiteConnection.CreateFile(AppDomain.CurrentDomain.BaseDirectory + DEFAULT_DB);
                    res.Add(AppDomain.CurrentDomain.BaseDirectory + DEFAULT_DB.Replace(".sqlite", ""), DEFAULT_DB.Replace(".sqlite", ""));
                    res.Add("New...", "New...");
                    return res;
                }
                else
                {
                    DEFAULT_DB = files[0];
                    foreach (string file in files)
                    {
                        res.Add(file, Path.GetFileNameWithoutExtension(file));
                    }
                    res.Add("New...", "New...");
                    return res;
                }
            }
            catch (Exception ex)
            {
                utils.ShowError("Error occured scaning DB files. " + ex.Message, true);
                utils.Log("Error occured scaning DB files." + Environment.NewLine + ex.ToString());
                return null;
            }
        }

        // Checks if a db exists
        public static void DBPreCheck()
        {
            DBConnect();
            DBInit();
        }

        // Connect to the database
        private static void DBConnect()
        {
            try
            {
                sqlc = new SQLiteConnection("Data Source=" + DEFAULT_DB + ";Version=3;");
            }
            catch(Exception ex)
            {
                utils.Log("Cannot connect to the database: " +  ex.ToString());
                utils.ShowError("Could not connect to the database.", true);
            }
        }

        // Close and dispose of the current connection
        public static void CloseCurrent()
        {
            if(sqlc.State == System.Data.ConnectionState.Open)
            {
                sqlc.Close();
            }
            sqlc.Dispose();
        }

        private static void DBInit()
        {
            // Merge arrays and remove duplicates
            List<string> attribs = new List<string>();
            attribs.AddRange(playerAttributes);
            attribs.AddRange(goalieAttributes);
            attribs = attribs.Distinct().ToList();

            // Table containing all players information
            string query = @"CREATE TABLE IF NOT EXISTS Players(
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            playerID INTEGER, playerType TEXT,";

            foreach (string attr in commonAttributes)
            {
                query += attr + " TEXT,";
            }
            
            foreach(string attr in attribs)
            {
                if (attr == "Ingame_Date") { query += attr + " TEXT,"; }
                else
                {
                    query += attr + " INTEGER,";
                }
            }

            query = query.Remove(query.LastIndexOf(','), 1);
            query += ");";
            SimpleWriteQuery(query);
        }

        // Execute a simple query
        public static void SimpleWriteQuery(string query)
        {
            try
            {
                sqlc.Open();
                using (SQLiteCommand cmd = sqlc.CreateCommand())
                {
                    
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }
                sqlc.Close();
            }
            catch (Exception ex)
            {
                utils.ShowError("Error querying DB. " + ex.Message);
                utils.Log("Error querying DB." + Environment.NewLine + ex.ToString());
            }
        }

        // Multiple queries (write)
        public static void MultiQueries(string[] queries)
        {

            SQLiteTransaction trans = null;
            SQLiteCommand cmd = null;

            try
            {
                sqlc.Open();
                trans = sqlc.BeginTransaction();
                cmd = sqlc.CreateCommand();
                foreach (string query in queries)
                {
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }

                trans.Commit();
                sqlc.Close();
            }
            catch (SQLiteException ex)
            {
                utils.Log(ex.ToString());
                utils.ShowError("Error inserting records in the database");
                if (trans != null)
                {
                    try
                    {
                        trans.Rollback();
                    }
                    catch (Exception rex)
                    {                        
                        utils.Log("Rollback failed: " + rex.ToString());
                    }
                    finally
                    {
                        trans.Dispose();
                    }
                }
            }
            finally
            {
                if (cmd != null)
                {
                    cmd.Dispose();
                }

                if (trans != null)
                {
                    trans.Dispose();
                }                
            }
        }

        public static int GetNewPlayerId()
        {
            try
            {
                sqlc.Open();
                using (SQLiteCommand cmd = sqlc.CreateCommand())
                {
                    cmd.CommandText = "SELECT MAX(playerID) FROM Players;";
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader.IsDBNull(0))
                            {
                                sqlc.Close();
                                return 1;
                            }
                            int result = reader.GetInt32(0) + 1;
                            sqlc.Close();
                            return result;
                        }
                    }
                }

                sqlc.Close();
                return -1;
            }
            catch (Exception ex)
            {
                utils.ShowError("Error querying DB. " + ex.Message);
                utils.Log("Error querying DB -> getting playerID" + Environment.NewLine + ex.ToString());
                return -1;
            }

        }

        // Read all players from DB.
        public static List<Player> ReadAllPlayers()
        {
            List<Player> result = new List<Player>();
            PlayerType pt;
            try
            {
                sqlc.Open();
                using (SQLiteCommand cmd = sqlc.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM Players;";
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            switch (reader["playerType"].ToString())
                            {
                                case ("goalie"):
                                    pt = PlayerType.goalie;
                                    break;
                                case ("player"):
                                    pt = PlayerType.player;
                                    break;
                                default:
                                    pt = PlayerType.player;
                                    break;
                            }
                            Player p = new Player(int.Parse(reader["playerID"].ToString()), reader["First_Name"].ToString()
                                , reader["Last_Name"].ToString(), pt, reader["BirthDate"].ToString());

                            p.Snapshots.Add(new Snapshot(p.playerType));
                            Snapshot tempSnapshot = new Snapshot(p.playerType);
                            foreach (KeyValuePair<string, string> attr in tempSnapshot.attributes)
                            {
                                p.Snapshots[0].attributes[attr.Key] = reader[attr.Key].ToString();
                            }
                            result.Add(p);
                        }
                    }
                }

                sqlc.Close();
                return result;
            }
            catch (Exception ex)
            {                
                utils.Log("Could not read players from DB -> ReadALL" + Environment.NewLine + ex.ToString());
                utils.ShowError("Could not read players from DB. " + ex.Message, true);
                return null;
            }
        }

        public static void PlayerAdd(Player player)
        {
            string query = "";
            string values = "";
            query = "INSERT INTO Players (" +
                "playerID, playerType, First_Name, Last_Name, BirthDate,";
            values = ") VALUES ('" +
                player.playerID + "','" + player.playerType.ToString() + "',\"" + player.firstName + "\",\"" + player.lastName + "\",'" + player.birthDate + "','";
            foreach (KeyValuePair<string,string> attr in player.Snapshots[0].attributes)
            {
                query += attr.Key + ", ";
            }
            foreach (KeyValuePair<string, string> attr in player.Snapshots[0].attributes)
            {
                values += attr.Value + "', '";
            }
            values = values.Remove(values.LastIndexOf(','), 1);
            values = values.Remove(values.LastIndexOf('\''), 1);
            query = query.Remove(query.LastIndexOf(','), 1);
            values += ");";
            query = query + values;
            SimpleWriteQuery(query);
        }

        public static void PlayerRemove(Player player)
        {
            string query = "DELETE FROM Players WHERE playerID = '" + player.playerID + "';";
            SimpleWriteQuery(query);
        }

        public static void SnapshotRemove(Snapshot snapshot, Player player)
        {
            string query = "DELETE FROM Players WHERE Ingame_Date = '" + snapshot.attributes["Ingame_Date"] + "' AND playerID = '"+ player.playerID + "';";
            SimpleWriteQuery(query);
        }

        public static void EditName(Player player, string first, string last)
        {
            string query = "";
            query = "UPDATE Players SET First_Name = \"" + first + "\", Last_Name = \"" + last + "\" WHERE playerID = " + player.playerID + ";";
            SimpleWriteQuery(query);
        }

        public static List<Player> MergeSnapshots(List<Player> players)
        {
            List<Player> FinalList = new List<Player>();
            // Complete list to smaller lists by identical players (IDs)
            List<List<Player>> result = players
                .GroupBy(u => u.playerID)
                .Select(grp => grp.ToList())
                .ToList();
                        
            foreach(List<Player> l in result)
            {
                // Pick the first player (all the same anyways)
                Player newPlayer = l.First();                                
                foreach(Player p in l) // For all player, add snapshots to the temp
                {
                    newPlayer.Snapshots.Add(p.Snapshots.First());
                }
                newPlayer.Snapshots.RemoveAt(0); // Remove the first snapshot (duplicate)
                FinalList.Add(newPlayer); // add the temp
            }
            return FinalList;
        }

    }
}
