using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;


namespace EHMProgressTracker
{
   
   

    public partial class AddPlayer : Window
    {
        private static List<TextBox> tbsAttrCollection = new List<TextBox>();       // Keep track of the textboxes for attributes
        private static List<TextBox> tbsCommonCollection = new List<TextBox>();     // Keep track of the textboxes for common information
        private static List<ComboBox> cbsCollection = new List<ComboBox>();         // Keep track of dropdowns
        public static readonly string[] Days = Enumerable.Range(1, 31).Select(x => x.ToString()).ToArray();
        public static readonly string[] Years = Enumerable.Range(1960, 1000).Select(x => x.ToString()).ToArray();


        // Form constructor
        public AddPlayer(PlayerType pt, bool isSnapshot = false, Player SelectedPlayer = null, int[] ingameDate = null)
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
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
            cbBdMonth.ItemsSource = DateTimeFormatInfo.InvariantInfo.MonthNames.Reverse().Skip(1).Reverse();
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
        public static void PreFillForSnapshot(AddPlayer fmAddPlayer, Player SelectedPlayer)
        {
            List<Control> Controls = new List<Control>();
            tbsCommonCollection.GetTextBoxByName("tbFirst_Name").Text = SelectedPlayer.firstName;
            tbsCommonCollection.GetTextBoxByName("tbLast_Name").Text = SelectedPlayer.lastName;
            Controls.Add(tbsCommonCollection.GetTextBoxByName("tbFirst_Name"));
            Controls.Add(tbsCommonCollection.GetTextBoxByName("tbLast_Name"));
            string[] bdate = SelectedPlayer.birthDateSplit();
            string day = bdate[0];
            if (day.StartsWith("0"))
            {
                day = day.Replace("0", "");
            }
            cbsCollection.GetComboBoxByName("cbBdDay").Text = day;
            Controls.Add(cbsCollection.GetComboBoxByName("cbBdDay"));
            try
            {
                cbsCollection.GetComboBoxByName("cbBdMonth").Text = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(int.Parse(bdate[1]));
                Controls.Add(cbsCollection.GetComboBoxByName("cbBdMonth"));        
            }
            catch(Exception ex)
            {
                utils.ShowError("Could not parse a valid date from " + SelectedPlayer.fullName + ": " + SelectedPlayer.birthDate);
                utils.Log("Birthdate parse error when creating form for a snapshot. " + Environment.NewLine + ex.ToString());
            }
            cbsCollection.GetComboBoxByName("cbBdYear").Text = bdate[2];
            Controls.Add(cbsCollection.GetComboBoxByName("cbBdYear"));
            ToggleControls(Controls, false);
        }

        private static void ToggleControls(List<Control> Controls, bool enabled = true)
        {
            foreach(Control ui in Controls)
            {
                ui.IsEnabled = enabled;                
            }
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
        private static void GenerateLabels(AddPlayer fmAddPlayer, PlayerType playerType, bool isSnapshot = false, Player SelectedPlayer = null, int[] dateIndex = null)
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


            // Add Checkbox
            CheckBox cbUseLast = new CheckBox();
            //cbUseLast.Margin = new Thickness(0, 5, 0, 0);
            cbUseLast.Content = "Pre-fill using last snapshot";
            cbUseLast.Click += (sender, args) => LoadSnapshot(SelectedPlayer, cbUseLast);
            fmAddPlayer.spName.Children.Add(cbUseLast);

            // Add the in-game date field/combos
            fmAddPlayer.spName.Children.Add(AddIngameDateField(dateIndex));



            // If snapshot, fill the needed info and lock controls
            if (isSnapshot)
            {
                PreFillForSnapshot(fmAddPlayer, SelectedPlayer);
            }

            // Add labels and textbox for every attributes
            for (int i = 0; i < attributes.Count(); i++)
            {
                int indexTechnical = 0;
                if (playerType == PlayerType.player)
                {
                    indexTechnical = 12;
                }
                if (playerType == PlayerType.goalie)
                {
                    indexTechnical = 9;
                }

                DockPanel dp = CreateAttributeDockPanel(attributes[i]);
                if (i == 0 || i < indexTechnical)
                {
                    Grid.SetColumn(dp, 0);
                    Grid.SetRow(dp, i);
                }
                else if (i == indexTechnical || i < indexTechnical + 9)
                {
                    Grid.SetColumn(dp, 1);
                    Grid.SetRow(dp, i - indexTechnical);
                }
                else if (i >= indexTechnical + 9)
                {
                    Grid.SetColumn(dp, 2);
                    Grid.SetRow(dp, i - indexTechnical - 9);
                }                    
                fmAddPlayer.gridAttributes.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                fmAddPlayer.gridAttributes.Children.Add(dp);                
            }
            
            // Finally add a save button
            Button bt = new Button();
            bt.Content = "Save";
            bt.Click += (sender, EventArgs) => { Bt_Click(sender, EventArgs, playerType, isSnapshot, SelectedPlayer, fmAddPlayer); };
            bt.Width = 200;
            bt.Margin = new Thickness(0, 20, 0, 0);
            bt.VerticalAlignment = VerticalAlignment.Center;
            bt.HorizontalAlignment = HorizontalAlignment.Center;            
            Grid.SetRow(bt, fmAddPlayer.gridAttributes.RowDefinitions.Count - 1);
            Grid.SetColumn(bt, 1);
            fmAddPlayer.gridAttributes.Children.Add(bt);
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
            cbMonth.ItemsSource = DateTimeFormatInfo.InvariantInfo.MonthNames.Reverse().Skip(1).Reverse();
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

        // Uses last snapshot to pre-populate fields
        private static void LoadSnapshot(Player p, ToggleButton cb)
        {
            if (cb.IsChecked == true)
            {
                if (p.Snapshots.Count < 1)
                {
                    utils.ShowError("This player has no snapshots to start from.");
                    cb.IsChecked = false;
                    return;
                }

                foreach (var tb in tbsAttrCollection)
                {
                    tb.Text = p.Snapshots[0].GetAttribute(tb.Name.Substring(2).Replace(' ','_'));
                }
            }
            else
            {
                foreach (var tb in tbsAttrCollection)
                {
                    tb.Text = "";
                }
            }            

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
                    utils.ShowError("Please fill all dates.");
                    return false;
                }
            }

            return true;
        }

        // Save button method (add player or snapshot)
        private static void Bt_Click(object sender, RoutedEventArgs e, PlayerType pt, bool isSnapshot, Player SelectedPlayer, AddPlayer fm)
        {
            try
            {
                if (!DataCheck()) { return; }
                Player p = null;
                if (isSnapshot)
                {
                    if (SelectedPlayer != null)
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
            catch (Exception ex)
            {
                utils.ShowError("Error saving: " + ex.Message);
                utils.Log("Error saving: " + ex.ToString());
            }
            }




    }
}
