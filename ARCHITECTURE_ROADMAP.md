# ProjetoVN — Architecture Roadmap

> Living document. It records the conclusions of the architectural audit of **2026-09-15** (commit `2757f98`) and is the **single place** to plan and track architectural work.
> Update statuses and notes here; do not create separate architecture plans elsewhere.

**Project constraint:** a Unity 6 2D point-and-click / visual novel built by **one developer**. Optimize for productivity, local reasoning and easy debugging, while keeping reasonable modularity. Every item below exists because it solves a concrete problem found in this codebase, not for theoretical "clean architecture".

Paths are relative to `Assets/Scripts/` unless they start with `Assets/` or `ProjectSettings/`.

---

## How to use this document

1. Pick the next item from **[Execution order](#execution-order)** whose dependencies are `DONE`, or whose **Trigger** has happened.
2. **Re-verify "Current problem" against the code first.** This document is a snapshot and the code may have changed.
3. Implement only that item. Satisfy its **Definition of done**, including the README updates it lists.
4. Set **Status**, add a dated note with the commit hash and any deviations, and add a line to the [Changelog](#changelog).
5. If you find a new architectural problem, add a new `ARCH-xx` entry using the same template.
6. **Before "improving" anything that isn't listed here, read [Decisions to keep](#decisions-to-keep).** Several things look "improvable" but were deliberately kept.

**Status values:** `TODO` · `IN PROGRESS` · `DONE` · `DEFERRED` (trigger not reached) · `DROPPED` (explain why)
**Priority values:** `Critical` (realistic serious bug) · `Important` (will get expensive as the game grows) · `Minor` (worth doing when nearby) · `Optional` (only if the project grows into it)
**Complexity:** `Trivial` (<30 min) · `Low` (one sitting) · `Medium` (a few sittings) · `High`

---

## Current architecture in one paragraph

The project uses feature modules with asmdefs: **Core** (MessageBroker, StateMachine) ← **Dialogue**, **Inventory**, **PointNClick** ← **GameFlow** (orchestrator, also references **Inventory** directly since ARCH-09's `GameSaveManager` needs it) ← **UI** (its own asmdef since ARCH-15, referencing only Core and Inventory). The dependency graph has no cycles.
- **Content:** authored data is read-only ScriptableObjects (`DialogueData`, `ItemDataSO`), wired to scene objects with UnityEvents. An `ItemRegistry` SO (ARCH-09) maps stable string ids back to `ItemDataSO` assets for save/load.
- **Logic:** plain C# classes (`DialogueController`, `InventoryService`/`InventoryModel`) behind thin MonoBehaviour singletons (`DialogueManager`, `InventoryManager`, `GameSaveManager`).
- **Communication:** mostly a static, synchronous, strongly-typed pub/sub `MessageBroker`.
- **Lifecycle (since ARCH-08):** all four managers (`GameStateController`, `DialogueManager`, `DialogueInputHandler`, `InventoryManager`, plus `GameSaveManager`) live on one `Assets/Prefabs/Resources/Managers.prefab` (the `Resources` folder is nested inside `Prefabs` so `Resources.Load` finds it while prefabs stay in their usual folder), spawned exactly once by `ManagersBootstrap` (`RuntimeInitializeOnLoadMethod(AfterSceneLoad)`) and `DontDestroyOnLoad`'d as a unit. No scene authors these components directly any more.
- **Execution model:** everything runs on the main thread, with no async, coroutines, threads or Jobs. There are **no true race conditions**. The real risks are **ordering, reentrancy and lifecycle** problems caused by using the bus for commands, queries and state.

## Communication rules (agreed)

These rules drive most items below. They must be reflected in the READMEs (ARCH-05).

| Interaction | Use | Example in this project |
|---|---|---|
| **Command:** exactly one owner must do it | Direct method call on the owner (singleton or serialized reference) | `DialogueManager.Instance.StartDialogue(data)`, `InventoryManager.Instance.TryUse(item)` |
| **Query:** you need an answer | Direct call / property. **Never** request-response over the bus | `InventoryManager.Instance.HasItem(item)` |
| **State that late joiners must know** | Queryable property as the source of truth, optionally plus a change notification | `PlayerInputGate.IsEnabled`, `InventoryManager.Items` |
| **Notification:** "X happened", 0..N listeners, crosses modules | `MessageBroker` message (readonly struct) | `DialogueStartedMessage`, `DialogueEndedMessage`, `ItemCollectedMessage`, `ItemUsedMessage`, `InventoryReplacedMessage` |
| **Story effect:** a dialogue changes game state | `DialogueEffectSO` asset, executed directly (ARCH-20) | `GiveItemEffect`, `SetFlagEffect` on a `DialogueNode`/`DialogueChoice` |
| **Scene object composition** | UnityEvents in the Inspector | `InteractableItem.OnInteract` → `Collect` / `Interact` / `TriggerDialogue` |
| **Module must query a module it cannot reference** | Small interface owned by the querying module (only when needed) | Future dialogue conditions (ARCH-17) |

Additional rules:
- UI may **read** state from managers; it **mutates** only by calling the owner's methods.
- Message handlers must not assume other handlers ran before or after them.
- Static state must be reset in `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`.

---

## Index

| ID | Title | Priority | Phase | Complexity | Depends on | Status |
|---|---|---|---|---|---|---|
| ARCH-01 | Harden MessageBroker | Critical | 1 | Low | — | DONE |
| ARCH-02 | World input gate (fix dialogue-end click-through) | Critical | 1 | Low | — (same session as 03) | DONE |
| ARCH-03 | Dialogue owns its start/end lifecycle | Critical | 1 | Low | ARCH-01 (recommended) | DONE |
| ARCH-04 | Inventory interactions via direct calls | Important | 2A | Low | — | DONE |
| ARCH-05 | Replace README messaging rules | Important | 2A | Trivial | — | DONE |
| ARCH-06 | Simplify state machine, remove demo code | Important | 2A | Medium | ARCH-02, ARCH-03 | DONE |
| ARCH-07 | Inventory data ownership & pullable UI | Important | 2A | Low | ARCH-04 | DONE |
| ARCH-08 | Persistent managers & scene lifecycle | Important | 2B (trigger) | Low–Medium | ARCH-02, ARCH-06, ARCH-07 | DONE |
| ARCH-09 | Persistence foundation: GameState + stable IDs | Important | 2B (trigger) | Medium | ARCH-07 (ARCH-08 recommended) | DONE |
| ARCH-10 | EditMode tests: DialogueController & MessageBroker | Minor | 3 | Low | ARCH-01, ARCH-03 | DONE |
| ARCH-11 | DialogueController publish order & reentrancy | Minor | 3 | Low | ARCH-03, ARCH-10 | DONE (folded into ARCH-20) |
| ARCH-12 | Typed dialogue trigger identifiers | Minor | 3 | Low | ARCH-11 | DROPPED (superseded by ARCH-20) |
| ARCH-13 | Logging cleanup | Minor | 3 | Low | ARCH-01 | DONE |
| ARCH-14 | UI bug fixes & dead code removal | Minor | 3 | Low | — | DONE |
| ARCH-15 | UI assembly definition | Minor | 3 | Low | ARCH-07 | DONE |
| ARCH-16 | Namespace & asmdef hygiene | Minor | 3 | Low (per file) | — | DONE |
| ARCH-17 | Dialogue conditions via query interface | Optional | 4 | Low–Medium | ARCH-07, ARCH-09 | DEFERRED |
| ARCH-18 | Async/animated presentation lifecycle | Optional | 4 | Low | ARCH-03 | TODO |
| ARCH-19 | Asset loading / Addressables strategy | Optional | 4 | Medium–High | — | DEFERRED |
| ARCH-20 | Dialogue effects as ScriptableObject assets | Important | 3 | Medium | ARCH-10, ARCH-11 | DONE |
| ARCH-21 | Authoring-time id & null-safety gaps | Important | 3 | Trivial–Low | — | DONE |
| ARCH-22 | Gated interactions remember their state | Critical | 3 | Low–Medium | ARCH-20 | DONE |

## Execution order

Phases group items by **kind**. This list gives the **order**, based on dependencies and risk.

1. **ARCH-05:** docs only, no code risk. Stops AI-assisted sessions from reintroducing command/query/state-over-bus while the other items are pending.
2. **ARCH-01:** everything else publishes through the broker; make it safe first.
3. **ARCH-02 + ARCH-03** (same session). Both change the dialogue ↔ GameFlow ↔ PointNClick flow; together they fix the two soft-lock/loop bugs.
4. **ARCH-10:** lock in the new broker and dialogue behaviour with tests before further refactors.
5. **ARCH-06:** GameFlow is fresh in mind; moves the input-gate writes into state `Enter()`.
6. **ARCH-04 → ARCH-07:** inventory API first, then data ownership (same API direction; avoids changing signatures twice).
7. **Opportunistic, when touching the area:** ARCH-13, ARCH-14, ARCH-15, ARCH-16. (Done as of 2026-09-17.)
8. **ARCH-08 → ARCH-09** (triggers fired 2026-09-18, both **done** the same day): persistent managers first, then GameState + stable IDs, as recommended.
9. **Active now (started 2026-09-21): ARCH-21 → ARCH-20.** ARCH-21's fixes are mutually disconnected and trivial, so they land first and clear the ground. ARCH-20 then builds the first dialogue-effect consumer — which is precisely **ARCH-11's trigger**, so ARCH-11 is folded into it rather than done as a separate pass over the same two methods. ARCH-12 is dropped along the way; see its entry.
10. **Also active:**
   - **ARCH-18:** typewriter/fades/voice lines are coming soon; design the presentation lifecycle when that dialogue UI work starts (its only dependency, ARCH-03, is done). Independent of ARCH-20 — that one changes *what a node does*, this one changes *how a line is shown*.
11. **Still triggered by something that hasn't happened:**
   - **ARCH-17:** when conditional choices are needed. Note that ARCH-20 puts `StoryFlags` in Core, which shrinks ARCH-17's scope — see its entry.
   - **ARCH-19:** only when profiling shows memory or load-time problems.

```
ARCH-01 ─► ARCH-03 ─► ARCH-10 ─► ARCH-11 ─► ARCH-12
   │          │  └──────────────► ARCH-18
   │          ▼
   └► ARCH-13  ARCH-06 ◄─ ARCH-02
                  │          │
ARCH-04 ─► ARCH-07 ─┬────────┴─► ARCH-08 ─(recommended)─► ARCH-09 ─► ARCH-17
                    ├─► ARCH-15                            ▲
                    └──────────────────────────────────────┘
```

---

## Phase 1 — Critical architectural issues

### ARCH-01 — Harden MessageBroker
- **Priority:** Critical · **Complexity:** Low (one file, ~50 lines) · **Depends on:** — · **Status:** DONE
- **Current problem:**
  - `Publish` calls handlers with no exception isolation. A throwing subscriber aborts the remaining handlers **and** propagates into the publisher's call stack.
  - Handler order is subscription order, which in practice is `OnEnable` subscribers before `Start` subscribers. So `DialogueUIController` always runs before `GameStateController` for `DialogueEndedMessage`.
  - `_wrappers` is keyed only by the handler delegate. Subscribing the same handler twice overwrites the wrapper, and the first subscription can never be removed. `Clear<T>()` also leaves wrappers behind.
  - There is no static reset hook.
- **Why it matters:** every module depends on the broker. One Inspector mistake in a view (for example an unassigned `dialogueBox`) keeps GameFlow from ever receiving `DialogueEndedMessage`, so the game soft-locks behind a misleading stack trace. If "Enter Play Mode Options → no domain reload" is ever enabled for faster iteration, handlers from destroyed objects would survive between play sessions.
- **Recommended solution:**
  - Store handlers per type in a private generic static holder (`static class Handlers<T> { List<Action<T>> }`) and iterate over a snapshot.
  - Wrap each handler call in `try/catch`, calling `Debug.LogException(e)`.
  - Make duplicate subscription idempotent.
  - `Clear()` resets every holder, keeping a list of clear actions; call it from `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`.
  - Optionally, one `[Conditional("VN_TRACE_MESSAGES")]` log in `Publish`, used by ARCH-13.
  - Keep the public API (`Subscribe`, `Unsubscribe`, `Publish`, `Clear`) unchanged.
- **Expected benefit:** subscriber bugs stay local and visible; unsubscribe is correct; the broker works with any Enter Play Mode setting; message flow can be traced from one place. Removes per-publish boxing and array allocation as a side effect (not the motivation).
- **Relevant files/boundaries:** `Core/Messaging/MessageBroker.cs` only. Every module that subscribes is affected at runtime.
- **Risks/considerations:**
  - Subscribe/Unsubscribe during a Publish must keep working, which the snapshot handles. A handler removed during a dispatch still receives the in-flight message (same as today).
  - Caught exceptions must always be logged, never swallowed.
- **Definition of done:**
  - The API is unchanged and all callers compile.
  - A throwing subscriber is logged and the other subscribers still run.
  - Double subscribe followed by a single unsubscribe leaves no handler.
  - The broker is empty at play start even with domain reload disabled.
  - The `Core/Messaging/README.md` flow section is updated.
- **Notes:**
  - *2026-09-17 (working tree, commit pending):* Implemented. Handlers now live in a private generic holder `Handlers<T>` inside `MessageBroker`, which registers a clear action in a static list the first time it is touched, so `Clear()` can reset every message type. `_handlers`/`_wrappers` and the `IMessage` boxing they forced are gone.
  - **Deviation from the recommended solution:** the holder keeps an `Action<T>[]` that is *replaced* on every Subscribe/Unsubscribe (copy-on-write) instead of a `List<T>` copied per publish. This gives the same snapshot semantics the item asked for — including "a handler removed during a dispatch still receives the in-flight message" — while allocating nothing per `Publish` and staying safe under reentrant publishes of the same message type (which `DialogueController` does when chaining). The cost moves to Subscribe/Unsubscribe, which are rare.
  - `Subscribe` is idempotent via `Array.IndexOf`, so a double subscribe followed by a single unsubscribe leaves no handler. `Subscribe`/`Unsubscribe` also ignore a null handler.
  - The optional `[Conditional("VN_TRACE_MESSAGES")]` trace was included: it logs the message type name and handler count per publish. It deliberately does not log the message contents, which would box the struct.
  - Verified by compiling every assembly with `dotnet build` against the Unity-generated csprojs. **Not verified in Play Mode** — the runtime behaviours (throwing subscriber, reset with domain reload disabled) are covered by ARCH-10's EditMode tests, which are still `TODO`.
  - *2026-09-18:* Committed as `742bb83`. ARCH-10's EditMode tests now exist and pass (18/18, including the throwing-subscriber and duplicate-subscribe cases), so this item's runtime behaviour is covered. A full Play Mode session (dialogue + inventory flows, see ARCH-03/04/07 notes below) also ran through the broker repeatedly with zero unexpected console errors.

### ARCH-02 — World input gate (fix dialogue-end click-through)
- **Priority:** Critical · **Complexity:** Low · **Depends on:** — (do in the same session as ARCH-03; ARCH-06 later moves the writes into states) · **Status:** DONE
- **Current problem:**
  1. **Click-through:** `AdvanceDialogue` is bound to `<Mouse>/leftButton`. There is no Input System settings asset, so input is processed before `Update`. The click that ends a dialogue runs this chain: `DialogueEndedMessage` → `TogglePlayerInputMessage(true)` → `PointNClickSelector.enabled = true`. The same frame's `Update` then sees `wasPressedThisFrame` and clicks the object under the cursor, so the dialogue can reopen immediately. *(Inferred from code and bindings; confirm in Play Mode first.)*
  2. **State broadcast as a message:** input availability is sent as a bool message. Objects that were inactive at that moment (their `Awake` never ran) or were loaded later never learn the state. Future modes (inventory, pause, cutscene) would overwrite each other's true/false.
  3. **UI click-through:** `PointNClickSelector` ignores whether the pointer is over UI, so clicks on the inventory panel also hit the world.
- **Why it matters:** it breaks the core point-and-click loop, and state-as-event gets worse with every new mode or scene.
- **Recommended solution:**
  - New static `PointNClick/PlayerInputGate.cs` with:
    - `IsEnabled`
    - `SetEnabled(bool)`, which records `Time.frameCount` when enabling
    - `CanClickThisFrame`: enabled, not the frame it was enabled, and `!EventSystem.current.IsPointerOverGameObject()`
    - a `SubsystemRegistration` reset
  - `PointNClickSelector` reads the gate in `Update` and clears hover while disabled. `ScreenPanController` reads `IsEnabled`. Neither toggles `enabled` any more, and their bus subscriptions are removed.
  - **Only GameFlow writes the gate:** `GameStateController.EnterGameplay/EnterDialogue` for now, state `Enter()` after ARCH-06.
  - Delete `TogglePlayerInputMessage`.
- **Expected benefit:** fixes a likely live bug and gives one source of truth that is safe for late joiners and scene loads. A static gate avoids serialized references across the DDOL boundary, because GameFlow persists and PointNClick is scene-local.
- **Relevant files/boundaries:** `PointNClick/PointNClickSelector.cs`, `PointNClick/ScreenPanController.cs`, `PointNClick/Messages/TogglePlayerInputMessage.cs` (delete), `GameFlow/GameStateController.cs`, new `PointNClick/PlayerInputGate.cs`. PointNClick **owns** the gate; GameFlow is its **only writer**.
- **Risks/considerations:**
  - Reproduce the bug before fixing it.
  - "Only GameFlow writes" is a convention; document it.
  - Don't pre-build a multi-source blocker stack. Revisit only if a system outside GameFlow genuinely needs to block input.
  - Call `IsPointerOverGameObject` from `Update`, not from Input System callbacks.
- **Definition of done:**
  - Clicking through a dialogue with the cursor over its trigger object doesn't reopen it or interact.
  - Clicking on UI doesn't hit the world.
  - An object enabled during a dialogue cannot interact.
  - No references to `TogglePlayerInputMessage` remain.
  - The PointNClick and GameFlow READMEs are updated.
- **Notes:**
  - *2026-09-17 (working tree, commit pending):* Implemented as specified. New `PointNClick/PlayerInputGate.cs` with `IsEnabled`, `SetEnabled(bool)`, `CanClickThisFrame` and a `SubsystemRegistration` reset. `PointNClickSelector` clears hover and returns early while the gate is disabled, and checks `CanClickThisFrame` before invoking `OnClick`. `ScreenPanController` reads `IsEnabled` in `Update`. Neither toggles `enabled` any more and both lost their bus subscriptions. `TogglePlayerInputMessage.cs` and the now-empty `PointNClick/Messages/` folder were deleted.
  - `ProjetoVN.PointNClick.asmdef` gained a reference to `UnityEngine.UI` (GUID `2bafac87e7f4b9b418d9448d219b01ab`), which is where `UnityEngine.EventSystems.EventSystem` lives. `PlayerInputGate` treats a missing `EventSystem.current` as "pointer not over UI".
  - `IsEnabled` defaults to `true`, matching the previous behaviour where `PointNClickSelector` started enabled. `SetEnabled` only records the frame on a `false → true` transition, so the redundant `EnterGameplay()` in `GameStateController.Start()` does not swallow the first frame of input.
  - **Open:** the click-through bug itself is still unconfirmed in Play Mode — see [Open questions](#open-questions-answers-change-priorities). The fix was written from the inferred chain described above; confirm it closes the loop before considering this item observed rather than merely implemented.
  - **Not done here (deliberately):** hover still highlights a world object that sits behind a UI panel, because `CanClickThisFrame` only gates the click. Only revisit if it actually looks wrong in game.
  - *2026-09-18:* Committed as `3abbbd5`. Play Mode session confirmed `PlayerInputGate.IsEnabled` correctly flips to `false` on dialogue start and back to `true` on dialogue end, across four separate dialogue triggers (an NPC, a locked door, an unlocked door, and a choice-ending dialogue) with zero console errors. Still not a literal mouse-driven click-through repro — see [Open questions](#open-questions-answers-change-priorities).

### ARCH-03 — Dialogue owns its start/end lifecycle
- **Priority:** Critical · **Complexity:** Low · **Depends on:** ARCH-01 (recommended); same session as ARCH-02 · **Status:** DONE
- **Current problem:**
  - The start flow is `InteractableDialogueTrigger` → `DialogueRequestMessage` → `GameStateController.OnDialogueRequested`. That handler calls `DialogueManager.Instance.StartDialogue`, **then** enters `DialogueState` and disables input.
  - `DialogueController.StartDialogue` returns silently for null or 0-node data. Input stays disabled, `DialogueEndedMessage` never arrives, and the player is **soft-locked**.
  - The first node's line and triggers are dispatched while the state is still Gameplay.
  - `DialogueRequestMessage` is defined in Dialogue but Dialogue never handles it.
- **Why it matters:** mistakes when authoring SOs are routine, and the result is an unrecoverable lock. The flow also takes an unnecessary detour through GameFlow.
- **Recommended solution:**
  - `DialogueManager.StartDialogue(DialogueData)` returns `bool` and logs a warning on invalid data.
  - `DialogueController` publishes a new `DialogueStartedMessage` on the inactive → active transition, **before** processing the first node, and not when chaining via `NextDialogueData`/`TargetDialogue`.
  - `InteractableDialogueTrigger` calls `DialogueManager.Instance.StartDialogue` directly. Optionally move it into the Dialogue module, since it then depends only on Dialogue.
  - `GameStateController` subscribes to `DialogueStartedMessage`/`DialogueEndedMessage` and calls `EnterDialogue`/`EnterGameplay`.
  - Delete `DialogueRequestMessage`.
- **Expected benefit:** no soft lock; the mode switches before any content is shown; one fewer message; a flow that reads top to bottom.
- **Relevant files/boundaries:** `Dialogue/Logic/DialogueController.cs`, `Dialogue/Logic/DialogueManager.cs`, `Dialogue/Messaging/DialogueRequestMessage.cs` (delete), new `Dialogue/Messaging/DialogueStartedMessage.cs`, `GameFlow/InteractableDialogueTrigger.cs`, `GameFlow/GameStateController.cs`. UnityEvent wiring (`TriggerDialogue`) lives in `Assets/Prefabs/Gameplay/Interactable.prefab` and `Assets/Scenes/[Teste] CameraPan.unity`.
- **Risks/considerations:**
  - If you move `InteractableDialogueTrigger`, move it **inside the Unity Editor** so its `.meta` GUID is kept, and keep the method name `TriggerDialogue`. Afterwards, check the UnityEvent wiring in the Inspector, since persistent calls store the target type name.
  - Chaining must not publish Started twice.
  - A choice whose target dialogue is empty should log a warning.
- **Definition of done:**
  - 0-node `DialogueData` logs a warning, the state stays Gameplay and input stays enabled.
  - For a valid dialogue, the state is Dialogue before the first line is shown.
  - A chained dialogue publishes Started once and Ended once.
  - No references to `DialogueRequestMessage` remain.
  - The Dialogue and GameFlow READMEs are updated.
- **Notes:**
  - *2026-09-17 (working tree, commit pending):* Implemented. `DialogueController.StartDialogue` and `DialogueManager.StartDialogue` now return `bool`; `DialogueStartedMessage` is published on the inactive → active transition only, before the first node is processed. `InteractableDialogueTrigger` calls `DialogueManager.Instance.StartDialogue` directly (logging an error when the singleton is missing). `GameStateController` subscribes to Started/Ended and no longer references `DialogueManager`. `DialogueRequestMessage.cs` was deleted.
  - **Deviation:** the invalid-data warning lives in `DialogueController`, not in `DialogueManager`. The manager just delegates. One place logs it, and it covers every entry point — including the chained `NextDialogueData` and a choice's `TargetDialogue`, which never pass through the manager. The DoD still holds: a 0-node `DialogueData` started through the manager logs a warning.
  - **Addition not in the original plan:** an invalid chain target (`NextDialogueData` or `TargetDialogue`) now ends the dialogue cleanly via a new private `EndDialogue()` instead of leaving `_currentData` pointing at the finished dialogue. Without this the soft lock ARCH-03 exists to remove would simply have moved from the start of a dialogue to the middle of one. `ClearDialogueState` also resets `_isWaitingForChoice`, which it previously leaked.
  - **Not done (optional in this item):** `InteractableDialogueTrigger` was **not** moved into the Dialogue module. It is not needed for any part of the DoD and moving it means an in-Editor move plus re-verifying UnityEvent wiring in `Interactable.prefab` and `[Teste] CameraPan.unity`. The class name, namespace and `TriggerDialogue()` method name are unchanged, so the existing wiring keeps working untouched.
  - Verified by compilation only; the Play Mode checks in the DoD have not been run.
  - *2026-09-18:* Committed as `49192d7`. Play Mode checks now run: a 0-node/invalid dialogue path was already covered by ARCH-10's tests; a valid dialogue (the "Gotica" NPC) entered Dialogue state before the first line rendered, ended cleanly on the player's choice, and the state returned to Gameplay. Also fixed a related gap found in this session: `DialogueChoiceButton.OnClicked()` had no null-check on `DialogueManager.Instance` (every other consumer in the codebase does); added one, consistent with the existing pattern.

---

## Phase 2 — High-value improvements

### 2A — Do proactively

### ARCH-04 — Inventory interactions via direct calls
- **Priority:** Important · **Complexity:** Low · **Depends on:** — (after Phase 1 preferred; must precede ARCH-07) · **Status:** DONE
- **Current problem:**
  - `LockedActionBehaviour` checks for the item via `CheckItemRequestMessage` (request plus callback). It then consumes the item via `UseItemCommandMessage` and **ignores the result**.
  - `CollectableItemBehaviour` uses `CollectItemCommandMessage`.
  - All of these publishers and their handler (`InventoryManager`) live in the same assembly, so the bus adds no decoupling.
- **Why it matters:** a query over pub/sub does nothing silently with 0 responders, and invokes the callback twice with 2 (double unlock). `OnUnlocked` fires even if using the item failed.
- **Recommended solution:**
  - `InventoryManager` exposes `HasItem(ItemDataSO)`, `Collect(ItemDataSO)` and `TryUse(ItemDataSO)`, and makes `Service` private.
  - The behaviours call `InventoryManager.Instance` directly, logging an error if it is null.
  - `OnUnlocked` is invoked only if `TryUse` returns `true`.
  - Delete the three command/query messages. Keep `ItemCollectedMessage` and `ItemUsedMessage`.
- **Expected benefit:** explicit, debuggable flow with correct failure paths, and three fewer message types.
- **Relevant files/boundaries:** `Inventory/LockedActionBehaviour.cs`, `Inventory/CollectableItemBehaviour.cs`, `Inventory/InventoryManager.cs`, `Inventory/Messages/CheckItemRequestMessage.cs`, `CollectItemCommandMessage.cs`, `UseItemCommandMessage.cs` (delete).
- **Risks/considerations:** use `ItemDataSO` parameters now (mapping to id internally), so ARCH-07 doesn't change the signatures again. The singleton must exist in the scene until ARCH-08.
- **Definition of done:**
  - The door shows `PortaTrancada` without the key.
  - With the key, it consumes the key once and shows `PortaDestrancada`.
  - The three messages are deleted.
  - The Inventory README flows are corrected.
- **Notes:**
  - *2026-09-17 (working tree, commit pending):* Implemented. `InventoryManager` exposes `HasItem`/`Collect`/`TryUse`, all taking `ItemDataSO`; `Service` is now a private field, and the three bus subscriptions (and `OnEnable`/`OnDisable` with them) are gone. `CheckItemRequestMessage`, `CollectItemCommandMessage` and `UseItemCommandMessage` were deleted.
  - **Deviation:** `LockedActionBehaviour` does **not** call `HasItem` before `TryUse`. `TryUse` returns `false` exactly when the player lacks the item, so it decides both branches on its own: `true` → `OnUnlocked`, `false` → `OnLocked`. This satisfies "`OnUnlocked` is invoked only if `TryUse` returns `true`" with one lookup instead of two and no check-then-act shape. `HasItem` is still exposed, because the [Communication rules](#communication-rules-agreed) name it as the query example and ARCH-17 will need it.
  - Removing `OnEnable`/`OnDisable` from `InventoryManager` also removed, as a side effect, the "duplicate `InventoryManager` runs `OnEnable` with `Service == null`" problem listed under ARCH-08. The rest of ARCH-08's duplicate-manager lifecycle is untouched.
  - `Collect` now returns `bool` (see ARCH-07) rather than `void`.
  - *2026-09-21 — **the definition-of-done item above needs one qualifier now.*** This item requires *"`OnUnlocked` is invoked only if `TryUse` returns `true`"*. After [ARCH-22](#arch-22--gated-interactions-remember-their-state) that still holds for every **item gate**: an already-open door takes the `OnAlreadyUnlocked` branch and never reaches `OnUnlocked`, and a failed `TryUse` still goes to `OnLocked`. The one new case is a **flag-only gate** (`requiredItem` empty, `requiredFlagId` set), which opens without any item to use, so `OnUnlocked` fires with no `TryUse` call at all. **Restated:** when a gate has a `requiredItem`, `OnUnlocked` fires only if `TryUse` returned `true`. *(An earlier draft of ARCH-22 broke this more widely by falling back from `OnAlreadyUnlocked` to `OnUnlocked`; that fallback was reverted the same day.)* Also note `LockedActionBehaviour` no longer lives in `Inventory/`, so this item's file list is stale.
  - *2026-09-18:* Committed as `942df8f`. Play Mode session verified the full loop: `PortaTrancada.Interact()` without the key correctly plays the locked dialogue; `Chave.Collect()` deactivates the world object and adds it to the model; `PortaTrancada.Interact()` with the key returns `true` from `TryUse` and plays the unlocked dialogue. Also fixed a related bug found in this session: `InventoryManager.Awake()`'s duplicate-singleton branch called `Destroy(this)` instead of `Destroy(gameObject)` (unlike `DialogueManager`), which leaked an empty GameObject under `DontDestroyOnLoad`. Now matches `DialogueManager`'s pattern.

### ARCH-05 — Replace README messaging rules
- **Priority:** Important · **Complexity:** Trivial · **Depends on:** — (do first; later items update their own module READMEs) · **Status:** DONE
- **Current problem:**
  - The READMEs, written partly as instructions for AI assistants, prescribe routing everything through `MessageBroker` ("UI never pulls state", "totally decoupled via bus", testing "by publishing messages").
  - They also describe flows that don't exist: dragging items in the UI, `ItemUsedMessage` unlocking `LockedActionBehaviour`, dialogue using `CheckItemRequestMessage`, ESC handling in `UIWindowManager`.
- **Why it matters:** AI-assisted sessions follow these READMEs and would keep re-creating the problems this roadmap fixes.
- **Recommended solution:**
  - Put the [Communication rules](#communication-rules-agreed) into `Core/Messaging/README.md` and link to them (and to this roadmap) from every module README.
  - Remove the non-existent flows, or mark them "planned".
- **Expected benefit:** documentation that steers new code toward the agreed patterns.
- **Relevant files:** `Core/Messaging/README.md`, `Core/StateMachine/README.md`, `Dialogue/README.md`, `GameFlow/README.md`, `Inventory/README.md`, `PointNClick/README.md`, `UI/README.md`, `Tests/README.md`.
- **Risks/considerations:** docs drift again. Mitigated because every ARCH item's definition of done includes its README update.
- **Definition of done:** no README tells readers to route commands, queries or state through the bus; no README describes behaviour that doesn't exist; the rules are present and linked.
- **Notes:**
  - *2026-09-17 (working tree, commit pending):* Done. The [Communication rules](#communication-rules-agreed) now live, translated to Portuguese to match the rest of the docs, in `Core/Messaging/README.md` under "Regras de comunicação". All nine READMEs (including `ScriptableObjects/`, which the item didn't list) open with a blockquote linking to those rules and to this roadmap.
  - **Ordering deviation:** the [Execution order](#execution-order) puts ARCH-05 first. It was done **last** in this session instead, because ARCH-04/06/07 each rewrite the flows in the same READMEs and doing the docs first would have meant writing `Inventory/README.md` and `UI/README.md` twice. The rationale for "do first" is to stop *future* sessions from reintroducing the old patterns, which a single session that follows the rules anyway does not need. The end state is identical.
  - Non-existent flows removed: UI item dragging and `ItemUsedMessage` unlocking `LockedActionBehaviour` (Inventory), dialogue using `CheckItemRequestMessage` (Inventory), ESC handling in `UIWindowManager` (UI), and testing by publishing messages plus the `IStateMachine` mention (Tests). `Core/Messaging/README.md` also lost its `StateChangedMessage` examples, which ARCH-06 deleted.
  - The UI README's "a UI nunca busca dados ativamente" rule was replaced with the agreed one: the UI **may read** manager state and mutates only by calling the owner.
  - `Tests/README.md` now says plainly that no tests exist yet and points at ARCH-10.

### ARCH-06 — Simplify state machine, remove demo code
- **Priority:** Important · **Complexity:** Medium (net deletion of ~150 lines, plus scene rewiring) · **Depends on:** ARCH-02, ARCH-03 · **Status:** DONE
- **Current problem:**
  - `Core/StateMachine` stacks a lot of machinery on two **empty** states (`GameplayState`, `DialogueState`):
    - a `StateMachine` MonoBehaviour
    - `IStateMachine`, `IStateFactory` and `StateFactory<T>` (using `Activator.CreateInstance`)
    - registration, a cache and a history stack
    - a C# event plus `StateChangedMessage`
    - controllers that hold a `MonoBehaviour` field cast to the interface
  - The real logic lives in `GameStateController`.
  - Constructors reached only through reflection can be stripped by IL2CPP managed stripping, which fails **only in builds**.
  - Core ships a demo `StateController` and `StatesTesting/` (`MenuState`, `InventoryState`, a second `DialogueState`). `SampleScene`, the only scene in Build Settings, runs that demo.
  - `GameStateController.Start` throws a NullReferenceException if `Awake` validation failed.
- **Why it matters:** every new mode costs boilerplate for a class that does nothing; a build-only failure risk sits hidden; demo code in Core misleads future sessions.
- **Recommended solution:**
  - Keep `BaseState` (`Enter`/`Exit`/`Update`).
  - Replace the machine with a plain C# `StateMachine`: `ChangeState`, `Push`/`Pop` for overlay modes such as inventory or pause, and `Tick`. `GameStateController` owns it and forwards `Update`.
  - Create states with `new`. States own their side effects, e.g. `GameplayState.Enter` → `PlayerInputGate.SetEnabled(true)` and `DialogueState.Enter` → `false`.
  - Delete `IStateMachine`, `IStateFactory`, `StateFactory`, the `StateMachine` component, `StateChangedMessage`, `StateController`, `StatesTesting/` and `BaseState.OnStateExit`.
  - Put the real game scene in Build Settings.
- **Expected benefit:** a new mode is one class plus one `ChangeState` call; no reflection; states hold real responsibility; less code.
- **Relevant files/boundaries:** `Core/StateMachine/*`, `GameFlow/GameStateController.cs`, `GameFlow/States/*`, `Assets/Scenes/SampleScene.unity`, `Assets/Scenes/[Teste] CameraPan.unity`, `ProjectSettings/EditorBuildSettings.asset`.
- **Risks/considerations:**
  - Remove the `StateMachine`/`StateController` components from scenes **in the Editor before** deleting the scripts, to avoid "Missing Script".
  - Keep the machine in Core (it's tiny).
  - No hierarchical states or transition tables.
- **Definition of done:**
  - No factories, `Activator` or `IStateMachine` remain.
  - Adding a state needs only a class.
  - Input gate writes live in state `Enter()`.
  - Core has no demo code, and no scene has missing scripts.
  - Build Settings contains the intended scene.
  - The StateMachine and GameFlow READMEs are updated.
- **Notes:**
  - *2026-09-17 (working tree, commit pending):* Implemented. `Core/StateMachine` is now just `BaseState.cs` and a plain C# `StateMachine.cs` (`ChangeState`, `Push`, `Pop`, `Tick`). `IStateMachine`, `IStateFactory`, `StateFactory`, `StateController`, `Messages/StateChangedMessage` and `StatesTesting/` were deleted, together with `BaseState.OnStateExit`. `GameStateController` owns the machine as a plain field, creates both states with `new` in `Awake`, and forwards `Update` via `Tick()`; its `stateMachineRef` serialized field is gone, so the `Start` NullReferenceException it could throw is gone with it.
  - Input gate writes moved into `GameplayState.Enter()` / `DialogueState.Enter()` as specified.
  - **Deviation:** `BaseState.FixedUpdate` was dropped along with the rest. The item says "Keep `BaseState` (`Enter`/`Exit`/`Update`)" and nothing overrode it; a VN with no physics has no use for it. Re-add it with a `FixedTick` on the machine if that ever changes.
  - **Scene edits were made by editing the `.unity` YAML directly, not in the Editor**, because this session has no interactive Editor. The `StateMachine` component and the `stateMachineRef` reference were removed from `GameController` in `[Teste] CameraPan.unity`, and the whole `Test State Machine` GameObject (Transform + `StateController` + `StateMachine`) was removed from `SampleScene.unity`, including its `SceneRoots` entry. Both scenes were then verified to have no dangling local `fileID` references and no references to any deleted script GUID. Unity was running during the session and re-imported both scenes without complaint.
  - **Build Settings** now contains only `Assets/Scenes/[Teste] CameraPan.unity` (decided with the project owner). `Menu.unity` stays out because there is no scene-loading code anywhere yet; adding a menu → gameplay transition is exactly what trips ARCH-08's trigger.
  - **SampleScene.unity was kept** (decided with the project owner), stripped of the demo components rather than deleted. It still holds a `DialogueManager` and some UI.
  - *2026-09-21 — **this decision was reversed**.* `SampleScene.unity` is deleted under [ARCH-21](#arch-21--authoring-time-id--null-safety-gaps). Re-decided with the project owner: the scene is not in Build Settings, nothing references it, and `[Teste] Mecanicas` has since superseded it as the sandbox. The deciding factor is the leftover `DialogueManager` noted above — since ARCH-08 made `Managers.prefab` the single owner of that component, a stray scene-authored one would overwrite `DialogueManager.Instance` if the scene were ever opened, which is a trap with no upside now that the scene has no other purpose.
  - Verified by compiling all six assemblies with Roslyn, each against only its asmdef-permitted references. The Play Mode checks in the DoD have not been run.
  - *2026-09-18:* Committed as `742bb83` (Core) and `7ebd50f` (scene edits). Play Mode confirmed states construct and run correctly: `GameplayState`/`DialogueState.Enter()` toggle `PlayerInputGate` as designed (see ARCH-02's 2026-09-18 note), and `[Teste] CameraPan.unity` loaded and played with zero missing-script errors.

### ARCH-07 — Inventory data ownership & pullable UI
- **Priority:** Important · **Complexity:** Low · **Depends on:** ARCH-04 · **Status:** DONE
- **Current problem:**
  - `Item` copies `ItemDataSO` fields and drops the reference, so `InventoryPresenter` needs a manually maintained `itemDatabase` list to find icons.
  - `InventoryModel.AddItem` allows duplicates while the presenter de-duplicates by id, so model and UI disagree.
  - The presenter builds its state only from events and unsubscribes in `OnDisable`. It misses items collected while hidden and can't rebuild after a load.
- **Why it matters:** there are two sources of truth, and the UI is wrong depending on enable timing. It also blocks saves (ARCH-09).
- **Recommended solution:**
  - The model stores `ItemDataSO` references. Add an entry type only once per-instance data such as quantity exists.
  - Reject duplicates, or define stacking explicitly.
  - `InventoryManager` exposes a read-only `Items`.
  - The presenter rebuilds from `Items` in `OnEnable` and applies `ItemCollectedMessage`/`ItemUsedMessage` incrementally; both messages carry `ItemDataSO`.
  - Delete `Item`, `ToDomainItem` and `itemDatabase`.
- **Expected benefit:** a single source of truth, UI that is always correct, and readiness for persistence.
- **Relevant files/boundaries:** `Inventory/InventoryModel.cs`, `InventoryService.cs`, `InventoryManager.cs`, `Item.cs` (delete), `ItemDataSO.cs`, `Inventory/Messages/ItemCollectedMessage.cs`, `ItemUsedMessage.cs`, `UI/Inventory/InventoryPresenter.cs`; the inventory UI in `[Teste] CameraPan.unity`.
- **Risks/considerations:** the UI now reads manager state, which the rule in ARCH-05 allows. Decide the stacking semantics now if quantities are likely. Don't merge the model and service (D-10).
- **Definition of done:**
  - Hide the panel, collect an item, show the panel: the item appears.
  - Collecting the same item twice follows the defined rule, and the UI matches the model.
  - `Item` and `itemDatabase` are gone.
  - The Inventory and UI READMEs are updated.
- **Notes:**
  - *2026-09-17 (working tree, commit pending):* Implemented. `InventoryModel` stores `ItemDataSO` references (`Add`/`Remove`/`Contains`/`Items`), `InventoryService` works in `ItemDataSO` throughout and returns `bool` from both mutations, `InventoryManager` exposes `IReadOnlyList<ItemDataSO> Items`, and `ItemCollectedMessage`/`ItemUsedMessage` carry the `ItemDataSO`. `Item.cs` and `ToDomainItem()` are gone.
  - **Stacking semantics decided: duplicates are rejected.** `Collect` returns `false` and publishes nothing when the item is already held. The collectable world object deactivates itself on pickup, so duplicates were already unreachable in practice, and the presenter was already de-duplicating by id — this makes the model agree with what the UI always showed. If quantities are ever needed, the place for them is an entry type (item + quantity) inside `InventoryModel`, not a list with repeats. This is written into `Inventory/README.md`.
  - Membership is by **reference** to the `ItemDataSO` asset, not by `Id`. Two distinct assets sharing an `Id` would be an authoring bug; the editor check for that is ARCH-09.
  - `ItemDataSO` gained public `ItemName` and `Description`, which `Item` used to carry. The UI slot reads the name and icon straight off the asset.
  - `InventoryPresenter` rebuilds from `InventoryManager.Items` in `OnEnable` and then applies both messages incrementally; `_activeSlots` is keyed by `ItemDataSO`. The serialized `itemDatabase` list was removed from the script **and** from `[Teste] CameraPan.unity`. It logs a warning and opens empty if there is no `InventoryManager`.
  - Verified by compilation only; the Play Mode checks in the DoD have not been run.

### 2B — Important, milestone triggers fired and both items are done

> **Trigger check, 2026-09-17:** both were still `DEFERRED`. `[Teste] CameraPan.unity` was the only gameplay scene and there was no `SceneManager`/`LoadScene` call anywhere in `Assets/Scripts`, so ARCH-08 had not fired. There was no save/load code (no `JsonUtility`, no `persistentDataPath`) and authored content was still 8 test dialogues plus 1 test item, so ARCH-09 had not fired either.
>
> **Trigger check, 2026-09-18:** asked the project owner directly. A second gameplay scene / scene transitions are coming **soon**, and save/load is **needed soon** (before the first playable). Both triggers have fired — status moved from `DEFERRED` to `TODO`, sequenced right after the opportunistic Phase 3 cleanup (see [Execution order](#execution-order)). Do ARCH-08 before ARCH-09, as originally recommended, and do ARCH-09 before authoring more rooms or dialogue content, since its own trigger note warns IDs get more expensive to retrofit once content grows.
>
> **New evidence for ARCH-08's problem, observed 2026-09-18:** Play Mode verification (after committing ARCH-01–16) reproduced the singleton-ordering risk live, not just in theory. At scene start in `[Teste] CameraPan.unity`, the console logs `[InventoryPresenter] Não há InventoryManager. O painel abrirá vazio.` — `InventoryPresenter.OnEnable()` (on `Canvas`, a root earlier in the scene hierarchy) runs before `InventoryManager.Awake()` (on `GameController`, the last root) has set `Instance`. The incremental path still works (collecting an item afterwards correctly updates the panel via `ItemCollectedMessage`), but the initial rebuild silently opens empty. This is exactly the "no bootstrap, so a scene without a well-ordered managers object breaks on Play" problem ARCH-08 already described, now confirmed rather than inferred.

### ARCH-08 — Persistent managers & scene lifecycle
- **Priority:** Important · **Trigger:** before adding a second gameplay scene or any scene transition — **fired 2026-09-18** · **Complexity:** Low–Medium · **Depends on:** ARCH-02, ARCH-06, ARCH-07 · **Status:** DONE
- **Current problem:**
  - `DialogueManager`, `InventoryManager`, `GameStateController` and `StateMachine` share one GameObject in `[Teste] CameraPan.unity`, and two singletons each call `DontDestroyOnLoad` on it.
  - Duplicate handling is now consistent (`Destroy(gameObject)` everywhere, fixed 2026-09-18 in `InventoryManager`), but nothing else about manager lifecycle changed.
  - `Instance` is never cleared.
  - Views and PointNClick are scene-local.
  - There is no bootstrap, so a scene without the managers object breaks on Play.
  - **Observed live, 2026-09-18:** `InventoryManager` (on `GameController`, last root in the scene) sets `Instance` in `Awake()` after `InventoryPresenter` (on `Canvas`, an earlier root) has already run `OnEnable()` and rebuilt from it — Unity does not guarantee cross-GameObject Awake-before-OnEnable ordering by hierarchy position. The panel logs `[InventoryPresenter] Não há InventoryManager. O painel abrirá vazio.` and opens empty; it only becomes correct once the first `ItemCollectedMessage`/`ItemUsedMessage` arrives. This is the concrete failure the "no bootstrap" problem above predicts, now reproduced rather than theoretical.
- **Why it matters:** once scenes change, this produces duplicate or ghost subscribers and mode state that disagrees with the loaded scene.
- **Recommended solution:**
  - Create one `Managers` prefab holding all persistent managers. Spawn it once through a `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` loader from `Resources`, so Play works from any room scene; a bootstrap scene is the fallback.
  - Only the prefab root does DDOL and the duplicate guard. Remove the per-manager DDOL and duplicate code, and clear `Instance` in `OnDestroy`.
  - Scene-local components read the current state on `Start` (possible after ARCH-02 and ARCH-07).
- **Expected benefit:** Play from any scene; safe scene transitions; no ghost handlers.
- **Relevant files/boundaries:** `Dialogue/Logic/DialogueManager.cs`, `Inventory/InventoryManager.cs`, `GameFlow/GameStateController.cs`, the new loader and prefab, all gameplay scenes.
- **Risks/considerations:**
  - Never serialize scene UI references into the persistent prefab; views stay scene-local.
  - `SceneManager.LoadScene` is enough. No scene-management framework until loading screens are needed.
- **Definition of done:**
  - Play works from any gameplay scene.
  - Loading a scene twice leaves exactly one of each manager and one subscription per handler.
  - No per-manager DDOL code remains.
- **Notes:**
  - *2026-09-18 (fired):* Trigger fired (project owner confirmed a second scene is coming soon); status moved `DEFERRED` → `TODO`.
  - *2026-09-18 (implemented):* `GameStateController`, `DialogueManager`, `DialogueInputHandler`, `InventoryManager` (the exact four components already on the scene's `GameController` GameObject, none of which had any scene-local serialized reference) were saved as a single `Assets/Resources/Managers.prefab` via `create_prefab` on the live Editor, then the scene's own `GameController` instance was deleted so only the bootstrap's copy exists. New `GameFlow/ManagersBootstrap.cs` spawns it once via `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]`, `DontDestroyOnLoad`'s the root, and resets its static guard on `SubsystemRegistration` (same pattern as `MessageBroker`/`PlayerInputGate`, the only other two `RuntimeInitializeOnLoadMethod` users in the codebase). `DialogueManager`/`InventoryManager` no longer self-DDOL or self-guard against duplicates in `Awake` (the bootstrap now guarantees exactly one spawn); both clear `Instance` in a new `OnDestroy`.
  - **Fixed the race this item's problem statement documents, not just described it:** `InventoryPresenter`'s first `Rebuild()` call moved from `OnEnable()` to a new `Start()` (`OnEnable()` only rebuilds on *subsequent* activations, guarded by an `_initialized` flag). This matters because `AfterSceneLoad` fires after the initial scene's `Awake`/`OnEnable` but before its `Start()` — so only `Start()` is guaranteed to run after the bootstrap has spawned `InventoryManager`. Verified live: entering Play mode no longer logs `[InventoryPresenter] Não há InventoryManager`, and `UnityEngine.Object.FindObjectsByType<InventoryManager>().Length == 1` after an in-session `SceneManager.LoadScene` reload of the same scene, with the collected key's world object correctly staying hidden across that reload (see ARCH-09's `IsWorldObjectConsumed` check).
  - **Not built:** a bootstrap *scene* fallback (the roadmap allows either) — the `Resources`-prefab path alone is enough for a single-scene project and is simpler to keep working than maintaining two bootstrap paths. Revisit only if a scene is ever opened directly without going through normal Play (e.g. a dedicated test scene) and the missing managers become a real friction point.
  - **New dependency:** `ProjetoVN.GameFlow.asmdef` now also references `ProjetoVN.Inventory` (needed for `GameSaveManager`, ARCH-09). No cycle: `Inventory`'s asmdef only references `Core`.
  - *2026-09-21 (correction):* the note above says the prefab is at `Assets/Resources/Managers.prefab`. It was **moved to `Assets/Prefabs/Resources/Managers.prefab`** later the same day (see the 2026-09-18 "New organized test scene" changelog entry), so `Resources.Load` still finds it while prefabs stay in their usual folder. The [Current architecture](#current-architecture-in-one-paragraph) paragraph already had the right path; this note did not.

### ARCH-09 — Persistence foundation: GameState + stable IDs
- **Priority:** Important · **Trigger:** before implementing save/load, **or** before authoring large amounts of content (IDs get more expensive to retrofit) — **fired 2026-09-18** · **Complexity:** Medium · **Depends on:** ARCH-07 (ARCH-08 recommended) · **Status:** DONE
- **Current problem:**
  - Runtime state is scattered: the inventory lives in the DDOL manager and the dialogue position in `DialogueController`.
  - Collected world objects are only `SetActive(false)`, so after a scene reload they respawn and can be collected again.
  - There are no story flags.
  - `ItemDataSO.id` uniqueness is not checked, and world objects have no persistent IDs.
- **Why it matters:** a VN needs flags, inventory, consumed objects and the current scene saved. Without stable IDs, saving becomes a refactor.
- **Recommended solution:**
  - One serializable `GameState` POCO: flag/variable lists, owned item ids, consumed world-object ids, current scene, and optionally seen dialogues.
  - One owner; systems read and write it directly.
  - Save and load with `JsonUtility` (lists, not dictionaries) to `Application.persistentDataPath`.
  - One registry asset mapping id → `ItemDataSO`.
  - An editor check for duplicate `ItemDataSO` ids.
  - A serialized persistent id on `CollectableItemBehaviour` (and any consumable world object), checked on `Start`.
  - Place `GameState` in Core if Dialogue must read flags (ARCH-17); otherwise in GameFlow.
- **Expected benefit:** saving becomes plain serialization; fixes items respawning and being collected twice; flags are ready for dialogue triggers and conditions.
- **Relevant files/boundaries:** new `GameState` + owner, `Inventory/*`, `Inventory/CollectableItemBehaviour.cs`, `Inventory/ItemDataSO.cs`.
- **Risks/considerations:**
  - Don't build a generic save framework (ISaveable graphs, reflection).
  - Persistent ids generated in `Reset`/`OnValidate` get copied when a prefab is duplicated, so validate uniqueness in the editor.
- **Definition of done:**
  - A collected item stays collected across scene reloads.
  - Duplicate ids are flagged in the editor.
  - `GameState` round-trips through JSON in a test.
  - Save/load restores the inventory, and the UI rebuilds.
- **Notes:**
  - *2026-09-18 (fired):* Trigger fired (project owner confirmed save/load is needed for the first playable); status moved `DEFERRED` → `TODO`. Scope confirmed with the project owner: full save/load now, not just the data foundation.
  - *2026-09-18 (implemented):* `ItemDataSO.id` (already existed, free-text, only warned on empty) is now resolved through a new `ItemRegistry` ScriptableObject (`Inventory` module, one authored instance at `Assets/Scripts/ScriptableObjects/Items/ItemRegistry.asset`) with an `OnValidate` duplicate-id check (`#if UNITY_EDITOR`, same style as `ItemDataSO`'s own validation). `CollectableItemBehaviour` got a `persistentId` string generated once via `Reset()` (plus a `[ContextMenu("Regenerate Persistent Id")]` for the prefab-duplication collision case the roadmap warns about — no automatic scene-wide scanner was built for that, since no concrete collision has happened yet).
  - **Consumed-world-object tracking lives in `InventoryManager`, not a new service:** `CollectableItemBehaviour` (Inventory module) can't depend on GameFlow without creating a cycle (GameFlow already depends on Inventory), so `IsWorldObjectConsumed`/`MarkWorldObjectConsumed`/`ConsumedWorldObjectIds`/`ReplaceConsumedWorldObjectIds` were added directly to `InventoryManager`. `CollectableItemBehaviour.Start()` hides itself if already consumed; `Collect()` marks itself consumed in addition to calling `InventoryManager.Instance.Collect(itemData)`. This alone — once ARCH-08's persistent managers exist — already satisfies "stays collected across scene reloads" **within a running session**, with no disk I/O: verified live via `SceneManager.LoadScene` on the same scene while Play mode was running.
  - `GameState` (POCO: `ownedItemIds`, `consumedWorldObjectIds`, `currentScene`) and `GameSaveManager` (`Save()`/`Load()` via `JsonUtility` to `Application.persistentDataPath/savegame.json`) live in `GameFlow/Persistence/`, added to the `Managers` prefab. `InventoryModel`/`InventoryService`/`InventoryManager` each got a `ReplaceAll` pass-through so loading a save doesn't hit the "reject duplicates" `Collect()` path or fire spurious `ItemCollectedMessage`s.
  - **Found and fixed during verification, not anticipated in the original plan:** `ReplaceAll` silently updating the model doesn't reach `InventoryPresenter` on its own — the presenter only reacts to `ItemCollectedMessage`/`ItemUsedMessage`, neither of which `ReplaceAll` publishes. A `Load()` (or any bulk restore) would leave the panel showing stale contents until the panel was hidden and re-shown. Fixed with a new `InventoryReplacedMessage` (empty notification struct, `Inventory/Messages/`), published by `InventoryService.ReplaceAll`, that `InventoryPresenter` subscribes to and reacts to with a full `Rebuild()`. Caught by testing with active-child counts (`GetChild(i).gameObject.activeSelf`) instead of the pooled panel's raw `Transform.childCount`, which never shrinks — returned slots are deactivated, not removed, so a naive child-count check would have looked correct by coincidence and hidden the bug.
  - **No UI/keybind wiring** — there's no save/load menu yet, so `Save()`/`Load()` are public methods on `GameSaveManager.Instance`, verified by calling them directly through the connected Editor's `eval`. Wire them to a menu when one exists.
  - **Explicitly out of scope, matching this project's "no speculative building" stance:** no flags/variables list in `GameState` (nothing reads flags yet; that's ARCH-17, still deferred); `currentScene` is captured but not acted on (no scene-transition loader exists to consume it yet); no automatic save-on-quit/load-on-boot hook (not requested).
  - Verified end-to-end in Play mode via the connected Editor: collected the key, called `Save()`, read the resulting JSON directly off disk (`{"ownedItemIds":["item-teste-01"],"consumedWorldObjectIds":["<guid>"],"currentScene":"[Teste] CameraPan"}`), wiped the in-memory inventory via `ReplaceAll(empty)` (panel correctly went to 0 active slots), called `Load()`, and confirmed both the model and the panel were restored to 1 item. All 20 EditMode tests pass (18 existing + 2 new `GameStateTests` covering the JSON round-trip with and without content).

---

## Phase 3 — Refactor when touching related systems

### ARCH-10 — EditMode tests: DialogueController & MessageBroker
- **Priority:** Minor (high value per line) · **Trigger:** right after ARCH-01 and ARCH-03; must exist before ARCH-11 · **Complexity:** Low · **Depends on:** ARCH-01, ARCH-03 · **Status:** DONE
- **Current problem:** there are zero tests. `Tests/Tests.asmdef` uses the legacy `optionalUnityReferences` format and references no project assembly. Dialogue flow is the logic most likely to break silently as content grows.
- **Why it matters:** these tests are cheap insurance for the content-sensitive logic and for the broker behaviour that everything now relies on.
- **Recommended solution:**
  - Modernize the asmdef: Editor platform; references to `ProjetoVN.Core`, `ProjetoVN.Dialogue` (later Inventory); `nunit.framework.dll`; `UNITY_INCLUDE_TESTS`.
  - Write EditMode tests using `ScriptableObject.CreateInstance<DialogueData>()`, with `MessageBroker.Clear()` in `SetUp`/`TearDown`.
  - No mocking framework and no PlayMode tests yet.
- **Expected benefit:** safe refactoring of dialogue and messaging.
- **Relevant files:** `Tests/Tests.asmdef`, new `Tests/EditMode/*`.
- **Risks/considerations:** the static broker is shared between tests, so always `Clear()`. Keep the tests few and behaviour-level.
- **Definition of done:** Test Runner passes tests for:
  - **Dialogue:** sequential advance, choice selection, target jump, `NextDialogueData` chaining (Started and Ended once each), empty data rejected.
  - **Broker:** unsubscribe, duplicate subscribe, exception isolation, `Clear`.
- **Notes:**
  - *2026-09-17 (working tree, commit pending):* Done. `Tests/Tests.asmdef` modernized: Editor-only, `UNITY_INCLUDE_TESTS` define constraint, `overrideReferences` with `nunit.framework.dll`, `autoReferenced: false`, and references to `ProjetoVN.Core`, `ProjetoVN.Dialogue` and the two TestRunner assemblies. Inventory was deliberately left out until there is something to test there.
  - Two files in `Tests/EditMode/`. Both `Clear()` the broker in `SetUp` **and** `TearDown`, because the broker is static and shared across tests.
  - **`MessageBrokerTests`** (7 tests): delivery, no-subscriber publish, unsubscribe, duplicate subscribe followed by one unsubscribe, throwing subscriber (asserted with `LogAssert.Expect` and a check that the next subscriber still ran and that nothing escaped to the publisher), `Clear`, and subscribing during a dispatch not affecting the in-flight message.
  - **`DialogueControllerTests`** (11 tests): null and 0-node data rejected with nothing published, Started published before the first line, sequential advance ending exactly once, advancing past the end being a no-op, chaining publishing Started once and Ended once, an invalid chain target ending instead of locking, choice with and without a target dialogue, an out-of-range choice index, and advancing while a choice is pending being ignored.
  - **Addition beyond the DoD:** the "subscribing during dispatch" and "advance while waiting for a choice" tests are not in the list, but they pin the two behaviours most likely to be broken accidentally by a future refactor.
  - Verification: **all 18 tests pass.** They were run for real by the Unity Test Runner (`Unity.exe -batchmode -runTests -testPlatform EditMode`) against an isolated copy of the project with no pre-existing `Library`, so the run also cold-imported every asset: 0 compile errors, no missing scripts, and all seven assemblies built. That incidentally re-verified the scene YAML edits from Phase 1 and Phase 2, which until now had only been checked by reading the files.
  - *2026-09-18:* Committed as `9b3d45b`. Re-ran via `unity cmd run_tests` against the live connected Editor (not just an isolated copy): still **18/18 passed**, confirming the suite survived the subsequent commits, the ARCH-16 namespace rename, and the two small bug fixes made in this session (`DialogueChoiceButton` null-check, `InventoryManager` duplicate-destroy fix).

### ARCH-11 — DialogueController publish order & reentrancy

> **Trigger check, 2026-09-17:** not fired, so ARCH-11 and ARCH-12 were **not** implemented and stay `DEFERRED`. A grep over `Assets/Scripts` finds `DialogueTriggerMessage` only where it is defined, where `DialogueController` publishes it, and in prose in two READMEs — there is no subscriber anywhere, and no handler that calls back into `DialogueManager`. The publish-order bug is therefore still unreachable. Note that ARCH-13 changed `PublishTriggers` (it lost its now-constant `source` log parameter), but the **order** it is called in is untouched, which is what ARCH-11 is about.
> **Trigger check, 2026-09-21: FIRED.** ARCH-20 builds the first consumer of dialogue triggers, so the publish-order bug becomes reachable in the same change. `DEFERRED` → `IN PROGRESS`, **folded into ARCH-20** rather than done as a separate pass: ARCH-20 rewrites `ProcessCurrentNode` and `SelectChoice` anyway, and doing the reorder first would mean writing both methods twice. The ordering requirement and its reentrancy test are carried verbatim into ARCH-20's definition of done.
- **Priority:** Minor · **Trigger:** before the first `DialogueTriggerMessage` consumer, or any handler that calls back into `DialogueManager` — **fired 2026-09-21** · **Complexity:** Low · **Depends on:** ARCH-03, ARCH-10 · **Status:** **DONE** (folded into ARCH-20)
- **Current problem:**
  - `ProcessCurrentNode` publishes triggers before the line and before setting `_isWaitingForChoice`.
  - `SelectChoice` publishes triggers before resolving the target.
  - A handler that starts or advances dialogue mutates `_currentData`/`_currentNodeIndex` mid-method, so the outer call publishes the wrong line or choices.
- **Why it matters:** this is exactly "a system reacting after state has changed". It's harmless today because nothing consumes triggers.
- **Recommended solution:** settle state first, publish the line and choices, then publish the node's collected triggers **after** processing completes. Add a test in which a trigger handler starts another dialogue.
- **Expected benefit:** deterministic dialogue flow when story triggers drive other systems.
- **Relevant files:** `Dialogue/Logic/DialogueController.cs`, `Tests/EditMode/*`.
- **Risks/considerations:** keep it to ordering; no command queue or scheduler.
- **Definition of done:** the reentrancy test passes, and the UI never shows a stale line or choices.
- **Notes:**
  - *2026-09-21:* Implemented inside ARCH-20's rewrite of `DialogueController`. `ProcessCurrentNode` now captures the node, publishes the line, handles the choices and only then runs the node's effects; `SelectChoice` hoists `_isWaitingForChoice = false` out of `ProcessChoiceTarget`, runs the choice's effects, and only then resolves the target.
  - **Addition beyond the original recommendation:** the item asked only for reordering, but reordering alone still left a choice-effect that starts a dialogue silently clobbered by the subsequent `ProcessChoiceTarget`. A `_isExecutingEffects` guard now makes `StartDialogue` refuse and warn in that window, turning a silent content bug into a console warning. This is still "just ordering" — no command queue, no scheduler, as the item's risks section requires.
  - The hoist has a free side effect the original plan didn't note: a reentrant `SelectChoice` from inside an effect now fails `CanSelectChoice()` and is ignored, with no extra guard.
  - Covered by 4 of ARCH-20's new tests: effects after the line, effects after the choices, choice effects before the target's line, and the reentrant-start refusal.

### ARCH-12 — Typed dialogue trigger identifiers
- **Priority:** Minor · **Trigger:** with the first trigger consumer (same session as ARCH-11) · **Complexity:** Low · **Depends on:** ARCH-11 · **Status:** **DROPPED** — superseded by [ARCH-20](#arch-20--dialogue-effects-as-scriptableobject-assets)
- **Current problem:** `DialogueTrigger.TriggerType` is a free-form string, so typos fail silently and the valid types aren't discoverable. No content uses triggers yet.
- **Recommended solution:** a `DialogueTriggerTypes` static class of `const string` values that consumers use; warn about unknown types in `DialogueData.OnValidate` or at runtime. **Don't** build ScriptableObject command/action assets until there are more than ~5 trigger kinds or parameters beyond one string.
- **Expected benefit:** typos caught early with near-zero overhead.
- **Relevant files:** `Dialogue/Data/DialogueTrigger.cs`, `Dialogue/Data/DialogueData.cs`, trigger consumers.
- **Risks/considerations:** keeping strings keeps the existing assets compatible.
- **Definition of done:** all consumers use the constants, and an unknown type produces a warning.
- **Notes:**
  - *2026-09-21 (dropped):* ARCH-20 replaces the whole `TriggerType`/`Parameter` string pair with ScriptableObject effect assets, so there is no string left to constrain and a `DialogueTriggerTypes` constants class would have nothing to name. Both this item's problem (silent typos) and its benefit (typos caught early) are satisfied more strongly by ARCH-20: an effect is a typed asset reference, so an invalid one cannot be expressed at all.
  - **This contradicts the recommendation above, deliberately, and the reason is this item's own second clause.** ARCH-12 says not to build SO action assets "until there are more than ~5 trigger kinds **or parameters beyond one string**". The second condition is already met: `GiveItemEffect`/`RemoveItemEffect` need a typed `ItemDataSO` **asset reference**, not a string. Encoding that as `Parameter: "item-teste-01"` would push resolution to runtime through `ItemRegistry.TryGetById`, turning a broken reference from a compile/Inspector-time error into a silent `LogError` during play — the exact failure mode ARCH-12 exists to prevent. Decided with the project owner, 2026-09-21.
  - **What was given up:** string triggers would have kept the 8 existing dialogue assets byte-compatible. This cost nothing in practice — every `Triggers:` list in all 8 assets was verified `[]` before the change, so there was no authored content to migrate.

### ARCH-13 — Logging cleanup
- **Priority:** Minor · **Trigger:** when touching a file with ad-hoc flow logs · **Complexity:** Low · **Depends on:** ARCH-01 (trace log) · **Status:** DONE
- **Current problem:** interpolated `Debug.Log` calls sit in nearly every handler and transition (`DialogueController`, `GameStateController`, PointNClick, `StateController`), and state changes are logged twice (event plus message). The console noise buries warnings, and the calls allocate in builds.
- **Recommended solution:** remove the flow-tracing logs in favour of the `VN_TRACE_MESSAGES` trace in the broker. Keep warnings and errors, passing a context object (`Debug.LogWarning(msg, this)`).
- **Expected benefit:** a readable console, with message flow still traceable on demand.
- **Relevant files:** the modules listed above.
- **Risks/considerations:** none significant.
- **Definition of done:** the default Play Mode console shows only warnings and errors; defining `VN_TRACE_MESSAGES` shows the message flow.
- **Notes:**
  - *2026-09-17 (working tree, commit pending):* Done. Every unconditional `Debug.Log` is gone: 7 from `DialogueController`, 1 from `InteractableDialogueTrigger`, and the `StateMachine` transition log. A grep for `Debug.Log(` now returns exactly two hits, both inside `[Conditional("VN_TRACE_MESSAGES")]` methods. The double state-change logging the item mentions disappeared earlier, with `StateController` and `StateChangedMessage` in ARCH-06.
  - **Deviation:** the `StateMachine` transition log was **kept**, moved into a `[Conditional("VN_TRACE_MESSAGES")]` helper rather than deleted. State changes are no longer published as messages (ARCH-06 removed `StateChangedMessage`), so the broker trace alone would no longer show them and turning tracing on would give an incomplete picture. One define still controls everything.
  - All surviving warnings and errors now pass a context object where the caller is a `MonoBehaviour` or `ScriptableObject`, so clicking the console message selects the offending object. Their interpolated `gameObject.name` was dropped, since the context supplies it — which also removes the string interpolation from those paths.
  - `Core/Messaging/README.md` documents the define, where to set it (Project Settings → Player → Scripting Define Symbols) and why the console is quiet by default.

### ARCH-14 — UI bug fixes & dead code removal
- **Priority:** Minor · **Trigger:** next UI or menu work · **Complexity:** Low · **Depends on:** — · **Status:** DONE
- **Current problem:**
  - **Bugs:**
    - `UIWindowManager.OpenWindow` throws a NullReferenceException when `currentWindow` is null.
    - The starting window is never `Show()`n.
    - `UIWindow.Hide` uses the `root` field instead of `Root`.
    - `UISelectableBase.OnPointerExit` resets `isSelected` instead of `isPointerOver`, so the hover highlight sticks.
    - `IsInterectable => button == false || …` is misleading.
  - **Dead code:** `UI/Components/OnOffSettingUI.cs` (empty), `UI/Input/UIInputRouter.cs` (empty), `UI/Framework/IUIAdjustable.cs` (unused), and `using System.Reflection.Emit` in `MenuButtonUI.cs`.
- **Recommended solution:** fix the listed bugs and delete the placeholders; recreate them when they are actually needed.
- **Expected benefit:** a menu that works reliably and less misleading code.
- **Relevant files:** `UI/Framework/*`, `UI/Components/*`, `UI/Input/UIInputRouter.cs`; `Assets/Scenes/Menu.unity`.
- **Risks/considerations:** check that no scene references the deleted scripts. At audit time, `Menu.unity` used only `MenuButtonUI`, `UIWindow` and `UIWindowManager`.
- **Definition of done:**
  - Menu hover and keyboard navigation highlight correctly.
  - Closing and then opening windows doesn't throw.
  - The placeholders are gone and no scene has missing scripts.
- **Notes:**
  - *2026-09-17 (working tree, commit pending):* Done. All five bugs fixed. `UIWindowManager.Start` no longer pre-assigns `currentWindow`, which is what made `OpenWindow` take its "already open" branch and never `Show()` the starting window; `OpenWindow` now null-checks before `Hide()`. `UIWindow.Hide` uses `Root` instead of the raw `root` field. `UISelectableBase.OnPointerExit` clears `isPointerOver` instead of `isSelected`, so the highlight stops sticking and the mouse leaving no longer clobbers the keyboard selection.
  - `IsInterectable` was both misleading and misspelled. It is now `IsInteractable`, and `button == false` (which only worked via Unity's Object-to-bool conversion) is an explicit `button == null`. **Renaming the property goes slightly beyond "fix the misleading expression"**, but it is `protected` with exactly one caller, in `MenuButtonUI`, which was updated in the same pass.
  - Deleted: `UI/Components/OnOffSettingUI.cs` (a namespace with nothing in it), `UI/Input/UIInputRouter.cs` (an empty MonoBehaviour, and the now-empty `UI/Input/` folder), `UI/Framework/IUIAdjustable.cs` (no implementors), and the `System.Reflection.Emit` import plus a redundant self-namespace import in `MenuButtonUI.cs`.
  - Each deleted script's GUID was grepped across `Assets/Scenes` and `Assets/Prefabs` first: none was referenced, so no scene can end up with a missing script.
  - The menu behaviour itself (hover, keyboard navigation, opening and closing windows) has **not** been checked in Play Mode.
  - *2026-09-18:* Committed as `6cec367`. A follow-up read-only scan (before committing) confirmed the deletions left no dangling `m_Script` references anywhere under `Assets/Scenes` or `Assets/Prefabs`, and additionally found one leftover stale `m_EditorClassIdentifier` string (an editor-only hint, not a real reference) outside that scan's original scope, in `Assets/_Project/UIFramework/Prefabs/ButtonSemBorda.prefab` — fixed the same day. **Still not checked in Play Mode:** `Menu.unity` was not the loaded scene during this session's Play Mode pass (only `[Teste] CameraPan.unity` was exercised), so the menu hover/keyboard-navigation/window bug fixes remain unverified at runtime.

### ARCH-15 — UI assembly definition
- **Priority:** Minor · **Trigger:** when UI grows beyond the inventory panel and menu, or when restructuring UI · **Complexity:** Low · **Depends on:** ARCH-07 · **Status:** DONE
- **Current problem:** UI scripts compile into Assembly-CSharp and can silently reference any module. The README claim "delete UI and nothing breaks" isn't enforced.
- **Recommended solution:** `UI/ProjetoVN.UI.asmdef` with explicit references (Core, Inventory, TMP, uGUI, InputSystem as needed). Dialogue UI stays in the Dialogue module.
- **Expected benefit:** an honest, visible dependency graph.
- **Relevant files:** new `UI/ProjetoVN.UI.asmdef`.
- **Risks/considerations:** scripts left in Assembly-CSharp can no longer see UI types unless they reference the asmdef (none do at audit time). Verify the UnityEvent wiring after the move.
- **Definition of done:** UI compiles in its own assembly and all UI in scenes still works.
- **Notes:**
  - *2026-09-17 (working tree, commit pending):* Done. New `UI/ProjetoVN.UI.asmdef` referencing exactly `ProjetoVN.Core`, `ProjetoVN.Inventory`, `Unity.TextMeshPro` and `UnityEngine.UI`. It deliberately does **not** reference Dialogue, PointNClick or GameFlow, so the README's "delete UI and nothing breaks" claim is now enforced by the compiler instead of being a convention. Dialogue UI stays in the Dialogue module. InputSystem was not needed.
  - **Trigger note:** the item's trigger is "when UI grows beyond the inventory panel and menu, **or when restructuring UI**". The UI has not grown, but ARCH-14 restructured it in this same session, and the dependency on ARCH-07 is `DONE`, so the second half of the trigger applies. The item's status was `TODO`, not `DEFERRED`.
  - This empties `Assembly-CSharp` of project scripts: every `.cs` under `Assets` now belongs to a module asmdef.
  - **Moving UI out of `Assembly-CSharp` invalidates UnityEvent bindings**, because a persistent call stores its target as `"Namespace.Type, Assembly"`. The four affected strings in `Menu.unity` (two `m_TargetAssemblyTypeName` for `UIWindowManager`, two `m_ObjectArgumentAssemblyTypeName` for `UIWindow`) were rewritten to `ProjetoVN.UI` in the YAML. Doing it in the file is more reliable than checking them by eye in the Inspector, which is what the item suggests.
  - Verified by compiling `ProjetoVN.UI` with **only** its declared references; it builds clean, which proves it is not secretly reaching into another module.

### ARCH-16 — Namespace & asmdef hygiene
- **Priority:** Minor · **Trigger:** per file when touched; no big-bang rename · **Complexity:** Low per file · **Depends on:** — · **Status:** DONE
- **Current problem:** two conventions coexist: `Assets.Scripts.*` in Core and Dialogue, `ProjetoVN.*` elsewhere. asmdef `rootNamespace` is empty, and `ProjetoVN.Core.asmdef` has a meaningless `versionDefines` entry (`"1.18"`).
- **Recommended solution:** adopt `ProjetoVN.<Module>[.<Sub>]`, set `rootNamespace` per asmdef, and remove the bogus version define.
- **Expected benefit:** consistency and less confusion for future sessions.
- **Relevant files:** all `Assets.Scripts.*` files and every `*.asmdef`.
- **Risks/considerations:**
  - MonoBehaviour and ScriptableObject serialization uses the script GUID, so renaming namespaces is safe for them.
  - **UnityEvent persistent calls store the target type name**, so verify `Interactable.prefab`, the choice buttons and the menu buttons in the Inspector after renaming.
  - `[SerializeReference]` would break (not used at audit time; re-check).
- **Definition of done:** touched files use `ProjetoVN.*` and UnityEvent wiring is verified.
- **Notes:**
  - *2026-09-17 (working tree, commit pending):* Done, at the **full** scope rather than per-file, decided with the project owner. A namespace cannot be renamed halfway — every `using` that points at it has to move in the same change — and ARCH-13 had this session touching most of Core and Dialogue anyway. 28 `.cs` files moved from `Assets.Scripts.Core.*` / `Assets.Scripts.Dialogue.*` to `ProjetoVN.Core.*` / `ProjetoVN.Dialogue.*`. No `Assets.Scripts.` string remains anywhere under `Assets`.
  - `rootNamespace` is now set on all six module asmdefs, so new scripts created from Unity's templates start in the right namespace. The bogus `versionDefines` entry in `ProjetoVN.Core.asmdef` (which defined a symbol literally named `1.18`, and which Roslyn was already rejecting with MSB3052) was removed.
  - **UnityEvent wiring:** the four `DialogueChoiceButton` persistent calls in `[Teste] CameraPan.unity` had their `m_TargetAssemblyTypeName` rewritten to the new namespace. While doing that, **a pre-existing broken binding was found and fixed**: a prefab-instance override still carried `Assets.Scripts.GameStates.InteractableDialogueTrigger, Assembly-CSharp`, a namespace and assembly that had not existed for some time. It now reads `ProjetoVN.GameFlow.InteractableDialogueTrigger, ProjetoVN.GameFlow`. Every `m_TargetAssemblyTypeName` in the project was then listed and checked to resolve to a real type in a real assembly.
  - Stale `m_EditorClassIdentifier` hints were corrected in the same pass across scenes, both prefabs and the eight `DialogueData` assets. These are editor hints that Unity rewrites on save and do not affect loading, but several were wrong from older refactors and made the YAML misleading to read.
  - *2026-09-21 (correction — the sweep was incomplete):* three stale hints survive in `Menu.unity` (lines 784, 2895, 6296), all naming `ProjetoVN.Core::ProjetoVN.UIFramework.UIWindowManager` / `.UIWindow` — both a namespace and an assembly that no longer exist. `Menu.unity` was almost certainly skipped because it is not in Build Settings and was not the scene exercised in that session's Play Mode pass. The `m_Script` GUIDs are correct, so the components still bind; this is cosmetic. Fixed under [ARCH-21](#arch-21--authoring-time-id--null-safety-gaps).
  - `[SerializeReference]` was re-checked: still unused, so nothing depends on stored type names beyond the UnityEvent strings above.
  - The wiring was verified by reading the serialized files, **not** in the Inspector — this session has no interactive Editor. Opening `[Teste] CameraPan.unity` and `Menu.unity` and confirming the buttons still list their methods is a worthwhile five-minute check.
  - *2026-09-18:* Committed as `942df8f`. Play Mode confirmed the presenter's incremental path: after collecting the key, `InventoryPanel`'s child count went from 0 to 1 without needing to hide/show the panel. **New finding, not part of this item's original scope:** the presenter's *initial* rebuild (`OnEnable`) can still run before `InventoryManager.Instance` exists, because Canvas is an earlier scene root than GameController — see the new bullet added under ARCH-08's "Current problem" and the 2026-09-18 note under [2B](#2b--important-milestone-triggers-have-now-fired). This is a manager-lifecycle/bootstrap-ordering issue, not a data-ownership issue, so it belongs to ARCH-08 rather than being reopened here.

### ARCH-20 — Dialogue effects as ScriptableObject assets
- **Priority:** Important · **Trigger:** the first time a dialogue must change game state — **fired 2026-09-21** · **Complexity:** Medium · **Depends on:** ARCH-10, ARCH-11 (folded in) · **Status:** DONE
- **Current problem:**
  - `DialogueTrigger` (`TriggerType` + `Parameter`, both free-form strings) is published as `DialogueTriggerMessage` by `DialogueController.PublishTriggers`, and **nothing anywhere subscribes to it**. The authoring type, the serialized `Triggers` lists on nodes and choices, and the message all terminate in a `Publish` with a zero-length handler array.
  - Verified 2026-09-21: every `Triggers:` list in all 8 dialogue assets is `[]`. The feature has never been used.
  - There are no story flags, so a dialogue cannot record that anything happened. ARCH-09 explicitly left flags out of `GameState` because nothing read them.
- **Why it matters:** this is the seam every branching-story feature needs. Until a dialogue can give an item or set a flag, content is limited to text that changes nothing, and conditional choices (ARCH-17) have nothing to read.
- **Recommended solution:**
  - `StoryFlags` (static, `Core/State/`) with a `SubsystemRegistration` reset, mirroring `MessageBroker`/`PlayerInputGate`. **Core, not GameFlow**, per ARCH-09's own instruction: *"Place `GameState` in Core if Dialogue must read flags (ARCH-17); otherwise in GameFlow."* `PointNClick` and `UI` will want flags too, and neither references Dialogue.
  - `GameState.storyFlagIds` (`List<string>`, since `JsonUtility` cannot serialize a `HashSet`), saved and restored by `GameSaveManager`, wiped by `SaveLoadDebugPanel.ResetSession`.
  - Abstract `DialogueEffectSO : ScriptableObject { void Execute(); }` in **Dialogue** (the field's declaring module); concrete effects in **GameFlow** (which already references Dialogue + Inventory + Core). `GiveItemEffect`, `RemoveItemEffect`, `SetFlagEffect`, `ClearFlagEffect`.
  - `DialogueNode.Triggers` / `DialogueChoice.Triggers` → `List<DialogueEffectSO> Effects`. Delete `DialogueTrigger` and `DialogueTriggerMessage`.
  - **Fold in ARCH-11:** effects execute *after* the line and choices are published and state has settled; choice effects execute *before* the target dialogue resolves.
- **Expected benefit:** dialogues can change game state; an invalid effect is unrepresentable rather than a silent runtime typo; flags are ready for ARCH-17.
- **Relevant files/boundaries:** new `Core/State/StoryFlags.cs`, new `Dialogue/Data/DialogueEffectSO.cs`, new `GameFlow/DialogueEffects/*`, `Dialogue/Logic/DialogueController.cs`, `Dialogue/Data/DialogueNode.cs`, `DialogueChoice.cs`, `GameFlow/Persistence/GameState.cs`, `GameSaveManager.cs`, `GameFlow/DevTools/SaveLoadDebugPanel.cs`, `Tests/EditMode/*`.
- **Risks/considerations:**
  - **Never `[SerializeReference]`.** SO *asset* references store `{fileID, guid, type}` and resolve the concrete type from the target asset, so cross-assembly polymorphism works unconditionally. `[SerializeReference]` stores `"Type, Assembly"` inline and breaks on rename or move. ARCH-16 verified it is unused project-wide; keep it that way.
  - Effect assets are **shared** between nodes, so per [D-02](#decisions-to-keep) they must hold no runtime state — a `[SerializeField]` written during play dirties the `.asset` and persists across sessions.
  - `Execute()` takes **no context object**. The dependency direction is protected by which assembly a subclass lives in; a context exposing `.Inventory`/`.Flags` is a service locator, which [D-04](#decisions-to-keep) forbids by name.
  - Wrap each `Execute()` in try/catch like `MessageBroker.Publish` does — one bad content asset must not break dialogue flow.
  - Old saves have no `storyFlagIds`; `ReplaceAll`'s null guard covers it, so no `version` field is needed. Pin it with a test rather than assuming `JsonUtility`'s behaviour.
  - Do **not** add a `StoryFlagChangedMessage` — nothing would consume it, which is exactly the mistake this item undoes.
- **Definition of done:**
  - No reference to `DialogueTrigger` or `DialogueTriggerMessage` remains, and no `Triggers:` key survives in any dialogue asset after reimport.
  - A dialogue choice gives an item and the inventory panel updates; a node sets a flag that survives Save → Load and is cleared by Reset Session.
  - **(from ARCH-11)** Effects run after the line and choices are published; a choice's effects run before its target dialogue's first line; an effect that starts a dialogue warns instead of corrupting state; the UI never shows a stale line or choices.
  - A null entry in an `Effects` list, and a throwing effect, each leave the remaining effects running.
  - `Dialogue/README.md`, `Core/Messaging/README.md`, `GameFlow/README.md`, `ScriptableObjects/README.md` and `Tests/README.md` are updated.
- **Notes:**
  - *2026-09-21 (implemented):* Built as specified. `Core/State/StoryFlags.cs` (static, `SubsystemRegistration` reset), `GameState.storyFlagIds`, save/restore in `GameSaveManager`, `StoryFlags.ClearAll()` in `SaveLoadDebugPanel.ResetSession`, abstract `Dialogue/Data/DialogueEffectSO.cs`, and four concrete effects in `GameFlow/DialogueEffects/`. `DialogueTrigger.cs` and `DialogueTriggerMessage.cs` deleted.
  - **The asset migration needed a forced reserialize, which the plan did not anticipate.** Swapping the field type left the 8 dialogue assets still carrying the dead `Triggers:` key — Unity drops an unknown key on *load* but does not rewrite the file until something dirties the asset, and merely recompiling does not. `AssetDatabase.ForceReserializeAssets` over the 8 `t:DialogueData` assets rewrote all 12 `Triggers: []` into `Effects: []` in place, with the dialogue content (speakers, text, choice targets, `NextDialogueData`) untouched. Worth remembering for any future field rename on an authored type.
  - **`ResetSession` clears flags *before* the `InventoryManager` null-check**, not after. Flags are static and do not depend on the manager, so an early return on a missing manager would otherwise leave a "reset" session with its flags still set — the hardest possible state to debug.
  - **Effects execute through a direct `Execute()` call, not a message.** This is the whole point of the item: a dialogue effect is a *command with one owner*, and the deleted `DialogueTriggerMessage` proved what happens when a command is modelled as a notification — it published for months to zero subscribers with no error. Each call is still wrapped in try/catch like `MessageBroker.Publish`, so a broken content asset cannot break dialogue flow.
  - **Cross-assembly polymorphism confirmed in the authored data**, not just in theory: `Cap1DD1.asset`'s "Sim" choice now holds `{fileID: 11400000, guid: …, type: 2}` references to a `GiveItemEffect` and a `SetFlagEffect`, both defined in `ProjetoVN.GameFlow`, on a field declared in `ProjetoVN.Dialogue`. `ProjetoVN.Dialogue.asmdef` was not touched.
  - **Verified in Play Mode** via the connected Editor, in `[Teste] Mecanicas`: triggering the Gótica dialogue flipped `PlayerInputGate` to `false`; choosing "Sim" ran both effects (inventory `item-teste-01`, flag `falou-com-gotica`); `Save()` wrote `{"ownedItemIds":["item-teste-01"],"consumedWorldObjectIds":[],"storyFlagIds":["falou-com-gotica"],"currentScene":"[Teste] Mecanicas"}`; Reset Session cleared inventory, flags and the 1 active UI slot; `Load()` restored all three; and the locked door still consumed the key and played the unlocked branch, confirming the `OnLocked`/`OnUnlocked` wiring survived. **0 console errors.**
  - **48/48 EditMode tests pass** (20 before this work → 32 after StoryFlags → 43 after the effect tests → 48 with the flag-effect tests). New: `StoryFlagsTests` (11), `StoryFlagEffectsTests` (5), a `GameStateTests` case feeding literal pre-change JSON, and 11 `#region Efeitos` cases in `DialogueControllerTests`.
  - **Not built (deliberately):** no `StoryFlagChangedMessage` (nothing would consume it — the exact mistake this item undoes); no `version` field on `GameState` (the change is purely additive and `StoryFlags.ReplaceAll` null-guards, pinned by a test rather than assumed); no editor tooling to list existing flag ids, which is the obvious next papercut once more than a handful exist.
  - *2026-09-21:* Committed as `f25ddc2`.

### ARCH-21 — Authoring-time id & null-safety gaps
- **Priority:** Important · **Trigger:** found while planning ARCH-20 — **fired 2026-09-21** · **Complexity:** Trivial–Low · **Depends on:** — · **Status:** DONE
- **Current problem:** seven mutually independent defects, none of which blocks the others:
  - **`ItemDataSO.id` defaults to `"default-id"`** — a *non-empty* default, so the `OnValidate` emptiness check can never fire. Two item assets created and never edited silently share an id, and `ItemRegistry`'s duplicate check only sees items already added to the registry. Ids are the save format's primary key (ARCH-09), so this corrupts saves.
  - **`CollectableItemBehaviour.Collect()` ignores the `bool` from `InventoryManager.Collect`.** ARCH-07 decided duplicates are rejected, so on `false` the world object is still marked consumed and deactivated — the item is destroyed rather than collected.
  - **`GameSaveManager.Load()` does not null-guard `state.ownedItemIds`** before iterating; a truncated or hand-edited save throws.
  - **`DialogueInputHandler.advanceDialogueAction` is dereferenced unguarded** in `OnEnable` → `NullReferenceException` if the reference is ever unassigned.
  - **`ScreenPanController.environmentContainer` is dereferenced unguarded** in `ApplyPan`.
  - **`SampleScene.unity` is dead** — not in Build Settings, referenced by nothing, and carrying a stray `DialogueManager` that would stomp `Instance` if the scene were ever loaded.
  - **Three stale `m_EditorClassIdentifier` strings in `Menu.unity`** naming `ProjetoVN.Core::ProjetoVN.UIFramework.*` — a namespace *and* an assembly that no longer exist. ARCH-16 claimed to have swept these "across scenes" and missed this file.
- **Why it matters:** the first three are silent data loss on paths that already exist, and they are each a few lines. The rest is noise that makes the project harder to read and misleads future sessions.
- **Recommended solution:** fix each independently; no shared design. `id = ""` so the existing check works; only mark consumed when `Collect` returns `true`; guard the two lists and the two references; delete `SampleScene.unity`; correct the three editor-hint strings.
- **Relevant files:** `Inventory/ItemDataSO.cs`, `Inventory/CollectableItemBehaviour.cs`, `GameFlow/Persistence/GameSaveManager.cs`, `Dialogue/Input/DialogueInputHandler.cs`, `PointNClick/ScreenPanController.cs`, `Assets/Scenes/SampleScene.unity`, `Assets/Scenes/Menu.unity`.
- **Risks/considerations:**
  - **Do not** make `DialogueInputHandler` consult `PlayerInputGate`. Dialogue runs with the gate **disabled** (`DialogueState.Enter`), so gating the advance action would make dialogue unadvanceable. The gate covers world clicks only; the name is broader than the responsibility.
  - Deleting `SampleScene.unity` **reverses a recorded decision** — ARCH-06's note says it "was kept (decided with the project owner)". Re-decided 2026-09-21; recorded in ARCH-06's notes.
  - Changing `ItemDataSO`'s default does not touch existing assets — `ItemTeste.asset` already carries a real id. It only affects assets created from now on.
- **Definition of done:** a new `ItemDataSO` warns until given an id; collecting an already-held item leaves the world object alone; a truncated save logs instead of throwing; `SampleScene.unity` is gone with no dangling references; no `UIFramework` string remains under `Assets/Scenes`.
- **Notes:**
  - *2026-09-21 (implemented):* All seven fixed. `ItemDataSO.id` now defaults to `""`; `CollectableItemBehaviour.Collect()` returns early with a warning when `Collect` reports `false`; `GameSaveManager.Load()` guards a null `state` and both lists; `DialogueInputHandler` guards `advanceDialogueAction` in `OnEnable`/`OnDisable`; `ScreenPanController` logs once in `Awake` and returns silently in `Update` (logging in `Update` would flood the console); `SampleScene.unity` deleted; the three `Menu.unity` hints corrected.
  - **`ItemDataSO` and `CollectableItemBehaviour` touch the same bug from two sides.** The id default made two un-edited assets collide on the save format's primary key; the ignored `bool` meant the collision's consequence — a duplicate `Collect` — silently destroyed the world object instead of leaving it. Verified live: with the item already held, `Collect()` left the object active and unconsumed and logged the warning; with an empty inventory the same call deactivated it and added 1 item.
  - **`m_EditorClassIdentifier` is not refreshed by saving, and ARCH-16's note claiming otherwise is wrong.** Three approaches were tried against the live Editor and all three left the stale string: opening and re-saving the scene; re-serializing each component via `SerializedObject` + `SetDirty` + `SaveScene`; and `AssetDatabase.ForceReserializeAssets` on the scene. Unity preserves the stored value rather than recomputing it from the resolved script. The three strings were ultimately corrected in the YAML — with the scene closed first, to avoid the Editor overwriting the file — and the scene reimported. The correct shape is `<Assembly>::<Namespace>.<Type>`, visible on the same file's already-correct `MenuButtonUI` entries. **This only matters as a reading hazard:** `m_Script` is the real binding, so the components were never broken. Note this before trusting a future "the Editor will rewrite it" assumption.
  - **`DialogueInputHandler` was deliberately *not* made to consult `PlayerInputGate`**, which looks like the obvious fix for a second, ungated input path. It would break dialogue outright: `DialogueState.Enter()` disables the gate for the whole duration of a dialogue, so a gated advance action could never fire. The gate covers world clicks only; its name is broader than its responsibility. Recorded here because it is an easy "improvement" to reach for twice.
  - `SampleScene.unity` was confirmed unreferenced before deletion: its GUID appeared nowhere under `Assets`/`ProjectSettings`, no C# named it, and Build Settings held only the two `[Teste]` scenes.
  - Verified: 20/20 EditMode tests still passed immediately after this batch, before any ARCH-20 work began.
  - *2026-09-21:* Committed as `dfd9506`.

### ARCH-22 — Gated interactions remember their state
- **Priority:** Critical (live gameplay bug) · **Trigger:** found in play, 2026-09-21 · **Complexity:** Low–Medium · **Depends on:** ARCH-20 (`StoryFlags`) · **Status:** DONE
- **Current problem:**
  - `LockedActionBehaviour.Interact()` was stateless: it called `InventoryManager.TryUse(requiredItem)` on **every** interaction, and `TryUse` *consumes* the item. After the door was opened once the key was gone, so `TryUse` returned `false` forever and the door reported itself locked again. Observed in play: open the door with the key, click it again, get "A porta está trancada."
  - Nothing in the project could express "this interaction already happened" except `CollectableItemBehaviour`'s `persistentId` + consumed-ids set, which is shaped for objects that disappear, not for gates that stay.
- **Why it matters:** it makes any consumable-key puzzle unfinishable-looking, and it is the shape of every future gate (a door, a passage, a guard who lets you past once).
- **Recommended solution:**
  - Record the unlock as a **story flag** under a designer-typed readable id (`unlockedFlagId`), checked first in `Interact()`.
  - Add an optional `requiredFlagId` so a gate can be flag-locked as well as item-locked.
  - Add `OnAlreadyUnlocked` as a pure hook for what happens when the player goes *through* an open gate.
  - Move the component from `ProjetoVN.Inventory` to `ProjetoVN.GameFlow`.
- **Expected benefit:** doors stay open, across scene reloads and saves; the first gameplay-side reader of `StoryFlags`; flag-gated content becomes authorable with no new system.
- **Relevant files/boundaries:** `GameFlow/LockedActionBehaviour.cs` (moved from `Inventory/`), both `[Teste]` scenes, `Inventory/README.md`, `GameFlow/README.md`, `Tests/README.md`.
- **Risks/considerations:**
  - The move must use `AssetDatabase.MoveAsset` so the `.meta` travels and the `m_Script` GUID survives; a lost GUID means two Missing Script components with all UnityEvent wiring destroyed.
  - Never blanket-replace `ProjetoVN.Inventory` in the scene YAML — `CollectableItemBehaviour` shares that string.
  - Distinct `unlockedFlagId` per door: `StoryFlags` is one global set, so a shared id would open both doors at once.
- **Definition of done:**
  - Opening the door, then interacting again with no key, does **not** report it locked.
  - The unlock survives a scene reload and a Save → Reset → Load round-trip.
  - A gate requiring both a flag and an item does not consume the item when the flag gate fails.
  - An empty `unlockedFlagId` degrades to the old stateless behaviour and warns at authoring time.
- **Notes:**
  - *2026-09-21 (implemented):* Built as specified. `LockedActionBehaviour` moved to `GameFlow` (GUID `21fec084…` confirmed unchanged before and after), gained `requiredFlagId`, `unlockedFlagId` and `OnAlreadyUnlocked`, and `Interact()` became four flat guards: no-requirement → already-unlocked → flag gate → item gate. The two existing doors were authored with `porta-mecanicas-destrancada` and `porta-camerapan-destrancada`.
  - **Zero save-system changes were needed**, which was the deciding factor for choosing `StoryFlags` over the consumed-world-objects set: both stores are already fields of `GameState`, so the new flag serialized, restored and got wiped by Reset Session for free. Verified by reading `savegame.json` off disk mid-session.
  - **The flag is checked before the item, and that ordering is load-bearing.** `StoryFlags.IsSet` consumes nothing; `TryUse` removes the item. Reversed, a gate requiring both would eat the player's key just to discover the flag was not set — the same class of bug this item fixes. There is a comment in the source saying so, and a Play Mode assertion that catches a future reorder.
  - **Graceful degradation cost zero code** because `StoryFlags` already validates: `IsSet("")` is always false (so an empty `unlockedFlagId` behaves exactly like the old stateless component) and `Set("")` stores nothing (so no empty string can reach `GameState.storyFlagIds`).
  - **`OnAlreadyUnlocked` started with a fallback to `OnUnlocked` and that was reverted the same day.** The fallback (via `GetPersistentEventCount()`) meant both existing scenes kept working with no rewiring, at the cost of **replaying** "A chave serviu! Destrancou a porta." on every later click. Once the project owner saw it, the intent became explicit: the already-unlocked path must be a **pure hook** for what happens when you go *through* the door (a scene transition, say), not a dialogue. The fallback was removed — `OnAlreadyUnlocked` now fires alone, and does nothing when nothing is wired. **Lesson worth keeping:** a convenience fallback that silently re-purposes another event hides the fact that a hook is unwired, which is the opposite of what a hook is for.
  - **Separating state from action is the core of the final design.** A first pass put the green placeholder on `OnAlreadyUnlocked`, which made green mean "you clicked an open door". The project owner corrected the intent: the gate should enter its open state **the moment it unlocks**, and *stay* open. That split the events in two kinds, and the distinction is now load-bearing:
    - **`OnOpened` — state.** Fires in `Unlock()` and again in `Start()` on every scene where the flag is already set. This is what makes a gate *stay* open across reloads and loads. Presentation and passage only.
    - **`OnAlreadyUnlocked` — action.** Fires only when the player interacts with an already-open gate. This is where a scene transition belongs.
    - **Wiring a player-initiated action to `OnOpened` would fire it automatically at `Start()`** — a scene transition there would teleport the player on every load. The tooltips and README say so explicitly, because the two events otherwise look interchangeable.
  - **A placeholder makes the state visible:** `GameFlow/DevTools/PlaceholderTint` tints a `SpriteRenderer` green and is wired to `OnOpened` on both doors, so green reads as "this gate is open". It exists as a component because a UnityEvent persistent call cannot take a `Color` argument, so `SpriteRenderer.color` can't be wired from the Inspector. Replace it with open-door art and delete the component.
  - **This reverses the plan's "no `Start()` restore hook" decision**, which assumed an opened gate needs no visual state. Once the gate had one, a gate that forgets it is open on every scene load is not "open" in any useful sense.
  - **Known limitation, inherited from `CollectableItemBehaviour`:** a mid-scene `Load()` restores the flag but `Start()` has already run, so the gate's *appearance* only catches up on the next scene load. The debug panel's Reload Scene button next to Load is the workaround.
  - *2026-09-21:* Committed as `49c1971`. Docs (this roadmap and all touched module READMEs) committed separately as `01e1654`.
  - **Third confirmation that Unity does not refresh `m_EditorClassIdentifier`.** A full `SaveScene` after re-authoring both doors wrote the two new fields but left both stale `ProjetoVN.Inventory::…` strings intact. Together with ARCH-21's finding (re-save, `SerializedObject` re-serialize and `ForceReserializeAssets` all fail), treat this as settled: assembly/namespace strings in scene YAML must be hand-edited, with the Editor parked off the scene first.
  - **Harness gotcha that produced three false failures — worth knowing before trusting any Play Mode check.** The `Start()` restore appeared broken: the scene reloaded, the flag was set, the wiring was correct, and the gate still came back closed. The cause was the harness, not the code. Player Settings has `runInBackground: 0`, and an Editor driven over the CLI is **unfocused**, so Play Mode **freezes**: `Time.frameCount` sat at 4764 across repeated calls seconds apart. `SceneManager.LoadScene` still completed (it is pumped by the load machinery), so objects were genuinely rebuilt — but no frame ever ticked, so Unity never called `Start()`. Setting `UnityEngine.Application.runInBackground = true` at the top of the test made frames advance (4764 → 6765) and the restore worked immediately. That assignment is runtime-only and does not dirty `ProjectSettings.asset` (re-checked: still `0`). **Any Play Mode verification that depends on `Start()`, `Update()`, coroutines or elapsed frames must set it first**; assertions driven purely by direct method calls, like every earlier check in ARCH-20/22, are unaffected and remain valid.
  - **Verified in Play Mode**, 0 console errors: locked without the key → `PortaTrancada`; first unlock → key consumed, flag set, `PortaDestrancada`; **second and third interaction with no key → `PortaDestrancada`** (the bug, fixed); a flag-gated test object kept its key when the flag was unset and consumed it once set; Save wrote `storyFlagIds: ["porta-mecanicas-destrancada"]`; Reset Session re-locked the door; Load re-opened it; and the unlock survived a `SceneManager.LoadScene`. 48/48 EditMode tests still pass.
  - **No EditMode tests added** — see `Tests/README.md`; this follows the `GiveItemEffect`/`RemoveItemEffect` precedent and is recorded there with the reason the guard ordering is the part that would most merit one.

---

## Phase 4 — Optional, only if the project grows into it

### ARCH-17 — Dialogue conditions via query interface
- **Priority:** Optional · **Trigger:** choices or nodes must appear or disappear based on items or flags · **Complexity:** Low–Medium · **Depends on:** ARCH-07, ARCH-09 · **Status:** DEFERRED
- **Current problem (future):** Dialogue cannot reference Inventory or GameFlow (asmdef direction). A bus query would bring back the ARCH-04 failure modes.
- **Recommended solution:** a small interface owned by Dialogue, e.g. `IDialogueConditionEvaluator { bool Evaluate(string condition, string parameter); }`. GameFlow implements it (adding a reference to Inventory, or reading `GameState`) and assigns it to `DialogueManager` at startup. This is one of the few interfaces that is justified, because it protects the dependency direction.
- **Expected benefit:** conditional story content without coupling Dialogue to gameplay modules.
- **Relevant files/boundaries:** `Dialogue/Logic/*`, `Dialogue/Data/DialogueChoice.cs`, `DialogueNode.cs`, `GameFlow/*`.
- **Risks/considerations:** keep conditions data-driven and simple; no expression language.
- **Definition of done:** conditional choices show and hide correctly, and the Dialogue asmdef still doesn't reference Inventory.
- **Notes:**
  - *2026-09-21 (precedent set by ARCH-22):* `LockedActionBehaviour.requiredFlagId` is now the first **flag-gated content** in the project — a scene interaction that branches on `StoryFlags.IsSet`. It lives in GameFlow, so it reads flags directly with no interface. When this item is picked up, that is the shape to match for a *flag* condition on a choice; the interface below is only needed for an *item* condition, which Dialogue still cannot query.
  - *2026-09-21 (scope reduced by ARCH-20):* ARCH-20 puts `StoryFlags` in **Core**, which every module can see, so **Dialogue can read flags directly** — no interface needed for the flag half of this item. `IDialogueConditionEvaluator` is now only justified for conditions that query **Inventory** ("show this choice if the player holds the key"), which Dialogue still cannot reference. Consider splitting: flag conditions as a plain `StoryFlags.IsSet` call on `DialogueChoice`, item conditions through the interface — or skip the interface entirely if item conditions never turn out to be needed. Re-read this before designing; the original recommendation above assumes flags are unreachable from Dialogue, which is no longer true.

### ARCH-18 — Async/animated presentation lifecycle
- **Priority:** Optional · **Trigger:** adding a typewriter effect, fades, timed sequences, voice lines or async scene loads — **fired 2026-09-18** · **Complexity:** Low · **Depends on:** ARCH-03 · **Status:** TODO
- **Current problem (future):** coroutine and `Awaitable` continuations resume after the object was destroyed or the dialogue advanced or ended. This is a lifecycle issue (still main thread), not a concurrency issue.
- **Recommended solution:**
  - Coroutines, owned by the view, for simple UI effects.
  - `Awaitable` with `destroyCancellationToken`, re-checking state after each `await`.
  - Skip/fast-forward input goes through `DialogueManager` (single owner).
  - No `Task.Run` or threads for game logic, and no async message bus.
- **Expected benefit:** effects that can't show stale text or throw on scene change.
- **Relevant files:** `Dialogue/UI/*`, any future effect scripts.
- **Risks/considerations:** keep effect state in the view; don't let the view drive dialogue logic.
- **Definition of done:** rapid clicking during effects never shows stale text, and a scene change mid-effect doesn't throw.
- **Notes:**
  - *2026-09-18:* Trigger fired (project owner confirmed typewriter/fades/voice lines are coming soon); status moved `DEFERRED` → `TODO`. Not yet designed — pick this up once dialogue UI work resumes, after ARCH-08/09 (see [Execution order](#execution-order)).
  - *2026-09-18 (context for later):* the dialogue UI was rebuilt from the SVG art and now has a `continueIndicator` (the `>>` graphic) that `DialogueUIController` shows with each line and hides when choices open. It is deliberately **not** clickable: `AdvanceDialogue` is bound to left-click, so a clickable `>>` would advance twice per click (press via the input action, release via the button). When a typewriter effect lands, the natural extension is "hide the indicator until the line finishes revealing", and the first click during reveal should complete the line rather than advance — which is exactly the skip/fast-forward-through-`DialogueManager` rule above.

### ARCH-19 — Asset loading / Addressables strategy
- **Priority:** Optional · **Trigger:** **measured** memory or load-time problems (large backgrounds, CGs, voice), or content-update needs · **Complexity:** Medium–High · **Depends on:** — · **Status:** DEFERRED
- **Current problem (future):** direct references load everything they point to. `DialogueData` chains (`NextDialogueData`, `TargetDialogue`) pull in the whole connected graph and every asset it references, once portraits, CGs or audio are added.
- **Recommended solution:** profile first (Memory Profiler). Try cheap mitigations first, such as keeping heavy assets referenced by scenes rather than by dialogue SOs. Adopt Addressables only for heavy assets (backgrounds, CGs, audio).
- **Expected benefit:** memory under control without paying Addressables' complexity early.
- **Relevant files/boundaries:** `Dialogue/Data/*`, art and audio references.
- **Risks/considerations:** Addressables adds build steps and async loading (see ARCH-18).
- **Definition of done:** the decision is backed by profiler data, and heavy assets load on demand.

---

## Decisions to keep

These were evaluated during the audit and **should not be refactored**. Change one only with a concrete problem that its "Revisit only if" condition covers, and record the change here.

| ID | Decision | Why it is right for this project | Revisit only if |
|---|---|---|---|
| D-01 | Feature-module asmdefs (Core ← Dialogue/Inventory/PointNClick ← GameFlow), acyclic | Enforced boundaries at low cost; right granularity | A module becomes a dumping ground. Never split into Data/Logic/UI sub-asmdefs "for purity" |
| D-02 | ScriptableObjects as **read-only** authored content; runtime state copied into models | Designer-friendly; no Play Mode data corruption | Never mutate SOs at runtime |
| D-03 | Plain C# logic behind thin MonoBehaviours (`DialogueController`/`DialogueManager`, `InventoryService`/`InventoryManager`) | Testable without DI or interfaces | — Don't add interfaces "for testability" |
| D-04 | A few global singletons for true game-wide managers; direct calls into them from input and UI (`DialogueChoiceButton`, `DialogueInputHandler`) | Explicit, debuggable, minimal boilerplate | Many managers must be swapped at runtime. **No DI container (VContainer/Zenject) or service locator** |
| D-05 | Keep `MessageBroker` (after ARCH-01) for cross-module **notifications** | Typed struct messages keep Find References usable; synchronous dispatch gives deterministic stack traces; avoids wiring references between DDOL managers and scene objects | Don't replace with MessagePipe/UniRx, don't make it async, queued or prioritized, don't remove it, and don't route commands, queries or state through it |
| D-06 | Synchronous, main-thread-only game logic | No real concurrency risk; simplest debugging | CPU-bound work appears (unlikely for a VN). No Jobs, Burst or threads before that |
| D-07 | UnityEvent composition on interactables and prefabs (`InteractableItem.OnInteract`, `LockedActionBehaviour.OnLocked/OnUnlocked`) | The right decoupling tool for point-and-click content; iteration without code | Wiring becomes unmanageable across hundreds of objects |
| D-08 | Input: `InputActionReference` for dialogue advance, `Mouse.current` polling for the world, EventSystem for UI | Simple and sufficient; ARCH-02 adds the only arbitration needed | Gamepad/keyboard world navigation is added. No input-router framework before that |
| D-09 | No Addressables | Unneeded at the current scale | See ARCH-19 (profiling evidence required) |
| D-10 | Inventory model/service/manager split | Slightly more layers than needed, but merging isn't worth the churn | — Don't add more layers (no repositories, use-cases, DDD aggregates) |
| D-11 | `InventoryPresenter` slot pooling; `DialogueUIController` fixed choice-button array | Simple and adequate | Choice count becomes variable and large |
| D-12 | `DialogueLineMessage`/`DialogueChoicesMessage` between `DialogueController` and `DialogueUIController` (even though both are in the same module) | Harmless, readable, and it lets the logic stay free of MonoBehaviour references; converting buys nothing | The dialogue view needs to query dialogue state (then read `DialogueManager`) |
| D-13 | No generic UI framework (MVVM, window stack systems) | Real menus don't exist yet; `UIWindow`/`UIWindowManager` is enough | Several stacked menus with shared navigation exist |
| D-14 | No Clean Architecture / DDD / hexagonal layering | Ceremony without a concrete problem for a solo VN | — |
| D-15 | Mono scripting backend (confirmed 2026-09-18) | Target is PC/Mac/Linux only; Mono means faster iteration and no IL2CPP build times, with nothing in the project (no Jobs/Burst/threads per D-06, no Addressables per D-09) that needs IL2CPP. ARCH-06 already removed the only reflection-based construction that would have made IL2CPP managed stripping risky, so this choice is low-stakes either way | A mobile (Android/iOS) or console release is added to the target platforms — iOS requires IL2CPP outright, and 64-bit Android in practice does too |

---

## Open questions (answers change priorities)

- ~~**Rooms:** are multiple rooms/scenes planned soon? That decides when ARCH-08 fires.~~ **Answered 2026-09-18: yes, soon.** ARCH-08 moved to `TODO`.
- ~~**Saves:** is a save system needed for the first playable? That decides when ARCH-09 fires.~~ **Answered 2026-09-18: needed soon.** ARCH-09 moved to `TODO`.
- ~~**Build backend:** IL2CPP, and at what managed stripping level?~~ **Answered 2026-09-18: Mono, targeting PC/Mac/Linux only.** Recorded as [D-15](#decisions-to-keep); revisit only if mobile/console targets are added.
- ~~**Dialogue presentation:** will dialogue use typewriter text or voice lines? That triggers ARCH-18.~~ **Answered 2026-09-18: yes, soon.** ARCH-18 moved to `TODO`.
- **Click-through (ARCH-02):** partially confirmed 2026-09-18 in Play Mode via the connected Editor — `PlayerInputGate.IsEnabled` and `CanClickThisFrame`'s enabling-frame guard behave exactly as designed when driven directly (dialogue start/end correctly toggles the gate, no soft-lock, no stray console errors across a full dialogue → choice → inventory → locked-door → unlocked-door sequence). What's still unverified is the literal mouse-driven repro (moving the cursor over the trigger object and clicking through with real OS input) — the CLI has no click-simulation command, so this was exercised via direct method calls instead of Input System events. Low remaining risk, but not the same as an eyes-on Play Mode click test.

---

## Changelog

| Date | Change |
|---|---|
| 2026-09-21 | **ARCH-22: the door remembers it was opened.** A live bug found in play: `LockedActionBehaviour` was stateless, so after the key was consumed `TryUse` failed forever and an opened door reported itself locked again. Fixed by recording the unlock as a story flag (`unlockedFlagId`), which — because `StoryFlags` was already a `GameState` field from ARCH-20 — **needed no save-system change at all**; the flag persisted, restored and got wiped by Reset Session for free. The component also gained an optional `requiredFlagId` (flag-gated as well as item-gated) and an `OnAlreadyUnlocked` event. That event initially fell back to `OnUnlocked` so existing scenes needed no rewiring, but the fallback was **reverted the same day**: it meant an already-open door replayed the "destrancou" line forever, while the actual intent is a pure hook for going *through* the door (a scene transition). The events then split into **state** (`OnOpened`, fired on unlock *and* in `Start()` whenever the flag is already set — this is what keeps a gate open across reloads and loads) and **action** (`OnAlreadyUnlocked`, fired only by a click, which is where a scene transition belongs; wiring one to `OnOpened` would teleport the player automatically on load). `DevTools/PlaceholderTint` is wired to `OnOpened`, so green reads as "this gate is open" until there is open-door art. Also recorded: an Editor driven over the CLI is unfocused and `runInBackground` is off, which **freezes Play Mode entirely** — `Start()` never runs and restore logic looks broken; set `Application.runInBackground = true` in any frame-dependent Play Mode check. `LockedActionBehaviour` moved from `ProjetoVN.Inventory` to `ProjetoVN.GameFlow`, since it now combines an item (Inventory) with a flag (Core): the move used `AssetDatabase.MoveAsset` to keep the `m_Script` GUID, and the four stale type strings in the two scenes were hand-edited — carefully, since `CollectableItemBehaviour` shares the `ProjetoVN.Inventory` string and a blanket replace would have destroyed the collectable key. The ordering inside `Interact()` is load-bearing and commented: the flag is checked **before** the item, because `IsSet` consumes nothing while `TryUse` consumes — reversed, a gate needing both would eat the player's key to discover the flag wasn't set. This also **breaks one of ARCH-04's definition-of-done items** ("`OnUnlocked` only if `TryUse` returns `true`"), deliberately and now restated there. Third independent confirmation that Unity never refreshes `m_EditorClassIdentifier`. Verified in Play Mode with 0 console errors across locked / first-unlock / re-interact / idempotence / flag-gate-keeps-the-key / Save / Reset / Load / scene-reload; 48/48 EditMode tests still pass. |
| 2026-09-21 | **Dialogues can finally change the game: ARCH-21, then ARCH-20 (with ARCH-11 folded in); ARCH-12 dropped.** ARCH-21 cleared seven disconnected defects first — `ItemDataSO.id` defaulting to a *non-empty* `"default-id"` (which made its own `OnValidate` emptiness check unreachable, so two un-edited assets silently shared the save format's primary key), `CollectableItemBehaviour` ignoring `Collect`'s `bool` (destroying the world object on a duplicate instead of leaving it), missing null guards in `GameSaveManager.Load`, `DialogueInputHandler` and `ScreenPanController`, the dead `SampleScene.unity` with its singleton-stomping `DialogueManager`, and three stale `m_EditorClassIdentifier` strings ARCH-16 had missed. ARCH-20 then replaced the never-subscribed `DialogueTrigger`/`DialogueTriggerMessage` string pair with `DialogueEffectSO` assets: the abstract base in `Dialogue`, four concrete effects (`GiveItem`, `RemoveItem`, `SetFlag`, `ClearFlag`) in `GameFlow`, and a new static `StoryFlags` in `Core` persisted through `GameState.storyFlagIds`. ARCH-11's reordering came along for the ride since it rewrote the same two methods, plus a `_isExecutingEffects` guard so a reentrant `StartDialogue` warns instead of being clobbered. **ARCH-12 was dropped**, contradicting its own "don't build SO action assets" advice on the strength of its second clause — `GiveItemEffect` needs a typed `ItemDataSO` reference, not a string, and a string id would move a broken reference from an Inspector-time error to a silent runtime `LogError`. Two things the plan didn't anticipate: the 8 dialogue assets kept their dead `Triggers:` key until `AssetDatabase.ForceReserializeAssets` was run (a field-type swap alone doesn't rewrite authored files), and `m_EditorClassIdentifier` turned out **not** to be refreshed by saving — re-save, `SerializedObject` re-serialize and `ForceReserializeAssets` all left it stale, so it was corrected in the YAML with the scene closed. Verified live in `[Teste] Mecanicas`: choosing "Sim" with the Gótica gave the key and set `falou-com-gotica`, Save wrote both to JSON, Reset Session cleared both plus the UI slot, Load restored all three, and the locked door still consumed the key and played its unlocked branch. 0 console errors. **48/48 EditMode tests pass**, up from 20. |
| 2026-09-18 | **UI textures resized to 1x; vector graphics package reinstalled.** Two corrections to the previous entry. (1) Removing `com.unity.vectorgraphics` was a mistake: the built-in importer still imports Textured Sprite, but the SVG import-settings Inspector lives in the package, so without it the Inspector shows only a warning. Reinstalled (still `3.0.0-preview.7`), kept for the Inspector only — the scene uses plain `Image`, not `SVGImage`. (2) The 2x-oversized textures still aliased: the SVG importer has no mipmap option, so a texture drawn at half size or less is minified with plain bilinear filtering and thin edges break up again (worse in a Game view set to "16:9 Aspect", which renders below 1080p). Every UI texture now matches its 1080p on-screen size exactly (box 1500×368, nameplate 230×72, `>>` 168×150), and choice buttons use a new `ChoicePill.svg` (a copy of `Nameplate.svg` rasterized at exactly 560×120) since one texture can't be 1:1 at two sizes. Sampling raised from 4 to 8 per pixel: at 1x, 4 samples left a visible triangle seam in the box's translucent fill. Verified in a 1920×1080 Play-mode capture. |
| 2026-09-18 | **Dialogue UI art no longer pixelated; vector graphics package removed.** `SVGImage` draws the SVG as a raw triangle mesh with no anti-aliasing (URP MSAA is off, and Overlay canvases don't get MSAA anyway), so every curved/diagonal edge was jagged. The three UI SVGs now import as **Textured Sprite** (rasterized with the importer's 4x sampling, at ~2x their on-screen size at 1080p: box 3000×736, nameplate 1120×349, `>>` 336×301) and the scene uses plain uGUI `Image`s. That made `com.unity.vectorgraphics@3.0.0-preview.7` unnecessary, so it was uninstalled; the built-in SVG importer handles Textured Sprite. Rejected alternative: Screen Space - Camera canvas + URP MSAA 4x — a whole-scene render change and cost to keep a preview package for UI a plain `Image` can draw. |
| 2026-09-18 | **New organized test scene, dialogue UI from the SVG art.** `Assets/Scenes/[Teste] Mecanicas.unity` (first in Build Settings; `[Teste] CameraPan` kept) groups everything under `Systems` / `Environment` / `UI`, and exercises every current mechanic: dialogue with choices (`NPC_Gotica`), pickup (`Item_Chave`, with its own fresh `persistentId`), locked/unlocked door (`Door_PortaTrancada`), inventory panel, edge panning, and save/load through a scene-only `Canvas_Debug` panel (`GameFlow/DevTools/SaveLoadDebugPanel`: Save / Load / Reset Session / Reload Scene). Dialogue UI uses `SVGImage` from the newly added `com.unity.vectorgraphics@3.0.0-preview.7` (the only version the registry offers — a **preview** package); the three UI SVGs were switched to the "UI SVGImage" import type. `DialogueUIController` gained optional `speakerNameplate` (hidden on narration lines with an empty speaker) and `continueIndicator` fields. Inventory UI fixed: `SlotUI.prefab` was a SpriteRenderer with unwired references and showed nothing — now UI `Image` + icon + TMP name, wired. Data fix: `PortaTrancada`/`PortaDestrancada` had their line typed into `SpeakerName`. `Managers.prefab` moved to `Assets/Prefabs/Resources/`. Verified in Play mode via the connected Editor with screenshots and the buttons' real `onClick` wiring; 20/20 EditMode tests still pass. |
| 2026-09-18 | **ARCH-08 and ARCH-09 implemented and Play Mode-verified.** All persistent managers (`GameStateController`, `DialogueManager`, `DialogueInputHandler`, `InventoryManager`, new `GameSaveManager`) now live on one `Assets/Resources/Managers.prefab`, spawned once by a new `ManagersBootstrap` and never scene-authored again. This also fixed the `InventoryPresenter`/`InventoryManager` ordering race found the same day (moved the presenter's first rebuild from `OnEnable` to `Start`, the only point guaranteed to run after the bootstrap). New `ItemRegistry` SO plus a `persistentId` on `CollectableItemBehaviour` give items and world objects stable ids; `GameSaveManager.Save()`/`Load()` round-trip a `GameState` through JSON to `Application.persistentDataPath`. Along the way, found and fixed a real gap the plan hadn't anticipated: bulk inventory restores didn't reach the UI, since `ReplaceAll` published no message — fixed with a new `InventoryReplacedMessage`. `GameFlow`'s asmdef now also references `Inventory` (no cycle). Verified live via the connected Editor: zero console errors through the whole session, exactly one `InventoryManager` after an in-session scene reload, a collected item's world object staying hidden across that reload with no disk I/O, and a full save → wipe → load round-trip restoring both the model and the UI. 20/20 EditMode tests pass (18 existing + 2 new `GameStateTests`). |
| 2026-09-18 | **Closed the last open question.** Player Settings confirms Scripting Backend is Mono; project owner confirmed the target is PC/Mac/Linux only. Recorded as [D-15](#decisions-to-keep) (Mono is low-stakes given ARCH-06 already removed the only reflection-based construction, and there's no plan to need IL2CPP). No code or setting changes made. |
| 2026-09-18 | **Committed and Play Mode-verified the 2026-09-17 refactor; un-deferred ARCH-08/09/18.** The entire ARCH-01–16 working tree (previously uncommitted) was split into 11 reviewable commits by module (`742bb83`…`e8590e7`). A read-only integrity scan found no dangling references from the deletions, plus one out-of-scope stale `m_EditorClassIdentifier` string in `ButtonSemBorda.prefab` (fixed). Live Play Mode verification via the connected Editor CLI exercised the full dialogue lifecycle (start/choice/end, gate toggling, no soft-lock), the inventory loop (collect key → locked door → unlocked door), and re-ran all 18 EditMode tests (still 18/18) — all with zero unexpected console errors. Two small bugs found during this pass were fixed: `DialogueChoiceButton` missing a null-check on `DialogueManager.Instance`, and `InventoryManager` destroying only the component instead of the GameObject on a duplicate singleton. One new bug was found and left open: `InventoryPresenter.OnEnable()` can race `InventoryManager.Awake()` at scene start (documented under ARCH-08). Asked the project owner about the roadmap's open questions: a second scene, save/load, and dialogue presentation effects are all coming soon, so ARCH-08, ARCH-09 and ARCH-18 moved from `DEFERRED` to `TODO` and were resequenced ahead of the Phase 4 optional items. |
| 2026-09-15 | Roadmap created from the architectural audit (commit `2757f98`). No code changed. |
| 2026-09-17 | **Phase 3 done.** ARCH-10 (18 EditMode tests for `MessageBroker` and `DialogueController`; Tests asmdef modernized), ARCH-13 (every ad-hoc flow log removed; tracing lives behind `VN_TRACE_MESSAGES`; warnings and errors carry a context object), ARCH-14 (five UI bugs fixed, four placeholders deleted), ARCH-15 (`ProjetoVN.UI.asmdef`; `Assembly-CSharp` now holds no project scripts) and ARCH-16 (`Assets.Scripts.*` → `ProjetoVN.*` across 28 files, `rootNamespace` set, bogus versionDefine removed, UnityEvent type strings corrected including one that was already broken). ARCH-11 and ARCH-12 remain `DEFERRED`: still no `DialogueTriggerMessage` consumer. |
| 2026-09-17 | **Phase 2A done.** ARCH-05 (agreed communication rules now live in `Core/Messaging/README.md`, linked from all nine module READMEs; non-existent flows removed), ARCH-06 (state machine reduced to `BaseState` + a plain C# `StateMachine`; factories, reflection and demo code deleted; input-gate writes moved into state `Enter()`; Build Settings points at the gameplay scene), ARCH-04 (inventory commands and queries are direct calls; three messages deleted) and ARCH-07 (model owns `ItemDataSO`; `Item` and `itemDatabase` deleted; the inventory panel rebuilds from `InventoryManager.Items` on enable). ARCH-08 and ARCH-09 remain `DEFERRED`: their triggers were re-checked and have not fired. Play Mode verification still pending; see each item's notes. |
| 2026-09-17 | **Phase 1 done.** ARCH-01 (MessageBroker hardened: per-type generic holders, exception isolation, idempotent subscribe, play-mode reset), ARCH-02 (`PlayerInputGate` replaces `TogglePlayerInputMessage`) and ARCH-03 (Dialogue owns its start/end lifecycle; `DialogueStartedMessage` replaces `DialogueRequestMessage`) implemented and compiling. Play Mode verification still pending; see each item's notes. |
