# POC Integration

## What is POC?

POC is a Bannerlord mod allowing to freely set specific or randomised colours to banners and shields for given kingdoms, clans and units. For instance, companions or unit types can have their own colours and banners.

For more information, check out [POC's nexus page](https://www.nexusmods.com/mountandblade2bannerlord/mods/792?tab=description).

## DADG Use Cases

### Configuration

The highly customisable mod uses a unique configuration file named `config.json` which is expected to be at its root module folder. POC does not natively read the configuration file anywhere else.

This causes third party mods needing to fully integrate the mod in its module folder to rely on an external POC instance. It also does not support the Steam Workshop.

So in order to kill two birds with one stone, we implemented a [Harmony patch](../src/DellarteDellaGuerra.Infrastructure/Poc/Patches/PocConfigReaderOverriderPatch.cs) addressing both issues.

It detects whether POC is integrated in DADG or is in an external module and overrides the configuration path to a custom location. For now, the POC configuration is expected to be in DellarteDellaGuerra config folder and named `poc.config.json`.

You can check out [DADG's configuration in the repository](../config/poc.config.json).

### Heraldry

All English clans and kingdoms have a custom banner. We use POC to set custom banners as Native enforces all clans to have the same background colour as their kingdom.

Custom banners can be created with the [online Banner Editor](https://bannerlord.party/banner).

![Heraldry](https://github.com/user-attachments/assets/c9345b7f-17b3-4470-9cfa-2d981b634249)

### Randomised Colour Clothing

POC enables Bannerlord armour pieces to have random colours. It allows to have many variations of an armour piece. It also allows to have many civilian clothes.

![Battle](https://github.com/user-attachments/assets/70c23828-1e22-4b35-9c92-7cb2ba347998)

![Town](https://github.com/user-attachments/assets/a916d246-df76-4388-81b2-78f48e07758d)
