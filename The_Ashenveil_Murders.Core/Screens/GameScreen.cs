using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using The_Ashenveil_Murders.Core.Data;
using The_Ashenveil_Murders.Core.Services;

namespace The_Ashenveil_Murders.Core.Screens
{
    public class GameScreen
    {
        // -- rendering resources --
        private readonly Texture2D _pixel;
        private readonly SpriteFont _font;

        // -- game state --
        private readonly GameState _state = new();

        // -- async tasks --
        private Task<bool> _statusTask;
        private Task<string> _worldDescTask;
        private Task<DialogueResponse> _dialogueTask;
        private string _pendingNpc;
        private bool _accusationSucceedsPending;

        // -- input --
        private MouseState _prevMouse;
        private KeyboardState _prevKeyboard;
        private string _inputText = "";
        private double _cursorBlink;
        private bool _cursorVisible = true;

        // -- UI state --
        private int _dialogueScroll;

        // -- map node positions (fractions of the map box) --
        private static readonly (string Id, float Fx, float Fy)[] MapNodes =
        {
            ("inn",       0.25f, 0.55f),
            ("market",    0.48f, 0.38f),
            ("watchpost", 0.30f, 0.18f),
            ("docks",     0.65f, 0.65f),
            ("caldren",   0.82f, 0.78f),
        };

        // -- colors --
        private static readonly Color CBg        = new Color(10,  10,  15);
        private static readonly Color CPanelBg   = new Color(14,  14,  22);
        private static readonly Color CBorder     = new Color(60,  60,  85);
        private static readonly Color CText       = new Color(220, 220, 230);
        private static readonly Color CNarrator   = new Color(200, 170, 80);
        private static readonly Color CPlayer     = new Color(180, 150, 255);
        private static readonly Color CNpcSel     = new Color(100, 190, 255);
        private static readonly Color CNpcNormal  = new Color(160, 160, 180);
        private static readonly Color CItemShown  = new Color(255, 220, 80);
        private static readonly Color CItemHeld   = new Color(190, 190, 210);
        private static readonly Color CItemMissing= new Color(65,  65,  85);
        private static readonly Color CBtn        = new Color(35,  35,  60);
        private static readonly Color CBtnHover   = new Color(55,  55,  90);
        private static readonly Color CAccent     = new Color(120, 100, 170);
        private static readonly Color CHeader     = new Color(230, 230, 245);
        private static readonly Color CDisabled   = new Color(70,  70,  90);
        private static readonly Color CWin        = new Color(80,  200, 120);
        private static readonly Color COver       = new Color(200, 80,  80);

        public GameScreen(Texture2D pixel, SpriteFont font, Game game)
        {
            _pixel = pixel;
            _font = font;
            game.Window.TextInput += OnTextInput;
            _state.AddNarrator("Connecting to NPC Forge server at localhost:5000...");
            _statusTask = NpcForgeClient.CheckReady();
        }

        // -------------------------------------------------------------------------
        // Update
        // -------------------------------------------------------------------------

        public void Update(GameTime gameTime, int viewW, int viewH)
        {
            _cursorBlink += gameTime.ElapsedGameTime.TotalSeconds;
            if (_cursorBlink >= 0.53)
            {
                _cursorBlink = 0;
                _cursorVisible = !_cursorVisible;
            }

            PollAsyncTasks();

            var mouse = Mouse.GetState();
            var kb    = Keyboard.GetState();

            // Scroll dialogue with mouse wheel
            int scrollDelta = mouse.ScrollWheelValue - _prevMouse.ScrollWheelValue;
            if (scrollDelta != 0)
                _dialogueScroll = Math.Max(0, _dialogueScroll + scrollDelta / 120);

            // Click handling
            if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
            {
                var layout = ComputeLayout(viewW, viewH);
                HandleClick(mouse.X, mouse.Y, viewW, viewH, layout);
            }

            // Enter key to send
            if (kb.IsKeyDown(Keys.Enter) && _prevKeyboard.IsKeyUp(Keys.Enter))
                TrySend();

            _prevMouse    = mouse;
            _prevKeyboard = kb;
        }

        private void PollAsyncTasks()
        {
            if (_statusTask?.IsCompleted == true)
            {
                bool ready = _statusTask.Result;
                _statusTask = null;
                _state.DialogueHistory.Clear();
                if (ready)
                {
                    _state.AddNarrator("Server ready. Starting investigation...");
                    _worldDescTask = NpcForgeClient.GetWorldDescription("The Tallow & Thorn Inn");
                }
                else
                {
                    _state.AddNarrator("NPC Forge server not available — running in offline mode.");
                    _state.AddNarrator("You are in Ashenveil. A man named Ser Aldric Mourne is dead. Find the killer.");
                    if (_state.ActiveNpcName == null)
                        _state.ActiveNpcName = "Sera";
                }
            }

            if (_worldDescTask?.IsCompleted == true)
            {
                string desc = _worldDescTask.Result;
                _worldDescTask = null;
                _state.AddNarrator(string.IsNullOrWhiteSpace(desc)
                    ? "You are in the common room of the Tallow & Thorn Inn. The fire is low. A man died here."
                    : desc);
                if (_state.ActiveNpcName == null)
                    _state.ActiveNpcName = "Sera";
                _dialogueScroll = 0;
            }

            if (_dialogueTask?.IsCompleted == true)
            {
                var resp = _dialogueTask.Result;
                _dialogueTask = null;
                _state.RequestInFlight = false;

                string npc = _pendingNpc;
                _pendingNpc = null;

                if (npc != null)
                {
                    _state.AddDialogue(npc, resp.dialogue ?? "...");
                    HandleAction(resp.action ?? "idle", npc);
                }

                if (_accusationSucceedsPending)
                {
                    _accusationSucceedsPending = false;
                    _state.GameWon = true;
                    _state.RemovedNpcs.Add("Vessa Caldren");
                }

                _dialogueScroll = 0;
            }
        }

        private void HandleAction(string action, string npcName)
        {
            switch (action)
            {
                case "follow":
                    _state.FollowingNpcs.Add(npcName);
                    break;

                case "flee":
                    _state.FledNpcs.Add(npcName);
                    if (_state.ActiveNpcName == npcName)
                        _state.ActiveNpcName = _state.GetNpcsAtCurrentLocation().FirstOrDefault()?.Name;
                    break;

                case "call_guard":
                    _state.ExtraNpcs.Add(("Guard Holt", _state.CurrentLocationId));
                    _state.AddNarrator("A guard appears.");
                    break;

                case "attack":
                    _state.AddNarrator("You were attacked. You retreat to the inn.");
                    TravelTo("inn");
                    break;

                case "give_item":
                    string tag = GetGiveItem(npcName);
                    if (tag != null && _state.TryAddItem(tag))
                    {
                        var item = GameData.FindItem(tag);
                        _state.AddNarrator($"You receive: {item?.DisplayName ?? tag}.");
                    }
                    break;

                case "unlock":
                    _state.AddNarrator("A new path is open.");
                    break;

                case "idle":
                case "approach":
                case "retreat":
                case "block":
                    break;
            }
        }

        private string GetGiveItem(string npcName) => npcName switch
        {
            "Mira"            => !_state.Inventory.Contains("room_key")     ? "room_key"     : null,
            "Conn"            => !_state.Inventory.Contains("dinner_invite")? "dinner_invite" : null,
            "Osric"           => !_state.Inventory.Contains("sale_record")  ? "sale_record"  : null,
            "Fisherman Iddo"  => !_state.Inventory.Contains("iddo_account") ? "iddo_account" : null,
            "Servant Lena"    => !_state.Inventory.Contains("torn_cloak")   ? "torn_cloak"   : null,
            "Dockworker Cray" => !_state.Inventory.Contains("ledger")       ? "ledger"       : null,
            "Captain Brek"    => !_state.Inventory.Contains("bribe_note")   ? "bribe_note"   : null,
            _                 => null
        };

        // -------------------------------------------------------------------------
        // Text input
        // -------------------------------------------------------------------------

        private void OnTextInput(object sender, TextInputEventArgs e)
        {
            if (_state.RequestInFlight || _state.MapOpen || _state.GameWon || _state.GameOver) return;

            if (e.Character == '\b')
            {
                if (_inputText.Length > 0)
                    _inputText = _inputText[..^1];
            }
            else if (e.Character == '\r' || e.Character == '\n')
            {
                // handled by keyboard state to avoid double-firing
            }
            else if (!char.IsControl(e.Character))
            {
                _inputText += e.Character;
            }
        }

        private void TrySend()
        {
            if (_state.RequestInFlight) return;
            if (_state.GameWon || _state.GameOver) return;
            if (string.IsNullOrWhiteSpace(_inputText)) return;
            if (_state.ActiveNpcName == null) return;

            string text = _inputText.Trim();
            _inputText = "";

            // Check for search command
            if (text.Equals("search", StringComparison.OrdinalIgnoreCase) ||
                text.StartsWith("search ", StringComparison.OrdinalIgnoreCase) ||
                text.StartsWith("look", StringComparison.OrdinalIgnoreCase) ||
                text.StartsWith("examine", StringComparison.OrdinalIgnoreCase))
            {
                HandleSearch();
                return;
            }

            // Check for accusation when talking to Brek or Holt
            if ((_state.ActiveNpcName == "Captain Brek" || _state.ActiveNpcName == "Guard Holt")
                && text.Contains("accuse", StringComparison.OrdinalIgnoreCase))
            {
                HandleAccusation(text);
                return;
            }

            SendDialogue(_state.ActiveNpcName, text, null);
        }

        private void SendDialogue(string npcName, string playerText, string contextOverride)
        {
            var npc = GameData.FindNpc(npcName);
            if (npc == null) return;

            _state.AddPlayer(playerText);
            _state.RequestInFlight = true;
            _pendingNpc = npcName;

            string context = _state.BuildContext(contextOverride);
            _dialogueTask = NpcForgeClient.GetDialogue(
                npc.Name, npc.Role, npc.VoicePrompt,
                playerText, context, npc.DefaultActions);
        }

        private void HandleSearch()
        {
            var found = new List<string>();
            switch (_state.CurrentLocationId)
            {
                case "inn":
                    if (_state.Inventory.Contains("room_key"))
                    {
                        foreach (var tag in new[] { "journal", "poison_vial", "dinner_invite", "signet_ring" })
                            if (_state.TryAddItem(tag))
                                found.Add(GameData.FindItem(tag)?.DisplayName ?? tag);
                    }
                    else
                    {
                        _state.AddNarrator("Aldric's room is locked. You need a key.");
                    }
                    break;

                case "docks":
                    foreach (var tag in new[] { "ledger", "torn_cloak" })
                        if (_state.TryAddItem(tag))
                            found.Add(GameData.FindItem(tag)?.DisplayName ?? tag);
                    break;

                case "watchpost":
                    if (_state.Inventory.Contains("ledger"))
                    {
                        if (_state.TryAddItem("bribe_note"))
                            found.Add("Caldren Bribe Note");
                    }
                    else
                    {
                        _state.AddNarrator("Nothing of immediate use here.");
                    }
                    break;

                default:
                    _state.AddNarrator("You find nothing of note.");
                    break;
            }

            if (found.Count > 0)
                _state.AddNarrator("Found: " + string.Join(", ", found) + ".");
        }

        private void HandleAccusation(string text)
        {
            string accused = null;
            foreach (var npc in GameData.Npcs)
            {
                if (text.Contains(npc.Name, StringComparison.OrdinalIgnoreCase))
                {
                    accused = npc.Name;
                    break;
                }
            }

            if (accused == null)
            {
                _state.AddNarrator("State your accusation clearly. Who do you accuse?");
                return;
            }

            string ctx;
            if (accused == "Vessa Caldren")
            {
                if (_state.HasWinningEvidence())
                {
                    var ev = new List<string> { "ledger", "poison_vial", "dinner_invite" };
                    if (_state.Inventory.Contains("sale_record"))   ev.Add("sale_record");
                    if (_state.Inventory.Contains("iddo_account"))  ev.Add("iddo_account");
                    ctx = $"{_state.CurrentLocationId}. accusation_succeeds, evidence: {string.Join(", ", ev)}.";
                    _accusationSucceedsPending = true;
                }
                else
                {
                    ctx = $"{_state.CurrentLocationId}. accusation_insufficient_evidence.";
                }
            }
            else
            {
                ctx = $"{_state.CurrentLocationId}. accusation_wrong, accused: {accused}.";
            }

            SendDialogue(_state.ActiveNpcName, text, ctx);
        }

        // -------------------------------------------------------------------------
        // Click handling
        // -------------------------------------------------------------------------

        private void HandleClick(int mx, int my, int viewW, int viewH, Layout layout)
        {
            if (_state.MapOpen)
            {
                HandleMapClick(mx, my, viewW, viewH);
                return;
            }

            // [Map] button
            var mapBtn = MapButton(layout);
            if (mapBtn.Contains(mx, my)) { _state.MapOpen = true; return; }

            // [Search] button
            var searchBtn = SearchButton(layout);
            if (searchBtn.Contains(mx, my)) { HandleSearch(); return; }

            // [Send] button
            if (!_state.RequestInFlight && !string.IsNullOrWhiteSpace(_inputText))
            {
                var sendBtn = SendButton(layout);
                if (sendBtn.Contains(mx, my)) { TrySend(); return; }
            }

            // Inventory items
            if (layout.Inventory.Contains(mx, my))
            {
                HandleInventoryClick(mx, my, layout.Inventory);
                return;
            }

            // NPC list
            if (layout.NpcList.Contains(mx, my))
            {
                HandleNpcClick(mx, my, layout.NpcList);
                return;
            }
        }

        private void HandleInventoryClick(int mx, int my, Rectangle panel)
        {
            const int PAD = 10;
            const int ITEM_H = 22;
            int y = panel.Y + 30 + PAD;

            foreach (var item in GameData.AllItems)
            {
                if (!_state.Inventory.Contains(item.Tag)) { y += ITEM_H; continue; }
                var row = new Rectangle(panel.X + PAD, y, panel.Width - PAD * 2, ITEM_H);
                if (row.Contains(mx, my))
                {
                    if (_state.ShownItems.Contains(item.Tag))
                        _state.ShownItems.Remove(item.Tag);
                    else
                        _state.ShownItems.Add(item.Tag);
                    return;
                }
                y += ITEM_H;
            }
        }

        private void HandleNpcClick(int mx, int my, Rectangle panel)
        {
            const int PAD = 10;
            const int NPC_H = 22;
            int y = panel.Y + 30 + PAD;

            foreach (var npc in _state.GetNpcsAtCurrentLocation())
            {
                var row = new Rectangle(panel.X + PAD, y, panel.Width - PAD * 2, NPC_H);
                if (row.Contains(mx, my))
                {
                    if (_state.ActiveNpcName != npc.Name)
                    {
                        _state.ActiveNpcName = npc.Name;
                        _state.ShownItems.Clear();
                        // Request world description for location when switching context
                    }
                    return;
                }
                y += NPC_H;
            }
        }

        private void HandleMapClick(int mx, int my, int viewW, int viewH)
        {
            // Close button
            var closeBtn = MapCloseButton(viewW, viewH);
            if (closeBtn.Contains(mx, my)) { _state.MapOpen = false; return; }

            var mapBox = MapBox(viewW, viewH);
            foreach (var (id, fx, fy) in MapNodes)
            {
                int nx = mapBox.X + (int)(mapBox.Width  * fx);
                int ny = mapBox.Y + (int)(mapBox.Height * fy);
                if (Distance(mx, my, nx, ny) < 28)
                {
                    if (id != _state.CurrentLocationId)
                        TravelTo(id);
                    _state.MapOpen = false;
                    return;
                }
            }
        }

        private void TravelTo(string locationId)
        {
            if (locationId == _state.CurrentLocationId) return;
            _state.CurrentLocationId = locationId;
            _state.ShownItems.Clear();

            // Deselect NPC if they're not at this location
            var npcsHere = _state.GetNpcsAtCurrentLocation();
            if (_state.ActiveNpcName != null && !npcsHere.Any(n => n.Name == _state.ActiveNpcName))
                _state.ActiveNpcName = npcsHere.FirstOrDefault()?.Name;

            var loc = GameData.FindLocation(locationId);
            _worldDescTask = NpcForgeClient.GetWorldDescription(loc?.DisplayName ?? locationId);
            _dialogueScroll = 0;
        }

        // -------------------------------------------------------------------------
        // Draw
        // -------------------------------------------------------------------------

        public void Draw(GameTime gameTime, SpriteBatch sb, int viewW, int viewH)
        {
            var layout = ComputeLayout(viewW, viewH);
            var mouse  = Mouse.GetState();

            // Background
            DrawRect(sb, new Rectangle(0, 0, viewW, viewH), CBg);

            DrawHeader(sb, layout, mouse);
            DrawDialogue(sb, layout.Dialogue);
            DrawInventory(sb, layout.Inventory, mouse);
            DrawNpcList(sb, layout.NpcList, mouse);
            DrawInputBar(sb, layout, mouse);

            // Borders between panels
            DrawRect(sb, new Rectangle(layout.Dialogue.Right, layout.Header.Bottom, 1, layout.Dialogue.Height + layout.NpcList.Height), CBorder);
            DrawRect(sb, new Rectangle(layout.Inventory.X, layout.Inventory.Bottom, layout.Inventory.Width, 1), CBorder);
            DrawRect(sb, new Rectangle(0, layout.Header.Bottom, viewW, 1), CBorder);
            DrawRect(sb, new Rectangle(0, layout.Input.Top, viewW, 1), CBorder);

            if (_state.MapOpen)
                DrawMap(sb, viewW, viewH, mouse);

            if (_state.GameWon)
                DrawEndScreen(sb, viewW, viewH, won: true);
        }

        private void DrawHeader(SpriteBatch sb, Layout layout, MouseState mouse)
        {
            DrawRect(sb, layout.Header, new Color(20, 20, 32));

            var loc = GameData.FindLocation(_state.CurrentLocationId);
            DrawText(sb, "LOCATION: " + (loc?.DisplayName ?? _state.CurrentLocationId),
                new Vector2(layout.Header.X + 14, layout.Header.Y + 15), CHeader, 1.1f);

            // [Map] button
            var mapBtn = MapButton(layout);
            bool mapHover = mapBtn.Contains(mouse.X, mouse.Y);
            DrawRect(sb, mapBtn, mapHover ? CBtnHover : CBtn);
            DrawRect(sb, mapBtn, CBorder, 1);
            DrawText(sb, "MAP", Center(mapBtn, "[MAP]"), CText);

            // [Search] button
            var srchBtn = SearchButton(layout);
            bool srchHover = srchBtn.Contains(mouse.X, mouse.Y);
            DrawRect(sb, srchBtn, srchHover ? CBtnHover : CBtn);
            DrawRect(sb, srchBtn, CBorder, 1);
            DrawText(sb, "SEARCH", Center(srchBtn, "SEARCH"), CText);
        }

        private void DrawDialogue(SpriteBatch sb, Rectangle panel)
        {
            DrawRect(sb, panel, CPanelBg);

            const int PAD = 12;
            float maxW = panel.Width - PAD * 2;
            int lineH  = _font.LineSpacing + 1;

            // Build list of (text, color) lines
            var lines = new List<(string text, Color color)>();
            foreach (var entry in _state.DialogueHistory)
            {
                Color c = entry.IsNarrator ? CNarrator
                        : entry.IsPlayer   ? CPlayer
                        : CText;

                string prefix = entry.IsNarrator ? "  "
                             : entry.IsPlayer    ? "[You] "
                             : $"[{entry.Speaker}] ";

                string full = prefix + entry.Text;
                var wrapped = WrapText(full, maxW);
                foreach (var line in wrapped)
                    lines.Add((line, c));
            }

            int visLines   = Math.Max(1, (panel.Height - PAD * 2) / lineH);
            int total      = lines.Count;
            int scrollCap  = Math.Max(0, total - visLines);
            _dialogueScroll = Math.Min(_dialogueScroll, scrollCap);

            int startLine = Math.Max(0, total - visLines - _dialogueScroll);
            int y = panel.Y + PAD;

            for (int i = startLine; i < lines.Count && y + lineH <= panel.Bottom - PAD; i++)
            {
                sb.DrawString(_font, lines[i].text, new Vector2(panel.X + PAD, y), lines[i].color);
                y += lineH;
            }

            // Requesting indicator
            if (_state.RequestInFlight)
                DrawText(sb, "...", new Vector2(panel.X + PAD, panel.Bottom - PAD - lineH), CDisabled);
        }

        private void DrawInventory(SpriteBatch sb, Rectangle panel, MouseState mouse)
        {
            DrawRect(sb, panel, CPanelBg);

            const int PAD  = 10;
            const int ITEM_H = 22;
            DrawText(sb, "INVENTORY", new Vector2(panel.X + PAD, panel.Y + 8), CAccent);
            DrawRect(sb, new Rectangle(panel.X + PAD, panel.Y + 26, panel.Width - PAD * 2, 1), CBorder);

            int y = panel.Y + 30 + PAD;
            foreach (var item in GameData.AllItems)
            {
                bool held   = _state.Inventory.Contains(item.Tag);
                bool shown  = _state.ShownItems.Contains(item.Tag);
                Color c     = shown ? CItemShown : held ? CItemHeld : CItemMissing;

                string marker = shown ? "[*] " : held ? "[ ] " : "    ";
                var row = new Rectangle(panel.X + PAD, y, panel.Width - PAD * 2, ITEM_H);

                if (held && row.Contains(mouse.X, mouse.Y))
                    DrawRect(sb, row, new Color(30, 30, 50));

                DrawText(sb, marker + item.DisplayName, new Vector2(panel.X + PAD, y + 2), c);
                y += ITEM_H;
                if (y + ITEM_H > panel.Bottom - PAD) break;
            }
        }

        private void DrawNpcList(SpriteBatch sb, Rectangle panel, MouseState mouse)
        {
            DrawRect(sb, panel, CPanelBg);

            const int PAD  = 10;
            const int NPC_H = 22;
            DrawText(sb, "NPCS HERE", new Vector2(panel.X + PAD, panel.Y + 8), CAccent);
            DrawRect(sb, new Rectangle(panel.X + PAD, panel.Y + 26, panel.Width - PAD * 2, 1), CBorder);

            int y = panel.Y + 30 + PAD;
            foreach (var npc in _state.GetNpcsAtCurrentLocation())
            {
                bool active = npc.Name == _state.ActiveNpcName;
                Color c     = active ? CNpcSel : CNpcNormal;
                string marker = active ? "> " : "  ";

                var row = new Rectangle(panel.X + PAD, y, panel.Width - PAD * 2, NPC_H);
                if (!active && row.Contains(mouse.X, mouse.Y))
                    DrawRect(sb, row, new Color(30, 30, 50));

                DrawText(sb, marker + npc.Name, new Vector2(panel.X + PAD, y + 2), c);
                y += NPC_H;
                if (y + NPC_H > panel.Bottom - PAD) break;
            }
        }

        private void DrawInputBar(SpriteBatch sb, Layout layout, MouseState mouse)
        {
            DrawRect(sb, layout.Input, new Color(14, 14, 22));

            bool disabled = _state.RequestInFlight || _state.GameWon || _state.GameOver
                         || _state.ActiveNpcName == null;
            Color inputColor = disabled ? CDisabled : CText;

            // Prompt and text
            string cursor   = (!disabled && _cursorVisible) ? "|" : "";
            string display  = "> " + _inputText + cursor;
            DrawText(sb, display, new Vector2(layout.Input.X + 14, layout.Input.Y + 18), inputColor);

            // Talking-to indicator
            if (_state.ActiveNpcName != null)
            {
                string talkingTo = "Talking to: " + _state.ActiveNpcName;
                float tw = _font.MeasureString(talkingTo).X;
                DrawText(sb, talkingTo,
                    new Vector2(layout.Input.Right - 90 - tw - 14, layout.Input.Y + 4),
                    CDisabled, 0.85f);
            }

            // [Send] button
            var sendBtn   = SendButton(layout);
            bool canSend  = !disabled && !string.IsNullOrWhiteSpace(_inputText);
            bool sendHov  = sendBtn.Contains(mouse.X, mouse.Y) && canSend;
            DrawRect(sb, sendBtn, canSend ? (sendHov ? CBtnHover : CBtn) : new Color(25, 25, 35));
            DrawRect(sb, sendBtn, CBorder, 1);
            DrawText(sb, "SEND", Center(sendBtn, "SEND"), canSend ? CText : CDisabled);
        }

        private void DrawMap(SpriteBatch sb, int viewW, int viewH, MouseState mouse)
        {
            // Dim the background
            DrawRect(sb, new Rectangle(0, 0, viewW, viewH), new Color(0, 0, 0, 180));

            var box = MapBox(viewW, viewH);
            DrawRect(sb, box, new Color(18, 18, 30));
            DrawRect(sb, box, CAccent, 2);

            DrawText(sb, "MAP -- click a location to travel",
                new Vector2(box.X + 14, box.Y + 10), CAccent, 1.1f);

            // Draw connection lines first
            var connections = new HashSet<string>();
            foreach (var loc in GameData.Locations)
            {
                var (_, fx1, fy1) = MapNodes.First(n => n.Id == loc.Id);
                int x1 = box.X + (int)(box.Width * fx1);
                int y1 = box.Y + (int)(box.Height * fy1);
                foreach (var conn in loc.Connections)
                {
                    string key = string.Compare(loc.Id, conn) < 0 ? loc.Id + conn : conn + loc.Id;
                    if (connections.Contains(key)) continue;
                    connections.Add(key);
                    var (_, fx2, fy2) = MapNodes.First(n => n.Id == conn);
                    int x2 = box.X + (int)(box.Width * fx2);
                    int y2 = box.Y + (int)(box.Height * fy2);
                    DrawLine(sb, x1, y1, x2, y2, CBorder);
                }
            }

            // Draw nodes
            foreach (var (id, fx, fy) in MapNodes)
            {
                int nx = box.X + (int)(box.Width  * fx);
                int ny = box.Y + (int)(box.Height * fy);
                bool isCurrent = id == _state.CurrentLocationId;
                bool isHover   = Distance(mouse.X, mouse.Y, nx, ny) < 28;

                Color nodeColor = isCurrent ? CItemShown
                                : isHover   ? CNpcSel
                                : CBtn;

                DrawCircle(sb, nx, ny, 20, nodeColor);
                DrawCircle(sb, nx, ny, 20, isCurrent ? CItemShown : CBorder, outline: true);

                var loc = GameData.FindLocation(id);
                string label = loc?.DisplayName ?? id;
                float lw = _font.MeasureString(label).X * 0.8f;
                DrawText(sb, label, new Vector2(nx - lw / 2, ny + 24), isCurrent ? CItemShown : CNpcNormal, 0.8f);
            }

            // Close button
            var closeBtn = MapCloseButton(viewW, viewH);
            bool closeHov = closeBtn.Contains(mouse.X, mouse.Y);
            DrawRect(sb, closeBtn, closeHov ? CBtnHover : CBtn);
            DrawRect(sb, closeBtn, CBorder, 1);
            DrawText(sb, "CLOSE", Center(closeBtn, "CLOSE"), CText);
        }

        private void DrawEndScreen(SpriteBatch sb, int viewW, int viewH, bool won)
        {
            DrawRect(sb, new Rectangle(0, 0, viewW, viewH), new Color(0, 0, 0, 210));

            Color titleColor = won ? CWin : COver;
            string title     = won ? "CASE SOLVED" : "INVESTIGATION FAILED";
            string sub       = won
                ? "Vessa Caldren has been arrested for the murder of Ser Aldric Mourne."
                : "Your investigation has come to an end.";

            float tw = _font.MeasureString(title).X * 1.6f;
            float sw = _font.MeasureString(sub).X;
            DrawText(sb, title, new Vector2((viewW - tw) / 2, viewH / 2f - 40), titleColor, 1.6f);
            DrawText(sb, sub,   new Vector2((viewW - sw) / 2, viewH / 2f + 10), CText);
        }

        // -------------------------------------------------------------------------
        // Layout helpers
        // -------------------------------------------------------------------------

        private struct Layout
        {
            public Rectangle Header;
            public Rectangle Dialogue;
            public Rectangle Inventory;
            public Rectangle NpcList;
            public Rectangle Input;
        }

        private static Layout ComputeLayout(int w, int h)
        {
            const int headerH = 50;
            const int footerH = 60;
            int bodyH  = h - headerH - footerH;
            int leftW  = (int)(w * 0.62f);
            int rightW = w - leftW;
            int invH   = (int)(bodyH * 0.52f);
            int npcH   = bodyH - invH;

            return new Layout
            {
                Header    = new Rectangle(0,               0,                   w,      headerH),
                Dialogue  = new Rectangle(0,               headerH,             leftW,  bodyH),
                Inventory = new Rectangle(leftW,           headerH,             rightW, invH),
                NpcList   = new Rectangle(leftW,           headerH + invH,      rightW, npcH),
                Input     = new Rectangle(0,               headerH + bodyH,     w,      footerH),
            };
        }

        private static Rectangle MapButton(Layout l)
            => new Rectangle(l.Header.Right - 75, l.Header.Y + 9, 65, 32);

        private static Rectangle SearchButton(Layout l)
            => new Rectangle(l.Header.Right - 155, l.Header.Y + 9, 72, 32);

        private static Rectangle SendButton(Layout l)
            => new Rectangle(l.Input.Right - 80, l.Input.Y + 10, 70, 40);

        private static Rectangle MapBox(int viewW, int viewH)
            => new Rectangle(viewW / 2 - 380, viewH / 2 - 230, 760, 460);

        private static Rectangle MapCloseButton(int viewW, int viewH)
        {
            var box = MapBox(viewW, viewH);
            return new Rectangle(box.Right - 90, box.Bottom - 48, 80, 36);
        }

        // -------------------------------------------------------------------------
        // Draw primitives
        // -------------------------------------------------------------------------

        private void DrawRect(SpriteBatch sb, Rectangle r, Color c)
            => sb.Draw(_pixel, r, c);

        private void DrawRect(SpriteBatch sb, Rectangle r, Color c, int thickness)
        {
            sb.Draw(_pixel, new Rectangle(r.X, r.Y, r.Width, thickness), c);
            sb.Draw(_pixel, new Rectangle(r.X, r.Bottom - thickness, r.Width, thickness), c);
            sb.Draw(_pixel, new Rectangle(r.X, r.Y, thickness, r.Height), c);
            sb.Draw(_pixel, new Rectangle(r.Right - thickness, r.Y, thickness, r.Height), c);
        }

        private void DrawText(SpriteBatch sb, string text, Vector2 pos, Color c, float scale = 1f)
        {
            if (string.IsNullOrEmpty(text)) return;
            text = SanitizeText(text);
            if (scale == 1f)
                sb.DrawString(_font, text, pos, c);
            else
                sb.DrawString(_font, text, pos, c, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        private Vector2 Center(Rectangle r, string text)
        {
            var size = _font.MeasureString(text);
            return new Vector2(r.X + (r.Width - size.X) / 2, r.Y + (r.Height - size.Y) / 2);
        }

        private void DrawLine(SpriteBatch sb, int x1, int y1, int x2, int y2, Color c)
        {
            float len = MathF.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
            float angle = MathF.Atan2(y2 - y1, x2 - x1);
            sb.Draw(_pixel, new Vector2(x1, y1), null, c, angle, Vector2.Zero, new Vector2(len, 1f), SpriteEffects.None, 0f);
        }

        private void DrawCircle(SpriteBatch sb, int cx, int cy, int r, Color c, bool outline = false)
        {
            if (outline)
            {
                for (int t = 0; t < 36; t++)
                {
                    float a1 = t * MathF.PI * 2 / 36;
                    float a2 = (t + 1) * MathF.PI * 2 / 36;
                    int x1 = cx + (int)(MathF.Cos(a1) * r);
                    int y1 = cy + (int)(MathF.Sin(a1) * r);
                    int x2 = cx + (int)(MathF.Cos(a2) * r);
                    int y2 = cy + (int)(MathF.Sin(a2) * r);
                    DrawLine(sb, x1, y1, x2, y2, c);
                }
            }
            else
            {
                sb.Draw(_pixel, new Rectangle(cx - r, cy - r, r * 2, r * 2), c);
            }
        }

        // -------------------------------------------------------------------------
        // Text helpers
        // -------------------------------------------------------------------------

        // Replace characters the font can't render (em/en dashes, curly quotes, ellipsis, etc.)
        private static string SanitizeText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var sb = new StringBuilder(text.Length + 8);
            foreach (char c in text)
            {
                // Font covers: Latin 32-254, Cyrillic 1040-1103, Hiragana 12352-12447,
                // Katakana 12448-12543, CJK 19968-40879
                if ((c >= 32 && c <= 254)
                 || (c >= 1040 && c <= 1103)
                 || (c >= 12352 && c <= 12543)
                 || (c >= 19968 && c <= 40879))
                {
                    sb.Append(c);
                }
                else switch (c)
                {
                    case '–': case '—': sb.Append('-');   break; // en/em dash
                    case '‘': case '’': sb.Append('\'');  break; // curly single quotes
                    case '“': case '”': sb.Append('"');   break; // curly double quotes
                    case '…': sb.Append("...");                break; // ellipsis
                    case '·': sb.Append('.');                  break; // middle dot
                    default:
                        if (c >= 32) sb.Append('?');
                        break;
                }
            }
            return sb.ToString();
        }

        private List<string> WrapText(string text, float maxWidth)
        {
            text = SanitizeText(text);
            var result = new List<string>();
            foreach (var paragraph in text.Split('\n'))
            {
                var words = paragraph.Split(' ');
                var line  = new StringBuilder();
                foreach (var word in words)
                {
                    if (word.Length == 0) continue;
                    string test = line.Length == 0 ? word : line + " " + word;
                    if (_font.MeasureString(test).X > maxWidth && line.Length > 0)
                    {
                        result.Add(line.ToString());
                        line.Clear();
                        line.Append(word);
                    }
                    else
                    {
                        if (line.Length > 0) line.Append(' ');
                        line.Append(word);
                    }
                }
                if (line.Length > 0) result.Add(line.ToString());
                else if (paragraph.Length == 0) result.Add("");
            }
            return result;
        }

        private static float Distance(int x1, int y1, int x2, int y2)
            => MathF.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
    }
}
