# Äänityökalu

`aani.sh` mittaa äänitiedostojen tasot ja nostaa niitä limitterillä (ffmpeg).

## Käyttö

```bash
# 1. Mittaa: keskitaso (mean) ja huippu (max) dB:nä, tässä järjestyksessä
tools/aanet/aani.sh mittaa Assets/Resources/Sfx/horhe/*.mp3 Assets/Resources/Sfx/horhe/*.wav

# 2. Nosta tietty määrä (dB) ja korvaa tiedosto; huiput pysyvät noin -0,6 dB:ssä
tools/aanet/aani.sh nosta 5 Assets/Resources/Sfx/horhe/horhe_speak1.mp3
```

- Sääntö: puheet ovat usein hiljaisia, vaikka huiput osuvat jo rajalle. Nosta keskitasoa (mittaa: mean) kohti noin -13 dB.
- MP3 koodataan uudelleen (`-q:a 2`), joten älä nosta samaa tiedostoa monta kertaa peräkkäin. Hae alkuperäinen gitistä.
- Unity ei vaadi muutoksia: tiedostonimi ja .meta pysyvät ennallaan.

## Horhen äänille käytetyt nostot (dB)

puheet (speak1, speak2) +6 · nauru +6 · avunhuuto +4 · kaatuminen +5 · ylösnousu +5 ·
kipuäänet (gasp1…4) +3, +4, +3, +3 · vasara +3 · läpsyt (slap1…3) +6, +3, +5.
Alkuperäiset: `git show 00dc4bc^:Assets/Resources/Sfx/horhe/<nimi> > <nimi>`.
