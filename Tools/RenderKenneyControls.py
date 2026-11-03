"""Re-export the unchanged official Kenney SVG artwork. Build-time tool only.
Install: python -m pip install resvg-py
Run from any directory: python Tools/RenderKenneyControls.py
"""
from pathlib import Path
import resvg_py
root=Path(__file__).resolve().parents[1]/'Assets/SharedUI/Sprites/Controls'
for source in sorted((root/'Source').glob('*.svg')):
    (root/(source.stem+'.png')).write_bytes(resvg_py.svg_to_bytes(svg_path=str(source),width=512,height=512))
    print(source.stem)
