using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace IoTDemoSharedLibrary
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

        private string _imageUri;
        [JsonProperty("imageUri")]
        public string ImageUri { get { return "https://marekiotdemostrg.blob.core.windows.net/entrantsphotos/" + _imageUri; } set { _imageUri = value; } }


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
