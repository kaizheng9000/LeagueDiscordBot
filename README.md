# LeagueDiscordBot

A Discord bot that fetches League of Legends player stats using the Riot Games API.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10)
- A Discord bot token from the [Discord Developer Portal](https://discord.com/developers/applications)
- A Riot Games API key from the [Riot Developer Portal](https://developer.riotgames.com)
- Your Oracle VM SSH private key
- [Git Bash](https://git-scm.com/downloads) (Windows only — Mac has bash built in)

### Windows: Install rsync for faster deploys

Git Bash on Windows does not include `rsync` by default. Without it, deploys will fall back to `scp` and copy all files every time. To get fast incremental deploys, install `rsync` via [MSYS2](https://www.msys2.org):

1. Install [MSYS2](https://www.msys2.org)
2. Open the MSYS2 terminal and run:

```bash
pacman -S rsync
```

3. Copy `rsync.exe` and its dependencies to your Git Bash `bin` folder (usually `C:\Program Files\Git\usr\bin\`):

```
C:\msys64\usr\bin\rsync.exe
C:\msys64\usr\bin\msys-iconv-2.dll
C:\msys64\usr\bin\msys-intl-8.dll
C:\msys64\usr\bin\msys-2.0.dll
C:\msys64\usr\bin\msys-xxhash-0.8.3.dll
C:\msys64\usr\bin\msys-zstd-1.dll
C:\msys64\usr\bin\msys-lz4-1.dll
```

> If you skip this, deploys still work — they just copy every file each time.

## Fresh Machine Setup

### 1. Fill in your credentials

The `Backend/config.json` file is not committed to the repo. Fill in your credentials:

```json
{
  "DiscordBotToken": "<your-discord-bot-token>",
  "RiotAPIToken": "<your-riot-api-key>",
  "RiotAPIHeaderName": "X-Riot-Token",
  "ErrorWebhookUrl": "<your-discord-error-webhook-url>",
  "DatabasePath": "/home/ubuntu/bot-data/bot.db"
}
```

> **Note:** The Riot development API key expires every 24 hours. Regenerate it at the [Riot Developer Portal](https://developer.riotgames.com) and update `RiotAPIToken` in `config.json` daily. After updating, run `deploy-bot` to push the new key to the VM.

### 2. Place your SSH key

Place your Oracle VM SSH private key at:

```
~/Downloads/Oracle VM SSH Keys/ssh-key-2026-03-29.key
```

Then set the correct permissions:

```bash
chmod 600 "$HOME/Downloads/Oracle VM SSH Keys/ssh-key-2026-03-29.key"
```

> If your key is stored elsewhere, update `ORACLE_SSH_KEY` in your shell profile after running the setup script.

### 3. Run the setup script

```bash
bash scripts/setup.sh
```

This will automatically:

- Add the `ORACLE_SSH_KEY` environment variable to your shell profile
- Add all aliases pointing to the correct repo location
- Make the scripts executable
- Reload your shell profile

### 4. Run locally (optional)

```bash
cd Backend
dotnet run
```

---

## Bot Features

### Discord Commands

| Command                     | Description                                                                                                                                     |
| --------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------- |
| `/kda [player] [queueType]` | Average KDA over last 20 matches. Queue type defaults to normal, use `ranked` for ranked only. Leave `player` blank to use your linked account. |
| `/info [player]`            | Account overview — level, solo/duo rank, flex rank, and top 3 most played champions. Leave `player` blank to use your linked account.           |
| `/link [player]`            | Link your Discord account to your League IGN for use with `/kda` and `/info` without specifying a player.                                       |
| `/unlink`                   | Unlink your Discord account from your League IGN.                                                                                               |
| `/facts`                    | Spits some facts.                                                                                                                               |

### Other Features

- **Summoner cache** — Players are cached in SQLite after first lookup to reduce Riot API calls. Stale entries are removed automatically on 404.
- **Linked accounts** — Link your Discord account to your League IGN. Name changes are detected and updated automatically.
- **Autocomplete** — IGN search box suggests previously looked up players as you type.
- **Error reporting** — Command failures are posted to a private Discord channel via webhook.

---

## Deployment

The bot is hosted on an Oracle Cloud VM and managed via `systemd`. Scripts are located in `scripts/`:

- `scripts/deploy.sh` — builds, publishes, and deploys the bot to the VM
- `scripts/bot.sh` — controls the bot (start, stop, status)

### Available Commands

| Command      | Description                                  |
| ------------ | -------------------------------------------- |
| `deploy-bot` | Build, publish, and deploy the bot to the VM |
| `bot-start`  | Start the bot on the VM                      |
| `bot-stop`   | Stop the bot on the VM                       |
| `bot-status` | Check if the bot is running                  |
