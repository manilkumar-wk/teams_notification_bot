using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Newtonsoft.Json;
using ProactiveBot.Models;

namespace ProactiveBot.Helpers
{
    public class BotHelper
    {
        internal async Task SaveUserDetails(
            ITurnContext<IInstallationUpdateActivity> turnContext,
            CancellationToken cancellationToken
        )
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

            // Get the path to the Documents folder on macOS
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            // Specify the file name
            string fileName = "data.json";

            // Combine the documents path and file name to get the full file path
            string filePath = Path.Combine(documentsPath, fileName);

            // Write JSON string to file
            try
            {
                File.WriteAllText(filePath, jsonString);
                Console.WriteLine($"JSON data successfully written to {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing JSON data to file: {ex.Message}");
            }
        }

        public BotUserEntity ReadUSerDetails(string email)
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string filePath = Path.Combine(documentsPath, "data.json");

            // Read JSON data from file
            try
            {
                string jsonString = File.ReadAllText(filePath);

                // Deserialize JSON string to object
                var jsonData = JsonConvert.DeserializeObject(jsonString);
                BotUserEntity botUser = JsonConvert.DeserializeObject<BotUserEntity>(jsonString);
                Console.WriteLine("JSON data read from file:");
                Console.WriteLine(jsonData); // Output the deserialized object
                return botUser;
            }
            catch (Exception ex)
            {
                return null;
                Console.WriteLine($"Error reading JSON data from file: {ex.Message}");
            }
        }
    }
}
