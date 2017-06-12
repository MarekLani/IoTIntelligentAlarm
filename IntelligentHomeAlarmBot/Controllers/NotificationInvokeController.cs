using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace IntelligentHomeAlarmBot.Controllers
{
    public class NotificationInvokeController : ApiController
    {
        [HttpPost]
        
        [Route("api/NotificationInvoke")]
        public async Task<HttpResponseMessage> SendMessage([FromBody]Entry entry)
        {
            try
            {
                if (!string.IsNullOrEmpty(ConversationStarter.fromId))
                {
                    await ConversationStarter.Resume(ConversationStarter.conversationId, ConversationStarter.channelId, entry); //We don't need to wait for this, just want to start the interruption here

                    var resp = new HttpResponseMessage(HttpStatusCode.OK);
                    resp.Content = new StringContent($"<html><body>Message sent, thanks.</body></html>", System.Text.Encoding.UTF8, @"text/html");
                    return resp;
                }
                else
                {
                    var resp = new HttpResponseMessage(HttpStatusCode.OK);
                    resp.Content = new StringContent($"<html><body>You need to talk to the bot first so it can capture your details.</body></html>", System.Text.Encoding.UTF8, @"text/html");
                    return resp;
                }
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex);
            }
        }
    }
}
