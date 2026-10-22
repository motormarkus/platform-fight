@echo off
rem Mittaa kaikki puheet ja tehosteet ja kirjoittaa Assets\Resources\AudioLevels.json (peli tasaa äänet sen mukaan).
cd /d "%~dp0\..\.."
python -X utf8 Tools\AudioLevels\measure_audio.py %*
pause
