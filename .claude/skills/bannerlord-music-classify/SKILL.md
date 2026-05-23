---
name: bannerlord-music-classify
description: Analyse an audio file, classify it for Bannerlord's PSAI soundtrack system (Battle, Losing Battle, Campaign, or Dramatic Campaign), update the corresponding segment in soundtrack.xml with computed BPM, beat timing, sample rates, and other metadata, then convert to OGG and deploy. Use this skill whenever the user wants to add a music track to Bannerlord, classify audio for the PSAI soundtrack system, update a soundtrack.xml segment, or says things like "classify this track", "add this music to Bannerlord", "update the soundtrack xml", "what kind of music is this for PSAI", or "register this ogg in the soundtrack".
---

# bannerlord-music-classify

Analyse an audio file in its original format, classify it for Bannerlord's PSAI soundtrack system, update `soundtrack.xml`, then convert to OGG and deploy as the very last step.

> **Why OGG conversion is last**: analysing the original file (MP3, WAV, FLAC…) gives the most accurate BPM, beat times, and spectral features. Converting to a lossy OGG first and then analysing the result degrades those measurements. The OGG is purely a deployment artefact and is produced only after all metadata has been committed to the XML.

## Usage

```
/bannerlord-music-classify --file <path/to/audio.mp3> --soundtrack <path/to/soundtrack.xml>
```

- `--file` — absolute path to the source audio file (MP3, WAV, FLAC, OGG, etc.)
- `--soundtrack` — absolute path to the PSAI `soundtrack.xml`

If either argument is missing, stop and tell the user which argument is missing before proceeding.

## Instructions

The user has invoked this skill with: $ARGUMENTS

Parse `--file` and `--soundtrack` from the arguments.

---

### Step 1: Analyse the audio

Run `mcp__music-analysis__full_analysis` on the **original** `--file` path (not a converted copy).

This single tool returns all needed data: duration, tempo, beat times, key, spectral features, and MFCCs.

---

### Step 2: Classify the music

Using the results, evaluate two independent axes.

#### Field paths in the analysis JSON

| Metric                  | JSON path                          |
|-------------------------|------------------------------------|
| tempo (BPM)             | `rhythm.tempo_bpm`                 |
| beat times              | `rhythm.beat_times_seconds`        |
| duration (seconds)      | `duration.seconds`                 |
| sample rate             | `sample_rate_hz`                   |
| RMS energy              | `spectral.rms_energy`              |
| zero-crossing rate      | `spectral.zero_crossing_rate`      |
| spectral centroid       | `spectral.centroid_hz`             |
| estimated key           | `harmony.estimated_key`            |

#### Name-based pre-classification (check first)

Before applying the metric rules, inspect the **filename** of `--file` for strong semantic signals:

| If the filename contains (case-insensitive) | Likely classification |
|---|---|
| "siege", "assault", "storm" | Battle music (siege) |
| "battle", "combat", "fight", "skirmish" | Battle music |
| "retreat", "rout", "fallen", "defeat", "lose" | Losing battle music |
| "march", "campaign", "travel", "road" | Campaign music |
| "dark", "dread", "lament", "elegy", "dirge" | Dramatic campaign music |

If the filename gives a clear signal, state the name-based suggestion alongside the metric-based result. If they disagree, **flag the conflict explicitly** and ask the user to confirm before continuing.

#### Energy axis — Action vs Chill

- **Action** if: `bpm > 140` OR (`bpm > 100` AND `rms_energy > 0.12` AND `zero_crossing_rate > 0.03`)
- **Chill** otherwise

> **Double-tempo warning (orchestral tracks):** librosa's beat tracker frequently locks onto the subdivision rather than the true pulse of composed orchestral music, reporting 2× the felt tempo. This is especially likely when **all three** of the following are true:
> - Detected BPM is in the range 100–200
> - The filename or classification suggests a theme, march, lament, or campaign piece (i.e. not frantic combat)
> - `spectral.centroid_hz < 2500` (orchestral texture, not electronic/percussive)
>
> When these conditions hold, **always flag the potential double-tempo to the user** and ask them to confirm the felt tempo before continuing. If the user confirms the BPM is doubled, set `confirmed_bpm = round(tempo_bpm / 2)` and use that in place of `bpm` for all beat-length calculations in Step 4 (`beat_length_samples`, `pre_beats`, `post_beats`). The sample-position values (`pre_beat_samples`, `post_beat_samples`, `total_samples`) are derived from timestamps and are never affected by tempo correction.
>
> **Note on long tracks (> 5 min):** do NOT split the file before analysis. librosa's beat tracker already estimates time-varying tempo at each point in the signal, so splitting introduces cut-point artefacts without any accuracy benefit. Run `full_analysis` on the complete original file.

#### Drama axis — Dramatic vs Standard

Score one point for each (max 3):
- Estimated key is **minor** → +1
- `spectral.centroid_hz < 1500` (dark / bass-heavy) → +1
- `spectral.zero_crossing_rate < 0.03` (very smooth, sustained texture) → +1

**Dramatic** if score ≥ 2, **Standard** if score < 2.

#### Final classification table

| Energy | Drama    | Bannerlord role               |
|--------|----------|-------------------------------|
| Action | Standard | **Battle music**              |
| Action | Dramatic | **Losing battle music**       |
| Chill  | Standard | **Campaign music**            |
| Chill  | Dramatic | **Dramatic campaign music**   |

Present the classification with all supporting metric values, the name-based signal (if any), and any confidence caveats, then **ask the user to confirm or override the classification before continuing to Step 3**.

---

### Step 3: Determine path format

Before computing values, inspect the soundtrack XML to determine what path format PSAI expects. Grep for existing `<Path>` values:

```
pattern: <Path>[^<]+</Path>
```

Count results with a subdirectory prefix (e.g. `PC/filename.wav`) vs bare basenames. If the **majority** use a prefix, apply the same prefix to the OGG basename derived from `--file`. If results are mixed, bare, or absent, use the bare basename only.

Derive `ogg_basename` = basename of `--file` with the extension replaced by `.ogg`.

Set `audio_filename` = `<prefix>/ogg_basename` (or just `ogg_basename` if no prefix).

---

### Step 4: Compute PSAI segment fields

Calculate the following from the analysis results (which were run on the original file):

```
sample_rate         = sample_rate_hz                                    (from full_analysis)
bpm                 = round(rhythm.tempo_bpm)  [or confirmed_bpm if double-tempo was corrected]
total_samples       = round(duration.seconds × sample_rate)
first_beat_time     = rhythm.beat_times_seconds[0]
last_beat_time      = rhythm.beat_times_seconds[-1]
pre_beat_samples    = round(first_beat_time × sample_rate)
post_beat_samples   = max(0, round((duration.seconds − last_beat_time) × sample_rate))
beat_length_samples = round((60 / bpm) × sample_rate)
pre_beats           = 0 if pre_beat_samples == 0
                      else max(1, round(pre_beat_samples / beat_length_samples))
post_beats          = round(post_beat_samples / beat_length_samples)
```

Notes:
- `post_beat_samples` is clamped to 0 — the analyser can return a last beat timestamp that slightly overshoots the actual duration.
- `pre_beats` has a floor of 1 when there is a non-zero pre-beat; if `pre_beat_samples` is 0 (track starts exactly on beat 1), `pre_beats` must also be 0 to avoid declaring a pre-beat with no samples.
- `post_beats` has no floor — zero is valid.

Display all computed values (including `audio_filename`) in a table before making any edits.

---

### Step 5: Identify the target segment

> The soundtrack XML is too large to read in full. Use Grep for all lookups.

#### 5a — Scan theme names and IDs

Grep for `<ThemeTypeInt>` with 5 lines of context before each match to retrieve every theme's `<Name>` and `<Id>`:

```
pattern: <ThemeTypeInt>
context: 5 lines before
```

#### 5b — Match theme to the confirmed classification

| Classification          | Theme name must contain (case-insensitive)   | Exclude if name also contains      |
|-------------------------|----------------------------------------------|------------------------------------|
| **Battle music**        | "Battle", "Combat", "Fight", "Action"        | —                                  |
| **Losing battle music** | "Lose", "Loss", "Defeat", "Retreat"          | —                                  |
| **Campaign music**      | "Main", "Campaign", "Travel"                 | "Dark", "Dramatic", "Lose"         |
| **Dramatic campaign**   | "Dark", "Dramatic", "Tension", "Dread"       | —                                  |

If no name match is found, fall back to `<ThemeTypeInt>` value. Known values observed in Bannerlord PSAI files:
- `7` — Main Theme
- `1` — Dark / tense campaign variant
- `3` — Battle

If still ambiguous after both passes, list all candidate themes and **ask the user to choose one** before proceeding.

#### 5c — Identify the specific segment within the theme

Grep for `<ThemeId>X</ThemeId>` (where X is the matched theme's `<Id>`) with **40 lines of before-context**:

```
pattern: <ThemeId>X</ThemeId>
context: 40 lines before
```

Within each context window:
- Locate the `<Name>` that appears **after the most recent `<Segment>` opening tag** — this is the segment's name. Ignore any `<Name>` before the last `<Segment>` tag, as it belongs to a preceding segment.
- Note the current value of `<CalculatePostAndPrebeatLengthBasedOnBeats>` — if it is `true`, it must be set to `false` in Step 6 (both inside `<AudioData>` and at segment level), otherwise PSAI silently ignores all manually provided sample values.

If there is exactly one segment, use it. If there are multiple, **list all segment names and ask the user which one to update** before proceeding.

---

### Step 6: Update the segment

#### 6a — Update audio fields

Update all audio fields in the identified segment. The segment contains two copies of most fields — one nested inside `<AudioData>` and one at the `<Segment>` level directly. Both must be updated.

> **Edit uniqueness requirement**: The XML file contains hundreds of segments with identical field values (e.g. many segments share `<Bpm>100</Bpm>`, `<SampleRate>44100</SampleRate>`). Every Edit `old_string` must include **at least 3–4 surrounding lines** of context to make the match unique to the target segment. Never edit a single-line field in isolation.

**Inside `<AudioData>`** — edit as one block covering all fields to guarantee uniqueness:

| Field | New value |
|---|---|
| `<_prebeatLengthInSamplesEnteredManually>` | `pre_beat_samples` |
| `<_postbeatLengthInSamplesEnteredManually>` | `post_beat_samples` |
| `<Path>` | `audio_filename` |
| `<Bpm>` | `bpm` |
| `<PreBeatLengthInSamples>` | `pre_beat_samples` |
| `<PostBeatLengthInSamples>` | `post_beat_samples` |
| `<TotalLengthInSamples>` | `total_samples` |
| `<SampleRate>` | `sample_rate` |
| `<CalculatePostAndPrebeatLengthBasedOnBeats>` | `false` (only if currently `true`) |

**On the `<Segment>` itself (outside `<AudioData>`)** — similarly edit as one block:

| Field | New value |
|---|---|
| `<PreBeatLengthInSamples>` | `pre_beat_samples` |
| `<PostBeatLengthInSamples>` | `post_beat_samples` |
| `<PreBeats>` | `pre_beats` |
| `<PostBeats>` | `post_beats` |
| `<Bpm>` | `bpm` |
| `<SampleRate>` | `sample_rate` |
| `<CalculatePostAndPrebeatLengthBasedOnBeats>` | `false` (only if currently `true`) |

**Do NOT change:**
- `<Name>` — segment identity; may be referenced by name elsewhere in the project
- `<Id>` — numeric key used in cross-references throughout the XML (linked/blocked segment lists, bridges)
- `<ThemeId>` — binds segment to its parent theme; changing it would migrate the segment to a different theme
- `<BitsPerSample>` — decoded PCM format, not derivable from analysis; OGG always decodes to 16-bit
- `<Intensity>` — PSAI scheduling parameter (0.0–1.0); design decision, unrelated to audio file properties
- `<IsUsableAtStart>`, `<IsUsableInMiddle>`, `<IsUsableAtEnd>` — structural placement rules; design decisions
- `<DefaultCompatibiltyAsFollower>` — transition permission setting; design decision
- `<Serialization_ManuallyBlocked/Linked/BridgeSegmentIds>` — hand-authored transition rules; destroying them would remove intentional compatibility constraints
- `<IsAutomaticBridgeSegment>` — marks segment as a structural bridge; changing it breaks transition architecture

#### 6b — Verify

Grep for `<Name>SEGMENT_NAME</Name>` (the full tag, not just the value) with 40 lines of after-context to re-read the updated block and confirm all changed fields show their new values. Display a final summary table of all changed fields alongside the confirmed music classification.

---

### Step 7: Convert to OGG and deploy

Only after the XML has been verified in Step 6b, convert the original file to OGG (Opus) and place it where the soundtrack expects it.

Derive `ogg_dest`:
- Take the directory of `--soundtrack`
- Append the path prefix used in `audio_filename` (e.g. `PC/`)
- Append `ogg_basename`

Use **OGG Vorbis** (not Opus) with the original sample rate. Bannerlord's PSAI reads OGG Vorbis natively. Opus internally forces 48 kHz regardless of `-ar`, so using libopus would silently break all sample-count timing computed from the original file.

```bash
ffmpeg -y -i "<--file>" -vn -c:a libvorbis -q:a 6 -ar <sample_rate> "<ogg_dest>"
```

- If `--file` is already a `.ogg` file, copy it to `ogg_dest` unchanged (do not re-encode).
- If ffmpeg fails, show the full error. The XML edit is already done; the user must resolve the conversion separately.
- On success, report the final OGG path and file size.
