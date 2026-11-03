"""Run from the project root after Unity's SharedUIExtraction.Run."""
from pathlib import Path
import re
import subprocess
import tarfile

root = Path(__file__).resolve().parents[2]

def tracked_bytes(path):
    return subprocess.check_output(['git', 'show', 'HEAD:' + path], cwd=root)

def guid(data):
    return re.search(rb'^guid: (\w+)', data, re.M).group(1)

for name in ['Level_01_Tutorial', 'Level_02_ColoredSwitches']:
    path = f'Assets/Scenes/{name}.unity'
    assert (root / path).read_bytes() == tracked_bytes(path), f'Scene changed: {name}'

moves = {
    'Assets/Scripts/WinSequence.cs.meta': 'Assets/SharedUI/Scripts/WinSequence.cs.meta',
    'Assets/Scripts/VisualUI/VisualIcon.cs.meta': 'Assets/SharedUI/Scripts/VisualIcon.cs.meta',
    'Assets/Scripts/VisualUI/HintSpotlight.cs.meta': 'Assets/SharedUI/Scripts/HintSpotlight.cs.meta',
}
for name in ['Blue', 'Red', 'Left', 'Right', 'Jump']:
    moves[f'Assets/Art/KenneyControls/{name}.png.meta'] = f'Assets/SharedUI/Sprites/Controls/{name}.png.meta'
for name in ['Arrow', 'Bulb', 'Hand', 'Panel', 'Replay', 'Ring', 'Smile', 'Star']:
    moves[f'Assets/Resources/KenneyUI/{name}.png.meta'] = f'Assets/SharedUI/Sprites/Resources/KenneyUI/{name}.png.meta'
for old, new in moves.items():
    assert guid(tracked_bytes(old)) == guid((root / new).read_bytes()), f'GUID changed: {new}'

prefabs = list((root / 'Assets/SharedUI/Prefabs').rglob('*.prefab'))
assert len(prefabs) == 25
for prefab in prefabs:
    content = prefab.read_text(encoding='utf8')
    assert 'm_MethodName: Jump' not in content
    assert 'm_MethodName: SetTouchMove' not in content

with tarfile.open(root / 'Exports/TogetherWeMove-SharedUI.unitypackage') as package:
    paths = [package.extractfile(m).read().decode('utf8') for m in package.getmembers() if m.name.endswith('/pathname')]
    assert not any(p.startswith(('Assets/Scripts/', 'Assets/Scenes/', 'Assets/Editor/', 'Assets/Art/')) for p in paths), 'Game dependency exported'
    assert 'Assets/SharedUI/Scripts/SharedUI.Runtime.asmdef' in paths
print(f'PASS: 2 unchanged scenes; {len(moves)} preserved GUIDs; {len(prefabs)} reusable prefabs; package excludes game code/scenes.')
