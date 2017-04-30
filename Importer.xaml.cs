using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using System.Windows.Media.Animation;
using Microsoft.VisualBasic;


namespace EHMProgressTracker
{
    /// <summary>
    /// Interaction logic for Importer.xaml
    /// </summary>
    public partial class Importer : Window
    {
        private Dictionary<string, int> HeaderIndexer = new Dictionary<string, int>();
        private List<Player> globaPlayersList = new List<Player>();
        private string[] date;  

        public Importer(List<Player> globalPlayersList, string[] date)
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            tbImport.IsReadOnly = true;
            globaPlayersList = globalPlayersList;
            this.date = date;
            lbDate.Content = Player.ComboToDateStr(date);
        }

        private void btBrowse_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog(); 
            dlg.DefaultExt = ".csv";
            dlg.Filter = "CSV Files (*.csv)|*.csv|Text Files (*.txt)|*.txt";
            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                string filename = dlg.FileName;
                tbImport.Text = filename;
                try
                {
                    LoadPlayers();
                }
                catch (Exception ex)
                {
                    utils.ShowError("Could not load players from csv: " + ex.Message);
                    utils.Log("Error loading players form CSV " + ex);
                    Close();
                }
            }
        }

        private void LoadPlayers()
        {
            List<string> raw = File.ReadAllLines(tbImport.Text).ToList();
            if (raw.Count() < 2)
            {
                utils.ShowError("Not enough lines in this file.");
                utils.Log("LoadPlayers: Not enough lines: " + raw.Count);
                Close();
                return;
            }
            List<string> header = raw[0].Replace(' ', '_').Split(';').ToList();

            // Add already used attributes
            List<string> attributes = new List<string>(dbHelper.playerAttributes);
            attributes.AddRange(dbHelper.goalieAttributes);

            // Other needed attributes
            attributes.Add("Name");
            attributes.Add("Id");


            // Check list header compatibility
            foreach (var attr in attributes)
            {                
                if (!header.Contains(attr))
                {
                    if (attr == "Ingame_Date")
                    {
                        continue;
                    }
                    utils.ShowError("Incompatible header line, missing attribute: " + attr);
                    utils.Log("Incompatible header line, missing attribute: " + attr);
                    Close();
                    break;
                }
            }

            HeaderIndexer.Clear();
            
            // Find the required attributes in the header line and store the index
            foreach (var attr in header)
            {
                if (attributes.Contains(attr))
                {
                    HeaderIndexer[attr] = header.IndexOf(attr);
                }
            }

            // Search player matches
            foreach (var line in raw)
            {
                string[] data = line.Split(';');
                foreach (var player in globaPlayersList)
                {
                    if (player.fullName == data[HeaderIndexer["Name"]])
                    {
                        ListBoxItem item = new ListBoxItem();
                        item.Tag = data;
                        item.Content = player;
                        lbPlayers.Items.Add(item);
                    }
                }
            }        
        }

        private void btImport_Click(object sender, RoutedEventArgs e)
        {
            List<string> attributes = new List<string>();
            foreach (ListBoxItem item in lbImport.Items)
            {
                string[] playerData = item.Tag as string[];
                Player player = item.Content as Player;

                if (player.playerType == PlayerType.player)
                {
                    attributes = new List<string>(dbHelper.playerAttributes);
                }
                if (player.playerType == PlayerType.goalie)
                {
                    attributes = new List<string>(dbHelper.goalieAttributes);
                }
            
                Snapshot s = new Snapshot(player.playerType);
                s.attributes["Ingame_Date"] = Player.ComboToDateStr(date);
                foreach (var attr in  attributes)
                {
                    if (attr == "Ingame_Date") continue;
                    try
                    {
                        s.attributes[attr] = playerData[HeaderIndexer[attr]];
                    }
                    catch(Exception ex)
                    {
                        utils.ShowError("Could not import " + player.fullName + ", " + ex.Message);
                        utils.Log("Could not import " + player.fullName + ", " + ex);
                    }
                }
                player.Snapshots.Clear();
                player.Snapshots.Add(s);
                dbHelper.PlayerAdd(player);
            }
            DialogResult = true;
            Close();
        }

        private void btDown_Click(object sender, RoutedEventArgs e)
        {
            if (lbPlayers.SelectedIndex > -1)
            {
                List<object> buffer = new List<object>();
                foreach (var item in lbPlayers.SelectedItems)
                {
                    buffer.Add(item);
                }
                foreach (var item in buffer)
                {
                    lbPlayers.Items.Remove(item);
                    lbImport.Items.Add(item);
                }
            }
        }

        private void btUp_Click(object sender, RoutedEventArgs e)
        {
            if (lbPlayers.SelectedIndex > -1)
            {
                List<object> buffer = new List<object>();
                foreach (var item in lbImport.SelectedItems)
                {
                    buffer.Add(item);
                }
                foreach (var item in buffer)
                {
                    lbImport.Items.Remove(item);
                    lbPlayers.Items.Add(item);
                }
            }
        }

        private void btAll_Click(object sender, RoutedEventArgs e)
        {
            List<object> buffer = new List<object>();
            foreach (var item in lbPlayers.Items)
            {
                buffer.Add(item);
            }
            foreach (var item in buffer)
            {
                lbPlayers.Items.Remove(item);
                lbImport.Items.Add(item);
            }
        }
    }
}
