using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace IoTUWPDemo
{
    public class ViewModel : ViewModelBase
    {
        private int _temp = 20;
        public int Temp {
            get { return _temp; }
            set { _temp = value; RaisePropertyChanged("Temp"); }
        }

        private int _hum = 20;
        public int Hum
        {
            get { return _hum; }
            set { _hum = value; RaisePropertyChanged("Hum"); }
        }

        private bool _buzzer = false;
        public bool Buzzer
        {
            get { return _buzzer; }
            set { _buzzer = value; RaisePropertyChanged("Buzzer"); }
        }

        private string _displayText = "";
        public string DisplayText
        {
            get { return _displayText; }
            set { _displayText = value; RaisePropertyChanged("DisplayText"); }
        }

        private SolidColorBrush _displayBackground = new SolidColorBrush(Colors.White);
        public SolidColorBrush DisplayBackground
        {
            get { return _displayBackground; }
            set { _displayBackground = value; RaisePropertyChanged("DisplayBackground"); }
        }

        private double _distance = 80;
        public double Distance
        {
            get { return _distance; }
            set { Set(() => Distance, ref _distance, value); }
        }
    }
}
