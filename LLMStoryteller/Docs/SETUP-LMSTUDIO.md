# LLM Storyteller: setting up LM Studio on Windows

LM Studio is a desktop app that downloads and runs language models on your own PC, and can serve them on `http://localhost:1234` with an OpenAI-compatible API. The mod talks to `http://localhost:1234/v1`. Nothing leaves your computer.

## What you need

- Windows 10 or 11 (x64, or ARM with Snapdragon X).
- For the recommended 8B model: a GPU with **6 GB or more of VRAM**, or 16 GB of RAM to run on CPU (slower, often 20 to 60 seconds per decision).
- About 5 GB of free disk space per model.

## 1. Install LM Studio

1. Download the Windows installer from <https://lmstudio.ai>.
2. Run it and open LM Studio. You can skip the first-run model suggestion.

## 2. Download a model

1. Click the **Discover** tab (magnifying glass in the left sidebar).
2. Search for **Llama 3.1 8B Instruct**.
3. Pick the **Q4_K_M** download (about 4.9 GB). LM Studio marks the variants that fit your GPU.
4. Wait for the download to finish.

Other good choices:

| Search for | Size (Q4_K_M) | Notes |
|---|---|---|
| Llama 3.1 8B Instruct | 4.9 GB | Recommended. Good all-rounder. |
| Qwen2.5 7B Instruct | 4.7 GB | Very reliable at answering in JSON. |
| Llama 3.2 3B Instruct | 2.0 GB | For weak GPUs or CPU only. Picks are less clever. |

Avoid reasoning ("thinking") models such as DeepSeek R1 or Qwen3 with thinking on. They are much slower and can run out of room before giving the JSON answer.

## 3. Start the server

1. Click the **Developer** tab in the left sidebar (the `>_` icon; called **Local Server** in older versions).
2. Use **Select a model to load** at the top and pick the model you downloaded. The default context length (4096) is enough.
3. Switch the server **Status** toggle to **Running**. The log shows `Server started on port 1234`.
4. Note the model's **API identifier** shown on the loaded model, for example `meta-llama-3.1-8b-instruct`. That is the name the mod needs.

Leave **Serve on Local Network** off; the game runs on the same PC.

To start the server automatically, open **Settings** (gear icon, bottom left) and turn on starting the LLM service on login, or use the command line tool:

```powershell
lms server start
lms load meta-llama-3.1-8b-instruct
```

Check it from PowerShell:

```powershell
Invoke-RestMethod http://localhost:1234/v1/models
```

You should see your model's identifier in the list.

## 4. Point the mod at it

1. In RimWorld: **Options > Mod Options > LLM Storyteller**.
2. Click **LM Studio (port 1234)**. The server address becomes `http://localhost:1234/v1`.
3. Set **Model name** to the API identifier from step 3, for example `meta-llama-3.1-8b-instruct`.
4. Leave **API key** blank (LM Studio does not need one).
5. Click **Test connection**. You want: *Connected. The server has "meta-llama-3.1-8b-instruct".* If the name is wrong, the test lists the names the server has; copy one exactly.
6. Start a new colony and pick **the Oracle** as storyteller, or change the storyteller of a loaded game from **Options > Gameplay > Change storyteller**.

About once a day (in game) the Oracle asks the model what happens next and sends a letter with its reason.

## Recommended LM Studio settings

- **Keep the model loaded.** If you use just-in-time loading, set its idle unload time (TTL) to an hour or more in the Developer tab's server settings, or load the model by hand as in step 3. Loading a model can take 10 to 30 seconds, which eats into the mod's timeout.
- **GPU offload.** When loading the model, keep GPU offload at the maximum that fits. A model only partly on the GPU is much slower.

## Ports

| What | Default |
|---|---|
| LM Studio server | `1234` |
| Address in the mod | `http://localhost:1234/v1` |

You can change the port in the Developer tab's server settings; change it in the mod's server address too. Only your own PC connects, so the Windows firewall does not need a rule.

## Troubleshooting

**Test connection says "Could not reach the server"**
- The server is not running. In the Developer tab, Status must say **Running**.
- Wrong port. Check the port in the Developer tab.
- Run `Invoke-RestMethod http://localhost:1234/v1/models` in PowerShell. If that fails too, the problem is LM Studio, not the mod.

**"Connected, but the server has no model called ..."**
- Use the API identifier exactly as LM Studio shows it (for example `meta-llama-3.1-8b-instruct`), not the file name. The test lists the identifiers the server has.

**A message says "the model could not decide (timed out ...)"**
- Raise **Give up on an answer after** in Mod Options (up to 180 seconds), keep the model loaded, raise GPU offload, or use a smaller model.

**A message says "... is not on the list of allowed incidents" or "No JSON object"**
- The model ignored the format twice. Lower **Model temperature** to about 0.4, or switch to Qwen2.5 7B Instruct.

**HTTP 400 or "No models loaded"**
- Load the model in the Developer tab, or turn on just-in-time loading.

**Nothing seems to happen**
- Decisions happen about once per in-game day (see **Hours between decisions**), and threats are not offered before day 11, misc events not before day 5, like Cassandra.
- Make sure **Use the model** is on.
- The Developer tab log shows every request the game sends. If nothing arrives, check the address in Mod Options.
- The game's log: turn on Development mode and open the debug log, or read `%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log`. Lines from the mod start with `[LLMStoryteller]`.

**What happens when the model is off?**
Cassandra Classic takes over until the next decision, so the game plays normally. You get one message when this starts.

**Multiplayer**
Not supported.
