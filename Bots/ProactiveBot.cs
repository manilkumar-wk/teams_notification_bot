// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Newtonsoft.Json;
using ProactiveBot.Helpers;
using ProactiveBot.Models;

namespace Microsoft.BotBuilderSamples
{
    public class ProactiveBot : TeamsActivityHandler
    {
        // Message to send to users when the bot receives a Conversation Update event
        private const string WelcomeMessage =
            "Welcome to the Proactive Bot sample.  Navigate to http://localhost:3978/api/notify to proactively message everyone who has previously messaged this bot.";

        // Dependency injected dictionary for storing ConversationReference objects used in NotifyController to proactively message users
        private readonly ConcurrentDictionary<
            string,
            ConversationReference
        > _conversationReferences;

        public ProactiveBot(
            ConcurrentDictionary<string, ConversationReference> conversationReferences
        )
        {
            _conversationReferences = conversationReferences;
        }

        private void AddConversationReference(Activity activity)
        {
            var conversationReference = activity.GetConversationReference();
            _conversationReferences.AddOrUpdate(
                conversationReference.User.Id,
                conversationReference,
                (key, newValue) => conversationReference
            );
        }

        protected override Task OnConversationUpdateActivityAsync(
            ITurnContext<IConversationUpdateActivity> turnContext,
            CancellationToken cancellationToken
        )
        {
            AddConversationReference(turnContext.Activity as Activity);

            return base.OnConversationUpdateActivityAsync(turnContext, cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(
            IList<ChannelAccount> membersAdded,
            ITurnContext<IConversationUpdateActivity> turnContext,
            CancellationToken cancellationToken
        )
        {
            foreach (var member in membersAdded)
            {
                // Greet anyone that was not the target (recipient) of this message.
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(
                        MessageFactory.Text(WelcomeMessage),
                        cancellationToken
                    );
                }
            }
        }

        protected override async Task OnMessageActivityAsync(
            ITurnContext<IMessageActivity> turnContext,
            CancellationToken cancellationToken
        )
        {
            AddConversationReference(turnContext.Activity as Activity);

            // Echo back what the user said
            await turnContext.SendActivityAsync(
                MessageFactory.Text($"You sent '{turnContext.Activity.Text}'"),
                cancellationToken
            );
        }

        protected override async Task OnInstallationUpdateActivityAsync(
            ITurnContext<IInstallationUpdateActivity> turnContext,
            CancellationToken cancellationToken
        )
        {
            string filePath = "";

            if (turnContext.Activity.Action.Equals("add"))
            {
                TeamsChannelAccount teamsUser = await TeamsInfo.GetMemberAsync(
                    turnContext,
                    turnContext.Activity.From.Id,
                    cancellationToken
                );
                var botUser = new BotUserEntity()
                {
                    TenantId = turnContext.Activity.Conversation.TenantId,
                    Email = teamsUser.Email,
                    UserId = turnContext.Activity.From.Id,
                    Name = teamsUser.Name,
                    ConversationId = turnContext.Activity.Conversation.Id,
                    ServiceUrl = turnContext.Activity.ServiceUrl
                };
                string jsonString = JsonConvert.SerializeObject(botUser, Formatting.Indented);
                string documentsPath = Environment.GetFolderPath(
                    Environment.SpecialFolder.MyDocuments
                );

                string fileName = "data.json";

                filePath = Path.Combine(documentsPath, fileName);

                try
                {
                    File.WriteAllText(filePath, jsonString);
                    Console.WriteLine($"JSON data successfully written to {filePath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error writing JSON data to file: {ex.Message}");
                }
                await turnContext.SendActivityAsync(
                    MessageFactory.Text($"Welcome to the bot"),
                    cancellationToken
                );
            }
            else if (turnContext.Activity.Action.Equals("remove"))
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Console.WriteLine($"JSON file successfully deleted from {filePath}");
                }
            }
        }
    }
}
