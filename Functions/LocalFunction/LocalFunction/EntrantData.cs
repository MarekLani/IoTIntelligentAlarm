using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalFunction
{
    public class EntrantData
    {
        public string imageUri { get; set; }

        public bool facesDetected { get; set; }
        public bool personIdentified { get; set; }

        public DateTime timeStamp { get; set; }

    }

    public class EntrantOutputData
    {
        public string id { get; set; }
        public string imageUri { get; set; }

        public bool facesDetected { get; set; }
        public bool personIdentified { get; set; }

        public DateTime timeStamp { get; set; }
        public string entrantName { get; set; }
    }
}
