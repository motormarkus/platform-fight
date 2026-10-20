"""Valitut ruudut (video, indeksit). Yhteinen mittakaava: kaikki videot 1920x1080, hahmo samankokoinen."""
PICK = {
    "idle": list(range(0, 36, 3)),
    "kavely": list(range(5, 35, 3)),
    "juoksu": list(range(10, 30, 2)),
    "hyppypotku": [("juoksu", i) for i in (70, 74, 78, 104, 110, 116, 124, 132, 136, 140)],
}

# Videot 3c7dcd53 (1920x1080) ja 8ffa0211 (1280x720; hahmo 567 px -> kerroin 865/567 idle-mittakaavaan)
PICK2 = {
    "jab": ("lyonnit", 1.0, [20, 22, 24, 25, 26, 27, 28, 30, 32, 34]),
    "takasuora": ("lyonnit", 1.0, [98, 100, 102, 104, 106, 107, 108, 110, 113, 116]),
    "uppercut": ("uppercut", 865 / 409, [20, 25, 30, 35, 40, 45, 50, 55, 60, 90, 95, 100, 105, 110]),  # video 910x512, hahmo 409 px
    "osuma": ("kaatuminen", 1.0, [29, 35, 37, 40]),                   # vain osuman alku
    "kaatuminen": ("kaatuminen", 1.0, [62, 66, 68, 70, 72, 74, 76, 78, 80, 82, 84, 88, 92, 98]),
    "ylosnousu": ("kaatuminen", 1.0, [100, 104, 108, 110, 114, 118, 122, 126, 130, 134, 138, 142, 146]),
    "etupotku": ("potkut", 865 / 567, [20, 22, 23, 24, 25, 26, 28, 30, 31, 32, 34]),
    "kierrepotku": ("potkut", 865 / 567, [66, 68, 70, 72, 74, 76, 78, 84, 88, 90, 92, 94]),
}
