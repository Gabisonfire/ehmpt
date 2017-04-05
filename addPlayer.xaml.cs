using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace EHMProgressTracker
{
   
   

    public partial class addPlayer : Window
    {
        private static List<TextBox> tbsAttrCollection = new List<TextBox>();       // Keep track of the textboxes for attributes
        private static List<TextBox> tbsCommonCollection = new List<TextBox>();     // Keep track of the textboxes for common information
        private static List<ComboBox> cbsCollection = new List<ComboBox>();         // Keep track of dropdowns
        public static readonly string[] Days = Enumerable.Range(1, 31).Select(x => x.ToString()).ToArray();
        public static readonly string[] Years = Enumerable.Range(1960, 1000).Select(x => x.ToString()).ToArray();


        // Form constructor
        public addPlayer(PlayerType pt, bool isSnapshot = false, Player SelectedPlayer = null, int[] ingameDate = null)
        {
            InitializeComponent();
            GenerateLabels(this, pt, isSnapshot, SelectedPlayer, ingameDate);
        }

        /// <summary>
        /// Create a stack panel with 3 combo boxes to use for dates entry
        /// </summary>
        /// <returns></returns>
        public static StackPanel CreateComboBoxDate()
        {
            ComboBox cbBdDay = new ComboBox();
            ComboBox cbBdYear = new ComboBox();
            ComboBox cbBdMonth = new ComboBox();
            cbBdDay.Name = "cbBdDay";
            cbBdYear.Name = "cbBdYear";
            cbBdMonth.Name = "cbBdMonth";
            cbBdDay.Width = 60;
            cbBdMonth.Width = 80;
            cbBdYear.Width = 60;
            cbBdDay.ItemsSource = Days;
            cbBdYear.ItemsSource = Years;
            cbBdMonth.ItemsSource = DateTimeFormatInfo.CurrentInfo.MonthNames.Reverse().Skip(1).Reverse();
            StackPanel stack = new StackPanel();
            stack.Orientation = Orientation.Horizontal;
            DockPanel.SetDock(stack, Dock.Right);
            stack.Children.Add(cbBdDay);
            stack.Children.Add(cbBdMonth);
            stack.Children.Add(cbBdYear);
            cbsCollection.Add(cbBdDay);
            cbsCollection.Add(cbBdMonth);
            cbsCollection.Add(cbBdYear);
            return stack;
        }

        /// <summary>
        /// Fill textboxes and Comboboxes for new snapshot entry
        /// </summary>
        /// <param name="fmAddPlayer">The form containing the fields</param>
        /// <param name="SelectedPlayer">The selected player</param>
        public static void PreFillForSnapshot(addPlayer fmAddPlayer, Player SelectedPlayer)
        {
            fmAddPlayer.spName.IsEnabled = false;
            tbsCommonCollection.GetTextBoxByName("tbFirst_Name").Text = SelectedPlayer.firstName;
            tbsCommonCollection.GetTextBoxByName("tbLast_Name").Text = SelectedPlayer.lastName;
            string[] bdate = SelectedPlayer.birthDateSplit();
            string day = bdate[0];
            if (day.StartsWith("0"))
            {
                day = day.Replace("0", "");
            }
            cbsCollection.GetComboBoxByName("cbBdDay").Text = day;
            try
            {
                cbsCollection.GetComboBoxByName("cbBdMonth").Text = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(int.Parse(bdate[1]));                
            }
            catch(Exception ex)
            {
                utils.ShowError("Could not parse a valid date from " + SelectedPlayer.fullName + ": " + SelectedPlayer.birthDate);
                utils.Log("Birthdate parse error when creating form for a snapshot. " + Environment.NewLine + ex.ToString());
            }
            cbsCollection.GetComboBoxByName("cbBdYear").Text = bdate[2];
        }

        /// <summary>
        /// Creates DockPanels for attributes input
        /// </summary>
        /// <param name="attribute">Attribute</param>
        /// <returns></returns>
        private static DockPanel CreateAttributeDockPanel(string attribute) 
        {
            DockPanel dp = new DockPanel();
            dp.Margin = new Thickness(10, 0, 10, 0);
            dp.LastChildFill = false;
            Label lbl = new Label();
            lbl.Content = attribute.Replace('_', ' ') + ":";
            DockPanel.SetDock(lbl, Dock.Left);
            TextBox tb = new TextBox();
            tb.Name = "tb" + attribute.Replace(' ', '_');
            tb.Width = 35;
            tb.TextAlignment = TextAlignment.Center;
            tb.VerticalContentAlignment = VerticalAlignment.Center;
            if (attribute == "Ingame_Date")
            {
                return dp;
            }
            else
            {
                tb.PreviewTextInput += Tb_PreviewTextInput;
            }
            DockPanel.SetDock(tb, Dock.Right);
            dp.Children.Add(lbl);
            dp.Children.Add(tb);
            tbsAttrCollection.Add(tb);
            return dp;
        }

        /// <summary>
        /// Creates a dockpanel for the common attributes
        /// </summary>
        /// <param name="attribute">Attribute</param>
        /// <returns></returns>
        private static DockPanel CreateCommonAttributeDockPanel(string attribute)
        {
            DockPanel dp = new DockPanel();

            dp.Margin = new Thickness(10, 0, 10, 0);

            dp.LastChildFill = false;

            // Label
            Label lbl = new Label();
            lbl.Content = attribute.Replace('_', ' ') + ":";

            DockPanel.SetDock(lbl, Dock.Left);

            // Textbox
            TextBox tb = new TextBox();
            tb.Name = "tb" + attribute.Replace(' ', '_');
            tb.Width = 200;
            tb.TextAlignment = TextAlignment.Center;
            tb.VerticalContentAlignment = VerticalAlignment.Center;
            DockPanel.SetDock(tb, Dock.Right);

            dp.Children.Add(lbl);
            if (attribute == "BirthDate")
            {
                StackPanel stack = CreateComboBoxDate();
                dp.Children.Add(stack);
            }
            else
            {
                dp.Children.Add(tb);
                tbsCommonCollection.Add(tb);
            }
            return dp;
        }
        
        /// <summary>
        /// Generates labels and text box for attributes based on the player's type
        /// </summary>
        /// <param name="fmAddPlayer">The form containing the labels</param>
        /// <param name="playerType">Player Type</param>
        /// <param name="isSnapshot">is this a snapshot?</param>
        /// <param name="SelectedPlayer">The selected player (in case of a snapshot)</param>
        /// <param name="dateIndex">The ingame date pushed into an array</param>
        private static void GenerateLabels(addPlayer fmAddPlayer, PlayerType playerType, bool isSnapshot = false, Player SelectedPlayer = null, int[] dateIndex = null)
        {
            // Clear collections.
            tbsAttrCollection.Clear();
            tbsCommonCollection.Clear();
            cbsCollection.Clear();

            string[] attributes;

            // Check player type and load attributes.
            switch (playerType)
            {
                case (PlayerType.player):
                    attributes = dbHelper.playerAttributes;
                    break;
                case (PlayerType.goalie):
                    attributes = dbHelper.goalieAttributes;
                    break;
                default:
                    throw new ArgumentException("Player type not recognized");
            }

            // Create common attributes controls
            foreach (string attr in dbHelper.commonAttributes)
            {                
                fmAddPlayer.spName.Children.Add(CreateCommonAttributeDockPanel(attr));
            }

            // If snapshot, fill the needed info and lock controls
            if (isSnapshot)
            {
                PreFillForSnapshot(fmAddPlayer, SelectedPlayer);
            }

            // Add labels and textbox for every attributes
            foreach (string attr in attributes)
            {
                fmAddPlayer.spAttributes.Children.Add(CreateAttributeDockPanel(attr));
            }

            // Add the in-game date field/combos
            fmAddPlayer.spAttributes.Children.Add(AddIngameDateField(dateIndex));

            // Finally add a save button
            Button bt = new Button();
            bt.Content = "Save";
            bt.Click += (sender, EventArgs) => { Bt_Click(sender, EventArgs, playerType, isSnapshot, SelectedPlayer, fmAddPlayer); };
            bt.Width = 100;
            bt.Margin = new Thickness(0, 70, 0, 0);
            bt.VerticalAlignment = VerticalAlignment.Center;
            bt.HorizontalAlignment = HorizontalAlignment.Center;
            fmAddPlayer.spAttributes.Children.Add(bt);
        }

        /// <summary>
        /// Create a stack panel for the ingame date field
        /// </summary>
        /// <param name="dateIndex">Ingame date set in a int array</param>
        /// <returns></returns>
        private static StackPanel AddIngameDateField(int[] dateIndex)
        {
            // Add ingame date field
            StackPanel topSp = new StackPanel();
            Label label = new Label { Content = "Ingame Date" };
            topSp.Margin = new Thickness(0, 15, 0, 0);
            topSp.Children.Add(label);
            StackPanel sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;
            ComboBox cbDay = new ComboBox();
            ComboBox cbYear = new ComboBox();
            ComboBox cbMonth = new ComboBox();
            cbDay.Name = "cbDay";
            cbYear.Name = "cbYear";
            cbMonth.Name = "cbMonth";
            cbDay.Width = 50;
            cbMonth.Width = 50;
            cbYear.Width = 50;
            cbDay.ItemsSource = Days;
            cbYear.ItemsSource = Years;
            cbMonth.ItemsSource = DateTimeFormatInfo.CurrentInfo.MonthNames.Reverse().Skip(1).Reverse();
            if (dateIndex != null)
            {
                cbDay.SelectedIndex = dateIndex[0];
                cbMonth.SelectedIndex = dateIndex[1];
                cbYear.SelectedIndex = dateIndex[2];
            }
            cbsCollection.Add(cbDay);
            cbsCollection.Add(cbMonth);
            cbsCollection.Add(cbYear);
            sp.Children.Add(cbDay);
            sp.Children.Add(cbMonth);
            sp.Children.Add(cbYear);
            topSp.Children.Add(sp);
            topSp.HorizontalAlignment = HorizontalAlignment.Center;
            topSp.VerticalAlignment = VerticalAlignment.Center;
            return topSp;
        }

        // Numbers only event for textboxes (attributes)
        private static void Tb_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        /// <summary>
        /// Check if provided data is correct
        /// </summary>
        /// <returns></returns>
        private static bool DataCheck()
        {
            // Check empty fields
            foreach(TextBox tb in tbsCommonCollection)
            {
                if(tb.Text == string.Empty)
                {
                    utils.ShowError("All fields need a value.");
                    return false;
                }
            }


            // Check dates provided
            foreach(ComboBox cb in cbsCollection)
            {
                if (cb.SelectedIndex == -1)
                {
                    utils.ShowError("Please select fill all the dates.");
                    return false;
                }
            }

            return true;
        }

        // Save button method (add player or snapshot)
        private static void Bt_Click(object sender, RoutedEventArgs e, PlayerType pt, bool isSnapshot, Player SelectedPlayer, addPlayer fm)
        {
            if (!DataCheck()) { return; }
            Player p = null;
            if (isSnapshot)
            {
                if(SelectedPlayer != null)
                {
                    p = new Player(SelectedPlayer.playerID, SelectedPlayer.firstName, SelectedPlayer.lastName, SelectedPlayer.playerType, SelectedPlayer.birthDate);
                }
                else
                {
                    throw new ArgumentNullException("A player object was expected but received NULL.");
                }
            }
            else
            {
                string bDate = Player.ComboToDateStr(new string[] {
                cbsCollection.GetComboBoxByName("cbBdDay").Text,
                cbsCollection.GetComboBoxByName("cbBdMonth").Text,
                cbsCollection.GetComboBoxByName("cbBdYear").Text
                });

                p = new Player(dbHelper.GetNewPlayerId(), tbsCommonCollection.GetTextBoxByName("tbFirst_Name").Text,
                tbsCommonCollection.GetTextBoxByName("tbLast_Name").Text,
                pt,
                bDate);
            }
            Snapshot s = new Snapshot(p.playerType);
            foreach (TextBox t in tbsAttrCollection)
            {
                s.attributes[t.Name.Substring(2, t.Name.Length - 2)] = t.Text;
            }

                s.attributes["Ingame_Date"] = Player.ComboToDateStr(new string[] {
                cbsCollection.GetComboBoxByName("cbDay").Text,
                cbsCollection.GetComboBoxByName("cbMonth").Text,
                cbsCollection.GetComboBoxByName("cbYear").Text
            });        
            p.Snapshots.Add(s);
            dbHelper.PlayerAdd(p);            
            fm.DialogResult = true;
            fm.Close();
        }

    }
}
