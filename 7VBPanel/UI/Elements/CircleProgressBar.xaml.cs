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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace _7VBPanel.UI.Elements
{
    /// <summary>
    /// Interaction logic for CircleProgressBar.xaml
    /// </summary>
    public partial class CircleProgressBar : UserControl
    {
        private double progress;

        public double Progress
        {
            get { return progress; }
            set
            {
                if (progress != value)
                {
                    progress = value;
                    OnPropertyChanged("Progress");
                    UpdateProgressBar();
                }
            }
        }
        private double maxValue = 100;
        public double MaxValue
        {
            get { return maxValue; }
            set
            {
                if (maxValue != value)
                {
                    maxValue = value;
                    OnPropertyChanged("MaxValue");
                    UpdateMaxValue();
                }
            }
        }
        private string _NameText;
        public string NameText
        {
            get { return _NameText; }
            set
            {
                if (_NameText != value)
                {
                    _NameText = value;
                    OnPropertyChanged("NameText");
                    UpdateProgressBarText();
                }
            }
        }
        private string _TextValue;
        public string TextValue
        {
            get { return _TextValue; }
            set
            {
                if (_TextValue != value)
                {
                    _TextValue = value;
                    OnPropertyChanged("TextValue");
                    UpdateProgressBarTextValue();
                }
            }
        }
        public CircleProgressBar()
        {
            InitializeComponent();
            Progress = 75;
        }
        public void UpdateProgressBarTextValue()
        {
            ProgressBarTextValue.Text = _TextValue;
        }
       
        private void UpdateMaxValue()
        {
            UpdateProgressBar();
        }
        private void UpdateProgressBarText()
        {
            ProgressBarText.Text = NameText;
        }
        private void UpdateProgressBar()
        {
            ProgressText.Text = $"{Progress}%";

            double angle = Progress / maxValue * 360;
            double radians = (angle - 90) * Math.PI / 180;

            double radius = 25 - 2.5;
            double centerX = 25;
            double centerY = 25;

            double x = centerX + radius * Math.Cos(radians);
            double y = centerY + radius * Math.Sin(radians);

            bool isLargeArc = angle > 180.0;

            PathFigure pathFigure = new PathFigure
            {
                StartPoint = new Point(centerX, centerY - radius),
                IsClosed = false
            };

            ArcSegment arcSegment = new ArcSegment
            {
                Point = new Point(x, y),
                Size = new Size(radius, radius),
                IsLargeArc = isLargeArc,
                SweepDirection = SweepDirection.Clockwise
            };

            PathSegmentCollection segments = new PathSegmentCollection();
            segments.Add(arcSegment);

            PathFigureCollection figures = new PathFigureCollection();
            pathFigure.Segments = segments;
            figures.Add(pathFigure);

            ProgressGeometry.Figures = figures;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
