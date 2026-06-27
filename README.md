# Dellarte Della Guerra

Dellarte Della Guerra (DADG) is an upcoming mod for *Mount and Blade II: Bannerlord*, setting the stage for the English War of the Roses in 1471, opposing the House of York and Lancaster for the English crown.

## Acknowledgements

The mod will integrate and build upon features for jousts, firearms, and cannons from **The Old Realms** mod. You can find the original implementation and details here: [The Old Realms Mod](https://github.com/TheOldRealms/TOR_Core/tree/development). 

We believe that open-sourcing our codebase will also enable other mods to build upon it effectively. However, please note that all XML and assets related to this mod are not available publicly.

## Planned Features

See our [open issues](https://github.com/DellarteDellaGuerraTeam/DellarteDellaGuerra.Core/labels/enhancement) for a list of features we plan to implement.

## Dependencies

- **Bannerlord.Harmony**: Required for patching and mod integration capabilities.
- **POC Colour Randomiser Mod**: Essential for customising heraldry, coats of arms, and liveries.
- **Bannerlord.ExpandedTemplate**: Utilised for implementing siege templates and managing equipment pools effectively.
- **BUTR Bannerlord Template**: Used for development purposes.

## Contributing

We welcome contributions! If you would like to help improve DADG, feel free to join our [Discord](https://discord.gg/ZVye6tDqyC).

## Agent Tooling

This repository uses [Microsoft APM](https://github.com/microsoft/apm) as the source of truth for agent instructions, skills, hooks, and MCP server configuration.

Install APM, then run this from `src/`:

```powershell
apm install
```

APM generates the local tooling for supported agent clients such as Codex and Claude. Generated client folders like `src/.agents/`, `src/.claude/`, and `src/.codex/` are ignored; edit `src/apm.yml` and `src/.apm/` instead.

## Licence

This mod is released under the [GPL V3](LICENSE).
