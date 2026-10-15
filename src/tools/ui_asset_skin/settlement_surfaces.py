"""Settlement-specific source artwork; native sprite registration stays unchanged."""
from functools import lru_cache
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageOps


def _curve(start, control1, control2, end, steps=96):
    return [tuple((1-t)**3 * start[axis] + 3*(1-t)**2*t * control1[axis]
                  + 3*(1-t)*t*t * control2[axis] + t**3 * end[axis]
                  for axis in (0, 1)) for t in (i/steps for i in range(steps+1))]


def _metallic_path(image, points, width=4.8):
    """Antialiased bronze edge, warm gold body and narrow pale highlight."""
    scale = 4
    overlay = Image.new('RGBA', (image.width*scale, image.height*scale))
    draw = ImageDraw.Draw(overlay)
    coordinates = [(round(x*scale), round(y*scale)) for x, y in points]
    for thickness, color in ((width, '#69400E'), (width-0.8, '#B47A21'),
                             (width-1.7, '#E5AE3F'), (width-2.8, '#F5D47B')):
        draw.line(coordinates, fill=color, width=max(1, round(thickness*scale)), joint='curve')
    draw.line([(round((x-0.45)*scale), round((y-0.45)*scale)) for x, y in points],
              fill='#FFF0AB', width=2, joint='curve')
    alpha = image.getchannel('A')
    image.alpha_composite(overlay.resize(image.size, Image.Resampling.LANCZOS))
    image.putalpha(alpha)
    return image


def info_header(native, baseline):
    if native.size != (999, 100):
        raise ValueError('Re-audit settlement info header geometry after native changes')
    return _metallic_path(baseline.copy(), [(2, 2), (996, 2), (996, 85), (2, 85), (2, 2)])


def group_frame(native):
    if native.size != (375, 87):
        raise ValueError('Re-audit settlement category frame geometry after native changes')
    result = ImageOps.colorize(ImageOps.grayscale(native), '#1E160A', '#F1D17C').convert('RGBA')
    result.putalpha(native.getchannel('A'))
    contour = [(25, -1), (25, 12)] + _curve((25, 12), (25, 40), (45, 61), (75, 61))[1:]
    return _metallic_path(result, contour + [(375, 61)])


def rounded_header(native, baseline, gilded=False):
    """Replace the thresholded native corner with smooth, continuous gold strokes."""
    if native.size != (967, 168):
        raise ValueError('Re-audit settlement header geometry after native changes')
    scale = 4
    size = tuple(value * scale for value in native.size)
    outer = [(9, -1), (9, 94)] + _curve((9, 94), (9, 132), (31, 156), (65, 156))[1:]
    outer += [(967, 156)]
    silhouette = Image.new('L', size, 0)
    ImageDraw.Draw(silhouette).polygon(
        [(x*scale, y*scale) for x, y in outer + [(967, -1)]], fill=255)
    result = ImageOps.colorize(ImageOps.grayscale(native), '#07172a', '#2870a9').convert('RGBA')
    # The native corner contains the old raster ornament and a dark bevel.
    # Reuse adjacent header texture beneath the new strokes so those lines do
    # not remain as doubled blue ghosts beside the smooth gold contours.
    clean = result.crop((100, 60, 200, 156))
    blend = Image.new('L', clean.size)
    blend.putdata([round((255 if x < 80 else 255*(99-x)/19) * min(1, y/12))
                   for y in range(clean.height) for x in range(clean.width)])
    result.paste(clean, (0, 60), blend)
    strokes = Image.new('RGBA', size)
    draw = ImageDraw.Draw(strokes)

    def line(points, width, color):
        draw.line([(round(x*scale), round(y*scale)) for x,y in points],
                  fill=color, width=round(width*scale), joint='curve')

    # Gold follows the native outline; the two inset ornamental contours are
    # traced at subpixel resolution instead of thresholding their raster highlights.
    line(outer, 2.5, '#997026')
    line([(10.2, -1), (10.2, 94)] + _curve((10.2, 94), (10.2, 131), (31, 154.8), (65, 154.8))[1:]
         + [(967, 154.8)], 1.1, '#E7CA77')
    upper = [(15, 71)] + _curve((15, 71), (20, 77), (25, 80), (25, 91))[1:] + [(25, 110)]
    upper += _curve((25, 110), (21, 108), (18, 106), (15, 105))[1:]
    lower = [(16, 109)] + _curve((16, 109), (24, 113), (27, 112), (27, 121))[1:] + [(27, 132)]
    tail = [(32, 120)] + _curve((32, 120), (40, 138), (54, 148), (71, 150))[1:]
    for contour in (upper, lower, tail):
        # Keep a solid gold core after UI downscaling, with the same warm edge
        # and pale highlight as the outer bevel rather than a faint flat line.
        line(contour, 3.2, '#69400E')
        line(contour, 2.4, '#E5AE3F')
        line([(x-0.3, y-0.3) for x, y in contour], 1.1, '#FFF0AB')
    strokes = strokes.resize(native.size, Image.Resampling.LANCZOS)
    result.alpha_composite(strokes)
    # Keep the opposite end and native straight-edge silhouette. Only the
    # rounded left corner needs its pixel staircase replaced by a smooth curve.
    alpha = native.getchannel('A').point(lambda value: 255 if value >= 128 else 0)
    alpha = alpha.filter(ImageFilter.GaussianBlur(0.55))
    curved_alpha = silhouette.resize(native.size, Image.Resampling.LANCZOS)
    alpha.paste(curved_alpha.crop((0, 0, 80, native.height)), (0, 0))
    result.putalpha(alpha)
    baseline = baseline.copy()
    baseline.paste(result.crop((0, 0, 100, native.height)), (0, 0))
    if gilded:
        contour = [(11, -1), (11, 94)] + _curve((11, 94), (11, 131), (31, 154), (65, 154))[1:]
        _metallic_path(baseline, contour + [(967, 154)])
    return baseline


@lru_cache(maxsize=1)
def _recruitment_fill():
    modules = Path(__file__).resolve().parents[4]
    owner = Image.open(modules / 'DellarteDellaGuerraMap/GUI/DadgRecruitmentSpriteParts/'
                       'ui_dadg_recruitment/DADG/Recruitment/owner.png').convert('RGBA')
    return owner.crop((2, 2, owner.width-2, owner.height-2))


def recruitment_portrait(native):
    """Use the approved recruitment portrait gradient, including for empty slots."""
    result = _recruitment_fill().resize(native.size, Image.Resampling.LANCZOS)
    result.putalpha(native.getchannel('A'))
    return result
