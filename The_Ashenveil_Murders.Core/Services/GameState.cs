using System.Collections.Generic;
using System.Linq;
using The_Ashenveil_Murders.Core.Data;

namespace The_Ashenveil_Murders.Core.Services
{
    public class DialogueLine
    {
        public string Speaker;
        public string Text;
        public bool IsNarrator;
        public bool IsPlayer;
    }

    public class GameState
    {
        public string CurrentLocationId = "inn";
        public string ActiveNpcName;
        public bool RequestInFlight;
        public bool MapOpen;
        public bool GameWon;
        public bool GameOver;

        public readonly List<string> Inventory = new();
        public readonly HashSet<string> ShownItems = new();
        public readonly List<DialogueLine> DialogueHistory = new();

        public readonly HashSet<string> FledNpcs = new();
        public readonly HashSet<string> RemovedNpcs = new();
        public readonly HashSet<string> FollowingNpcs = new();
        public readonly List<(string NpcName, string LocationId)> ExtraNpcs = new();

        public void AddDialogue(string speaker, string text)
            => DialogueHistory.Add(new DialogueLine { Speaker = speaker, Text = text });

        public void AddPlayer(string text)
            => DialogueHistory.Add(new DialogueLine { Speaker = "You", Text = text, IsPlayer = true });

        public void AddNarrator(string text)
            => DialogueHistory.Add(new DialogueLine { Speaker = "~", Text = text, IsNarrator = true });

        public string BuildContext(string overrideContext = null)
        {
            if (overrideContext != null) return overrideContext;
            var loc = GameData.FindLocation(CurrentLocationId)?.DisplayName ?? CurrentLocationId;
            var ctx = loc + ". Day.";
            if (ShownItems.Count > 0)
                ctx += " Player is showing: " + string.Join(", ", ShownItems) + ".";
            return ctx;
        }

        public bool HasWinningEvidence()
            => Inventory.Contains("ledger")
            && Inventory.Contains("poison_vial")
            && Inventory.Contains("dinner_invite");

        public bool TryAddItem(string tag)
        {
            if (Inventory.Contains(tag)) return false;
            Inventory.Add(tag);
            return true;
        }

        public List<NpcRecord> GetNpcsAtCurrentLocation()
        {
            var npcs = new List<NpcRecord>();

            // Sera is always present
            var sera = GameData.FindNpc("Sera");
            if (sera != null) npcs.Add(sera);

            foreach (var npc in GameData.Npcs)
            {
                if (npc.Name == "Sera") continue;
                if (FledNpcs.Contains(npc.Name)) continue;
                if (RemovedNpcs.Contains(npc.Name)) continue;
                if (npc.LocationId == CurrentLocationId)
                    npcs.Add(npc);
            }

            foreach (var name in FollowingNpcs)
            {
                if (!npcs.Any(n => n.Name == name))
                {
                    var npc = GameData.FindNpc(name);
                    if (npc != null) npcs.Add(npc);
                }
            }

            foreach (var (name, locId) in ExtraNpcs)
            {
                if (locId == CurrentLocationId && !npcs.Any(n => n.Name == name))
                {
                    var npc = GameData.FindNpc(name);
                    if (npc != null) npcs.Add(npc);
                }
            }

            return npcs;
        }
    }
}
