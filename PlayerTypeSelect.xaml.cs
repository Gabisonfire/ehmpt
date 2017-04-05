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
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class PlayerSelectWindow : Window
    {
        public PlayerSelectWindow()
        {
            InitializeComponent();
        }

        private void btPlayer_Click(object sender, RoutedEventArgs e)
        {
            fmPlayerType.DialogResult = true;
            fmPlayerType.Close();
        }

        private void btGoalie_Click(object sender, RoutedEventArgs e)
        {
            fmPlayerType.DialogResult = false;
            fmPlayerType.Close();
        }
    }
}
