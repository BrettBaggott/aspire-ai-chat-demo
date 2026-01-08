#!/usr/bin/env bash
set -euo pipefail

port="${1:-5173}"
ip="$(ip route get 1.1.1.1 2>/dev/null | awk '{for (i=1;i<=NF;i++) if ($i==\"src\") {print $(i+1); exit}}')"

if [ -z "$ip" ]; then
  ip="$(hostname -I 2>/dev/null | awk '{print $1}')"
fi

if [ -z "$ip" ]; then
  echo "Could not determine LAN IP."
  exit 1
fi

echo "LAN URL: http://$ip:$port/"
