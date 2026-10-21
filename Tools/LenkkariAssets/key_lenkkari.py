import os, sys, cv2, numpy as np
sys.path.insert(0, "G:/unity pelit/mopopeli/platform fight/Tools/GymAssets")
from key_video import key_frame
V = "C:/Users/marku/.claude/uploads/3b55fbe8-a93c-494a-9b49-32b59fdabe3e/078f5726-gemini_generated_video_0436C472.mp4"
out = "C:/Users/marku/AppData/Local/Temp/claude/pl/raw"; os.makedirs(out, exist_ok=True)
cap = cv2.VideoCapture(V); i = 0
while True:
    ok, f = cap.read()
    if not ok: break
    rgba = key_frame(f, cyan_safe=True)
    cv2.imwrite(f"{out}/{i:03d}.png", cv2.cvtColor(rgba, cv2.COLOR_RGBA2BGRA))
    i += 1
print(i)
