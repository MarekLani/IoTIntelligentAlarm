using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace IntelligentHomeAlarmBot
{
    [LuisModel("appID", "LUIS KEY")]
    [Serializable]
    class AlarmLuisDialog : LuisDialog<object>
    {

        Configuration config = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration(null);

        [LuisIntent("LastEntry")]
        public async Task ShowLastEntrant(IDialogContext context, LuisResult result)
        {
            Entry e;
            using (var client = new HttpClient())
            {
                var s = ConfigurationManager.AppSettings["BackendBaseUri"] + $"Entries/GetLast";
                var response =
                    await client.GetStringAsync(new Uri(s));
                e = JsonConvert.DeserializeObject<Entry>(response);
            }

            if (e.EntrantName != "")
            {
                var replyToConversation = context.MakeMessage();
                
                replyToConversation.Attachments = new List<Attachment>();
                List<CardImage> cardImages = new List<CardImage>();
                cardImages.Add(new CardImage(url: e.ImageUri));


                HeroCard plCard = new HeroCard()
                {
                    Title = "Last entry was at " + e.TimeStamp.ToString("HH:mm dd-MM-yyyy"),
                    Subtitle = "It was " + e.EntrantName,
                    Images = cardImages,
                    
                };
                Attachment plAttachment = plCard.ToAttachment();
                replyToConversation.Attachments.Add(plAttachment);
                await context.PostAsync(replyToConversation);
                
            }
            else
                await context.PostAsync($"Warning, last entrant was not recognized! It was at {e.TimeStamp.ToString("HH:mm")} on {e.TimeStamp.ToString("dd.MM.yyyy")}");
            
            context.Wait(MessageReceived);  
        }

        [LuisIntent("ShowMeTodaysEntrants")]
        public async Task ShowTodaysEntrants(IDialogContext context, LuisResult result)
        {
        
            List<Entry> e;
            using (var client = new HttpClient())
            {
                var s = ConfigurationManager.AppSettings["BackendBaseUri"] + $"Entrants";
                var response =
                    await client.GetStringAsync(new Uri(s));
                e = JsonConvert.DeserializeObject<List<Entry>>(response);
            }

            var uniqueEntrants = 
                 (from entry in e
                  select entry.EntrantName).Distinct().OrderBy(name => name);

            string names = "Entrants for today: ";

            foreach(var en in uniqueEntrants)
            {
                names += $"|{en}|";
            }
            
           await context.PostAsync($"{names}");
            context.Wait(MessageReceived);
        }

        [LuisIntent("PersonArrived")]
        public async Task PersonArrived(IDialogContext context, LuisResult result)
        {
            EntityRecommendation er;
            result.TryFindEntity("Person", out er);
            var name = er.Entity.ToString();
            Entry e;
            using (var client = new HttpClient())
            {
                var s = ConfigurationManager.AppSettings["BackendBaseUri"] + $"Entrants/{name}";
                var response =
                    await client.GetStringAsync(new Uri(s));
                e = JsonConvert.DeserializeObject<Entry>(response);
            }

            if(e.EntrantName != "")
            {
                var replyToConversation = context.MakeMessage();

                replyToConversation.Attachments = new List<Attachment>();
                List<CardImage> cardImages = new List<CardImage>();
                cardImages.Add(new CardImage(url: e.ImageUri));


                HeroCard plCard = new HeroCard()
                {
                    Title = e.EntrantName + " entered last at:" + e.TimeStamp.ToString("HH:mm dd-MM-yyyy"),
                    Images = cardImages,

                };
                Attachment plAttachment = plCard.ToAttachment();
                replyToConversation.Attachments.Add(plAttachment);
                await context.PostAsync(replyToConversation);
            }
            else
                await context.PostAsync("No entry for this person");


            context.Wait(MessageReceived);
        }

        [LuisIntent("")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            string message = $"Sorry I did not understand: " + string.Join(", ", result.Intents.Select(i => i.Intent));
            await context.PostAsync(message);
            context.Wait(MessageReceived);
        }
    }
}