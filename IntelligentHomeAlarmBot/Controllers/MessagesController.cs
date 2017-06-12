using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using Microsoft.Bot.Builder.Dialogs;

namespace IntelligentHomeAlarmBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {

                ConversationStarter.channelId = activity.ChannelId;
                ConversationStarter.conversationId = activity.Conversation.Id;
                
                ConversationStarter.fromId = activity.Recipient.Id;
                ConversationStarter.fromName = activity.Recipient.Name;
                
                ConversationStarter.serviceUrl = activity.ServiceUrl;
                ConversationStarter.toName = activity.From.Name;
                ConversationStarter.toId = activity.From.Id;

                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                Activity reply = activity.CreateReply();
                reply.Type = ActivityTypes.Typing;
                reply.Text = null;
                //reply.Locale = "sk-SK";
                await connector.Conversations.ReplyToActivityAsync(reply);

                //We want to send greeting just once
                StateClient stateClient = activity.GetStateClient();
                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                if (!userData.GetProperty<bool>("SentGreeting"))
                {
                    activity.CreateReply("Hi, I am your household watcher.\nI can give you information about when was your house entered last. Who entered the last or when did certain person arrive home. Just try to ask me on these things.");
                    userData.SetProperty<bool>("SentGreeting", true);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                }
                else
                {


                    try
                    {
                        await Conversation.SendAsync(activity, () => new AlarmLuisDialog());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}