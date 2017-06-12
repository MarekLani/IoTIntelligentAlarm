using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace IoTDemoMobileApp
{
    public abstract class Entity
    {
        public Entity()
        {
           
        }
        /// <summary>
        /// Object unique identifier
        /// </summary>
        [Key]
        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public class Entry : Entity
    {
        public Entry(string type) : base()
        {
        }


        [JsonProperty("imageUri")]
        public string ImageUri { get;  set;}

        [JsonProperty("facesDetected")]
        public bool FacesDetected { get; set; }

        [JsonProperty("personIdentified")]
        public bool PersonIdentified { get; set; }

        [JsonProperty("timeStamp")]
        public DateTime TimeStamp { get; set; }

        [JsonProperty("entrantName")]
        public string EntrantName { get; set; }
    }
}
