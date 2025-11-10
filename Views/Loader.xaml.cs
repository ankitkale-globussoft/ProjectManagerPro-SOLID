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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ProjectManagerPro_SOLID.Views
{
    /// <summary>
    /// Interaction logic for Loader.xaml
    /// </summary>
    public partial class Loader : UserControl
    {
        private const int DotCount = 8;
        private const double Radius = 40;
        private const double DotSize = 12;

        private Ellipse[] dots;
        private double angle = 0;
        private DispatcherTimer timer;

        public Loader()
        {
            InitializeComponent();
            Loaded += Loader_Loaded;
            Unloaded += Loader_Unloaded;
        }

        private void Loader_Loaded(Object sender, RoutedEventArgs e)
        {
            CreateDots();
            StartAnimation();
        }

        private void CreateDots()
        {
            LoaderCanvas.Children.Clear();
            dots = new Ellipse[DotCount];

            for(int i=0; i < DotCount; i++)
            {
                var dot = new Ellipse
                {
                    Width = DotSize,
                    Height = DotSize,
                    Fill = Brushes.DeepSkyBlue,
                    Opacity = (double)i / DotCount
                };
                LoaderCanvas.Children.Add(dot);
                dots[i] = dot;
            }
        }

        private void StartAnimation()
        {
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(80)
            };

            timer.Tick += (s, e) =>
            {
                angle += 360.0 / DotCount;
                UpdateDots();
            };

            timer.Start();
        }

        private void UpdateDots()
        {
            double centerX = LoaderCanvas.Width / 2;
            double centerY = LoaderCanvas.Width / 2;

            for(int i=0; i < DotCount; i++)
            {
                double currentAngle = (angle + (360.0 / DotCount) * i) * Math.PI / 180;
                double x = centerX + Math.Cos(currentAngle) * Radius - DotSize / 2;
                double y = centerY + Math.Sin(currentAngle) * Radius - DotSize / 2;

                Canvas.SetLeft(dots[i], x);
                Canvas.SetTop(dots[i], y);
                dots[i].Opacity = (double)(i + 1) / DotCount;
            }
        }

        private void Loader_Unloaded(object sender, RoutedEventArgs e)
        {
            timer?.Stop();
        }
    }
}
