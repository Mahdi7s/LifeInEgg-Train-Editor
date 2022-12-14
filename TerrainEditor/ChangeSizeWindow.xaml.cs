using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Interaction logic for ChangeSizeWindow.xaml
    /// </summary>
    public partial class ChangeSizeWindow : Window, INotifyPropertyChanged
    {
        public ChangeSizeWindow()
        {
            InitializeComponent();

            DataContext = this;
        }

        private double _terrainWidth;
        private double _terrainHeight;
        public double TerrainWidth
        {
            get
            {
                return _terrainWidth;
            }
            set
            {
                _terrainWidth = value;
                NotifyPropertyChange("TerrainWidth");
            }
        }
        public double TerrainHeight
        {
            get
            {
                return _terrainHeight;
            }
            set
            {
                _terrainHeight = value;
                NotifyPropertyChange("TerrainHeight");
            }
        }

        public double TerrainMarginTop { get; set; }
        public double TerrainMarginLeft { get; set; }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void NotifyPropertyChange(string name)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
