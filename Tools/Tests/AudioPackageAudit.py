"""Audit the exported package and the recorded license/download manifest."""
from pathlib import Path
import hashlib
import json
import tarfile

root = Path(__file__).resolve().parents[2]
package = root / 'Exports/SharedGameAudio.unitypackage'
manifest = json.loads((root / 'Assets/SharedGameAudio/Licenses/AudioManifest.json').read_text())
assert len(manifest) == 21
for item in manifest:
    assert item['license'] == 'CC0-1.0', item
    assert hashlib.sha256((root / item['file']).read_bytes()).hexdigest() == item['sha256'], item['file']

count = 0
with tarfile.open(package, 'r:gz') as archive:
    paths = []
    for entry in archive.getmembers():
        if not entry.name.endswith('/pathname'):
            continue
        path = archive.extractfile(entry).read().decode()
        assert path.startswith('Assets/SharedGameAudio'), path
        paths.append(path)
        prefix = entry.name.rsplit('/', 1)[0]
        try:
            asset = archive.getmember(prefix + '/asset')
        except KeyError:
            continue  # Folder entry.
        if asset.isfile():
            assert archive.extractfile(asset).read() == (root / path).read_bytes(), 'Stale package: ' + path
            count += 1
    for expected in ['README.md', 'AUDIO_CREDITS.md', 'Resources/SharedGameAudio/AudioManager.prefab',
                     'Resources/SharedGameAudio/AudioSettings.prefab', 'Resources/SharedGameAudio/SharedAudio.mixer',
                     'Resources/SharedGameAudio/SoundLibrary.asset']:
        assert 'Assets/SharedGameAudio/' + expected in paths, expected

print(f'PASS: {len(manifest)} CC0 audio hashes; {count} current packaged files; no external asset paths; required deliverables present.')
