# The Ashenveil Murders
### NPC Forge — Proof of Concept Demo Game
**Document Version:** 0.1  
**Date:** March 2026  
**Purpose:** Game design document for the NPC Forge proof of concept demo

---

## 1. Overview

A short single-player murder mystery set in a generic fantasy world. The player must investigate a killing, gather evidence, and accuse the correct suspect. There is no combat. The game exists to validate and demonstrate the NPC Forge framework.

**Primary goals of this demo:**
- Prove that fine-tuned small models hold world knowledge without runtime context injection
- Demonstrate the generic NPC model vs per-character model quality difference
- Test inventory items as dynamic context flags that change NPC responses
- Produce a shareable, playable demo that showcases the framework to potential users

---

## 2. The World — Ashenveil

A small, self-contained fantasy setting. Generic enough that the base LLM performs well, specific enough that fine-tuning has something to sink into.

### 2.1 World Summary

**Ashenveil** is a grey, fog-bound trading town built around a river crossing. It sits at the border between two rival noble houses — House Caldren (merchants, pragmatic, corrupt) and House Mourne (old blood, proud, declining). The town has prospered from trade but is rotten underneath — smuggling, bribes, old grudges.

The town has one temple (to a god of roads and travellers), one garrison of city watch, a market district, and a poorer dockside quarter. Strangers pass through often. Nobody asks too many questions.

### 2.2 Tone

Grounded, slightly dark, low-magic. NPCs are pragmatic and self-interested. Nobody is purely good. The world feels like it existed before the player arrived and will continue after they leave.

### 2.3 Key Factions

| Faction | Description | Attitude to Player |
|---|---|---|
| House Caldren | Merchant noble house. Controls most trade in Ashenveil. Ruthless. | Suspicious, transactional |
| House Mourne | Old noble house, fading power. Proud and bitter. | Cold, condescending |
| The City Watch | Underpaid, understaffed, mostly corrupt. Loyal to whoever pays. | Indifferent, easily bribed |
| The Dockworkers | Loose brotherhood of labourers and smugglers. Loyal to each other. | Hostile to outsiders, respect strength |

---

## 3. The Murder

### 3.1 The Victim

**Ser Aldric Mourne** — younger son of House Mourne. Found dead in his room at the Tallow & Thorn inn, three days into a visit to Ashenveil. Officially ruled a heart attack by the watch. Obviously wasn't.

He came to Ashenveil to collect a debt owed to House Mourne by a Caldren merchant. He found something he wasn't supposed to find.

### 3.2 The Truth

**The killer is Vessa Caldren** — steward and fixer for House Caldren. Aldric discovered that the Caldrens have been running a smuggling operation through the docks, skimming off trade taxes for years. He threatened to expose them. Vessa poisoned his wine at dinner.

This is not revealed until the player finds enough evidence and makes the correct accusation.

### 3.3 The Motive Chain

Aldric found the ledger → ledger proves Caldren smuggling → Aldric threatened blackmail → Vessa decided he had to go → poison sourced from the apothecary (under a false name) → administered at private dinner

---

## 4. Locations

Five locations the player can move between freely.

| # | Location | Description | Key NPCs |
|---|---|---|---|
| 1 | The Tallow & Thorn Inn | Where the victim stayed and died. Common room, victim's locked room (accessible later). | Mira (innkeeper), Conn (barman) |
| 2 | The Market District | Open stalls, permanent shops. Busy and loud. | Edda (cloth merchant), Pell (blacksmith), Osric (apothecary), Nan (food stall) |
| 3 | The City Watch Post | Small garrison building. One desk, a holding cell, a noticeboard. | Captain Brek, Guard Holt, Guard Toma |
| 4 | The Docks | River docks, warehouses, a fisherman's shack. Rough area. | Dockmaster Farren, Dockworker Cray, Fisherman Iddo |
| 5 | The Caldren House | Merchant manor on the edge of town. Players need a reason to get in. | **Vessa Caldren (killer)**, Servant Lena, Servant Pol |

The player starts at the inn.

---

## 5. Characters

Each named character below has three fields that map directly onto the runtime server's
`/npc/dialogue` request body: `npc_name`, `role`, and `voice_prompt`. These three fields
together build the system prompt the model is trained against:

```
"You are {npc_name}, {role}. {voice_prompt}"
```

The game client must send these strings **byte-for-byte identical** to the values used
during NPC Forge training, or the character will respond generically. Treat them as data,
not display strings — store them alongside each NPC's record.

### 5.1 Companion — Sera

**npc_name:** `Sera`
**role:** `wandering investigator with her own reasons for being in Ashenveil`
**voice_prompt:** `Sharp, dry, cynical. Clipped sentences. Hides genuine care for justice behind sarcasm. Old grudge against House Mourne she will not volunteer. Calls the player by a self-chosen nickname.`
**model tier:** `world_npc_high.gguf`
**default actions:** `["idle", "follow", "approach", "retreat"]`

**Mechanical role:** Sounding board. Always available regardless of location (game tracks
her as ever-present). Player can ask Sera about clues, suspects, locations. She reacts to
inventory items — particularly the ledger and the poison vial — passed via the `context`
field.

**Fine-tuning priority:** High. Primary character model test. She must hold personality
across a full playthrough and respond consistently to all evidence items.

---

### 5.2 The Killer — Vessa Caldren

**npc_name:** `Vessa Caldren`
**role:** `steward and fixer for House Caldren`
**voice_prompt:** `Outwardly helpful, formal, measured. Long sentences that say little. Answers questions with questions. Never volunteers information. Subtly threatening when pressed, never overt — always plausible deniability. Cannot confess.`
**model tier:** `world_npc_high.gguf`
**default actions:** `["idle", "call_guard", "block"]`

**Mechanical role:** The player will interact with Vessa multiple times. Her dialogue must
be consistent — she cannot accidentally confirm guilt, but she must give the impression of
concealment. Her responses change meaningfully when the `context` flag carries `ledger` or
`poison_vial`.

**Fine-tuning priority:** High. If Vessa breaks character or accidentally confesses too
early the mystery collapses.

---

### 5.3 Key Witness — Captain Brek

**npc_name:** `Captain Brek`
**role:** `captain of the Ashenveil city watch`
**voice_prompt:** `Tired, pragmatic, defensive. Short answers. Took a bribe from House Caldren to rule the death a heart attack and is not proud of it. Will only reveal what he knows if the player presents real leverage — the bribe note or the ledger.`
**model tier:** `world_npc_medium.gguf`
**default actions:** `["idle", "block", "call_guard"]`

**Mechanical role:** Holds the bribe secret. Gates the accusation system — accusations
are made by speaking to him.

**Fine-tuning priority:** Medium. Needs to hold his secret consistently but doesn't need
the full personality depth of Sera or Vessa.

---

### 5.4 Generic NPCs (~13)

All handled by `world_npc_generic.gguf`. They know the world, know the town, have basic
opinions about the murder and the noble houses. They do not have deep personal stakes.

Each row's `role` and `voice_prompt` columns are what the game sends in the `/npc/dialogue`
request. The `npc_name` is the NPC column verbatim.

| NPC | Location | role | voice_prompt (abridged) | Default actions |
|---|---|---|---|---|
| Mira | Inn | innkeeper of the Tallow & Thorn | Anxious. Saw Aldric the night he died. Wants no trouble. | `idle, give_item` |
| Conn | Inn | barman of the Tallow & Thorn | Tight-lipped. Served the drinks that night. Knows something was off. | `idle` |
| Edda | Market | cloth merchant in the market district | Chatty gossip. Knows town politics. No direct stake. | `idle` |
| Pell | Market | blacksmith in the market district | Gruff. Heard rumours of smuggling at the docks. | `idle` |
| Osric | Market | apothecary in the market district | Cautious, evasive. Sold poison under a false name. Has the sale record if pressured. | `idle, give_item` |
| Nan | Market | food-stall vendor in the market district | Cheerful gossip. Local colour. | `idle` |
| Guard Holt | Watch Post | junior city watchman | Not corrupt. Uncomfortable, restrained. Two years in the watch. | `idle, block, call_guard` |
| Guard Toma | Watch Post | junior city watchman | Loyal to Brek. Unhelpful. Defensive. | `idle, block, call_guard` |
| Dockmaster Farren | Docks | dockmaster of Ashenveil | Hostile to questions. Runs the smuggling operation. | `idle, block, attack, call_guard` |
| Dockworker Cray | Docks | dockworker on the Ashenveil docks | Scared. Knows about the ledger. Will fold under pressure. | `idle, flee, retreat` |
| Fisherman Iddo | Docks | old fisherman at the Ashenveil docks | Sleepless, observant. Saw a Caldren servant at the docks late the night Aldric died. | `idle, give_item` |
| Servant Lena | Caldren House | house servant of the Caldren estate | Nervous, kind. Has seen things. Can be befriended. | `idle, give_item` |
| Servant Pol | Caldren House | house servant of the Caldren estate | Loyal to Vessa. Will report player questions back to her. | `idle, call_guard, block` |

---

## 6. Inventory & Evidence Items

Ten items. Each has a short **context tag** the game appends to the `context` field of the
`/npc/dialogue` request whenever the player has shown that item to the NPC (e.g. clicked it
in the inventory panel during a conversation). NPCs that have relevant knowledge of an item
will respond differently when the tag is present.

The game builds the `context` string by joining the location, time, and currently-shown
item tags. Example:

```
context: "Watch Post. Day. Player is showing: ledger, bribe_note."
```

| # | Item | context tag | Where Found | What It Proves | NPCs That React |
|---|---|---|---|---|---|
| 1 | Aldric's Room Key | `room_key` | Given by Mira | Access to victim's room | Mira, Brek |
| 2 | Aldric's Journal | `journal` | Victim's room | He knew about the smuggling | Sera, Vessa, Cray |
| 3 | The Caldren Ledger | `ledger` | Hidden in dockside warehouse | Proof of smuggling operation | Sera, Vessa, Farren, Brek |
| 4 | Poison Vial (empty) | `poison_vial` | Victim's room, hidden | Confirms poison, not natural death | Sera, Osric, Vessa |
| 5 | Apothecary Sale Record | `sale_record` | Osric's shop | False name used to buy poison | Osric, Brek |
| 6 | Dinner Invitation | `dinner_invite` | Victim's room | Aldric dined with Vessa the night he died | Mira, Conn, Sera |
| 7 | Caldren Bribe Note | `bribe_note` | Brek's desk (if accessed) | Proof watch was paid off | Brek, Sera |
| 8 | Witness Account (Iddo) | `iddo_account` | Written note from Iddo | Caldren servant at docks that night | Sera, Vessa |
| 9 | Aldric's Signet Ring | `signet_ring` | Victim's room | Proof of victim's identity and house | Vessa, Lena |
| 10 | Servant's Torn Cloak | `torn_cloak` | Dockside alley | Links a Caldren servant to the docks | Lena, Farren |

---

## 6.5 Runtime API Contract

The game is an HTTP client of the local NPC Forge runtime server (`npc_server/`, see
`Engine-Agnostic-AI.md`). All NPC dialogue and world description flow through three
endpoints on `http://localhost:5000`.

### 6.5.1 Server expectations

- Server is started **before** the game (`uvicorn main:app --host 127.0.0.1 --port 5000`)
- Game polls `GET /status` on launch and refuses to start play until `status == "ready"`
- One inference in flight at a time — the server serialises requests internally, but the
  game should disable the input field while a request is pending to avoid pile-ups
- Default per-request timeout on the server side is 20s; the game should set its own
  client-side timeout slightly higher (e.g. 25s) and treat any failure as a fallback
  `"..." / idle` response (the server already returns this on its own failures)

### 6.5.2 Dialogue request flow

For every NPC turn the game POSTs to `/npc/dialogue`:

```json
{
  "npc_name":     "Vessa Caldren",
  "role":         "steward and fixer for House Caldren",
  "voice_prompt": "Outwardly helpful, formal, measured. Long sentences that say little. ...",
  "player_said":  "Where were you the night Aldric died?",
  "context":      "Caldren House. Evening. Player is showing: ledger.",
  "actions":      ["idle", "call_guard", "block"]
}
```

Response shape (server strips the trailing `[action]` tag):

```json
{ "dialogue": "An odd question to ask a host.", "action": "idle" }
```

### 6.5.3 World description flow

Used for location flavour text on first entry and for narrator beats (e.g. the room
description when the player first unlocks Aldric's room).

```
POST /world/describe
{ "location": "abandoned tavern", "time_of_day": "dusk", "weather": "raining" }
→ { "description": "..." }
```

### 6.5.4 Action handling on the game side

The 10 possible action tags the server may return:

| Action | Game's response |
|---|---|
| `idle` | No physical change. Show dialogue and continue conversation. (Most common.) |
| `approach` | NPC sprite moves one step toward player |
| `retreat` | NPC sprite moves one step away |
| `block` | NPC visibly bars an exit. Travel through that exit is disabled until conversation ends or NPC re-evaluates |
| `attack` | Game-over / "you were attacked" beat. Demo can simply end the conversation and bounce player to the inn |
| `flee` | NPC leaves the location. Removed from the NPC list until a scripted return |
| `give_item` | Server returns dialogue only; the game decides which item to add (driven by the conversation's quest state) |
| `call_guard` | A guard NPC appears in the location on the next tick |
| `unlock` | An adjacent locked node on the map becomes accessible |
| `follow` | NPC joins the player's party and appears in every location until dismissed |

If the server returns an unknown action string (model hallucination), the game treats it
as `idle`.

### 6.5.5 Model tier routing

The game does **not** route between GGUF files at runtime — the server loads one model
at startup (`NPC_MODEL` env var). The demo ships in three configurations:

| Config | Loaded model | Used for |
|---|---|---|
| `generic` | `world_npc_generic.gguf` | Background NPCs, smoke tests |
| `medium` | `world_npc_medium.gguf` | Captain Brek scenes |
| `high` | `world_npc_high.gguf` | Sera and Vessa scenes |

For the proof-of-concept playthrough the player runs all three sequentially or the
demonstrator hot-swaps between scenes. A future `/reload` endpoint (see
`Engine-Agnostic-AI.md` stretch goals) would let one server serve all three.

---

## 7. Accusation System

At any point the player can make a formal accusation by speaking to Captain Brek (or later, if Brek is compromised, to Guard Holt).

The accusation itself is **decided by the game's evidence check**, not by the model. The
model is asked only for the in-character reaction once the game knows whether the
accusation succeeded.

Game-side rule (deterministic): success requires the player to hold all three of
`ledger`, `poison_vial`, and `dinner_invite`. The apothecary record (`sale_record`) and
Iddo's account (`iddo_account`) are not required, but strengthen the case and are
included in `context` if held.

The game POSTs to `/npc/dialogue` with the accused NPC name and an explicit context flag
that tells the model which branch to play:

**Correct accusation (Vessa Caldren) + sufficient evidence:**
- `context` includes `accusation_succeeds, evidence: ledger, poison_vial, dinner_invite[, ...]`
- Game-side: Vessa is arrested, ending text rolls
- `actions` constrained to `["idle"]` — no escape paths

**Correct accusation + insufficient evidence:**
- `context` includes `accusation_insufficient_evidence`
- Brek dismisses it. Sera comments on what's still missing.

**Wrong accusation:**
- `context` includes `accusation_wrong, accused: <name>`
- Brek or the accused NPC reacts. Sera pushes back.

---

## 8. UI — Minimal 2D

Simple enough to build quickly. Purpose is to make the demo playable and presentable, not to be a polished game.

### 8.1 Layout

```
┌─────────────────────────────────────────────────────┐
│  LOCATION: The Tallow & Thorn Inn         [Map]      │
├──────────────────────────┬──────────────────────────┤
│                          │                          │
│   DIALOGUE PANEL         │   INVENTORY              │
│                          │   ─────────              │
│   Mira: "I don't know    │   □ Room Key             │
│   what you want me       │   □ Aldric's Journal     │
│   to say. He seemed      │   □ Poison Vial          │
│   fine at dinner."       │                          │
│                          │                          │
│   [Sera]: "She's lying.  │   NPCS HERE              │
│   Look at her hands."    │   ─────────              │
│                          │   • Mira (innkeeper)     │
│                          │   • Conn (barman)        │
│                          │   • Sera (companion)     │
├──────────────────────────┴──────────────────────────┤
│  > What did you see the night Aldric died?          │
│                                              [Send] │
└─────────────────────────────────────────────────────┘
```

### 8.2 Key UI Elements

- **Dialogue panel** — scrollable conversation history with the current NPC. Clearly labels who is speaking. Each entry is the `dialogue` field returned from `/npc/dialogue`. The accompanying `action` value drives sprite/state changes but is **not** displayed as text.
- **Inventory panel** — list of held evidence items. Clicking an item toggles its `context tag` into the next request's `context` field (multi-select). Visually highlight items currently being "shown".
- **NPC list** — who is present in the current location. Click to switch conversation target. The client sends each NPC's stored `npc_name`, `role`, and `voice_prompt` from its character record.
- **Location header** — current location name with a button to open a simple map for travel. On first entry to a location, the client fires `/world/describe` and prints the result as a narrator beat in the dialogue panel.
- **Text input** — free text. Player types anything. Disabled while a request is in flight.
- **Sera panel** — Sera is always available as a conversation target regardless of location. Internally she is just another NPC record sent to `/npc/dialogue`; the client injects her into the NPC list everywhere.

### 8.3 Map

Simple clickable node map. Five nodes (locations), click to travel. No animation needed.

---

## 9. NPC Forge Framework Tests This Demo Validates

| Test | What It Checks |
|---|---|
| World knowledge in weights | Generic NPCs know Ashenveil, the houses, the town without world context in prompt |
| Character model consistency | Sera and Vessa hold personality across full playthrough |
| Inventory as context flags | NPCs react differently when player holds relevant evidence |
| Tier differentiation | Measurable quality difference between generic model and character models |
| Runtime context size | Confirm calls stay under 512 tokens with minimal context |
| Evaluation harness | Automated test suite can flag character breaks and lore errors |

---

## 10. What Gets Built, In Order

1. **Generate the world** — use a large LLM to expand the world bible, write NPC backstories, write sample dialogue for all characters
2. **Manual fine-tuning run** — ingest the lore, generate training data, fine-tune base models for all three tiers — validate core thesis before any tooling is built
3. **Runtime server** — `npc_server/` (FastAPI + llama-cpp-python). **Built.** Exposes `/npc/dialogue`, `/world/describe`, `/status`
4. **Build the 2D UI client** — playable shell that speaks the HTTP contract in §6.5. Stores NPC records (`npc_name`, `role`, `voice_prompt`, default actions) as data, not strings in code
5. **Refine the pipeline** — use this game as the test case for every pipeline improvement

---

*This document will evolve as the proof of concept develops. World details, NPC dialogue, and evidence chains are subject to change during development.*
