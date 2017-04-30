using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LiveCharts;
using LiveCharts.Wpf;
using System.Windows.Media.Imaging;
using System.Globalization;
using System.Windows.Media;
using Microsoft.VisualBasic;
using System.Windows.Input;

namespace EHMProgressTracker
{
    /*
     * 
     * TODO
     * Track Height / Weight
     * Treeview Sorting
     * All attributes chart
     * Attribute growth (all) Bars
     * Prefill new snapshots
     * Edit players name
     * Edit snapshot
     * */


    public partial class MainWindow : Window
    {
        public static List<Player> globalPlayersList = new List<Player>();
        public static readonly string VER = "0.2 Beta";

        // Chart
        public List<string> ChartDates = new List<string>();
        public string[] ChartAttrValues = Enumerable.Range(1, 20).Select(x => x.ToString()).ToArray();
        public static readonly string[] OtherStats = { "Total Attributes", "Age/Attribute Ratio" };
        public static readonly string[] AllStats = { "Total Attributes", "Age/Attribute Ratio", "Growth per month" };


        public MainWindow()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            try
            {
                Width = int.Parse(utils.Cfg("width"));
                Height = int.Parse(utils.Cfg("height"));
            }
            catch(Exception ex)
            {
                utils.ShowError("Error setting resolution from the config file. " + ex.Message);
                utils.Log("Error setting resolution." + Environment.NewLine + ex.ToString());
            }
        }

        /// <summary>
        /// Fill charts with empty data
        /// </summary>
        /// <param name="chart"></param>
        private void FillChartEmpty(CartesianChart chart)
        {
            try
            {
                chart.AxisY.Add(new Axis());
                chart.AxisX.Add(new Axis());
                chart.AxisY[0].ShowLabels = false;
                chart.AxisX[0].ShowLabels = false;
            }
            catch (Exception ex)
            {
                utils.ShowError("Error creating charts.");
                utils.Log("Error creating charts." + Environment.NewLine + ex.ToString());
            }
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            cbDatabase.ItemsSource = dbHelper.SearchDbFiles();
            cbDatabase.SelectedValuePath = "Key";
            cbDatabase.DisplayMemberPath = "Value";
            cbDatabase.SelectedIndex = 0;
            dbHelper.DBPreCheck();


            globalPlayersList = dbHelper.MergeSnapshots(dbHelper.ReadAllPlayers());
            GenerateTreeList(globalPlayersList);
            FillChartEmpty(chartAttributes);
            FillChartEmpty(chartOthers);
            FillChartEmpty(chartAll);
            ChartAllTotalAttributes();

            tabControl.SelectedIndex = tabControl.Items.Count - 1; // Set selected tab to general
            cbAllTotal.ItemsSource = AllStats;
            cbAllTotal.SelectedIndex = 0;

            cbYear.ItemsSource = AddPlayer.Years;
            cbDay.ItemsSource = AddPlayer.Days;
            cbMonth.ItemsSource = DateTimeFormatInfo.InvariantInfo.MonthNames.Reverse().Skip(1).Reverse();
        }

        /// <summary>
        /// Order all snapshots for players in a list
        /// </summary>
        /// <param name="pList">List of players</param>
        private void OrderSnapshots(List<Player> pList)
        {
            // Order snapshots
            foreach (Player p in pList)
            {
                p.OrderSnapshots();
            }
        }

        /// <summary>
        /// Pupolate the main treeview with the list of players
        /// </summary>
        /// <param name="pList">List of players</param>
        /// <param name="Rebuild">Clear the list and read the players from scratch</param>
        private void GenerateTreeList(List<Player> pList, bool Rebuild = false)
        {
            // Empty the list for rebuild.
            if (Rebuild) {
                tvMain.Items.Clear();
                globalPlayersList = dbHelper.MergeSnapshots(dbHelper.ReadAllPlayers());
                pList = globalPlayersList;
            }

            OrderSnapshots(pList);

            foreach (Player p in pList)
            {
                TreeViewItem Parent = new TreeViewItem();
                StackPanel stack = new StackPanel();
                stack.Orientation = Orientation.Horizontal;

                Image img = new Image();
                if (p.playerType == PlayerType.player)
                {
                    try
                    {
                        img.Source = new BitmapImage
                        (new Uri("pack://application:,,/Icons/player.png"));
                    }
                    catch (Exception ex)
                    {
                        utils.ShowError("Could not find image goalie.png");
                        utils.Log("Could not find image goalie.png" + Environment.NewLine + ex.ToString());
                    }
                }
                else if (p.playerType == PlayerType.goalie)
                {
                    try
                    {
                        img.Source = new BitmapImage
                            (new Uri("pack://application:,,/Icons/goalie.png"));
                    }
                    catch (Exception ex)
                    {
                        utils.ShowError("Could not find image goalie.png");
                        utils.Log("Could not find image goalie.png" + Environment.NewLine + ex.ToString());
                    }

                }
                img.Height = 32;
                img.Height = 32;
                stack.Children.Add(img);
                stack.Children.Add(new Label() { Content = p.fullName });
                Parent.Header = stack;
                Parent.Tag = p;

                tvMain.Items.Add(Parent);
                foreach (Snapshot s in p.Snapshots)
                {
                    TreeViewItem Node = new TreeViewItem();
                    Node.Header = s;
                    Node.Tag = s;
                    Parent.Items.Add(Node);
                }
            }
        }

        // Add player event
        private void bt_AddPlayer_Click(object sender, RoutedEventArgs e)
        {
            Window typeSelect = new PlayerSelectWindow();
            typeSelect.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Window addPlayer;
            int[] dateIndex = null;
            if (cbDay.SelectedIndex > -1 && cbMonth.SelectedIndex > -1 && cbYear.SelectedIndex > -1)
            {
                dateIndex = new int[] { cbDay.SelectedIndex, cbMonth.SelectedIndex, cbYear.SelectedIndex };
            }
            if ((bool)typeSelect.ShowDialog())
            {
                addPlayer = new AddPlayer(PlayerType.player, ingameDate: dateIndex);
            }
            else
            {
                addPlayer = new AddPlayer(PlayerType.goalie, ingameDate: dateIndex);
            }
            if ((bool)(addPlayer.ShowDialog())) {
                GenerateTreeList(null, true);
                ChartAllTotalAttributes();
            }
        }

        // Add snapshot event
        private void bt_AddSnapshot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if ((tvMain.SelectedItem as TreeViewItem).Tag.GetType() != typeof(Player)) { return; }
            }
            catch (Exception)
            {
                return;
            }
            int[] dateIndex = null;
            if (cbDay.SelectedIndex > -1 && cbMonth.SelectedIndex > -1 && cbYear.SelectedIndex > -1)
            {
                dateIndex = new int[] { cbDay.SelectedIndex, cbMonth.SelectedIndex, cbYear.SelectedIndex };
            }
            Player p = null;
            try
            {
                p = ((tvMain.SelectedItem as TreeViewItem).Tag as Player);
            }
            catch(Exception)
            {
                return;
            }
            Window addPlayer = new AddPlayer(p.playerType, true, p, dateIndex);

            if ((bool)(addPlayer.ShowDialog())) {
                GenerateTreeList(null, true);
                ChartAllTotalAttributes();
            }
        }

        // Remove player event
        private void btRemovePlayer_Click(object sender, RoutedEventArgs e)
        {
            Player p = null;
            try
            {
               p  = (tvMain.SelectedItem as TreeViewItem).Tag as Player;
            }
            catch(Exception)
            {
                return;
            }
            if (p != null && p.GetType() == typeof(Player))
            {
                MessageBoxResult res = MessageBox.Show("Are you sure you want to delete:" + Environment.NewLine +
                    p.fullName + "?" + Environment.NewLine +
                    "This will delete all snapshots.", "Delete " + p.fullName, MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                if (res == MessageBoxResult.Yes)
                {
                    dbHelper.PlayerRemove(p);
                    GenerateTreeList(null, true);
                }
            }
        }

        // Remove snapshot event
        private void btRemoveSnapshot_Click(object sender, RoutedEventArgs e)
        {
            Snapshot s = null;
            Player p = null;
            try
            {
                s = (tvMain.SelectedItem as TreeViewItem).Tag as Snapshot;
                p = (((tvMain.SelectedItem as TreeViewItem).Parent) as TreeViewItem).Tag as Player;
            }
            catch (Exception)
            {                
                return;
            }
            if (s != null && s.GetType() == typeof(Snapshot) && p != null)
            {
                MessageBoxResult res = MessageBox.Show("Are you sure you want to delete snapshot:" + Environment.NewLine +
                    s.attributes["Ingame_Date"] + " for " + p.fullName + "?" + Environment.NewLine +
                    "This will delete all snapshots for this date.", "Delete " + s.attributes["Ingame_Date"], MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                if (res == MessageBoxResult.Yes)
                {
                    dbHelper.SnapshotRemove(s, p);
                    GenerateTreeList(null, true);
                }
            }
        }

        // Main tree selection changes events
        private void tvMain_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (tvMain.Items.Count < 1) { return; }
            Player p = null;
            Snapshot s = null;

            try
            {
                var item = (tvMain.SelectedItem as TreeViewItem).Tag;
                btAddSnapshot.IsEnabled = true;
                btRemoveSnapshot.IsEnabled = true;

                if (item.GetType() == typeof(Player))
                {
                    tabCharts.IsEnabled = true;
                    tabAttributes.IsEnabled = false;
                    tabChartsOthers.IsEnabled = true;
                    btRemoveSnapshot.IsEnabled = false;

                    p = item as Player;
                    ShowGeneralInformation(p);
                    FillAttributesMenu(p);
                }
                if (item.GetType() == typeof(Snapshot))
                {
                    tabAttributes.IsEnabled = true;
                    tabCharts.IsEnabled = false;
                    tabChartsOthers.IsEnabled = false;
                    btAddSnapshot.IsEnabled = false;

                    s = item as Snapshot;
                    ShowAttributes(s);
                }
            }
            catch (Exception ex)
            {
                utils.ShowError("An error occured loading the player, try again.");
                utils.Log("Error loading player" + Environment.NewLine + ex.ToString());
                return;
            }
        }

        /// <summary>
        /// Fill combo box with according attributes depending on player's type.
        /// </summary>
        /// <param name="p">Selected player</param>
        private void FillAttributesMenu(Player p)
        {
            cbOthers.SelectedIndex = -1;
            cbOthers.ItemsSource = OtherStats;
            cbAttributesMenu.SelectedIndex = -1;


            if (p.playerType == PlayerType.player)
            {
                List<string> Attributes = new List<string>(dbHelper.playerAttributes);
                Attributes.Remove("Ingame_Date");
                cbAttributesMenu.ItemsSource = Attributes;

                cbAttributesMenu.SelectedIndex = -1;
                cbOthers.SelectedIndex = -1;
            }
            else if (p.playerType == PlayerType.goalie)
            {
                List<string> Attributes = new List<string>(dbHelper.goalieAttributes);
                Attributes.Remove("Ingame_Date");
                cbAttributesMenu.ItemsSource = Attributes;
            }
            else
            {
                throw new ArgumentException("Invalid player type.");
            }
        }

        /// <summary>
        /// Set a label's color based on it's integer value
        /// </summary>
        /// <param name="lbl">Label</param>
        /// <returns></returns>
        private static Label SetLabelColor(Label lbl)
        {
            int value;
            int x;
            if (!int.TryParse(lbl.Content.ToString(), out x)) { return lbl;}
            value = x;
            BrushConverter bc = new BrushConverter();
            if(value >= 1 && value <= 6)
            {
                // set red
                lbl.Background = (Brush)bc.ConvertFrom("#721D24");
            }
            if(value >= 7 && value <= 11)
            {
                //set orange
                lbl.Background = (Brush)bc.ConvertFrom("#8A6028");
            }
            if(value >=12 && value <= 16)
            {
                // set dark green
                lbl.Background = (Brush)bc.ConvertFrom("#0F641B");
            }
            if(value > 16)
            {
                //set bright green
                lbl.Background = (Brush)bc.ConvertFrom("#168E26");
            }

            return lbl;
        }

        /// <summary>
        /// Displays attributes in the attributes tab
        /// </summary>
        /// <param name="s">Snapshot to display</param>
        private void ShowAttributes(Snapshot s)
        {
            gridTechnical.Children.Clear();
            gridMental.Children.Clear();
            gridPhysical.Children.Clear();            

            PlayerType pType = (((tvMain.SelectedItem as TreeViewItem).Parent as TreeViewItem).Tag as Player).playerType;
            string[] attributesSet = null;
            int indexTechnical = 0;
            if (pType == PlayerType.player)
            {
                attributesSet = dbHelper.playerAttributes;
                 indexTechnical = 12;
            }
            if(pType == PlayerType.goalie)
            {
                attributesSet = dbHelper.goalieAttributes;
                indexTechnical = 9;
            }

            for (int i = 0; i < attributesSet.Count(); i++)
            {
                try {
                    if (attributesSet[i] == "Ingame_Date") { continue; }
                    FontFamily ff = new System.Windows.Media.FontFamily(new Uri("pack://EHMProgressTracker:,,,Fonts/#Quantico-Regular", UriKind.Absolute), "Quantico");
                    Label lblAttName = new Label() { Content = attributesSet[i].Replace("_", " "), FontFamily = ff, FontSize = 18 };
                    Grid.SetColumn(lblAttName, 0);
                    lblAttName.Padding = new Thickness(5);
                    lblAttName.HorizontalAlignment = HorizontalAlignment.Left;
                    Label lblAttValue = new Label() { Content = s.attributes[attributesSet[i]], FontFamily = ff, FontSize = 18 };
                    Grid.SetColumn(lblAttValue, 1);
                    lblAttValue.Padding = new Thickness(5);
                    lblAttValue.Width = 50;
                    lblAttValue.Foreground = Brushes.White;
                    lblAttValue.HorizontalContentAlignment = HorizontalAlignment.Center;
                    lblAttValue.HorizontalAlignment = HorizontalAlignment.Center;

                    lblAttValue = SetLabelColor(lblAttValue);

                    if (i == 0 || i < indexTechnical)
                    {
                        Grid.SetRow(lblAttName, i);
                        Grid.SetRow(lblAttValue, i);
                        gridTechnical.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                        gridTechnical.Children.Add(lblAttName);
                        gridTechnical.Children.Add(lblAttValue);
                    }
                    else if (i == indexTechnical || i < indexTechnical + 9)
                    {
                        Grid.SetRow(lblAttName, i - indexTechnical);
                        Grid.SetRow(lblAttValue, i - indexTechnical);
                        gridMental.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                        gridMental.Children.Add(lblAttName);
                        gridMental.Children.Add(lblAttValue);
                    }
                    else if (i >= indexTechnical + 9)
                    {
                        Grid.SetRow(lblAttName, i - indexTechnical - 9);
                        Grid.SetRow(lblAttValue, i - indexTechnical - 9);
                        gridPhysical.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                        gridPhysical.Children.Add(lblAttName);
                        gridPhysical.Children.Add(lblAttValue);
                    }
                }
                catch (Exception ex)
                {
                    utils.ShowError("An error occured generating stats. " + ex.Message);
                    utils.Log("An error occured generating stats. " + Environment.NewLine + ex.ToString());
                    break;
                }
            }   

        }

        /// <summary>
        /// Shows the general informaiton of a player in the general tab
        /// </summary>
        /// <param name="p">Player to display</param>
        private void ShowGeneralInformation(Player p)
        {
            wpGeneral.Children.Clear();
            FontFamily ff = new System.Windows.Media.FontFamily(new Uri("pack://EHMProgressTracker:,,,Fonts/#Quantico-Regular", UriKind.Absolute), "Quantico");
            wpGeneral.Children.Add(new Label() { Content = "Name: " + p.fullName, FontFamily = ff, FontSize = 16 });
            wpGeneral.Children.Add(new Label() { Content = "Birth Date: " + p.birthDate, FontFamily = ff, FontSize = 16 });
            wpGeneral.Children.Add(new Label() { Content = "Age (latest snapshot): " + p.GetAge(), FontFamily = ff, FontSize = 16 });
            wpGeneral.Children.Add(new Label() { Content = "Type: " + p.playerType.ToString(), FontFamily = ff, FontSize = 16 });
            wpGeneral.Children.Add(new Label() { Content = "Total attributes (latest snapshot): " + p.Snapshots.First().AttributesTotal(), FontFamily = ff, FontSize = 16 });
            wpGeneral.Children.Add(new Label() { Content = "Total attribute growth per day: " + AttributePerDay(p), FontFamily = ff, FontSize = 16 });
        }

        // cbAttributes selection handler - Chart for individual attributes
        private void cbAttributesMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ChartDates.Clear();
            if (cbAttributesMenu.SelectedIndex == -1 || string.IsNullOrEmpty(cbAttributesMenu.SelectedItem.ToString())) { return; }
            string attr = cbAttributesMenu.SelectedItem.ToString();
            Player p = null;
            try
            {
                p = (tvMain.SelectedItem as TreeViewItem).Tag as Player;
            }
            catch (NullReferenceException)
            {
                return;
            }
            List<int> values = new List<int>();
            foreach (Snapshot s in p.Snapshots)
            {
                values.Add(int.Parse(s.attributes[attr]));
                ChartDates.Add(s.attributes["Ingame_Date"]);
            }

            // Reverse list
            values.Reverse();
            ChartDates.Reverse();

            // Display in chart
            SeriesCollection SeriesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    Title = attr,
                    Values = new ChartValues<int> (values),
                    LineSmoothness = 0
                }
            };

            CreateAxis(ChartDates, "Dates", ChartAttrValues, "Attributes", chartAttributes, SeriesCollection);
        }

        // cbOther selection handler - Chart for other stats
        private void cbOthers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbOthers.SelectedIndex == -1 || string.IsNullOrEmpty(cbOthers.SelectedItem.ToString())) { return; }
            switch (cbOthers.SelectedItem.ToString())
            {
                case ("Total Attributes"):
                    ChartTotalAttributes();
                    break;
                case ("Age/Attribute Ratio"):
                    AgeAttributeRatio();
                    break;
                default: break;
            }
        }

        /// <summary>
        /// Calculate attributes for selected player to age ratio and display in a chart
        /// </summary>
        private void AgeAttributeRatio()
        {
            Player p = null;
            try
            {
                p = (tvMain.SelectedItem as TreeViewItem).Tag as Player;
            }
            catch (NullReferenceException)
            {
                return;
            }
            List<double> values = new List<double>();
            foreach (Snapshot s in p.Snapshots)
            {
                double x = s.AttributesTotal() / int.Parse(s.GetAge(p));
                values.Add(Math.Round(x, 2));
                ChartDates.Add(s.GetAge(p));
            }

            // Reverse list
            values.Reverse();
            ChartDates.Reverse();

            // Display in chart
            SeriesCollection SeriesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Age/Attribute Ratio",
                    Values = new ChartValues<double> (values),
                }
            };
            CreateAxis(ChartDates, "Age", ChartAttrValues, "Attributes", chartOthers, SeriesCollection);
        }

        /// <summary>
        /// Calculates total attributes for selected player and display them in a chart.
        /// </summary>
        private void ChartTotalAttributes()
        {
            Player p = null;
            try
            {
                p = (tvMain.SelectedItem as TreeViewItem).Tag as Player;
            }
            catch (NullReferenceException)
            {
                return;
            }
            List<int> values = new List<int>();
            foreach (Snapshot s in p.Snapshots)
            {
                values.Add(s.AttributesTotal());
                ChartDates.Add(s.attributes["Ingame_Date"]);
            }

            // Reverse list
            values.Reverse();
            ChartDates.Reverse();

            // Display in chart
            SeriesCollection SeriesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Attributes Total",
                    Values = new ChartValues<int> (values),
                    LineSmoothness = 0
                }
            };
            CreateAxis(ChartDates, "Dates", Enumerable.Range(0, 500).Select(x => x.ToString()).ToArray(), "Attributes", chartOthers, SeriesCollection);
        }

        /// <summary>
        /// Calculate total attributes for all players and display them in a chart.
        /// </summary>
        public void ChartAllTotalAttributes()
        {
            string[] range = Enumerable.Range(0, 600).Select(x => x.ToString()).ToArray();
            string[] players = globalPlayersList.Select(x => x.lastName).ToArray();
            List<int> totalAttr = new List<int>();

            foreach (Player p in globalPlayersList)
            {
                totalAttr.Add(p.Snapshots.First().AttributesTotal());
            }

            SeriesCollection sc = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Total",
                    Values = new ChartValues<int>(totalAttr),                                        
                }
            };

            CreateAxis(players, "Players", range, "Total", chartAll, sc, 1, 0, 20);
        }

        public void ChartAllGrowthPerMonth()
        {
            string[] range = Enumerable.Range(0, 200).Select(x => x.ToString()).ToArray();
            string[] players = globalPlayersList.Select(x => x.lastName).ToArray();
            List<int> totalAttr = new List<int>();

            foreach (Player p in globalPlayersList)
            {
                double x = double.Parse(AttributePerDay(p));
                x = x * 30;
                int y = Convert.ToInt32(x);
                totalAttr.Add(y);   
                         
            }

            SeriesCollection sc = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Growth",
                    Values = new ChartValues<int>(totalAttr),
                }
            };
            CreateAxis(players, "Players", range, "Growth", chartAll, sc, 1, 0, 20);
        }

        public void ChartAllAgeRatio()
        {
            string[] range = Enumerable.Range(0, 25).Select(x => x.ToString()).ToArray();
            string[] players = globalPlayersList.Select(x => x.lastName).ToArray();
            List<double> totalAttr = new List<double>();

            foreach (Player p in globalPlayersList)
            {
                double x = p.Snapshots.First().AttributesTotal() / int.Parse(p.Snapshots.First().GetAge(p));
                totalAttr.Add(Math.Round(x, 2));
            }

            SeriesCollection sc = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Total",
                    Values = new ChartValues<double>(totalAttr),
                }
            };

            CreateAxis(players, "Players", range, "Total", chartAll, sc, 1, 1, 20);
        }

        /// <summary>
        /// Creates axises for charts.
        /// </summary>
        /// <param name="x">Values for the X axis</param>
        /// <param name="xtitle">Title of the X axis</param>
        /// <param name="y">Values for the Y axis</param>
        /// <param name="ytitle">Title for the Y axis</param>
        /// <param name="chart">Chart to apply the axises to</param>
        /// <param name="Collection">Values collection</param>
        /// <param name="xStep">How many steps on the x axis</param>
        /// <param name="rotation">Labels roatation on the X axis</param>
        private void CreateAxis(IList<string> x, string xtitle, IList<string> y, string ytitle, CartesianChart chart, SeriesCollection Collection, double xStep = 0.0, double yStep = 0.0, int rotation = 0)
        {
            Axis axisx = new Axis();
            axisx.Labels = x;
            axisx.Title = xtitle;
            if (xStep > 0)
            {
                axisx.Separator.Step = xStep;
            }       
            axisx.LabelsRotation = rotation;
            
            Axis axisy = new Axis();
            axisy.Labels = y;
            axisy.Title = ytitle;

            if (yStep > 0)
            {
                axisy.Separator.Step = yStep;
            }

            AxesCollection ax = new AxesCollection();
            AxesCollection ay = new AxesCollection();
            ax.Add(axisx);
            ay.Add(axisy);

            chart.Series = Collection;
            chart.AxisX = ax;
            chart.AxisY = ay;
            chart.DataContext = this;
        }

        /// <summary>
        /// Calculates attributes growth per day
        /// </summary>
        /// <param name="p">Player to calulate</param>
        /// <returns></returns>
        private string AttributePerDay(Player p)
        {
            try
            {
                // Invert first and last since the order is reverted.
                Snapshot first = p.Snapshots.Last();
                Snapshot last = p.Snapshots.First();
                DateTime firstDate = DateTime.ParseExact(first.attributes["Ingame_Date"], "dd-MM-yyyy", CultureInfo.InvariantCulture);
                DateTime lastDate = DateTime.ParseExact(last.attributes["Ingame_Date"], "dd-MM-yyyy", CultureInfo.InvariantCulture);
                TimeSpan span = lastDate.Subtract(firstDate);
                int total = last.AttributesTotal() - first.AttributesTotal();
                double res = Math.Round((double) total / (double) span.Days, 2);
                if (double.IsNaN(res)) return "0.00";
                return res.ToString();
            }
            catch (Exception ex)
            {
                utils.ShowError("Error occured parsing date. " + ex.Message);
                utils.Log("Error occured parsing date." + Environment.NewLine + ex.ToString());
                return "ERROR";
            }
        }

        // Exit Button
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
            Environment.Exit(0);
        }

        // All players chart change handler
        private void cbAllTotal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbAllTotal.SelectedIndex == -1 || string.IsNullOrEmpty(cbAllTotal.SelectedItem.ToString())) { return; }
            switch (cbAllTotal.SelectedItem.ToString())
            {
                case ("Total Attributes"):
                    ChartAllTotalAttributes();
                    break;
                case ("Age/Attribute Ratio"):
                    ChartAllAgeRatio();
                    break;
                case ("Growth per month"):
                    ChartAllGrowthPerMonth();
                    break;
                default: break;
            }
        }

        // Change database
        private void cbDatabase_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {            
            if (cbDatabase.IsDropDownOpen) // Check if action is user triggered
            {
                if (cbDatabase.SelectedValue.ToString() == "New...")
                {
                    string dbName = Interaction.InputBox("Enter a name for the new database",
                                           "Enter a name for the new database",
                                           "",
                                           -1, -1);
                    if (string.IsNullOrEmpty(dbName)) { return; }
                    dbHelper.DEFAULT_DB = dbName + ".sqlite";
                    dbHelper.CloseCurrent();
                    dbHelper.DBPreCheck();
                    cbDatabase.ItemsSource = dbHelper.SearchDbFiles(); // Called to repopulate the cbDatabase
                    cbDatabase.SelectedIndex = cbDatabase.Items.IndexOfStr(dbName);
                    dbHelper.DEFAULT_DB = dbName + ".sqlite";
                    globalPlayersList = dbHelper.MergeSnapshots(dbHelper.ReadAllPlayers());
                    GenerateTreeList(globalPlayersList, true);

                }
                else
                {
                    dbHelper.DEFAULT_DB = cbDatabase.SelectedValue.ToString();
                    dbHelper.CloseCurrent();
                    dbHelper.DBPreCheck();
                    globalPlayersList = dbHelper.MergeSnapshots(dbHelper.ReadAllPlayers());
                    GenerateTreeList(globalPlayersList, true);
                    ChartAllTotalAttributes();
                }

            }
        }

        // About
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("EHMProgressTracker " + VER + Environment.NewLine + "By Gabisonfire", "About", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Grid_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void btImport_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(cbDay.Text) || string.IsNullOrEmpty(cbMonth.Text) ||
                string.IsNullOrEmpty(cbYear.Text))
            {
                utils.ShowError("You must first choose an ingame date.");
                return;
            }
            Window importer = new Importer(globalPlayersList, new string[] {cbDay.Text, cbMonth.Text, cbYear.Text });
            if ((bool) importer.ShowDialog())
            {
                GenerateTreeList(null, true);
                ChartAllTotalAttributes();
            }
        }

        private void btEdit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if ((tvMain.SelectedItem as TreeViewItem).Tag.GetType() != typeof(Player)) { return; }
            }
            catch (Exception)
            {
                return;
            }
            EditPlayer fmEditPlayer = new EditPlayer((tvMain.SelectedItem as TreeViewItem).Tag as Player);
            if ((bool) fmEditPlayer.ShowDialog())
            {
                GenerateTreeList(null, true);
                ChartAllTotalAttributes();
            }
        }
    }
}
