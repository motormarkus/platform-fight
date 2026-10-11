#!/usr/bin/env bash
# Äänityökalu (ffmpeg): mittaa tasot ja nosta ääni limitterillä niin, ettei huippu särise.
#
#   tools/aanet/aani.sh mittaa  <tiedosto> [tiedosto ...]     keskitaso ja huippu dB:nä
#   tools/aanet/aani.sh nosta   <dB> <tiedosto> [tiedosto ...]  nostaa dB verran + limitteri, korvaa tiedoston
#
# Limitteri pitää huiput noin -0,6 dB:ssä (limit=0.93), joten nosto ei leikkaa (clippaa).
# Alkuperäiset: git-historiassa (git show <commit>^:polku > tiedosto), älä ylikirjoita ilman commitia.
set -euo pipefail
cmd="${1:-}"; shift || true
command -v ffmpeg >/dev/null || { echo "ffmpeg puuttuu"; exit 1; }

case "$cmd" in
  mittaa)
    for f in "$@"; do
      v=$(ffmpeg -hide_banner -i "$f" -af volumedetect -f null - 2>&1 | grep -E "max_volume|mean_volume" | awk '{print $5}' | tr '\n' ' ')
      echo "$f  keskitaso, huippu (dB): $v"
    done ;;
  nosta)
    db="$1"; shift
    for f in "$@"; do
      ext="${f##*.}"; q=()
      case "$ext" in mp3) q=(-q:a 2);; ogg) q=(-q:a 5);; esac
      tmp="${f%.*}.tmp.$ext"
      ffmpeg -v error -y -i "$f" -af "volume=${db}dB,alimiter=limit=0.93:attack=3:release=60:level=disabled" "${q[@]}" "$tmp"
      mv "$tmp" "$f"
      echo "nostettu $db dB: $f"
    done ;;
  *) sed -n '2,9p' "$0"; exit 1 ;;
esac
