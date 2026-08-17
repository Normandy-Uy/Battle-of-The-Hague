#!/usr/bin/env bash
set -euo pipefail

DEPLOY_PATH="${DEPLOY_PATH:-/var/www/psr/play}"
NGINX_SITE="${NGINX_SITE:-/etc/nginx/sites-enabled/psr}"
MARKER="# DEPLOY-1: Unity WebGL demo at https://psr.ovh/play/"

echo "[DEPLOY-1] Remote setup on $(hostname)"

mkdir -p "$DEPLOY_PATH"

if [ -f "$DEPLOY_PATH/index.html" ]; then
  BACKUP="$HOME/psr-play-backup-$(date +%Y%m%d_%H%M%S)"
  mkdir -p "$BACKUP"
  cp -a "$DEPLOY_PATH/." "$BACKUP/" 2>/dev/null || true
  echo "[DEPLOY-1] Backed up previous /play to $BACKUP"
fi

chmod -R a+rX "$DEPLOY_PATH"

if ! grep -q 'application/wasm[[:space:]]*wasm' /etc/nginx/mime.types 2>/dev/null; then
  echo "[DEPLOY-1] Adding application/wasm to /etc/nginx/mime.types"
  sudo cp /etc/nginx/mime.types "/etc/nginx/mime.types.bak.$(date +%Y%m%d_%H%M%S)"
  sudo awk '
    /^}/ && !done {
      print "    application/wasm                         wasm;"
      done = 1
    }
    { print }
  ' /etc/nginx/mime.types | sudo tee /etc/nginx/mime.types.new >/dev/null
  sudo mv /etc/nginx/mime.types.new /etc/nginx/mime.types
fi

if [ ! -f "$NGINX_SITE" ]; then
  echo "[DEPLOY-1] ERROR: nginx site not found: $NGINX_SITE" >&2
  exit 1
fi

if ! grep -qF "$MARKER" "$NGINX_SITE"; then
  echo "[DEPLOY-1] Patching nginx site: $NGINX_SITE"
  sudo cp "$NGINX_SITE" "$HOME/nginx-psr.bak.$(date +%Y%m%d_%H%M%S)"
  SNIPPET_FILE="$HOME/deploy-1-nginx-play.snippet"
  if [ ! -f "$SNIPPET_FILE" ]; then
    echo "[DEPLOY-1] ERROR: snippet missing at $SNIPPET_FILE" >&2
    exit 1
  fi
  sudo awk -v snippet="$SNIPPET_FILE" '
    $0 ~ /^[[:space:]]*location \/ \{/ && !done {
      while ((getline line < snippet) > 0) print line
      close(snippet)
      done = 1
    }
    { print }
  ' "$NGINX_SITE" | sudo tee "$NGINX_SITE.new" >/dev/null
  sudo mv "$NGINX_SITE.new" "$NGINX_SITE"
else
  echo "[DEPLOY-1] nginx /play/ block already present"
fi

echo "[DEPLOY-1] Testing nginx config..."
sudo nginx -t
sudo systemctl reload nginx
echo "[DEPLOY-1] nginx reloaded OK"
echo "[DEPLOY-1] Files in $DEPLOY_PATH:"
ls -la "$DEPLOY_PATH" | head -20
