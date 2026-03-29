#!/bin/bash

REPO_DIR="$(cd "$(dirname "$0")/.." && pwd)"

# Detect shell and profile file
if [[ "$OSTYPE" == "darwin"* ]]; then
    PROFILE="$HOME/.zshrc"
else
    PROFILE="$HOME/.bashrc"
fi

echo "Setting up LeagueDiscordBot aliases in $PROFILE..."

# Remove any existing entries to avoid duplicates
sed -i.bak '/ORACLE_SSH_KEY/d; /deploy-bot/d; /bot-start/d; /bot-stop/d; /bot-status/d' "$PROFILE"

cat >> "$PROFILE" << EOF

# LeagueDiscordBot
export ORACLE_SSH_KEY="\$HOME/Downloads/Oracle VM SSH Keys/ssh-key-2026-03-29.key"
alias deploy-bot='"$REPO_DIR/scripts/deploy.sh"'
alias bot-start='"$REPO_DIR/scripts/bot.sh" start'
alias bot-stop='"$REPO_DIR/scripts/bot.sh" stop'
alias bot-status='"$REPO_DIR/scripts/bot.sh" status'
EOF

chmod +x "$REPO_DIR/scripts/deploy.sh"
chmod +x "$REPO_DIR/scripts/bot.sh"

source "$PROFILE"

echo "Done! The following commands are now available:"
echo "  deploy-bot   - Build and deploy the bot to the VM"
echo "  bot-start    - Start the bot"
echo "  bot-stop     - Stop the bot"
echo "  bot-status   - Check if the bot is running"
echo ""
echo "If your SSH key is stored elsewhere, update ORACLE_SSH_KEY in $PROFILE."
