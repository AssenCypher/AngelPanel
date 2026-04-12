# AngelPanel Core

AngelPanel Core is the free AP host shell for VRChat world workflows. This release is Angel Panel 1.0.

- Author: E-Mommy
- Brand: 2d Angel
- Booth: 2d Angel VRC
- Public GitHub: https://github.com/AssenCypher/AngelPanel
- VPM GitHub: https://github.com/AssenCypher/AngelPanel-VPM
- Booth URL: https://2dangel.booth.pm/

## Included

- AP Host shell
- PolyCount workspace
- QuickOps base tools
- AP Config
- About / DLC status
- Optimizing pages
  - Debug
  - Shader
  - Occlusion
  - Occlusion Rooms

## Intended role

AP Core is the platform layer.

It should stay clean, stable, and free. Standalone tools and paid products can connect into AP later through the AP bridge layer instead of being hardwired into Core.

## Current structure

- Core
- Optimizing
- Info
- Tools appears only when external tool modules are installed

## Supported baseline

- Unity 2022.3
- VRChat Worlds package 3.7.6 or newer

## Install

### VCC Community Repository

1. Add the 2dAngel community repository to VCC.
2. Open your VRChat world project.
3. Add `AngelPanel Core` from the package list.
4. Wait for Unity import to finish.

### Local User Package

1. Open VCC Settings.
2. Go to Packages.
3. Add the local package folder that contains this `package.json`.
4. Add `AngelPanel Core` to your project.

## Menu

After import, open:

`2dAngel/AngelPanel/Main Panel`

## External products

- ProbeTools Free — Unreleased
- APK Free — Unreleased
- AP Lighting Tools — Unreleased
- APVS / ISO Zone — Unreleased
- Area Tools — Unreleased
- LockSystem — Unreleased
- Terrain Optimizer — Unreleased
- Point Cloud System — Unreleased
- APK Pro — Unreleased
- Hierarchy Tools — Unreleased

## Notes for packaging

This folder is the release skeleton for the VPM package.

Source development stays under `Assets/2dAngel/AngelPanel`. Keep the runtime/editor workflow aligned with this asset-root structure, then export packaging artifacts from there when you prepare a release.

The package repository is intended to be exposed through the AngelPanel-VPM community listing.