#!/bin/bash
set -e

SSH_KEY="${ORACLE_SSH_KEY:-$HOME/Downloads/Oracle VM SSH Keys/ssh-key-2026-03-29.key}"
VM="ubuntu@129.146.192.186"

ssh_cmd() {
    ssh -i "$SSH_KEY" "$VM" "$1"
}

case "$1" in
    start)
        if ssh_cmd "systemctl is-active leaguediscordbot" > /dev/null 2>&1; then
            echo "Bot is already running."
        else
            ssh_cmd "sudo systemctl start leaguediscordbot" && echo "Bot started." || echo "Bot failed to start."
        fi
        ;;
    stop)
        if ssh_cmd "systemctl is-active leaguediscordbot" > /dev/null 2>&1; then
            ssh_cmd "sudo systemctl stop leaguediscordbot" && echo "Bot stopped." || echo "Bot failed to stop."
        else
            echo "Bot is already stopped."
        fi
        ;;
    status)
        if ssh_cmd "systemctl is-active leaguediscordbot" > /dev/null 2>&1; then
            echo "Bot is running."
        else
            echo "Bot is stopped."
        fi
        ;;
    *)
        echo "Usage: bot.sh [start|stop|status]"
        ;;
esac
