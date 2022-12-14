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

namespace TerrainEditor
{
    /// <summary>
    /// Interaction logic for ShowCodeWindow.xaml
    /// </summary>
    public partial class ShowCodeWindow : Window
    {
        public ShowCodeWindow(TerrainDataModel model)
        {
            InitializeComponent();

            txtCode.Text = Helpers.GenerateGMCode(model);
        }
    }
}
