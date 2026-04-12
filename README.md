<div align="center">

# AngelPanel Core

**Free AP Core host shell for VRChat world creation under 2d Angel**

![Version](https://img.shields.io/badge/version-1.0.0-4c8bf5)
![Unity](https://img.shields.io/badge/Unity-2022.3-black)
![VRChat Worlds](https://img.shields.io/badge/VRCSDK%20Worlds-%3E%3D%203.7.6-00bcd4)
![Package](https://img.shields.io/badge/package-com.2dangel.angelpanel.core-8e44ad)

**Author:** E-Mommy  
**Brand:** 2d Angel  
**Booth Store:** 2d Angel VRC

</div>

---

## Language / 言語 / 언어 / 语言

[English](#english) · [日本語](#ja) · [한국어](#ko) · [简体中文](#zh-cn) · [繁體中文](#zh-tw)

---

## Quick Links

- [Public GitHub Repository](https://github.com/AssenCypher/AngelPanel)
- [VPM Repository](https://github.com/AssenCypher/AngelPanel-VPM)
- [VPM Releases](https://github.com/AssenCypher/AngelPanel-VPM/releases)
- [Booth Store](https://2dangel.booth.pm/)
- [Changelog](./CHANGELOG.md)
- [License](./LICENSE.md)

---

## Screenshot Placeholders

> Replace these with your final screenshot paths after upload.

```md
<!-- Main host window -->
![AngelPanel Main Panel](docs/images/main-panel.png)

<!-- Detached PolyCount window -->
![PolyCount Workspace](docs/images/polycount-workspace.png)

<!-- Debug page -->
![Debug Page](docs/images/debug-page.png)

<!-- Shader page -->
![Shader Tools](docs/images/shader-page.png)

<!-- Occlusion page -->
![Occlusion Tools](docs/images/occlusion-page.png)
```

---

<a id="english"></a>
# English

## Overview

**AngelPanel Core** is the free editor-side host shell of the AngelPanel ecosystem.

This 1.0.0 release is focused on giving VRChat world creators a clean and expandable AP base: a host window, a detachable PolyCount workspace, QuickOps base utilities, configuration and package detection, About / DLC status, and an initial Optimizing section with Debug, Shader, Occlusion, and Occlusion Rooms pages.

At this stage, **AP Core is intentionally editor-first**. The current package is built around Unity Editor tooling, host-side UI, module registration, and workflow scaffolding rather than runtime gameplay systems.

## What is included in AngelPanel Core 1.0.0

- **AP Host Shell**
  - Main host window under the 2dAngel menu.
  - Dynamic page system organized into **Core**, **Optimizing**, **Tools**, and **Info** areas.
  - Expandable module architecture so later AP modules and standalone tools can register into the host instead of being hardwired into Core.

- **PolyCount Workspace**
  - Embedded inside the main panel.
  - Can also be opened as a **detached dedicated window**.
  - Supports **Simple** and **Advanced** workspace modes.
  - Tracks selection triangle count and cached scene totals.
  - Supports total count refresh, active-only totals, cache clear, threshold colors, font scaling, and optional realtime refresh.

- **QuickOps Base Tools**
  - Selection-scoped utility block integrated into the workspace.
  - Includes collider operations, script cleanup, and LOD cleanup.
  - Built with Undo support and confirmation flow for destructive actions.

- **AP Config**
  - Shell layout configuration.
  - Navigation placement, adaptive/fixed scaling, overflow behavior, width, button height, and font size.
  - Integration detector for common world-production packages.

- **About / DLC Status**
  - Core identity page.
  - Installed module listing.
  - Suggested / missing product catalog.
  - Capability and path visibility for host-side inspection.

- **Optimizing Pages**
  - **Debug**
  - **Shader**
  - **Occlusion**
  - **Occlusion Rooms**

- **Tools Platform Entry**
  - The Tools area is reserved for external AP-compatible tools.
  - It appears when external tool modules are actually available.

## Menu Path

After installation, open AngelPanel from the Unity top menu:

`2dAngel -> AngelPanel -> Main Panel`  
`2dAngel -> AngelPanel -> PolyCount Workspace`

## Installation

### Option A — VCC / Community Repository

1. Add the **2d Angel** VPM community repository to VCC.
2. Open your VRChat world project.
3. Add **AngelPanel Core** to the project.
4. Wait for Unity import and script compilation to finish.
5. Open the panel from `2dAngel -> AngelPanel`.

### Option B — Local package / development package

1. Open your project in VCC or Unity Package Manager workflow.
2. Add the package that contains this `package.json`.
3. Import / resolve packages.
4. Open the panel from `2dAngel -> AngelPanel`.

## Supported Baseline

- **Unity:** 2022.3
- **VRChat Worlds SDK:** `>= 3.7.6`
- **Package Name:** `com.2dangel.angelpanel.core`

## Core Feature Breakdown

### 1. AP Host Shell

AngelPanel Core is not just one window with a few buttons. It is the base host layer of the AP ecosystem.

Current host behavior includes:

- Dynamic registration of pages and modules.
- Core and non-core module separation.
- Visibility control for host pages, About entries, installed module lists, and external tool surfaces.
- Capability registration so later packages can declare what they provide.
- Localization provider registration so future modules can extend AP without rewriting the host.

This means AP Core is meant to stay **clean, stable, and free**, while future standalone tools and paid products can connect into the host through the bridge layer.

### 2. PolyCount Workspace

The PolyCount Workspace is one of the main pillars of this release.

It currently supports:

- **Selection triangle counting** based on distinct selected roots.
- **Scene total triangle counting** across the active scene.
- Optional counting behavior for:
  - inactive objects,
  - disabled renderers.
- **Simple mode** for fast reading.
- **Advanced mode** for more detailed control.
- **Detached PolyCount window** for a dedicated workflow.
- **Color thresholds** for both selection count and total count.
- Adjustable:
  - selected font size,
  - total font size,
  - selected base color,
  - total base color,
  - threshold lists and colors.
- **Realtime total refresh** with configurable interval.
- **Cache state visibility** and manual refresh buttons.
- Saved settings through AP Core storage.

This page is built to support quick world-side triangle auditing without leaving the AP host workflow.

### 3. QuickOps

QuickOps is embedded directly into the workspace and is currently divided into three base sections.

#### Collider

- Count collider components in the current selection scope.
- Remove all colliders in scope.
- Add **BoxCollider** to eligible targets.
- Add **MeshCollider** to eligible targets.
- Add **Convex MeshCollider** to eligible targets.
- Supports working on roots only or including children.

How it behaves:

- BoxCollider creation uses mesh bounds or renderer bounds when possible.
- MeshCollider creation only runs on targets that do not already have a collider and that expose a valid source mesh.
- All actions are Undo-friendly.

#### Script Cleanup

- Count missing script slots.
- Remove missing script slots in scope.
- Remove live MonoBehaviour components in scope.
- Uses confirmation prompts for destructive actions.

This makes it useful for prefab cleanup, import cleanup, and fast prep before world packaging.

#### LOD

- Detect LODGroups in the current scope.
- Remove LODGroup components while keeping the highest LOD renderer set enabled.
- Lower LOD renderers are disabled instead of silently kept active.

This is especially useful when importing assets that were authored for a different LOD strategy than your final VRChat world workflow.

### 4. Debug Page

The Debug page is split into multiple audits.

#### Basic Scan

The Basic Scan can work on:

- the active scene,
- current selection,
- or a manually assigned root.

It scans for:

- missing scripts,
- missing prefab references,
- UdonBehaviour usage,
- UdonSharpBehaviour usage,
- VRCPickup usage,
- VRCObjectSync usage,
- shader buckets grouped by usage.

It also gives quick result selection actions so you can jump straight to the relevant objects.

#### Event Audit

The Event Audit is designed to catch common high-frequency callback risks that quietly scale with object count.

Tracked callbacks currently include:

- `Update`
- `LateUpdate`
- `FixedUpdate`
- `OnGUI`
- `OnWillRenderObject`
- `OnRenderObject`
- `OnPreCull`
- `OnPreRender`
- `OnPostRender`
- `OnRenderImage`
- `OnAudioFilterRead`
- `OnAnimatorMove`
- `OnAnimatorIK`

The audit groups results by callback type, counts matched components, and lets you select either the affected scene objects or the related script assets.

#### Script Asset Audit

This audit works directly on selected script assets.

It scans the chosen scripts for the tracked hot callbacks and gives you a quick script-side review path before you even jump back into scene objects.

### 5. Shader Page

The Shader page in AP Core already goes beyond a simple note list.

#### Material Scan & Replace

Current functionality includes:

- Scan scope options:
  - Scene,
  - Selection,
  - Both.
- Source shader filter.
- Target shader selection.
- Dry Run mode.
- Scan Standard materials.
- Scan selected hierarchy.
- Scan selected hierarchy filtered by a source shader.
- Replace the current scanned result set.
- Direct replace on current selected hierarchy.
- Result list with material count and per-material usage count.
- One-click material selection / ping workflow.

When applying a shader swap, the tool captures and reapplies a practical set of common properties, including:

- common colors,
- common floats,
- common textures,
- texture scale,
- texture offset.

That makes it much more useful than a blind shader reassignment pass.

#### Selected Hierarchy Audit

The page also audits the current selection and reports:

- selected root count,
- renderer count,
- material slot count,
- unique material count,
- unique shader count,
- shader usage summary inside the selected hierarchy.

#### Shader Library & Install Entry

AngelPanel Core 1.0.0 also includes a built-in shader library surface with installed-state detection and access links for common world-side shader families, including:

- Poiyomi
- lilToon
- Graphlit / z3y
- Filamented
- Silent Cel Shading Shader
- Mochies Unity Shaders
- Orels Unity Shaders
- UnlitWF
- ACLS
- RealToon
- GeneLit
- Unity Shaders Plus
- Quantum Shader

Depending on the entry, the page can expose:

- repo links,
- guide links,
- VPM listing links,
- store links,
- UPM Git installation entry,
- installed / missing status,
- practical compatibility notes such as VRCLV / MonoSH / SH-RNM markers when defined.

### 6. Occlusion Page

The Occlusion page is aimed at making Unity occlusion workflow less blind.

Current features include:

- **Suggestion profiles**:
  - Safe,
  - Balanced,
  - Fast.
- Analyze static samples and suggest:
  - smallest occluder,
  - smallest hole,
  - backface threshold.
- Copy the generated suggestion report.
- Apply or clear occlusion-related static flags.
- Work on either the current selection or the broader scene scope.
- Create **OcclusionArea** objects from current selection bounds.
- Expand area bounds by a percentage.
- Clear all generated occlusion areas.
- Open Unity’s Occlusion Culling window.
- Try to start background bake when the current Unity version supports it.

This makes it a practical helper page for scene-side occlusion preparation rather than just a passive note page.

### 7. Occlusion Rooms Page

Occlusion Rooms is a more workflow-specific page that generates candidate occlusion view volumes from **camera-occupiable interior space**.

It currently supports:

- selection-only or scene-wide analysis,
- voxel size control,
- eye height,
- minimum headroom,
- ceiling search height,
- minimum floor normal threshold,
- optional ceiling requirement for indoor bias,
- floor merge tolerance,
- ceiling merge tolerance,
- minimum cells per generated volume,
- XZ and Y padding.

Generation behavior:

- scans the volume of the scene using raycast-based floor/ceiling sampling,
- detects occupiable indoor cells,
- clusters them,
- creates candidate **OcclusionArea** view volumes,
- places them under a generated `__OcclusionRooms` parent.

This is especially useful in indoor scenes where colliders are already reasonably clean and where you want a faster first pass on occlusion room authoring.

### 8. AP Config

AP Config is the shell-side customization page for the host itself.

Configurable items include:

- navigation placement:
  - left sidebar,
  - right sidebar,
  - top bar,
  - bottom bar,
- shell scaling:
  - adaptive,
  - fixed,
- navigation overflow mode:
  - adaptive narrow buttons,
  - scroll strip,
- sidebar width,
- default button height,
- default font size.

The page also includes **integration status detection** for common world-production packages:

- VRCSDK Worlds
- UdonSharp
- Bakery
- Magic Light Probes
- VRCLightVolumes
- VRCLV Manager

This is useful both as a setup check and as a quick environment sanity page.

### 9. About / DLC Status

The About page is more than a branding page.

It includes:

- core product summary,
- author / version / community / store / support identity,
- installed core and external module visibility,
- missing / suggested module catalog,
- capability visibility,
- asset/config/localization path display,
- direct links to GitHub, VPM repo, and Booth.

It is also where the current AP ecosystem is framed clearly: Core stays free and stable, while additional tools can arrive as separate modules, paid products, or standalone releases.

### 10. Tools Surface and External Module Bridge

The current package already includes the groundwork for AP’s modular tool ecosystem.

That includes:

- module registration,
- module removal,
- external tool manifests,
- capability registry,
- localization provider registration,
- a Tools page that surfaces installed and recommended external tools.

Current suggested ecosystem entries exposed through the catalog include:

- ProbeTools Free
- APK Free
- AP Lighting Tools
- APVS / ISO Zone
- Area Tools
- LockSystem
- APK Pro
- Terrain Optimizer
- Point Cloud System
- Hierarchy Tools

Not all of these are released in Core. They are cataloged so the host can present the ecosystem direction clearly.

## Notes

- **Current release role:** editor-side host shell.
- **Tools tab visibility:** appears when compatible external tools are installed or registered.
- **Runtime systems:** not the focus of the current Core package.
- **Project structure direction:** AP Core is designed to remain a clean platform layer rather than absorb every future system directly.

## License

Please refer to [LICENSE.md](./LICENSE.md).

This package currently states that it is **all rights reserved unless otherwise stated by 2dAngel**.

---

<a id="ja"></a>
# 日本語

## 概要

**AngelPanel Core** は、AngelPanel エコシステムのための無料ホストシェルです。  
VRChat ワールド制作向けに、**クリーンで拡張可能な AP の土台**を提供することを目的としています。

1.0.0 では、以下のエディタ側機能が含まれています。

- AP ホストウィンドウ
- PolyCount Workspace
- QuickOps 基礎ツール
- AP Config
- About / DLC Status
- Optimizing セクション
  - Debug
  - Shader
  - Occlusion
  - Occlusion Rooms

現時点の AP Core は、**ランタイム機能よりも Unity Editor ワークフローを優先**しています。つまり、このパッケージはゲームプレイ用システムではなく、ホスト UI、登録基盤、制作補助、整理、監査のための基礎層です。

## メニュー

インストール後は Unity 上部メニューから開きます。

`2dAngel -> AngelPanel -> Main Panel`  
`2dAngel -> AngelPanel -> PolyCount Workspace`

## 導入方法

### VCC / Community Repository

1. VCC に **2d Angel** のコミュニティリポジトリを追加します。
2. VRChat ワールドプロジェクトを開きます。
3. **AngelPanel Core** を追加します。
4. Unity のインポートとコンパイル完了後、`2dAngel -> AngelPanel` から開きます。

### ローカルパッケージ

1. `package.json` を含むパッケージを追加します。
2. 依存解決後、`2dAngel -> AngelPanel` から開きます。

## 対応環境

- **Unity:** 2022.3
- **VRCSDK Worlds:** `>= 3.7.6`
- **Package Name:** `com.2dangel.angelpanel.core`

## 主な機能

### 1. AP Host Shell

AP Core は単なるツール集ではなく、今後の AP モジュール群を受け入れる**ホスト基盤**です。

現在のホスト機能:

- モジュール登録 / 削除
- Core / 外部モジュールの分離
- Host 表示 / About 表示 / Installed Modules 表示制御
- Capability 登録
- Localization Provider 登録
- 将来の外部ツールを Tools 領域へ統合するための橋渡し

つまり AP Core は、**無料で安定したベース層**として保ち、追加ツールは後から接続していく設計です。

### 2. PolyCount Workspace

このリリースの中心機能のひとつです。

対応内容:

- 選択中オブジェクトの三角形数集計
- アクティブシーン全体の三角形数集計
- inactive object を含める / 除外する設定
- disabled renderer を含める / 除外する設定
- **Simple / Advanced** モード切替
- **独立 PolyCount ウィンドウ**
- 選択数 / 総数それぞれの閾値カラー設定
- フォントサイズ調整
- 総数のリアルタイム更新
- キャッシュ状態表示と手動再計測
- 設定の保存

ワールド制作中に、ホストを離れず素早く面数確認したい人向けの設計です。

### 3. QuickOps

QuickOps は Workspace 内に統合されており、現在は 3 つの基礎セクションがあります。

#### Collider

- Collider 数のカウント
- 範囲内 Collider 一括削除
- BoxCollider 追加
- MeshCollider 追加
- Convex MeshCollider 追加
- root のみ / 子階層含む の切替

#### Script Cleanup

- Missing Script slot の検出
- Missing Script slot の削除
- Live MonoBehaviour の削除
- 危険操作には確認ダイアログあり

#### LOD

- LODGroup の検出
- LODGroup を削除しつつ最高 LOD の renderer を保持
- 下位 LOD renderer を無効化

インポート直後の整理や、VRChat 向けの最終調整で便利です。

### 4. Debug

Debug ページは複数の監査ページに分かれています。

#### Basic Scan

スキャン範囲:

- シーン全体
- 現在選択中
- 手動指定 Root

検出対象:

- Missing Scripts
- Missing Prefab References
- UdonBehaviour
- UdonSharpBehaviour
- VRCPickup
- VRCObjectSync
- Shader usage buckets

結果から該当オブジェクトを直接選択できます。

#### Event Audit

オブジェクト数に応じて負荷が膨らみやすい高頻度コールバックを監査します。

対象イベント:

- `Update`
- `LateUpdate`
- `FixedUpdate`
- `OnGUI`
- `OnWillRenderObject`
- `OnRenderObject`
- `OnPreCull`
- `OnPreRender`
- `OnPostRender`
- `OnRenderImage`
- `OnAudioFilterRead`
- `OnAnimatorMove`
- `OnAnimatorIK`

イベント種別ごとに、該当コンポーネント数・オブジェクト数・型数を整理し、オブジェクトまたはスクリプトへジャンプできます。

#### Script Asset Audit

選択中のスクリプトアセット自体を監査し、上記ホットコールバックが含まれているかを確認します。

### 5. Shader

Shader ページは、単なるリンク集ではありません。

#### Material Scan & Replace

- Scene / Selection / Both のスキャン範囲
- Source Shader Filter
- Target Shader 指定
- Dry Run
- Standard 材質スキャン
- 選択階層スキャン
- Source Shader に一致する選択階層のみスキャン
- 現在の結果セットへまとめて適用
- 選択階層へ直接置換
- 結果材質の選択 / 定位

Shader 置換時には、共通的な色・float・texture・texture scale・offset を保持するようになっており、単純な blind replace より実用的です。

#### Selected Hierarchy Audit

- Selected Roots
- Renderers
- Material Slots
- Unique Materials
- Unique Shaders
- 選択範囲内 Shader 使用状況

#### Shader Library & Install Entry

以下の代表的なワールド向け Shader 群に対して、導入状態確認とリンク導線を用意しています。

- Poiyomi
- lilToon
- Graphlit / z3y
- Filamented
- Silent Cel Shading Shader
- Mochies Unity Shaders
- Orels Unity Shaders
- UnlitWF
- ACLS
- RealToon
- GeneLit
- Unity Shaders Plus
- Quantum Shader

項目によって以下が利用できます。

- Repo
- Guide
- Listing
- Store
- UPM Git install
- Installed / Missing 状態
- VRCLV / MonoSH / SH-RNM 補助表示

### 6. Occlusion

Occlusion ページでは、Unity のオクルージョン設定作業を少しでも見通しやすくするための支援を行います。

含まれる内容:

- Suggestion Profile
  - Safe
  - Balanced
  - Fast
- static sample 分析
- smallest occluder / smallest hole / backface threshold 提案
- レポートのコピー
- Occluder / Occludee static flag の付与・解除
- Selection Only 対応
- 選択範囲から OcclusionArea 作成
- Bounds 拡張率
- Area 一括削除
- Occlusion Culling ウィンドウを開く
- Unity バージョン対応時は Background Bake を試行

### 7. Occlusion Rooms

カメラが移動可能な室内空間から、候補となる **OcclusionArea view volume** を生成するページです。

調整可能項目:

- voxel size
- eye height
- minimum headroom
- max ceiling search
- minimum floor normal
- require ceiling
- floor / ceiling merge tolerance
- minimum cells per volume
- XZ / Y padding

動作概要:

- レイキャストベースで移動可能セルを検出
- 室内候補セルをクラスタ化
- 候補 volume を生成
- `__OcclusionRooms` 親オブジェクトの下へ配置

室内シーンで Collider が整っている場合に特に有効です。

### 8. AP Config

ホスト UI 自体の設定ページです。

設定可能項目:

- ナビゲーション位置
  - 左
  - 右
  - 上
  - 下
- Shell Scaling
  - Adaptive
  - Fixed
- Overflow Mode
  - Adaptive Narrow Buttons
  - Scroll Strip
- Sidebar Width
- Button Height
- Font Size

さらに以下の統合状態を検出します。

- VRCSDK Worlds
- UdonSharp
- Bakery
- Magic Light Probes
- VRCLightVolumes
- VRCLV Manager

### 9. About / DLC Status

ブランド表示だけではなく、AP 全体の位置づけを見せるページです。

- 製品概要
- 作者 / 版数 / Community / Store / Support
- Installed Modules
- Missing / Suggested Modules
- Capability 表示
- Asset / Config / Loc path 表示
- GitHub / VPM / Booth へのリンク

### 10. Tools と外部モジュール基盤

Core にはすでに外部ツール接続の基礎があります。

- Module registration
- Capability registry
- Localization provider registration
- External tool manifest
- Installed / Recommended tools surface

カタログ上の候補:

- ProbeTools Free
- APK Free
- AP Lighting Tools
- APVS / ISO Zone
- Area Tools
- LockSystem
- APK Pro
- Terrain Optimizer
- Point Cloud System
- Hierarchy Tools

これらすべてが Core に同梱されているわけではありません。AP エコシステムの進行方向をホスト側で明確に見せるための一覧です。

## 補足

- 現在の Core は **エディタ側ホストシェル**です。
- Tools タブは対応ツールが導入されると有効になります。
- ランタイム製品は現在の Core の主目的ではありません。
- Core は「全部入り」にせず、安定したベース層として保つ方針です。

---

<a id="ko"></a>
# 한국어

## 개요

**AngelPanel Core**는 AngelPanel 생태계를 위한 무료 호스트 셸입니다.  
VRChat 월드 제작을 위해 **깔끔하고 확장 가능한 AP 기반**을 제공하는 것이 목적입니다.

현재 1.0.0에는 다음과 같은 에디터 중심 기능이 포함되어 있습니다.

- AP 호스트 창
- PolyCount Workspace
- QuickOps 기본 도구
- AP Config
- About / DLC Status
- Optimizing 섹션
  - Debug
  - Shader
  - Occlusion
  - Occlusion Rooms

현재 버전의 AP Core는 **런타임 시스템보다 Unity Editor 워크플로우를 우선**합니다. 즉, 이 패키지는 게임플레이용 시스템이 아니라 호스트 UI, 모듈 등록 기반, 제작 보조, 점검, 정리용 플랫폼입니다.

## 메뉴 경로

설치 후 Unity 상단 메뉴에서 열 수 있습니다.

`2dAngel -> AngelPanel -> Main Panel`  
`2dAngel -> AngelPanel -> PolyCount Workspace`

## 설치

### VCC / 커뮤니티 저장소

1. VCC에 **2d Angel** VPM 커뮤니티 저장소를 추가합니다.
2. VRChat 월드 프로젝트를 엽니다.
3. **AngelPanel Core**를 프로젝트에 추가합니다.
4. Unity import 및 컴파일 완료 후 `2dAngel -> AngelPanel`에서 실행합니다.

### 로컬 패키지

1. `package.json`이 포함된 패키지를 추가합니다.
2. 패키지 해석이 끝나면 `2dAngel -> AngelPanel`에서 엽니다.

## 지원 기준

- **Unity:** 2022.3
- **VRCSDK Worlds:** `>= 3.7.6`
- **Package Name:** `com.2dangel.angelpanel.core`

## 주요 기능

### 1. AP Host Shell

AP Core는 단순한 툴 모음이 아니라, 앞으로의 AP 모듈을 받아들이는 **호스트 플랫폼**입니다.

현재 포함된 구조:

- 모듈 등록 / 제거
- Core / 외부 모듈 분리
- Host 표시 / About 표시 / Installed Modules 표시 제어
- Capability 등록
- Localization Provider 등록
- 외부 도구를 Tools 영역에 연결하기 위한 브리지

즉, AP Core는 **무료이면서 안정적인 베이스 레이어**로 유지되고, 이후 도구들은 모듈 형태로 연결되는 구조입니다.

### 2. PolyCount Workspace

이번 릴리스의 핵심 기능 중 하나입니다.

지원 내용:

- 선택 오브젝트 triangle 수 계산
- 활성 씬 전체 triangle 수 계산
- inactive object 포함 여부 설정
- disabled renderer 포함 여부 설정
- **Simple / Advanced** 모드
- **독립 PolyCount 창**
- 선택 수 / 총 수 각각의 threshold color 설정
- 폰트 크기 조절
- 실시간 total refresh
- 캐시 상태 표시 및 수동 갱신
- 설정 저장

월드 제작 중 빠르게 폴리 수를 확인하려는 제작자에게 맞춘 구조입니다.

### 3. QuickOps

QuickOps는 Workspace 안에 통합되어 있으며, 현재 3개의 기본 섹션으로 구성됩니다.

#### Collider

- Collider 개수 집계
- 범위 내 Collider 일괄 삭제
- BoxCollider 추가
- MeshCollider 추가
- Convex MeshCollider 추가
- root만 / 자식 포함 범위 전환

#### Script Cleanup

- Missing Script slot 검사
- Missing Script slot 삭제
- Live MonoBehaviour 삭제
- 파괴적 작업에는 확인 절차 포함

#### LOD

- LODGroup 검사
- LODGroup 제거 후 최고 LOD renderer 유지
- 하위 LOD renderer 비활성화

가져온 에셋을 VRChat 월드용으로 정리할 때 유용합니다.

### 4. Debug

Debug 페이지는 여러 감사 도구로 나뉘어 있습니다.

#### Basic Scan

스캔 범위:

- 현재 씬 전체
- 현재 선택 범위
- 수동 지정 Root

검사 항목:

- Missing Scripts
- Missing Prefab References
- UdonBehaviour
- UdonSharpBehaviour
- VRCPickup
- VRCObjectSync
- Shader usage buckets

결과 오브젝트를 바로 선택할 수 있습니다.

#### Event Audit

오브젝트 수가 늘어날수록 비용이 커질 수 있는 고빈도 콜백을 검사합니다.

현재 추적 콜백:

- `Update`
- `LateUpdate`
- `FixedUpdate`
- `OnGUI`
- `OnWillRenderObject`
- `OnRenderObject`
- `OnPreCull`
- `OnPreRender`
- `OnPostRender`
- `OnRenderImage`
- `OnAudioFilterRead`
- `OnAnimatorMove`
- `OnAnimatorIK`

콜백 종류별로 매칭 수, 오브젝트 수, 타입 수를 정리하며, 해당 오브젝트 또는 스크립트로 바로 이동할 수 있습니다.

#### Script Asset Audit

선택한 스크립트 에셋 자체를 검사해서 위 콜백이 들어 있는지 확인합니다.

### 5. Shader

Shader 페이지는 단순 링크 모음이 아닙니다.

#### Material Scan & Replace

- Scene / Selection / Both 범위
- Source Shader Filter
- Target Shader 지정
- Dry Run
- Standard material 스캔
- 선택 hierarchy 스캔
- 특정 source shader로 필터한 선택 hierarchy 스캔
- 현재 결과 세트에 일괄 적용
- 선택 hierarchy에 직접 치환
- 결과 material 선택 / ping

Shader 교체 시 공통 color, float, texture, texture scale, offset을 최대한 보존하도록 설계되어 있어 단순 blind replace보다 실용적입니다.

#### Selected Hierarchy Audit

- Selected Roots
- Renderers
- Material Slots
- Unique Materials
- Unique Shaders
- 선택 범위 내 Shader 사용 현황

#### Shader Library & Install Entry

다음과 같은 대표적인 월드용 Shader에 대해 설치 상태와 링크 진입점을 제공합니다.

- Poiyomi
- lilToon
- Graphlit / z3y
- Filamented
- Silent Cel Shading Shader
- Mochies Unity Shaders
- Orels Unity Shaders
- UnlitWF
- ACLS
- RealToon
- GeneLit
- Unity Shaders Plus
- Quantum Shader

항목에 따라 다음을 제공합니다.

- Repo
- Guide
- Listing
- Store
- UPM Git install
- Installed / Missing 상태
- VRCLV / MonoSH / SH-RNM 보조 표시

### 6. Occlusion

Occlusion 페이지는 Unity의 오클루전 작업을 덜 막막하게 만들어 주는 보조 페이지입니다.

포함 기능:

- Suggestion Profile
  - Safe
  - Balanced
  - Fast
- static sample 분석
- smallest occluder / smallest hole / backface threshold 제안
- 리포트 복사
- Occluder / Occludee static flag 설정 및 해제
- Selection Only 지원
- 선택 범위에서 OcclusionArea 생성
- Bounds 확장 비율
- Area 일괄 삭제
- Occlusion Culling 창 열기
- 지원 버전에서는 Background Bake 시도

### 7. Occlusion Rooms

카메라가 실제로 이동 가능한 실내 공간을 기준으로 **OcclusionArea view volume 후보**를 생성합니다.

조절 항목:

- voxel size
- eye height
- minimum headroom
- max ceiling search
- minimum floor normal
- require ceiling
- floor / ceiling merge tolerance
- minimum cells per volume
- XZ / Y padding

동작 개요:

- 레이캐스트로 이동 가능한 셀 탐색
- 실내 셀 클러스터링
- 후보 volume 생성
- `__OcclusionRooms` 부모 아래 배치

Collider가 비교적 잘 정리된 실내 월드에서 특히 유용합니다.

### 8. AP Config

호스트 UI 자체를 설정하는 페이지입니다.

설정 항목:

- 내비게이션 위치
  - 좌측
  - 우측
  - 상단
  - 하단
- Shell Scaling
  - Adaptive
  - Fixed
- Overflow Mode
  - Adaptive Narrow Buttons
  - Scroll Strip
- Sidebar Width
- Button Height
- Font Size

또한 다음 패키지 통합 상태를 감지합니다.

- VRCSDK Worlds
- UdonSharp
- Bakery
- Magic Light Probes
- VRCLightVolumes
- VRCLV Manager

### 9. About / DLC Status

단순한 브랜드 소개 페이지가 아니라 AP 전체 구조를 보여 주는 페이지입니다.

- 제품 요약
- 작성자 / 버전 / Community / Store / Support
- Installed Modules
- Missing / Suggested Modules
- Capability 표시
- Asset / Config / Loc 경로 표시
- GitHub / VPM / Booth 링크

### 10. Tools 및 외부 모듈 기반

Core에는 이미 외부 도구 연동을 위한 기반이 포함되어 있습니다.

- Module registration
- Capability registry
- Localization provider registration
- External tool manifest
- Installed / Recommended tools surface

카탈로그에 노출되는 후보:

- ProbeTools Free
- APK Free
- AP Lighting Tools
- APVS / ISO Zone
- Area Tools
- LockSystem
- APK Pro
- Terrain Optimizer
- Point Cloud System
- Hierarchy Tools

이 항목들이 모두 Core에 포함된 것은 아닙니다. 호스트에서 AP 생태계의 방향을 명확하게 보여 주기 위한 카탈로그입니다.

## 참고

- 현재 Core는 **에디터 중심 호스트 셸**입니다.
- Tools 탭은 호환 외부 도구가 설치되면 표시됩니다.
- 런타임 제품은 현재 Core의 중심이 아닙니다.
- Core는 모든 기능을 한 번에 넣기보다, 안정적인 플랫폼 층으로 유지되는 방향을 목표로 합니다.

---

<a id="zh-cn"></a>
# 简体中文

## 概述

**AngelPanel Core** 是 AngelPanel 生态的免费宿主外壳。  
它的目标不是把所有功能都硬塞进一个窗口，而是先提供一个**干净、稳定、可扩展**的 AP 平台层，再让后续模块和独立工具接入进来。

当前 1.0.0 版本主要提供的是**Unity 编辑器侧**能力，包括：

- AP 主宿主窗口
- PolyCount Workspace
- QuickOps 基础工具
- AP Config
- About / DLC Status
- Optimizing 分区
  - Debug
  - Shader
  - Occlusion
  - Occlusion Rooms

也就是说，这一版 AP Core 的重点是**编辑器工作流、模块接入、制作辅助、检查与整理**，而不是运行时互动系统。

## 菜单入口

安装完成后，可在 Unity 左上角菜单打开：

`2dAngel -> AngelPanel -> Main Panel`  
`2dAngel -> AngelPanel -> PolyCount Workspace`

## 安装方式

### 方式一：通过 VCC / Community Repository 安装

1. 在 VCC 中添加 **2d Angel** 的社区仓库。
2. 打开你的 VRChat World 项目。
3. 将 **AngelPanel Core** 添加到项目中。
4. 等待 Unity 导入与编译完成。
5. 从 `2dAngel -> AngelPanel` 打开面板。

### 方式二：作为本地包安装

1. 将包含 `package.json` 的包加入项目。
2. 等待包解析完成。
3. 从 `2dAngel -> AngelPanel` 打开面板。

## 支持基线

- **Unity：** 2022.3
- **VRChat Worlds SDK：** `>= 3.7.6`
- **包名：** `com.2dangel.angelpanel.core`

## 功能说明

### 1. AP Host Shell

AP Core 不是几个零散按钮的合集，而是整个 AngelPanel 的**宿主层**。

当前已经具备：

- 模块注册 / 卸载
- Core 与外部模块分层
- Host 页、About 页、Installed Modules 等显示控制
- Capability 能力注册
- Localization Provider 语言提供器注册
- 为外部工具接入 Tools 区做准备的桥接结构

这意味着 AP Core 会尽量保持**免费、稳定、干净**，而不把所有后续系统直接硬焊进 Core 里。

### 2. PolyCount Workspace

PolyCount Workspace 是这一版最核心的功能之一。

当前支持：

- 选中物体面数统计
- 活动场景总面数统计
- 可选是否将以下内容计入总数：
  - 未激活物体
  - 被禁用的 Renderer
- **Simple / Advanced** 双模式切换
- **独立 PolyCount 窗口**
- 选中值与总值分别使用不同阈值颜色
- 可调：
  - 选中数字字号
  - 总数字字号
  - 选中基础色
  - 总数基础色
  - 阈值列表和阈值颜色
- 总数实时刷新与刷新间隔设置
- 缓存状态显示、手动刷新、清空缓存
- 设置可保存到 AP Core 存储

它的目的就是让你在不离开 AP 宿主工作流的前提下，快速做世界面数审查。

### 3. QuickOps

QuickOps 已经直接整合在 Workspace 里面，目前包含 3 个基础分区。

#### Collider

- 统计当前范围内的 Collider 数量
- 一键移除范围内全部 Collider
- 给符合条件的目标添加 **BoxCollider**
- 给符合条件的目标添加 **MeshCollider**
- 给符合条件的目标添加 **Convex MeshCollider**
- 支持只处理根节点，或包含子物体

行为细节：

- BoxCollider 会尽量基于 Mesh Bounds 或 Renderer Bounds 生成。
- MeshCollider 只会添加到当前没有 Collider 且存在有效源 Mesh 的目标上。
- 所有操作都走 Undo。

#### Script Cleanup

- 统计 Missing Script 槽位
- 移除 Missing Script 槽位
- 移除范围内现有 MonoBehaviour 组件
- 破坏性操作带确认步骤

这对 prefab 清理、导入清理、上传前清理都很实用。

#### LOD

- 统计当前范围内的 LODGroup
- 移除 LODGroup，并保留最高级 LOD 的 Renderer 处于启用状态
- 低级 LOD 的 Renderer 会被禁用，而不是混乱地全部保留

适合处理那些原本为别的项目结构制作、但不适合你当前 VRChat 工作流的模型资源。

### 4. Debug 页面

Debug 页面目前分成多个审查子页。

#### Basic Scan

扫描范围支持：

- 当前场景
- 当前选中
- 手动指定根节点

可扫描内容包括：

- Missing Scripts
- Missing Prefab References
- UdonBehaviour
- UdonSharpBehaviour
- VRCPickup
- VRCObjectSync
- Shader 分桶统计

并且可以直接选中对应结果对象，方便你回到场景里处理。

#### Event Audit

这个页面的重点是找出那些会随着对象数量放大、悄悄吞掉性能的高频回调。

当前追踪的回调包括：

- `Update`
- `LateUpdate`
- `FixedUpdate`
- `OnGUI`
- `OnWillRenderObject`
- `OnRenderObject`
- `OnPreCull`
- `OnPreRender`
- `OnPostRender`
- `OnRenderImage`
- `OnAudioFilterRead`
- `OnAnimatorMove`
- `OnAnimatorIK`

结果会按回调类型聚合，显示命中的组件数、物体数、脚本类型数，并允许你直接选中物体或脚本资源。

#### Script Asset Audit

这个子页直接针对你选中的脚本资源本体做审计。

它会扫描选中的脚本里是否包含这些高频回调，方便你在回到场景前，先从代码资产层面排查问题。

### 5. Shader 页面

Shader 页面在这一版里已经不是单纯的链接列表。

#### Material Scan & Replace

当前支持：

- 扫描范围：
  - Scene
  - Selection
  - Both
- Source Shader Filter
- Target Shader 指定
- Dry Run 模式
- 扫描 Standard 材质
- 扫描当前选中层级
- 扫描选中层级中匹配源 Shader 的材质
- 将当前结果集统一替换到目标 Shader
- 直接对当前选中层级执行替换
- 结果列表、材质计数、使用次数统计
- 一键选中 / Ping 材质

在替换 Shader 时，它并不是盲改。当前实现会尽量保留一组常见属性，包括：

- 常见颜色
- 常见 float 参数
- 常见纹理
- 纹理缩放
- 纹理偏移

所以它比“直接改 shader 字段”更适合实际工程使用。

#### Selected Hierarchy Audit

Shader 页面还会对当前选中层级做统计：

- 选中根节点数
- Renderer 数量
- 材质槽位数
- 唯一材质数
- 唯一 Shader 数
- 当前选区内 Shader 使用情况摘要

#### Shader Library & Install Entry

当前内置的 Shader 库入口已经覆盖了一批常见世界向 Shader：

- Poiyomi
- lilToon
- Graphlit / z3y
- Filamented
- Silent Cel Shading Shader
- Mochies Unity Shaders
- Orels Unity Shaders
- UnlitWF
- ACLS
- RealToon
- GeneLit
- Unity Shaders Plus
- Quantum Shader

根据条目不同，页面可以提供：

- Repo 链接
- Guide 链接
- Listing 链接
- Store 链接
- UPM Git 安装入口
- 已安装 / 未安装状态
- VRCLV / MonoSH / SH-RNM 等兼容性标记

### 6. Occlusion 页面

Occlusion 页面是为了让 Unity 的遮挡剔除流程不再完全盲调。

当前支持：

- 建议档位：
  - Safe
  - Balanced
  - Fast
- 分析静态样本并给出：
  - Smallest Occluder
  - Smallest Hole
  - Backface Threshold
- 一键复制建议报告
- 批量设置或清除 Occluder / Occludee 静态标记
- 可切换只对选中范围生效
- 根据当前选中 Bounds 生成 **OcclusionArea**
- Bounds 外扩百分比
- 清理已生成的 OcclusionArea
- 打开 Unity 原生 Occlusion Culling 窗口
- 在当前 Unity 支持时尝试后台 Bake

### 7. Occlusion Rooms 页面

Occlusion Rooms 是一个更偏工作流的页面，它会根据**摄像机可活动的室内空间**，自动生成候选遮挡 View Volume。

当前支持参数：

- 仅选中 / 全场景分析
- Voxel Size
- Eye Height
- Min Headroom
- Max Ceiling Search
- Min Floor Normal
- 是否要求命中天花板
- Floor Merge Tolerance
- Ceiling Merge Tolerance
- Min Cells Per Volume
- XZ / Y Padding

它的工作方式是：

- 基于 Raycast 扫描可活动空间
- 找到室内候选格子
- 对这些格子做聚类
- 生成候选 **OcclusionArea** 体积
- 放到自动创建的 `__OcclusionRooms` 父节点下

这在室内场景、Collider 已经相对规范的世界里会很有价值。

### 8. AP Config

AP Config 是宿主自身的外观与行为配置页。

当前可配置：

- 导航位置：
  - 左侧栏
  - 右侧栏
  - 顶部栏
  - 底部栏
- Shell Scaling：
  - Adaptive
  - Fixed
- Overflow 模式：
  - Adaptive Narrow Buttons
  - Scroll Strip
- Sidebar Width
- 默认按钮高度
- 默认字体大小

同时它还会检测常见世界开发包的集成状态：

- VRCSDK Worlds
- UdonSharp
- Bakery
- Magic Light Probes
- VRCLightVolumes
- VRCLV Manager

这既可以当环境自检页，也可以当宿主布局调整页使用。

### 9. About / DLC Status

About 页不只是一个品牌页。

当前包含：

- Core 产品概览
- 作者 / 版本 / Community / Store / Support 信息
- 已安装模块列表
- 缺失 / 推荐模块目录
- Capability 可视化
- Asset / Config / Loc 路径显示
- GitHub / VPM Repo / Booth 直达链接

它同时也是对整个 AP 生态定位的说明：Core 负责保持免费和稳定，附加工具则可以以后续模块、付费产品或独立产品形式加入。

### 10. Tools 与外部模块桥接

这一版 Core 已经包含了 AP 模块化工具生态的基础结构，包括：

- 模块注册
- 模块移除
- 外部工具 Manifest
- Capability Registry
- Localization Provider 注册
- Tools 页面对已安装工具和推荐工具的承载能力

当前目录中已经预留 / 展示的生态条目包括：

- ProbeTools Free
- APK Free
- AP Lighting Tools
- APVS / ISO Zone
- Area Tools
- LockSystem
- APK Pro
- Terrain Optimizer
- Point Cloud System
- Hierarchy Tools

这些并不代表它们都已经随 Core 一起发布，而是说明 AP 宿主已经为后续生态接入预留好了位置。

## 说明

- **当前版本定位：** 编辑器侧宿主外壳。
- **Tools 标签何时出现：** 当兼容外部工具被安装或注册后。
- **运行时系统：** 不是当前 Core 的重点。
- **产品方向：** Core 尽量保持为稳定的平台层，而不是把所有后续系统全部塞进来。

---

<a id="zh-tw"></a>
# 繁體中文

## 概述

**AngelPanel Core** 是 AngelPanel 生態中的免費宿主外殼。  
它的目標不是把所有功能一次塞進同一個視窗，而是先建立一個**乾淨、穩定、可擴展**的 AP 平台層，再讓後續模組與獨立工具接入。

目前 1.0.0 版本主要提供的是**Unity 編輯器側**能力，包括：

- AP 主宿主視窗
- PolyCount Workspace
- QuickOps 基礎工具
- AP Config
- About / DLC Status
- Optimizing 分區
  - Debug
  - Shader
  - Occlusion
  - Occlusion Rooms

也就是說，這一版 AP Core 的重點是**編輯器工作流、模組接入、製作輔助、檢查與整理**，而不是執行時互動系統。

## 選單入口

安裝完成後，可在 Unity 左上角選單開啟：

`2dAngel -> AngelPanel -> Main Panel`  
`2dAngel -> AngelPanel -> PolyCount Workspace`

## 安裝方式

### 方式一：透過 VCC / Community Repository 安裝

1. 在 VCC 中加入 **2d Angel** 的社群倉庫。
2. 開啟你的 VRChat World 專案。
3. 將 **AngelPanel Core** 加入專案。
4. 等待 Unity 匯入與編譯完成。
5. 從 `2dAngel -> AngelPanel` 開啟面板。

### 方式二：作為本地套件安裝

1. 將包含 `package.json` 的套件加入專案。
2. 等待套件解析完成。
3. 從 `2dAngel -> AngelPanel` 開啟面板。

## 支援基線

- **Unity：** 2022.3
- **VRChat Worlds SDK：** `>= 3.7.6`
- **套件名稱：** `com.2dangel.angelpanel.core`

## 功能說明

### 1. AP Host Shell

AP Core 不是幾個零散按鈕的集合，而是整個 AngelPanel 的**宿主層**。

目前已具備：

- 模組註冊 / 移除
- Core 與外部模組分層
- Host 頁面、About 頁面、Installed Modules 等顯示控制
- Capability 能力註冊
- Localization Provider 語言提供器註冊
- 為外部工具接入 Tools 區預留的橋接結構

這代表 AP Core 會盡量維持**免費、穩定、乾淨**，而不是把所有後續系統直接硬綁進 Core。

### 2. PolyCount Workspace

PolyCount Workspace 是這一版最核心的功能之一。

目前支援：

- 選取物件面數統計
- 目前場景總面數統計
- 可選是否將以下內容納入總數：
  - 未啟用物件
  - 被停用的 Renderer
- **Simple / Advanced** 雙模式
- **獨立 PolyCount 視窗**
- 選取值與總值分別使用不同閾值顏色
- 可調：
  - 選取數字字級
  - 總數字字級
  - 選取基礎色
  - 總數基礎色
  - 閾值清單與顏色
- 總數即時刷新與刷新間隔設定
- 快取狀態顯示、手動刷新、清空快取
- 設定可保存到 AP Core 儲存層

它的目的，就是讓你在不離開 AP 宿主工作流的情況下，快速進行世界面數審查。

### 3. QuickOps

QuickOps 已直接整合在 Workspace 裡，目前包含 3 個基礎分區。

#### Collider

- 統計目前範圍內的 Collider 數量
- 一鍵移除範圍內全部 Collider
- 為符合條件的目標加入 **BoxCollider**
- 為符合條件的目標加入 **MeshCollider**
- 為符合條件的目標加入 **Convex MeshCollider**
- 支援只處理根節點，或包含子物件

行為細節：

- BoxCollider 會盡量根據 Mesh Bounds 或 Renderer Bounds 生成。
- MeshCollider 只會加入到目前沒有 Collider 且存在有效來源 Mesh 的目標上。
- 所有操作皆支援 Undo。

#### Script Cleanup

- 統計 Missing Script 槽位
- 移除 Missing Script 槽位
- 移除目前範圍內的 MonoBehaviour 元件
- 破壞性操作有確認步驟

這對 prefab 清理、導入後清理、上傳前整理都相當實用。

#### LOD

- 統計目前範圍內的 LODGroup
- 移除 LODGroup，並保留最高層 LOD 的 Renderer 為啟用狀態
- 較低層的 LOD Renderer 會被停用，而不是混亂地全部保留

適合處理原本為其他專案流程建立、但不適合目前 VRChat 世界工作流的模型資源。

### 4. Debug 頁面

Debug 頁面目前分成多個審查子頁。

#### Basic Scan

掃描範圍支援：

- 目前場景
- 目前選取
- 手動指定根節點

可掃描內容包括：

- Missing Scripts
- Missing Prefab References
- UdonBehaviour
- UdonSharpBehaviour
- VRCPickup
- VRCObjectSync
- Shader 分桶統計

並且可以直接選取對應結果物件，方便你回到場景中處理。

#### Event Audit

這個頁面的重點是找出那些會隨著物件數量增加、悄悄放大效能成本的高頻回呼。

目前追蹤的回呼包括：

- `Update`
- `LateUpdate`
- `FixedUpdate`
- `OnGUI`
- `OnWillRenderObject`
- `OnRenderObject`
- `OnPreCull`
- `OnPreRender`
- `OnPostRender`
- `OnRenderImage`
- `OnAudioFilterRead`
- `OnAnimatorMove`
- `OnAnimatorIK`

結果會依照回呼類型聚合，顯示命中的元件數、物件數、腳本型別數，並允許你直接選取物件或腳本資產。

#### Script Asset Audit

這個子頁直接針對你選取的腳本資產本體做審計。

它會掃描選取的腳本中是否包含這些高頻回呼，方便你在回到場景前先從程式碼資產層面排查。

### 5. Shader 頁面

Shader 頁面在這一版裡已經不只是單純的連結列表。

#### Material Scan & Replace

目前支援：

- 掃描範圍：
  - Scene
  - Selection
  - Both
- Source Shader Filter
- Target Shader 指定
- Dry Run 模式
- 掃描 Standard 材質
- 掃描目前選取層級
- 掃描選取層級中符合來源 Shader 的材質
- 將目前結果集統一替換為目標 Shader
- 直接對目前選取層級執行替換
- 結果列表、材質計數、使用次數統計
- 一鍵選取 / Ping 材質

在替換 Shader 時，它不是盲改。當前實作會盡量保留一組常見屬性，包括：

- 常見顏色
- 常見 float 參數
- 常見紋理
- 紋理縮放
- 紋理偏移

因此它比單純「直接改 shader 欄位」更適合實際專案使用。

#### Selected Hierarchy Audit

Shader 頁面也會對目前選取層級做統計：

- 選取根節點數
- Renderer 數量
- 材質槽位數
- 唯一材質數
- 唯一 Shader 數
- 目前選區內 Shader 使用情況摘要

#### Shader Library & Install Entry

目前內建的 Shader 庫入口已覆蓋一批常見世界向 Shader：

- Poiyomi
- lilToon
- Graphlit / z3y
- Filamented
- Silent Cel Shading Shader
- Mochies Unity Shaders
- Orels Unity Shaders
- UnlitWF
- ACLS
- RealToon
- GeneLit
- Unity Shaders Plus
- Quantum Shader

依條目不同，頁面可提供：

- Repo 連結
- Guide 連結
- Listing 連結
- Store 連結
- UPM Git 安裝入口
- 已安裝 / 未安裝狀態
- VRCLV / MonoSH / SH-RNM 等相容性標記

### 6. Occlusion 頁面

Occlusion 頁面是為了讓 Unity 的遮擋剔除流程不再完全靠盲調。

目前支援：

- 建議檔位：
  - Safe
  - Balanced
  - Fast
- 分析靜態樣本並給出：
  - Smallest Occluder
  - Smallest Hole
  - Backface Threshold
- 一鍵複製建議報告
- 批量設定或清除 Occluder / Occludee 靜態標記
- 可切換只對選取範圍生效
- 根據目前選取 Bounds 生成 **OcclusionArea**
- Bounds 外擴百分比
- 清理已生成的 OcclusionArea
- 開啟 Unity 原生 Occlusion Culling 視窗
- 在目前 Unity 支援時嘗試背景 Bake

### 7. Occlusion Rooms 頁面

Occlusion Rooms 是一個更偏向工作流的頁面，它會根據**攝影機可活動的室內空間**，自動生成候選遮擋 View Volume。

目前支援參數：

- 僅選取 / 全場景分析
- Voxel Size
- Eye Height
- Min Headroom
- Max Ceiling Search
- Min Floor Normal
- 是否要求命中天花板
- Floor Merge Tolerance
- Ceiling Merge Tolerance
- Min Cells Per Volume
- XZ / Y Padding

它的工作方式是：

- 以 Raycast 掃描可活動空間
- 找出室內候選格子
- 對這些格子做聚類
- 生成候選 **OcclusionArea** 體積
- 放到自動建立的 `__OcclusionRooms` 父節點底下

這在室內場景、Collider 已經相對規範的世界裡會很有價值。

### 8. AP Config

AP Config 是宿主本身的外觀與行為設定頁。

目前可設定：

- 導覽位置：
  - 左側欄
  - 右側欄
  - 頂部欄
  - 底部欄
- Shell Scaling：
  - Adaptive
  - Fixed
- Overflow 模式：
  - Adaptive Narrow Buttons
  - Scroll Strip
- Sidebar Width
- 預設按鈕高度
- 預設字體大小

同時它也會檢測常見世界開發套件的整合狀態：

- VRCSDK Worlds
- UdonSharp
- Bakery
- Magic Light Probes
- VRCLightVolumes
- VRCLV Manager

這既可以當作環境自檢頁，也可以當作宿主排版調整頁使用。

### 9. About / DLC Status

About 頁不只是品牌頁。

目前包含：

- Core 產品概覽
- 作者 / 版本 / Community / Store / Support 資訊
- 已安裝模組列表
- 缺少 / 推薦模組目錄
- Capability 視覺化
- Asset / Config / Loc 路徑顯示
- GitHub / VPM Repo / Booth 直達連結

它同時也是對整個 AP 生態定位的說明：Core 負責保持免費與穩定，附加工具則可在之後以模組、付費產品或獨立產品形式加入。

### 10. Tools 與外部模組橋接

這一版 Core 已經包含 AP 模組化工具生態的基礎結構，包括：

- 模組註冊
- 模組移除
- 外部工具 Manifest
- Capability Registry
- Localization Provider 註冊
- Tools 頁面對已安裝工具與推薦工具的承載能力

目前目錄中已預留 / 顯示的生態條目包括：

- ProbeTools Free
- APK Free
- AP Lighting Tools
- APVS / ISO Zone
- Area Tools
- LockSystem
- APK Pro
- Terrain Optimizer
- Point Cloud System
- Hierarchy Tools

這些並不代表它們都已隨 Core 一起發佈，而是說明 AP 宿主已為後續生態接入預留了位置。

## 說明

- **目前版本定位：** 編輯器側宿主外殼。
- **Tools 分頁何時出現：** 當相容外部工具被安裝或註冊後。
- **執行時系統：** 不是目前 Core 的重點。
- **產品方向：** Core 盡量維持為穩定的平台層，而不是把所有後續系統全部塞進來。

