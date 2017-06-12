using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotUWPDemo
{
    public class SensorData
    {
        public Guid id { get; set; }
        public double temp { get; set; }
        public double humidity { get; set; }
        public DateTime timeStamp { get; set; }
       
    }

    public class EntryData
    {
        public string imageUri { get; set; }

        public bool facesDetected { get; set; }

        public bool personIdentified { get; set; }

        public DateTime timeStamp { get; set; }
    }
}
