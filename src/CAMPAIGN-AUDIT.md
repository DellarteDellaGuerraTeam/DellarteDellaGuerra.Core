# Campaign audit module

[Bannerlord.CampaignAudit](https://github.com/JoeFwd/Bannerlord.CampaignAudit) is a standalone optional module, pinned by the `src/submodules/CampaignAudit` Git submodule. It owns its source, tests, schema, module manifest and self-contained SQLite writer. DADG does not load its assemblies into Core or require it for normal play.

From the DADG repository root:

```powershell
git submodule update --init src/submodules/CampaignAudit
```

From `src`:

```powershell
./tools/deploy-campaign-audit.ps1
# Or another game installation:
./tools/deploy-campaign-audit.ps1 -GameDirectory 'D:/Games/Bannerlord'
```

This builds and deploys `Modules/CampaignAudit`, including its separate .NET 8 writer. Existing `audit.json` settings and databases are preserved. Enable **Campaign Balance Audit** after Sandbox and your gameplay modules in the launcher. For a direct `_MODULES_` launch, append `*CampaignAudit` inside the module list. It has no hard dependency on DADG or NavalDLC.

The native fast-forward multiplier defaults to **15** when the recorder is enabled. Daily and manual snapshots plus AI simulation aggregates are stored in SQLite. Usage, schema and current validation limits are in [the recorder README](submodules/CampaignAudit/README.md). It must be selected explicitly; ordinary DADG builds and launch profiles do not silently enable campaign recording or faster time.
