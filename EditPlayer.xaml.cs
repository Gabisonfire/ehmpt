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

namespace EHMProgressTracker
{
    /// <summary>
    /// Interaction logic for EditPlayer.xaml
    /// </summary>
    public partial class EditPlayer : Window
    {
        private Player player;

        public EditPlayer(Player player)
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.player = player;
            LoadPlayer(player);
        }

        private void LoadPlayer(Player p)
        {
            tbFirstName.Text = p.firstName;
            tbLastName.Text = p.lastName;
        }

        private void btSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                dbHelper.EditName(player, tbFirstName.Text, tbLastName.Text);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                utils.ShowError("Could not update informations. " + ex.Message);
                utils.Log("Error updating name in DB. " + ex);
                DialogResult = false;
                Close();
            }
        }
    }
}
