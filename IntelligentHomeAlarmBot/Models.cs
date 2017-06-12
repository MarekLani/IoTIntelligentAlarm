using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace IntelligentHomeAlarmBot
{
    public class Entry
    {
  

        
        [JsonProperty("imageUri")]
        public string ImageUri { get; set; }


        [JsonProperty("facesDetected")]
        public bool FacesDetected { get; set; }

        [JsonProperty("personIdentified")]
        public bool PersonIdentified { get; set; }

        [JsonProperty("timeStamp")]
        public DateTime TimeStamp { get; set; }

        [JsonProperty("entrantName")]
        public string EntrantName { get; set; } = "";
    }
}