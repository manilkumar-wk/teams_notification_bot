using System;
using Newtonsoft.Json;

namespace ProactiveBot.Models
{
    public class NotificationPayload
    {
        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("messageType")]
        public string MessageType { get; set; }
    }
}
