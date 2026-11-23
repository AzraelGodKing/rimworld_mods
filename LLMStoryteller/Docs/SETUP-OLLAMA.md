# LLM Storyteller: setting up Ollama on Windows

Ollama runs a language model on your own PC and serves it on `http://localhost:11434`. The mod talks to its OpenAI-compatible API at `http://localhost:11434/v1`. Nothing leaves your computer.

## What you need

- Windows 10 or 11.
- For the recommended 8B model: a GPU with **6 GB or more of VRAM** (NVIDIA or AMD), or a fast CPU and 16 GB of RAM. It also works on CPU only, just slower (often 20 to 60 seconds per decision).
- About 5 GB of free disk space per model.

## 1. Install Ollama

1. Download the Windows installer from <https://ollama.com/download> (`OllamaSetup.exe`).
2. Run it. It installs for your user, no admin rights needed.
3. When it finishes, Ollama runs in the background and shows a llama icon in the system tray. The server starts with it, so there is nothing else to launch.

Check it from PowerShell or Command Prompt:

```powershell
ollama --version
```

## 2. Download a model

Recommended: **Llama 3.1 8B** (about 4.9 GB). It follows the JSON answer format well.

```powershell
ollama pull llama3.1:8b
```

Other good choices:

| Model | Size | Notes |
|---|---|---|
| `llama3.1:8b` | 4.9 GB | Default in the mod. Good all-rounder. |
| `qwen2.5:7b` | 4.7 GB | Very reliable at answering in JSON. |
| `llama3.2:3b` | 2.0 GB | For weak GPUs or CPU only. Picks are less clever. |

Avoid "thinking" models (`deepseek-r1`, `qwen3` with thinking on) for now. They spend most of their answer reasoning, are much slower, and can run out of room before they give the JSON answer.

Check what you have:

```powershell
ollama list
```

The name in the first column (for example `llama3.1:8b`) is what goes in the mod's **Model name**.

## 3. Check the server

The server is already running if the tray icon is there. Test it:

```powershell
Invoke-RestMethod http://localhost:11434/v1/models
```

You should see your model in the list. If you get "Unable to connect", start Ollama from the Start menu.

Optional quick test that the model answers:

```powershell
ollama run llama3.1:8b "Say hello in five words."
```

## 4. Point the mod at it

1. In RimWorld: **Options > Mod Options > LLM Storyteller**.
2. Click **Ollama (port 11434)**. The server address becomes `http://localhost:11434/v1`.
3. Set **Model name** to the exact name from `ollama list`, for example `llama3.1:8b`.
4. Leave **API key** blank.
5. Click **Test connection**. You want: *Connected. The server has "llama3.1:8b".*
6. Start a new colony and pick **the Oracle** as storyteller, or change the storyteller of a loaded game from **Options > Gameplay > Change storyteller**.

About once a day (in game) the Oracle asks the model what happens next and sends a letter with its reason.

## Recommended Ollama settings

Ollama unloads a model after 5 idle minutes, and loading it again can take 10 to 30 seconds. Since the storyteller only asks about once an in-game day, keep the model loaded longer:

1. Quit Ollama from the tray icon (right-click > Quit Ollama).
2. Open **Start > "Edit environment variables for your account"**.
3. Add a new variable `OLLAMA_KEEP_ALIVE` with value `1h`.
4. Start Ollama again from the Start menu.

The same way you can set:

- `OLLAMA_HOST=127.0.0.1:11435` to use another port (then change the port in the mod's server address too).
- `OLLAMA_CONTEXT_LENGTH=8192` if you raise **Incidents offered per decision** a lot and the model starts ignoring the list.

## Ports

| What | Default |
|---|---|
| Ollama server | `11434` |
| Address in the mod | `http://localhost:11434/v1` |

Only your own PC connects, so the Windows firewall does not need a rule. Do not expose the port to the internet.

## Troubleshooting

**Test connection says "Could not reach the server"**
- Ollama is not running. Start it from the Start menu and look for the tray icon.
- Wrong port. Check `OLLAMA_HOST` if you set it.
- Run `Invoke-RestMethod http://localhost:11434/v1/models` in PowerShell. If that fails too, the problem is Ollama, not the mod.

**"Connected, but the server has no model called ..."**
- The name must match `ollama list` exactly, including the tag after the colon (`llama3.1:8b`, not `llama3.1`). The test lists the names the server has.

**A message says "the model could not decide (timed out ...)"**
- The model is too slow for the timeout. Raise **Give up on an answer after** in Mod Options (up to 180 seconds), set `OLLAMA_KEEP_ALIVE`, or use a smaller model.
- Check GPU use with `ollama ps`. `100% GPU` is best; a CPU/GPU split means the model does not fit in VRAM.

**A message says "... is not on the list of allowed incidents" or "No JSON object"**
- The model ignored the format twice. Lower **Model temperature** to about 0.4, or switch to `qwen2.5:7b`.

**Nothing seems to happen**
- Decisions happen about once per in-game day (see **Hours between decisions**), and threats are not offered before day 11, misc events not before day 5, like Cassandra.
- Make sure **Use the model** is on.
- Look in the log: turn on Development mode and open the debug log, or read `%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log`. Lines from the mod start with `[LLMStoryteller]`.

**What happens when the model is off?**
Cassandra Classic takes over until the next decision, so the game plays normally. You get one message when this starts.

**Multiplayer**
Not supported.
