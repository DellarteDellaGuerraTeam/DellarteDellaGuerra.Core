# Scotland datasource calibration

## Model

The bundled script fits an affine model from every control in `assets/scotland-calibration.json`:

```text
mapX = a*longitude + b*latitude + c
mapY = d*longitude + e*latitude + f
```

With the current 14 Scottish controls, the fitted coefficients are approximately:

```text
mapX = 61.3766923*longitude + 1.6663922*latitude + 702.2535773
mapY = -0.5265783*longitude + 116.6090928*latitude - 5817.4742161
```

The in-sample mean residual is about 0.89 DADG map units and the maximum is about 2.97 at Ayr. Run `scripts/place_settlements.py --calibration-report` for the authoritative current report.

## Why the previous projection failed

The previous transform was fitted from eight English controls. It looked acceptable in eastern England but accumulated structured error in western and northwestern Scotland. A later correction also treated `CROWN` and `ISLES` folders as mainland/island classifications. They are political groupings: Inchnadamph is a MacLeod/Isles record but is geographically on mainland Assynt.

The corrected model uses distributed, reviewed Scottish controls. Threave is present in the Crown datasource as `Threave Castle`; it is not an invented anchor.

## Updating calibration

Add a control only when both sides are independently known:

- longitude/latitude must come from a retained datasource record;
- map coordinates must be a user-confirmed DADG placement;
- retain the settlement id, exact datasource name, datasource filename, and provenance.

After changing controls, run the calibration report and inspect every residual. Prefer distributed controls over many points from one region. A control's current DADG position is not automatically authoritative.

Do not encode terrain-validity nudges as geographic controls. The projection and the final movement onto valid terrain are separate vectors.
