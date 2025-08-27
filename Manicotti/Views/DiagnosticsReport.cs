using System.ComponentModel;

namespace Manicotti.Views
{
    public class DiagnosticsReport : INotifyPropertyChanged
    {
        private int _totalEntities;
        public int TotalEntities { get => _totalEntities; set { _totalEntities = value; OnPropertyChanged(nameof(TotalEntities)); } }

        private int _wallCount;
        public int WallCount { get => _wallCount; set { _wallCount = value; OnPropertyChanged(nameof(WallCount)); } }

        private int _columnCount;
        public int ColumnCount { get => _columnCount; set { _columnCount = value; OnPropertyChanged(nameof(ColumnCount)); } }

        private int _doorCount;
        public int DoorCount { get => _doorCount; set { _doorCount = value; OnPropertyChanged(nameof(DoorCount)); } }

        private int _windowCount;
        public int WindowCount { get => _windowCount; set { _windowCount = value; OnPropertyChanged(nameof(WindowCount)); } }

        private int _nonZeroZCount;
        public int NonZeroZCount { get => _nonZeroZCount; set { _nonZeroZCount = value; OnPropertyChanged(nameof(NonZeroZCount)); } }

        private int _duplicateCount;
        public int DuplicateCount { get => _duplicateCount; set { _duplicateCount = value; OnPropertyChanged(nameof(DuplicateCount)); } }

        private double _maxGap;
        public double MaxGap { get => _maxGap; set { _maxGap = value; OnPropertyChanged(nameof(MaxGap)); } }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Reset()
        {
            TotalEntities = 0;
            WallCount = 0;
            ColumnCount = 0;
            DoorCount = 0;
            WindowCount = 0;
            NonZeroZCount = 0;
            DuplicateCount = 0;
            MaxGap = 0.0;
        }
    }
}
