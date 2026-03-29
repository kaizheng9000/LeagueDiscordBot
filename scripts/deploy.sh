#!/bin/bash
set -e

SSH_KEY="${ORACLE_SSH_KEY:-$HOME/Downloads/Oracle VM SSH Keys/ssh-key-2026-03-29.key}"
VM_USER="ubuntu"
VM_IP="129.146.192.186"

echo "Building and publishing..."
cd "$(dirname "$0")/../Backend"
dotnet publish -c Release -o ./publish

echo "Copying files to VM..."
scp -i "$SSH_KEY" -r ./publish/* "$VM_USER@$VM_IP:~/bot"

echo "Restarting bot..."
ssh -i "$SSH_KEY" "$VM_USER@$VM_IP" "sudo systemctl restart leaguediscordbot"

echo "Done!"
