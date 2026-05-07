---
name: art-review
description: Art-direction review cycle — a senior art-director critiques a screenshot, then an executor applies the approved fixes via Unity MCP + C#. Attach a screenshot to the chat (or pass its path).
effort: high
argument-hint: "(optional) path to screenshot — otherwise the skill captures one from Unity"
allowed-tools: Read, Grep, Glob, Bash, Agent, mcp__UnityMCP__execute_code, mcp__UnityMCP__refresh_unity, mcp__UnityMCP__read_console
---

# /art-review — Art Direction Review Cycle

Run a two-stage art critique + apply loop on a screenshot.

Input: `$ARGUMENTS` may contain a path to an existing screenshot (used as-is — useful for non-Unity sources like Figma exports). If empty, the skill captures the current Game view from Unity automatically.

> **Note:** images attached to the chat message are NOT usable here — sub-agents (`Task`) only receive prompt text, not multimodal content. Sub-agents need a real on-disk path to `Read`. Do not reintroduce a chat-attachment branch.

## Process

### Step 1 — Obtain screenshot path
1. **If `$ARGUMENTS` is a non-empty path** → use it as-is. Skip capture.
2. **Otherwise — capture from Unity:**
   - Compute target path: `Assets/Screenshots/art-review-<unix-ts>.png`.
   - Ensure folder exists via `Bash`: `mkdir -p Assets/Screenshots`.
   - Call `mcp__UnityMCP__execute_code` with a C# snippet that:
     - Resolves the camera: `Camera.main`, falling back to the first enabled `Camera` in the active scene.
     - Allocates a `RenderTexture` at 1280×1920 (portrait), sets `camera.targetTexture`, calls `camera.Render()`.
     - Reads back into a `Texture2D` (RGBA32), encodes via `ImageConversion.EncodeToPNG`, writes with `System.IO.File.WriteAllBytes`.
     - Restores `camera.targetTexture = null`, releases the RT, destroys the temp Texture2D.
     - Returns the absolute path of the PNG.
   - Call `mcp__UnityMCP__refresh_unity` so AssetDatabase imports the new PNG.
3. **If capture fails** (Unity not connected, no camera, write error) → ask the user once for a path, then stop.

### Step 2 — Critique (art-director agent)
Spawn the **art-director** agent. The prompt must include:
- The resolved screenshot path (so the agent can `Read` it directly — `Read` supports images).
- A short context block listing the relevant assets the agent may want to read:
  - `Assets/Scenes/Foyer.unity`
  - `Assets/Settings/SampleSceneProfile.asset` (URP Volume profile)
  - `Assets/Resources/Config/FoyerLayoutConfig.asset` + `Assets/Scripts/Presentation/Foyer/FoyerLayoutConfig.cs`
  - `Assets/Scripts/Presentation/Foyer/DeckSelectionView.cs`, `DeckCardView.cs`
  - `docs/GDD.md` (intended tone for Foyer)
- Instruction to follow its standard output format (Visual intent → Issues & fixes table → Suggested order → Out of scope) and to stop after the plan.

### Step 3 — Show plan and wait for approval
- Present the art-director's full plan to the user verbatim.
- Ask the user to approve, request edits, or reject.
- **Do not** proceed to Step 4 until the user explicitly approves. Apply user-requested edits to the plan before approval if they're trivial; otherwise re-spawn art-director with the corrections.

### Step 4 — Apply (art-implementer agent)
Spawn the **art-implementer** agent with:
- The approved plan in full.
- The screenshot path (so it can sanity-check before/after intent).
- A reminder that priorities P0 first, then P1, then P2; skipped items must be reported with a reason.

### Step 5 — Verify
After the implementer returns:
- Run `mcp__UnityMCP__refresh_unity` if any C# was edited.
- Run `mcp__UnityMCP__read_console` and inspect for errors / new warnings.
- If errors are present and clearly caused by this run, re-spawn art-implementer **once** with the error log and the relevant plan items to fix. If errors persist, stop and report.

### Step 6 — Report
Summarize:
- Plan items applied (with the asset/file each touched).
- Plan items skipped and why.
- Compilation/console status.
- Suggest the user re-run `/art-review` — it'll capture a fresh Game-view screenshot automatically.

## Rules

- Each agent runs in its own spawn — pass all needed context in the prompt; sub-agents do not see the chat history.
- No git / commit operations from this skill — art iteration is reversible at the asset level and the user decides when to commit.
- Never call `art-implementer` without explicit user approval of the plan.
- Never modify forbidden folders (`Assets/Plugins/`, `Assets/Art/Plugins/`, `Assets/TextMesh Pro/`, auto-generated Input Action files).
