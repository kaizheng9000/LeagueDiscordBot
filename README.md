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

| Command                     | Description                                                                            |
| --------------------------- | -------------------------------------------------------------------------------------- |
| `/kda [player] [queueType]` | Average KDA for a player. Queue type defaults to normal, use `ranked` for ranked only. |
| `/info [player]`            | Account overview — level, solo/duo rank, flex rank, and most played champion.          |
| `/facts`                    | Spits some facts.                                                                      |

### Other Features

- **Summoner cache** — Players are cached in SQLite after first lookup to reduce Riot API calls. Stale entries are removed automatically on 404. (Saves you like half a second tbh)
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

### Re-provisioning the VM

If you ever need to set up the VM from scratch:

1. Create a new Oracle Cloud VM (Ubuntu 22.04, VM.Standard.E2.1.Micro)
2. Assign a public IP to the VNIC
3. SSH in and install the .NET 10 runtime and create the data directory:

```bash
sudo apt update && sudo apt install -y dotnet-runtime-10.0
mkdir -p ~/bot-data
```

4. Copy your `config.json` with real credentials to the VM:

```bash
scp -i "$ORACLE_SSH_KEY" Backend/config.json ubuntu@<VM_IP>:~/bot/config.json
```

5. Run `deploy-bot` to deploy the bot
6. Set up the systemd service:

```bash
sudo nano /etc/systemd/system/leaguediscordbot.service
```

```ini
[Unit]
Description=League Discord Bot
After=network.target

[Service]
WorkingDirectory=/home/ubuntu/bot
ExecStart=/usr/bin/dotnet /home/ubuntu/bot/LeagueDiscordBot.dll
Restart=always
User=ubuntu

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl enable leaguediscordbot
sudo systemctl start leaguediscordbot
```
