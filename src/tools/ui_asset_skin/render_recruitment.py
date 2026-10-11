"""Render the approved code-native preview as text-free Gauntlet sprite inputs.

Only writes an isolated generator module; never invokes the generator on the live mod.
"""
import argparse
import hashlib
import json
import re
import shutil
from pathlib import Path

from playwright.sync_api import sync_playwright


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--review', type=Path, required=True)
    parser.add_argument('--module', type=Path, required=True)
    args = parser.parse_args()
    review = args.review.resolve()
    module = args.module.resolve()
    assert '.codex-work' in module.parts, 'Generator output must remain in staging'
    source = Path(__file__).with_name('preview.html').read_text(encoding='utf-8')
    styles = re.search(r'<style>(.*?)</style>', source, re.S).group(1)
    gilding = re.search(r'<svg class="outer-gilding".*?</svg>\s*</pattern>.*?</svg>', source, re.S).group(0)
    parts = module / 'GUI/SpriteParts/ui_dadg_recruitment/DADG/Recruitment'
    parts.mkdir(parents=True, exist_ok=True)
    shutil.copy2(Path(__file__).with_name('RecruitmentConfig.xml'), module / 'GUI/SpriteParts/Config.xml')
    samples = {
        'panel': (1500, 960, gilding),
        'title': (592, 161, '<div class="recruit-title"></div>'),
        'left': (1122, 780, '<div class="recruit-left"></div>'),
        'right': (248, 780, '<div class="recruit-right"></div>'),
        'troop': (124, 91, '<div class="slot"></div>'),
        'cart': (172, 81, '<div class="slot"></div>'),
        'owner': (134, 99, '<div class="slot"></div>'),
    }
    manifest = {'sprites': {}, 'preview_sha256': hashlib.sha256(source.encode()).hexdigest()}
    with sync_playwright() as playwright:
        browser = playwright.chromium.launch(channel='chrome', headless=True)
        page = browser.new_page(viewport={'width': 1600, 'height': 1000}, device_scale_factor=1)
        # Keep a file origin so Chrome permits the existing local source artwork.
        page.goto((review / 'index.html').as_uri(), wait_until='load')
        failures = []
        page.on('requestfailed', lambda request: failures.append(request.url))
        for name, (width, height, content) in samples.items():
            background = "background-image:url('preview/panel-dadg.png')" if name == 'panel' else 'background:none'
            page.set_content(f'''<html><head><base href="{review.as_uri()}/"><style>{styles}
                html,body{{margin:0;background:transparent}}
                #asset{{width:{width}px;height:{height}px;{background}}}
                #asset .recruit-title,#asset .recruit-left,#asset .recruit-right{{left:0;top:0;width:100%;height:100%;padding:0}}
                .slot{{box-sizing:border-box;width:100%;height:100%;background:linear-gradient(145deg,#0b2038,#061426);border:1px solid #d19a32;box-shadow:inset 0 0 0 1px #f5d675}}
                </style></head><body><div id="asset" class="panel-sample" data-artwork="dadg">{content}</div></body></html>''', wait_until='load')
            page.locator('#asset').screenshot(path=str(parts / f'{name}.png'), omit_background=True)
            assert not failures, f'Source artwork failed to load: {failures}'
            manifest['sprites'][name] = {'size': [width, height], 'sha256': hashlib.sha256((parts / f'{name}.png').read_bytes()).hexdigest()}
        browser.close()
    (module / 'render-manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')
    print(json.dumps(manifest, indent=2))


if __name__ == '__main__':
    main()
