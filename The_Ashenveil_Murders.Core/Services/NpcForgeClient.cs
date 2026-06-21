using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace The_Ashenveil_Murders.Core.Services
{
    public class DialogueResponse
    {
        public string dialogue { get; set; } = "...";
        public string action { get; set; } = "idle";
    }

    public static class NpcForgeClient
    {
        private static readonly HttpClient Http = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5000"),
            Timeout = TimeSpan.FromSeconds(25)
        };

        private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public static async Task<bool> CheckReady()
        {
            try
            {
                var r = await Http.GetAsync("/status");
                var body = await r.Content.ReadAsStringAsync();
                return body.Contains("ready", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        public static async Task<DialogueResponse> GetDialogue(
            string npcName, string role, string voicePrompt,
            string playerSaid, string context, string[] actions)
        {
            var payload = new
            {
                npc_name = npcName,
                role,
                voice_prompt = voicePrompt,
                player_said = playerSaid,
                context,
                actions
            };
            try
            {
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var r = await Http.PostAsync("/npc/dialogue", content);
                var body = await r.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<DialogueResponse>(body, JsonOpts) ?? new DialogueResponse();
            }
            catch { return new DialogueResponse(); }
        }

        public static async Task<string> GetWorldDescription(string location, string timeOfDay = "day", string weather = "fog")
        {
            var payload = new { location, time_of_day = timeOfDay, weather };
            try
            {
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var r = await Http.PostAsync("/world/describe", content);
                var body = await r.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);
                return doc.RootElement.GetProperty("description").GetString() ?? "";
            }
            catch { return ""; }
        }
    }
}
