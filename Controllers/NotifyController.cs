// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;using Microsoft.AspNetCore.Http.HttpResults;using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Teams;using Microsoft.Bot.Connector;using Microsoft.Bot.Connector.Authentication;using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using ProactiveBot.Helpers;using ProactiveBot.Models;namespace Microsoft.BotBuilderSamples.Controllers
{
    [Route("api/notify")]
    [ApiController]
    public class NotifyController : ControllerBase
    {
        private readonly IBotFrameworkHttpAdapter _adapter;
        private readonly string _appId;
        private readonly string _appPassword;
        private readonly string _apiKey;
        private readonly BotHelper _botHelper;
        private readonly ConcurrentDictionary<string, ConversationReference> _conversationReferences;

        public NotifyController(IBotFrameworkHttpAdapter adapter, IConfiguration configuration, ConcurrentDictionary<string, ConversationReference> conversationReferences, BotHelper botHelper)
        {
            _adapter = adapter;
            _conversationReferences = conversationReferences;
            _appId = configuration["MicrosoftAppId"] ?? string.Empty;
            _appPassword = configuration["MicrosoftAppPassword"] ?? string.Empty;
            _apiKey = configuration["ApiKey"] ?? string.Empty;
            _botHelper = botHelper;
        }

        public async Task<IActionResult> Get()
        {
            foreach (var conversationReference in _conversationReferences.Values)
            {
                await ((BotAdapter)_adapter).ContinueConversationAsync(_appId, conversationReference, BotCallback, default(CancellationToken));
            }            return new ContentResult()            {                Content = "<html><body><h1>Proactive messages have been sent.</h1></body></html>",                ContentType = "text/html",                StatusCode = (int)HttpStatusCode.OK,            };
        }
        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] NotificationPayload payload)        {            string apikey = Request.Headers["Api-Key"].ToString();            if (string.IsNullOrWhiteSpace(apikey) || apikey != _apiKey)                return Unauthorized();            var credentials = new MicrosoftAppCredentials(_appId, _appPassword);            var users = _botHelper.ReadUSerDetails(payload.Email);            //await GenerateConversationId(payload, users);            if (users.Email == payload.Email)            {                var activity = CreateBotActivity(payload.Message);                await SendProactiveMessage(credentials, users.ServiceUrl, users.ConversationId, activity);                return new ObjectResult("Notification Sent");            }            else            {                return NotFound(new { message = "We couldn't find a user with the provided details. Please check and try again." });            }        }        private async Task SendProactiveMessage(MicrosoftAppCredentials credentials, string serviceUrl, string conversationId, Activity activity)        {            var connectiorClient = new ConnectorClient(new Uri(serviceUrl), credentials);            //await SendNo             await connectiorClient.Conversations.SendToConversationAsync(conversationId, activity);        }        private Activity CreateBotActivity(string message)        {            var attachments = new List<Attachment>();            Activity activity = (Activity)MessageFactory.Attachment(attachments);            activity.Attachments = new List<Attachment>()            {                new Attachment()                {                    ContentType=AdaptiveCard.ContentType,                    Content=CreateAdaptiveCard(message)                }            };            activity.Summary = message;            activity.TeamsNotifyUser();            return activity;        }        private AdaptiveCard CreateAdaptiveCard(string message)        {            return new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))            {                Body = new List<AdaptiveElement>()                {                    new AdaptiveTextBlock()                    {                        Text=message,                        Size=AdaptiveTextSize.Medium,                        Weight=AdaptiveTextWeight.Bolder,                        Wrap=true                    }                }            };        }        private async Task BotCallback(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync("proactive hello");
        }
        private async Task<string> GenerateConversationId(NotificationPayload payload, BotUserEntity user)        {            var _serviceUrl = user.ServiceUrl;            var userId = "testuser"; //user.Email;            var botId = "3774a476-1561-4868-b043-c020d9931af9";            var botName = "bot_notification_test";            var tenantId = user.TenantId;            var credentials = new MicrosoftAppCredentials(_appId, _appPassword);            var connectorClient = new ConnectorClient(new Uri(_serviceUrl), credentials);            var parameters = new ConversationParameters            {                Members = new[] { new ChannelAccount(userId) },                Bot = new ChannelAccount(botId, botName),                TenantId = tenantId,                IsGroup = false            };            try            {                var conversationResource = await connectorClient.Conversations.CreateConversationAsync(parameters);                return conversationResource.Id;            }            catch (Exception ex)            {                Console.WriteLine($"Error creating conversation: {ex.Message}");                //throw;            }            return ""; ;
        }
    }}
