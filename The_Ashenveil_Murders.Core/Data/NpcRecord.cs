namespace The_Ashenveil_Murders.Core.Data
{
    public class NpcRecord
    {
        public string Name;
        public string Role;
        public string VoicePrompt;
        public string[] DefaultActions;
        public string LocationId; // null = appears in every location (Sera)
    }
}
