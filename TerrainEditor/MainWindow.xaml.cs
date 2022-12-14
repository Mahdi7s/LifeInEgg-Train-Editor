using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TerrainEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TerrainDataModel _terrain = new TerrainDataModel();
        private bool _isMiddlePressed = false;
        private Point? _dragStartPos = null;
        private Point _startDragZoom = new Point(0, 0);
        private string _projectPath = null;
        private Image _terrainImage = null;
        private PixelColor[,] _imagePixels = null;
        private TerrainSegment _selectedSegment = null;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnSize_Click(object sender, RoutedEventArgs e)
        {
            var changeSizeWin = new ChangeSizeWindow();
            if (_terrain != null)
            {
                changeSizeWin.TerrainWidth = _terrain.TerrainSize.Width;
                changeSizeWin.TerrainHeight = _terrain.TerrainSize.Height;
            }
            if (changeSizeWin.ShowDialog() == true)
            {
                var prevTerrainSize = _terrain.TerrainSize;
                var newTerrainSize = new Size(changeSizeWin.TerrainWidth, changeSizeWin.TerrainHeight);

                ChangeTerrainSize(prevTerrainSize, newTerrainSize);
            }
        }

        private void ChangeTerrainSize(Size prevTerrainSize, Size newTerrainSize)
        {
            if (prevTerrainSize == newTerrainSize) return;

            Func<TerrainPoint, TerrainPoint> getNewPos = (tPoint) =>
            {
                return new TerrainPoint
                {
                    X = (tPoint.X * newTerrainSize.Width) / prevTerrainSize.Width,
                    Y = (tPoint.Y * newTerrainSize.Height) / prevTerrainSize.Height
                };
            };

            foreach (var sp in _terrain.Segments.SelectMany(x => x.SegmentPoints))
            {
                var nSp = getNewPos(sp);
                sp.X = nSp.X;
                sp.Y = nSp.Y;
            }
            ReloadTerrain();

            _terrain.TerrainSize = newTerrainSize;
            UpdateSizeInfo(newTerrainSize);
        }

        private void UpdateSizeInfo(Size terrainSize)
        {
            btnSize.Content = string.Format("{0}x{1}", Math.Round(terrainSize.Width, 2), Math.Round(terrainSize.Height, 2));
        }

        private void btnSetImage_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "(*.png)|*.png";
            dlg.Multiselect = false;
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() == true)
            {
                _terrain.ImagePath = dlg.FileName;
                SetImage(_terrain.ImagePath);

                if (MessageBox.Show("Change Terrain Size With this image size?", "Changing Terrain Size!", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    var bitmap = new BitmapImage(new Uri(_terrain.ImagePath));
                    ChangeTerrainSize(_terrain.TerrainSize, new Size(bitmap.Width, bitmap.Height));
                }
            }
        }

        private void SetImage(string path)
        {
            if (_terrainImage != null)
            {
                grid.Children.Remove(_terrainImage);
            }
            _terrainImage = new Image();
            var imgBitmap = new BitmapImage(new Uri(path));
            _terrainImage.Source = imgBitmap;

            _imagePixels = Helpers.GetPixels(_terrainImage.Source as BitmapImage);

            terrainTranslate.X = 0;
            terrainTranslate.Y = 0;

            grid.Children.Add(_terrainImage);
            _terrainImage.MouseMove += (s, e) =>
            {
                var pos = e.GetPosition(_terrainImage);
                txtMousePos.Text = string.Format("X: {0}, Y: {1}", pos.X, pos.Y);
            };

            UpdateSizeInfo(new Size(imgBitmap.Width, imgBitmap.Height));
        }

        private void btnGenCode_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new ShowCodeWindow(_terrain);
            dlg.ShowDialog();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_terrain == null || _terrainImage == null) return;

            if (string.IsNullOrEmpty(_projectPath))
            {
                var openDlg = new OpenFileDialog();
                openDlg.DefaultExt = ".tep";
                openDlg.Filter = "(*.tep)|*.tep";
                openDlg.Multiselect = false;
                openDlg.RestoreDirectory = true;
                openDlg.CheckFileExists = false;

                if (openDlg.ShowDialog() == true)
                {
                    _projectPath = openDlg.FileName;
                }
            }

            Helpers.SaveProject(_terrain, _projectPath);
        }

        private void Grid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            terrainScale.ScaleX = Helpers.Clamp(terrainScale.ScaleX + e.Delta / 100, 0.4, 20);
            terrainScale.ScaleY = Helpers.Clamp(terrainScale.ScaleY + e.Delta / 100, 0.4, 20);
        }

        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isMiddlePressed)
            {
                var currPos = e.GetPosition(scrollViewer);
                terrainTranslate.X = _startDragZoom.X - (_dragStartPos.Value.X - currPos.X);
                terrainTranslate.Y = _startDragZoom.Y - (_dragStartPos.Value.Y - currPos.Y);
            }
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                _dragStartPos = e.GetPosition(scrollViewer);
                _isMiddlePressed = true;
                _startDragZoom = new Point(terrainTranslate.X, terrainTranslate.Y);

                if (_terrainImage != null)
                {
                    _terrainImage.CaptureMouse();
                }
            }

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var pos = e.GetPosition(grid);
                if ((e.OriginalSource is Ellipse) && (btnSelectSegment.IsChecked == true) && (_selectedSegment == null))
                {
                    var ell = e.OriginalSource as Ellipse;
                    var trans = ell.RenderTransform as TranslateTransform;
                    var tPos = new TerrainPoint { X = trans.X + ell.Width/2, Y = trans.Y+ell.Height/2 };
                    _selectedSegment = _terrain.Segments.FirstOrDefault(x => x.SegmentPoints.Any(sp => sp.Equals(tPos)));
                    foreach (var spoint in _selectedSegment.SegmentPoints)
                    {
                        var ellipse = GetEllipseAt(spoint.X, spoint.Y);
                        if (ellipse != null)
                        {
                            ellipse.Fill = Brushes.Yellow;
                        }
                    }
                }
                else
                {
                    if (btnEnableMagnet.IsChecked == true)
                    {
                        var mPos = Helpers.GetCircleNonTransparentFirstPixel(_terrainImage.Source as BitmapSource, _imagePixels, e.GetPosition(_terrainImage));
                        if (mPos.HasValue)
                        {
                            pos = mPos.Value;
                        }
                    }
                    AddPoint(pos);
                }
            }

            if (e.RightButton == MouseButtonState.Pressed)
            {
                var pos = e.GetPosition(grid);

                if (Keyboard.IsKeyDown(Key.LeftShift))
                {
                    var ellipse = e.OriginalSource as Ellipse;
                    if (ellipse != null)
                    {
                        var ellTrans = ellipse.RenderTransform as TranslateTransform;
                        var ellPos = new Point(ellTrans.X + ellipse.Width / 2, ellTrans.Y + ellipse.Height / 2);
                        RemovePoint(ellPos, ellipse);
                    }
                }
                else
                {
                    var mPos = Helpers.GetCircleNonTransparentFirstPixel(_terrainImage.Source as BitmapSource, _imagePixels, e.GetPosition(_terrainImage));
                    if (mPos.HasValue)
                    {
                        pos = mPos.Value;
                    }
                    AddPoint(pos, true);
                }
            }
        }

        private void Grid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Released)
            {
                _dragStartPos = null;
                _isMiddlePressed = false;
                _startDragZoom = new Point(0, 0);

                if (_terrainImage != null)
                {
                    _terrainImage.ReleaseMouseCapture();
                }
            }
        }

        private void btnCenter_Click(object sender, RoutedEventArgs e)
        {
            if (_terrainImage == null) return;

            terrainTranslate.X = (grid.ActualWidth / 2) - (_terrainImage.ActualWidth / 2);
            terrainTranslate.Y = (grid.ActualHeight / 2) - (_terrainImage.ActualHeight / 2);
        }

        private void RemovePoint(Point pos, Ellipse ellipse)
        {
            grid.Children.Remove(ellipse);

            TerrainPoint tPoint = null;
            TerrainSegment tSeg = null;

            foreach (var seg in _terrain.Segments)
            {
                tPoint = seg.SegmentPoints.FirstOrDefault(x => x.X == pos.X && x.Y == pos.Y);
                if (tPoint != null)
                {
                    var rem = seg.SegmentPoints.Remove(tPoint);
                    tSeg = seg;
                    var msg = rem ? "point " : "nothing ";
                    if (seg.StartPoint == tPoint)
                    {
                        rem = _terrain.Segments.Remove(tSeg);
                        msg = rem ? "start point " : "nothing ";
                    }
                    if (seg.EndPoint == tPoint)
                    {
                        seg.EndPoint = null;
                        msg = "end point ";
                    }

                    Debug.WriteLine(msg + "was removed.");
                    break;
                }
            }
        }

        private Ellipse GetEllipseAt(double x, double y)
        {
            foreach (var elm in grid.Children)
            {
                var ell = elm as Ellipse;
                if (ell != null)
                {
                    var trans = ell.RenderTransform as TranslateTransform;
                    if ((trans.X + ell.Width / 2 == x) && (trans.Y + ell.Height / 2 == y))
                    {
                        return ell;
                    }
                }
            }
            return null;
        }

        private void AddPoint(Point pos, bool isEndPoint = false, bool addToModel = true)
        {
            var circle = new Ellipse
            {
                Width = 5,
                Height = 5,
                Fill = isEndPoint ? Brushes.Red : Brushes.Blue,
            };

            var addToCanvas = true;
            if (addToModel)
            {
                var terrainPoint = new TerrainPoint { X = pos.X, Y = pos.Y };
                if (!_terrain.Segments.Any())
                {
                    _terrain.Segments.Add(new TerrainSegment { StartPoint = terrainPoint });
                    Debug.WriteLine("1st point");
                }

                var lastSeg = _terrain.Segments.Last();
                if (lastSeg.EndPoint == null)
                {
                    lastSeg.SegmentPoints.Add(terrainPoint);
                    if (isEndPoint)
                    {
                        lastSeg.EndPoint = terrainPoint;
                        Debug.WriteLine("end point");
                        if (lastSeg.EndPoint == lastSeg.StartPoint)
                        {
                            _terrain.Segments.Remove(lastSeg);
                            grid.Children.Remove(circle);
                            return;
                        }
                    }
                    else
                    {
                        Debug.WriteLine("new point");
                    }
                }
                else
                {
                    if (!isEndPoint)
                    {
                        lastSeg = new TerrainSegment { StartPoint = terrainPoint };
                        _terrain.Segments.Add(lastSeg);
                        lastSeg.SegmentPoints.Add(terrainPoint);

                        Debug.WriteLine("new segment");
                    }
                    else
                    {
                        addToCanvas = false;
                        Debug.WriteLine("adding escaped");
                    }
                }
            }

            if (addToCanvas)
            {
                grid.Children.Add(circle);
                circle.SetValue(Canvas.ZIndexProperty, 100);

                var translation = new TranslateTransform { X = pos.X - circle.Width / 2, Y = pos.Y - circle.Height / 2 };
                circle.RenderTransform = translation;
            }
        }

        private void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            var openDlg = new OpenFileDialog();
            openDlg.DefaultExt = ".tep";
            openDlg.Filter = "(*.tep)|*.tep";
            openDlg.Multiselect = false;
            openDlg.RestoreDirectory = true;
            openDlg.CheckFileExists = true;

            if (openDlg.ShowDialog() == true)
            {
                _projectPath = openDlg.FileName;

                _terrain = Helpers.LoadProject(_projectPath);
                ReloadTerrain();
            }
        }

        private void ReloadTerrain()
        {
            grid.Children.Clear();

            var path = _terrain.ImagePath;
            if (!string.IsNullOrEmpty(_projectPath))
            {
                path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(_projectPath), path);
            } 
            SetImage(path);

            foreach (var seg in _terrain.Segments)
            {
                foreach (var po in seg.SegmentPoints)
                {
                    AddPoint(new Point(po.X, po.Y), seg.EndPoint.Equals(po), false);
                }
            }
        }

        private void btnEnableMagnet_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void btnSelectSegment_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedSegment != null && btnSelectSegment.IsChecked != true)
            {
                foreach (var sp in _selectedSegment.SegmentPoints)
                {
                    var ell = GetEllipseAt(sp.X, sp.Y);
                    if (ell != null)
                    {
                        ell.Fill = sp.Equals(_selectedSegment.EndPoint) ? Brushes.Red : Brushes.Blue;
                    }
                }
            }
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new HelpWindow();
            dlg.Show();
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                if (_selectedSegment != null)
                {
                    _terrain.Segments.Remove(_selectedSegment);
                    _selectedSegment = null;
                    btnSelectSegment.IsChecked = false;

                    ReloadTerrain();
                }
            }
        }
    }
}
