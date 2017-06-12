using Microsoft.Azure.Devices;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.ProjectOxford.Face.Contract;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace LocalFunction
{
    public static class AlarmEventProcessor
    {


        private static DateTime identifiedNotSentTime = DateTime.MinValue;
        private static DateTime entryNotSentTime = DateTime.MinValue;


        [FunctionName("EventHubTriggerCSharp")]
        public static async Task Run([EventHubTrigger("alarmevents", Connection = "AlarmEventHub")]string myEventHubMessage, [DocumentDB(databaseName: "EntriesData", collectionName: "EntriesDataCollection", ConnectionStringSetting = "marekiotdemo_DOCUMENTDB")] ICollector<object> outputDocument, TraceWriter log)
        {
            log.Info($"C# Event Hub trigger function processed a message: {myEventHubMessage}");
            FaceServiceHelper.ApiKey = System.Configuration.ConfigurationSettings.AppSettings["FaceApiKey"].ToString();
            
            string personGroupId = "4c8ab3f7-7af5-4a89-ad7e-5544c67227f0";

            var fh = new FaceServiceHelper();

            var ed = JsonConvert.DeserializeObject<EntrantData>(myEventHubMessage);
            log.Info(ed.ToString());

            if ((DateTime.Now - entryNotSentTime).Seconds > 30)
            {
                SendNotification($"New entry");
                //notification.Add( $@"<toast><visual><binding template=""ToastText01""><text id=""1"">New entry</text></binding></visual></toast>");
                entryNotSentTime = DateTime.Now;
                SendBotNotification(myEventHubMessage);
            }

            //Entrant was not identified!!
            if (!ed.personIdentified)
            {
                SendNotification($"!!Unidentified entrant!!");
                //notification.Add( $@"<toast><visual><binding template=""ToastText01""><text id=""1"">Unidentified entrant</text></binding></visual></toast>");
                entryNotSentTime = DateTime.Now;
                SendBotNotification(myEventHubMessage);
            }

            //Fire Microsoft flow
            // var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://prod-05.westeurope.logic.azure.com:443/workflows/a005022065c0481e819688b70ab115db/triggers/manual/paths/invoke?api-version=2016-06-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=w2McQlP6-7kN2Ga3nTz1eERShzQMWMr6SCH_yxAes18");
            // httpWebRequest.ContentType = "application/json";
            // httpWebRequest.Method = "POST";
            // using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            // {
            //     string s = $"{{\"actualSmogLevel\":5}}";

            //     streamWriter.Write(s);
            //     streamWriter.Flush();
            //     streamWriter.Close();
            // }
            // var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

            EntrantOutputData eod = new EntrantOutputData();

            eod.id = Guid.NewGuid().ToString();
            eod.imageUri = ed.imageUri;
            eod.facesDetected = ed.facesDetected;
            eod.timeStamp = ed.timeStamp;
            eod.personIdentified = ed.personIdentified;
            eod.entrantName = "";
            if (ed.facesDetected)
            {
                var faces = await fh.DetectAsync(System.Configuration.ConfigurationSettings.AppSettings["StorageContainerUri"].ToString() + ed.imageUri);

                Guid[] detectedFaceIds = faces?.Select(f => f.FaceId).ToArray();
                IdentifyResult[] groupResults = await fh.IdentifyAsync(personGroupId, detectedFaceIds);
                foreach (var match in groupResults)
                {
                    if (!match.Candidates.Any())
                    {
                        continue;
                    }

                    Person person = await fh.GetPersonAsync(personGroupId, match.Candidates[0].PersonId);
                    //Send message to device, so we do not activate alarm
                    string connectionString = System.Configuration.ConfigurationSettings.AppSettings["IoTHub"].ToString();
                    ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(connectionString);
                    var commandMessage = new Message(Encoding.ASCII.GetBytes("{\"personIdentified\":1}"));
                    await serviceClient.SendAsync("Raspberry1", commandMessage);

                    log.Info(person.Name);
                    eod.entrantName = person.Name.ToLower();

                    //We want to send notifications only every 30 seconds
                    if ((DateTime.Now - identifiedNotSentTime).Seconds > 30)
                    {
                        SendNotification($"Entrant: {person.Name}");
                        //notification.Add( $@"<toast><visual><binding template=""ToastText01""><text id=""1"">Entrant: {person.Name}</text></binding></visual></toast>");
                        identifiedNotSentTime = DateTime.Now;
                        SendBotNotification(JsonConvert.SerializeObject(eod));
                    }

                }
            }
            outputDocument.Add(eod);
        }

        public static async void SendBotNotification(string message)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://localhost:3979/api/NotificationInvoke");
                var requestData = new StringContent(message, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(String.Format("http://localhost:3979/api/NotificationInvoke"), requestData);
            }
        }

        public static async void SendNotification(string payload)
        {
            NotificationHubClient hub = NotificationHubClient
                 .CreateClientFromConnectionString("Endpoint=sb://iotnothubnamespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=62LuWtoHpfvsi/am4iqSJM+I+D2Q626Y/EEw9HiYntQ=", "IoTDemoNotHub");
            var toast = $@"<toast><visual><binding template=""ToastText01""><text id=""1"">{payload}</text></binding></visual></toast>";
            await hub.SendWindowsNativeNotificationAsync(toast);
        }
    }
}