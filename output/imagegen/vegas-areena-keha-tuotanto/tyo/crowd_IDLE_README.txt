CROWD IDLE TOOLING

Run from PowerShell:
  & ./crowd_idle.ps1 -BasePath ../panoraama-natiivi.png -RegionsPath ./crowd-regions.json -OutputDirectory ../idle -MaskPreviewOnly
  & ./crowd_idle.ps1 -BasePath ../panoraama-natiivi.png -RegionsPath ./crowd-regions.json -OutputDirectory ../idle -SavePanoramaFrames

Input master must be15000x1884 for final output. The script never enlarges or downsizes production artwork.
Example region coordinates are placeholders and MUST be replaced by inspected person-interior polygons or ellipses on the final native master.
Use one person id per spectator. Separate head, chest and optional hands. Regions must lie INSIDE the person's silhouette; do not include seats, rails, ropes, clothing/background gaps, furniture or architecture. The tool holds region boundaries fixed, tapers displacement tozero at boundaries and samples from the original native master each frame. This produces tiny head/shoulder/hand movements without erasing or repainting the background.
Conservative interior deformation is intentional: this is a light idle, not new applause poses, mouth animation or large fullbody swaying. Hair and silhouette contours are held fixed. If full silhouette motion is required, supply extracted crowd RGBA and a cleanplate instead; this renderer does not pretend an exact fullperson extraction exists.
Ellipse form: [centerX,centerY,radiusX,radiusY]. Polygon form: list of [x,y] points. Optional person.origin offsets local tile annotations. Person phase is radians; absent phase uses deterministic golden-angle stagger. Regions of one person share phase unless phaseOffset is supplied.
Optional excludePolygons stay fixed even if they intersect a person annotation. Use them for any foreground ropes or rails crossing the human.
Mask previews and numbered annotation should be inspected BEFORE final render. Only mask pixels may differ between frames. All sixframes are recomputed from one immutable native source and use equally spaced phases of a sine/cosine loop, so frame6->frame1 is an ordinary cyclic step.
Exports:18 RGB PNGs,18 matching RGBA crowd-interior overlays,3 grayscale masks, qa-manifest.json. Optional6 fullmaster PNGs. All5K panel frames are cropped after world-space animation, so panel joins stay consistent during motion. Frame duration defaults200ms (5fps,1.2s loop).
The full RGB frame equals the static source with the sparse RGBA overlay blended over it, to within rounding of8bit compositing. The unmodified source contains the original people; overlay transparency is for the moving interior details, not a separate fullbody crowd cutout.
