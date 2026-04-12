# AngelPanel Core Release Checklist

## Before packaging

- Confirm all Editor scripts compile in a clean VRChat Worlds project
- Confirm `2dAngel/AngelPanel/Main Panel` opens without errors
- Confirm AP Config, About, PolyCount, QuickOps, Debug, Shader, Occlusion, and Occlusion Rooms open
- Confirm no external tool bridge placeholder file is left in Core by mistake
- Confirm user-facing pages do not contain leftover developer-facing explanation text

## Package prep

- Move the final package content into a package root layout
- Keep `package.json` at package root
- Keep Editor-only code under `Editor/`
- Verify asmdef names and references
- Update README and changelog for the actual release
- Set the final release zip URL in `package.json`

## Listing prep

- Generate release zip
- Compute zip SHA256
- Update `source.template.json` into the real repository listing entry
- Push release artifacts to the package host
- Update the repository listing URL if changed

## Final validation

- Add the repository to VCC in a clean environment
- Install AP Core into a clean VRChat world project
- Reopen Unity and verify package import stability
- Confirm package updates cleanly from the previous test version


## Branding lock

- Author name: `E-Mommy`
- Brand / publisher: `2d Angel`
- Booth store label: `2d Angel VRC`
- Source root remains `Assets/2dAngel/AngelPanel`


## Release lock

- Release name: `Angel Panel 1.0`
- Public GitHub: `https://github.com/AssenCypher/AngelPanel`
- VPM GitHub: `https://github.com/AssenCypher/AngelPanel-VPM`
- Booth: `https://2dangel.booth.pm/`
- Source root remains `Assets/2dAngel/AngelPanel`
