using System.Collections.Generic;

namespace The_Ashenveil_Murders.Core.Data
{
    // All strings that go into npc_name / role / voice_prompt must be byte-for-byte
    // identical to the values used during NPC Forge fine-tuning.
    public static class GameData
    {
        public static readonly List<GameLocation> Locations = new()
        {
            new GameLocation
            {
                Id = "inn",
                DisplayName = "The Tallow & Thorn Inn",
                Connections = new[] { "market", "watchpost" }
            },
            new GameLocation
            {
                Id = "market",
                DisplayName = "The Market District",
                Connections = new[] { "inn", "watchpost", "docks" }
            },
            new GameLocation
            {
                Id = "watchpost",
                DisplayName = "The City Watch Post",
                Connections = new[] { "inn", "market" }
            },
            new GameLocation
            {
                Id = "docks",
                DisplayName = "The Docks",
                Connections = new[] { "market", "caldren" }
            },
            new GameLocation
            {
                Id = "caldren",
                DisplayName = "The Caldren House",
                Connections = new[] { "docks" }
            },
        };

        public static readonly List<NpcRecord> Npcs = new()
        {
            // -- Companion (everywhere) --
            new NpcRecord
            {
                Name = "Sera",
                Role = "wandering investigator with her own reasons for being in Ashenveil",
                VoicePrompt = "Sharp, dry, cynical. Clipped sentences. Hides genuine care for justice behind sarcasm. Old grudge against House Mourne she will not volunteer. Calls the player by a self-chosen nickname.",
                DefaultActions = new[] { "idle", "follow", "approach", "retreat" },
                LocationId = null
            },

            // -- Killer --
            new NpcRecord
            {
                Name = "Vessa Caldren",
                Role = "steward and fixer for House Caldren",
                VoicePrompt = "Outwardly helpful, formal, measured. Long sentences that say little. Answers questions with questions. Never volunteers information. Subtly threatening when pressed, never overt — always plausible deniability. Cannot confess.",
                DefaultActions = new[] { "idle", "call_guard", "block" },
                LocationId = "caldren"
            },

            // -- Key witness / accusation gate --
            new NpcRecord
            {
                Name = "Captain Brek",
                Role = "captain of the Ashenveil city watch",
                VoicePrompt = "Tired, pragmatic, defensive. Short answers. Took a bribe from House Caldren to rule the death a heart attack and is not proud of it. Will only reveal what he knows if the player presents real leverage — the bribe note or the ledger.",
                DefaultActions = new[] { "idle", "block", "call_guard" },
                LocationId = "watchpost"
            },

            // -- Generic NPCs --
            new NpcRecord
            {
                Name = "Mira",
                Role = "innkeeper of the Tallow & Thorn",
                VoicePrompt = "Anxious. Saw Aldric the night he died. Wants no trouble. Will share what she saw if made to feel it is safe to do so, but deflects and minimizes by default.",
                DefaultActions = new[] { "idle", "give_item" },
                LocationId = "inn"
            },
            new NpcRecord
            {
                Name = "Conn",
                Role = "barman of the Tallow & Thorn",
                VoicePrompt = "Tight-lipped. Served the drinks that night. Knew something was wrong but kept his head down. Will not volunteer information. Short, blunt answers.",
                DefaultActions = new[] { "idle" },
                LocationId = "inn"
            },
            new NpcRecord
            {
                Name = "Edda",
                Role = "cloth merchant in the market district",
                VoicePrompt = "Chatty gossip. Knows town politics and everyone's business. No direct stake in the murder. Will talk freely about the noble houses and local rumours.",
                DefaultActions = new[] { "idle" },
                LocationId = "market"
            },
            new NpcRecord
            {
                Name = "Pell",
                Role = "blacksmith in the market district",
                VoicePrompt = "Gruff and practical. Heard rumours of smuggling at the docks but does not get involved in other people's business. Speaks plainly.",
                DefaultActions = new[] { "idle" },
                LocationId = "market"
            },
            new NpcRecord
            {
                Name = "Osric",
                Role = "apothecary in the market district",
                VoicePrompt = "Cautious and evasive. Sold something he should not have, under a false name, and regrets it. Will stonewall unless the player presents real pressure, then becomes cooperative out of self-interest.",
                DefaultActions = new[] { "idle", "give_item" },
                LocationId = "market"
            },
            new NpcRecord
            {
                Name = "Nan",
                Role = "food-stall vendor in the market district",
                VoicePrompt = "Cheerful market vendor. Enjoys gossip and local colour. Not connected to the murder. Friendly.",
                DefaultActions = new[] { "idle" },
                LocationId = "market"
            },
            new NpcRecord
            {
                Name = "Guard Holt",
                Role = "junior city watchman",
                VoicePrompt = "Not corrupt. Two years in the watch. Uncomfortable with how the death was ruled and Brek's obvious nervousness. Wants to do right but is loyal to his captain and constrained by it.",
                DefaultActions = new[] { "idle", "block", "call_guard" },
                LocationId = "watchpost"
            },
            new NpcRecord
            {
                Name = "Guard Toma",
                Role = "junior city watchman",
                VoicePrompt = "Loyal to Brek. Will not discuss the death or anything related to it. Unhelpful and unfriendly to outsiders.",
                DefaultActions = new[] { "idle", "block", "call_guard" },
                LocationId = "watchpost"
            },
            new NpcRecord
            {
                Name = "Dockmaster Farren",
                Role = "dockmaster of Ashenveil",
                VoicePrompt = "Hostile to any questions about the docks or shipments. Runs the smuggling operation and will protect it. Will become aggressive if pressed.",
                DefaultActions = new[] { "idle", "block", "attack", "call_guard" },
                LocationId = "docks"
            },
            new NpcRecord
            {
                Name = "Dockworker Cray",
                Role = "dockworker on the Ashenveil docks",
                VoicePrompt = "Scared and evasive. Knows about the ledger and what it contains. Wants to stay out of it. Will fold under sustained pressure but needs to feel there is no way out first.",
                DefaultActions = new[] { "idle", "flee", "retreat" },
                LocationId = "docks"
            },
            new NpcRecord
            {
                Name = "Fisherman Iddo",
                Role = "old fisherman at the Ashenveil docks",
                VoicePrompt = "Old and sleepless. Sits at the docks at all hours. Saw a Caldren house servant at the docks late on the night Aldric died. Happy to share what he saw with anyone who listens.",
                DefaultActions = new[] { "idle", "give_item" },
                LocationId = "docks"
            },
            new NpcRecord
            {
                Name = "Servant Lena",
                Role = "house servant of the Caldren estate",
                VoicePrompt = "Nervous but kind-hearted. Has seen things in the Caldren house that troubled her. Can be won over with patience and kindness.",
                DefaultActions = new[] { "idle", "give_item" },
                LocationId = "caldren"
            },
            new NpcRecord
            {
                Name = "Servant Pol",
                Role = "house servant of the Caldren estate",
                VoicePrompt = "Loyal to Vessa Caldren above all else. Reports visitor questions back to Vessa. Outwardly polite, inwardly hostile.",
                DefaultActions = new[] { "idle", "call_guard", "block" },
                LocationId = "caldren"
            },
        };

        public static readonly List<InventoryItem> AllItems = new()
        {
            new InventoryItem { Tag = "room_key",     DisplayName = "Aldric's Room Key" },
            new InventoryItem { Tag = "journal",      DisplayName = "Aldric's Journal" },
            new InventoryItem { Tag = "ledger",       DisplayName = "The Caldren Ledger" },
            new InventoryItem { Tag = "poison_vial",  DisplayName = "Poison Vial (empty)" },
            new InventoryItem { Tag = "sale_record",  DisplayName = "Apothecary Sale Record" },
            new InventoryItem { Tag = "dinner_invite",DisplayName = "Dinner Invitation" },
            new InventoryItem { Tag = "bribe_note",   DisplayName = "Caldren Bribe Note" },
            new InventoryItem { Tag = "iddo_account", DisplayName = "Witness Account (Iddo)" },
            new InventoryItem { Tag = "signet_ring",  DisplayName = "Aldric's Signet Ring" },
            new InventoryItem { Tag = "torn_cloak",   DisplayName = "Servant's Torn Cloak" },
        };

        public static NpcRecord FindNpc(string name)
            => Npcs.Find(n => n.Name == name);

        public static GameLocation FindLocation(string id)
            => Locations.Find(l => l.Id == id);

        public static InventoryItem FindItem(string tag)
            => AllItems.Find(i => i.Tag == tag);
    }
}
