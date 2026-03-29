# LeagueDiscordBot

A Discord bot that fetches League of Legends player stats using the Riot Games API.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- A Discord bot token from the [Discord Developer Portal](https://discord.com/developers/applications)
- A Riot Games API key from the [Riot Developer Portal](https://developer.riotgames.com)
- Your Oracle VM SSH private key

## Fresh Machine Setup

### 1. Fill in your credentials

The `Backend/config.json` file is not committed to the repo. Fill in your credentials:

```json
{
  "DiscordBotToken": "<your-discord-bot-token>",
  "RiotAPIToken": "<your-riot-api-key>",
  "RiotAPIHeaderName": "X-Riot-Token"
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

## Deployment

The bot is hosted on an Oracle Cloud VM and managed via `systemd`. Scripts are located in `scripts/`:

- `scripts/deploy.sh` — builds, publishes, and deploys the bot to the VM
- `scripts/bot.sh` — controls the bot (start, stop, status)

### Available Commands

| Command | Description |
|---|---|
| `deploy-bot` | Build, publish, and deploy the bot to the VM |
| `bot-start` | Start the bot on the VM |
| `bot-stop` | Stop the bot on the VM |
| `bot-status` | Check if the bot is running |

### Re-provisioning the VM

If you ever need to set up the VM from scratch:

1. Create a new Oracle Cloud VM (Ubuntu 22.04, VM.Standard.E2.1.Micro)
2. Assign a public IP to the VNIC
3. SSH in and install the .NET 8 runtime:
```bash
sudo apt update && sudo apt install -y dotnet-runtime-8.0
```
4. Run `deploy-bot` to deploy the bot
5. Set up the systemd service:
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
