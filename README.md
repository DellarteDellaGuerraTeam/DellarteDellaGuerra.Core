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

This repository uses [Microsoft APM](https://github.com/microsoft/apm) to deploy agent instructions, skills, hooks, and MCP server configuration. The agent harness lives in two APM packages: the generic [bannerlord-agent](https://github.com/JoeFwd/bannerlord-agent) (skills, playtester agent, hooks, MCP bridge, Bannerlord knowledge brain) and the DADG layer [dadg-agent](https://github.com/JoeFwd/dadg-agent) (DADG skills, settings, DADG knowledge brain), which depends on a tagged bannerlord-agent release. Both are submodules under `src/submodules/`; `src/apm.yml` depends on `dadg-agent` and declares the MCP servers.

Install APM, then run this from the repository root:

```powershell
git submodule update --init src/submodules/dadg-agent src/submodules/bannerlord-agent
cd src
apm install
```

Windows long paths must be enabled (`LongPathsEnabled` = 1), since APM stages the package under this repository's already deep Steam path. APM may also ask for GitHub credentials while looking up an organisation policy; cancelling the prompt does not affect the install.

APM generates the local tooling for supported agent clients such as Codex and Claude. Generated client folders like `src/.agents/`, `src/.claude/`, and `src/.codex/` are ignored. Edit DADG tooling in `src/submodules/dadg-agent` (commit and push it there), then rerun `apm install`. Generic tooling is edited in `src/submodules/bannerlord-agent`; release it by pushing a new tag, bumping the `JoeFwd/bannerlord-agent#<tag>` dependency in dadg-agent's `apm.yml`, and rerunning `apm install`. Edit MCP servers in `src/apm.yml`.

Both packages ship a `brain/` of Markdown notes (start at `brain/INDEX.md`). Open `src/submodules/` as an Obsidian vault to browse them with working links between the two brains.

## Licence

This mod is released under the [GPL V3](LICENSE).
