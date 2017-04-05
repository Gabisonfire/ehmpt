using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace EHMProgressTracker
{
    public enum PlayerType { player, goalie };
    public enum EntryType { player, snapshot };

    public class Player
    {
        public int playerID { get; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string fullName { get; }
        public string birthDate { get; }
        public List<Snapshot> Snapshots { get; set; }
        public PlayerType playerType { get; }


        public Player(int playerID, string firstName, string lastName, PlayerType playerType, string birthDate)
        {
            this.playerID = playerID;
            this.firstName = firstName;
            this.lastName = lastName;
            this.birthDate = birthDate;
            fullName = firstName + " " + lastName;
            this.playerType = playerType;
            Snapshots = new List<Snapshot>();
        }
       
        public static string ComboToDateStr(string[] dates)
        {
            if(dates.Length < 3)
            {
                utils.ShowError("Cannot convert date, not enough information.");
                return null;
            }
            string d = dates[0];
            if (d.Length < 2) d = "0" + d;
            string m = DateTime.ParseExact(dates[1], "MMMM", CultureInfo.InvariantCulture).Month.ToString();
            if (m.Length < 2) m = "0" + m;
            string y = dates[2];
            return d + "-" + m + "-" + y;
        }

        public override string ToString()
        {
            return fullName;
        }

        public void OrderSnapshots()
        {
            Snapshots = Snapshots.OrderBy(s => DateTime.ParseExact(s.attributes["Ingame_Date"], "dd-MM-yyyy", CultureInfo.InvariantCulture)).Reverse().ToList();
        }

        public string GetAge()
        {
            if(Snapshots.Count < 1)
            {
                return "No snapshot";
            }
            else
            {
                OrderSnapshots();
                DateTime latestDate = DateTime.ParseExact(Snapshots[0].attributes["Ingame_Date"], "dd-MM-yyyy", CultureInfo.InvariantCulture);
                DateTime bDate = DateTime.ParseExact(birthDate, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                TimeSpan age = latestDate.Subtract(bDate);
                return Math.Floor((age.TotalDays / 365)).ToString();

            }
        }

        public string[] birthDateSplit()
        {
            return birthDate.Split('-');
        }
    }

    public class Snapshot
    {
        public Dictionary<string, string> attributes { get; }
        public DateTime timeStamp { get; }

        public Snapshot(PlayerType pt)
        {
            timeStamp = DateTime.Now;
            attributes = new Dictionary<string, string>();
            string[] defaultAttr = null;
            if(pt == PlayerType.player)
                defaultAttr = dbHelper.playerAttributes;
            if (pt == PlayerType.goalie)
                defaultAttr = dbHelper.goalieAttributes;

            // Init dict. with keys (attributes)
            foreach(string attr in defaultAttr)
            {
                attributes.Add(attr, null);
            }            
        }

        public override string ToString()
        {
            return attributes["Ingame_Date"];
        }

        public string GetAge(Player p)
        {
            DateTime latestDate = DateTime.ParseExact(attributes["Ingame_Date"], "dd-MM-yyyy", CultureInfo.InvariantCulture);
            DateTime bDate = DateTime.ParseExact(p.birthDate, "dd-MM-yyyy", CultureInfo.InvariantCulture);
            TimeSpan age = latestDate.Subtract(bDate);
            return Math.Floor((age.TotalDays / 365)).ToString();
        }

        public string GetAttribute(string att)
        {
            if (attributes.ContainsKey(att))
            {
                return attributes[att];
            } else
                return null;
        }

        public void SetAttribute(string attribute, string value)
        {
            if (attributes.ContainsKey(attribute))
            {
                attributes[attribute] = value;
            } else
            {
                throw new ArgumentException("The provided attribute does not exist. " + attribute);
            } 

        }

        public int AttributesTotal()
        {
            int total = 0;
            foreach(KeyValuePair<string, string> kp in attributes)
            {
                if (kp.Key == "Ingame_Date") { continue; }
                total += int.Parse(kp.Value);
            }
            return total;
        }
       
    }

}
