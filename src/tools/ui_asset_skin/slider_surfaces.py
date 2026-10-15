"""Crisp gold artwork inside the native standard-slider sprite footprint."""
from PIL import Image, ImageDraw


def gold_slider_frame(native):
    if native.size != (400, 65):
        raise ValueError('Re-audit standard slider geometry after native changes')
    scale = 4
    result = Image.new('RGBA', (400 * scale, 65 * scale))
    draw = ImageDraw.Draw(result)
    # Trace the native straight rails and pointed ornamental ends, without
    # inheriting the low-frequency translucent gold haze of the raster source.
    main = [(44, 17), (355, 17), (359, 15), (368, 21), (379, 33),
            (368, 45), (359, 51), (355, 49), (44, 49), (40, 51),
            (31, 45), (20, 33), (31, 21), (40, 15), (44, 17)]
    left = [(32, 18), (24, 18), (11, 33), (24, 48), (32, 48)]
    right = [(367, 18), (375, 18), (388, 33), (375, 48), (367, 48)]
    for points in (left, right, main):
        coords = [(round(x * scale), round(y * scale)) for x, y in points]
        for width, color in ((5, '#684314'), (3.8, '#BC862C'),
                             (2.6, '#E8BD58'), (1.2, '#FFF0AD')):
            draw.line(coords, fill=color, width=round(width * scale), joint='curve')
    return result.resize(native.size, Image.Resampling.LANCZOS)
