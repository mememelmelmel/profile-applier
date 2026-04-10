# Mel Profile Applier

## Install via VCC

```
https://mememelmelmel.github.io/profile-applier/index.json
```

Add the URL above in VCC → Settings → Packages → Add Repository.

---

## Overview

A Unity Editor tool for saving and sharing Prefab Variant Overrides as JSON.

---

## Usage

### Editor Window — `Tools > Mel Profile Applier`

Intended for creators who manage individual Profile JSON files.

| Field         | Description                                   |
| ------------- | --------------------------------------------- |
| Target Prefab | The Prefab Variant to apply to or export from |
| Profile JSON  | A single-avatar profile JSON file (e.g. https://github.com/mememelmelmel/hair-profiles/blob/main/Packages/mememelmelmel.hair-profiles/Hairs/BraidBangsWolf/Chalo.json) |

- **Apply to Prefab** — Applies the Profile JSON to the Target Prefab as overrides.
- **Export from Prefab** — Exports the current overrides of the Target Prefab to a JSON file.

---

### ProfileApplier Component — `Add Component > Mel > Profile Applier`

Intended for end users who apply a profile to a distributed Prefab before uploading to VRChat.

| Field          | Description                                                 |
| -------------- | ----------------------------------------------------------- |
| Profile Bundle | A bundle JSON file containing profiles for multiple avatars (e.g. https://github.com/mememelmelmel/hair-profiles/tree/main/Packages/mememelmelmel.hair-profiles/Bundles) |
| Avatar         | Dropdown to select an avatar from the bundle (searchable)   |

Selecting an avatar immediately applies the corresponding profile to the Prefab.
When uploading to VRChat, the ProfileApplier component is automatically removed from the build so it does not appear on the uploaded avatar.

---

## 概要

Prefab Variant の Overrides を JSON として保存・共有するための Unity Editor ツールです。

---

## 使い方

### Editor Window — `Tools > Mel Profile Applier`

個別の Profile JSON ファイルを管理する製作者向けの機能です。

| 入力欄        | 説明                                                                                                                                                                     |
| ------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Target Prefab | 適用先または書き出し元の Prefab Variant                                                                                                                                  |
| Profile JSON  | 単体アバター用の Profile JSON ファイル(例.https://github.com/mememelmelmel/hair-profiles/blob/main/Packages/mememelmelmel.hair-profiles/Hairs/BraidBangsWolf/Chalo.json) |

- **Apply to Prefab** — Profile JSON の内容を Target Prefab に Overrides として適用します。
- **Export from Prefab** — Target Prefab の現在の Overrides を JSON ファイルとして書き出します。

---

### ProfileApplier コンポーネント — `Add Component > Mel > Profile Applier`

配布 Prefab を VRChat へアップロードする前にプロファイルを適用するエンドユーザー向けの機能です。

| 入力欄         | 説明                                                                                                                                                                      |
| -------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Profile Bundle | 複数のアバター分のプロファイルをまとめた bundle JSON ファイル (例. https://github.com/mememelmelmel/hair-profiles/tree/main/Packages/mememelmelmel.hair-profiles/Bundles) |
| Avatar         | bundle から適用するアバターを選択するドロップダウン（検索可能）                                                                                                           |

アバターを選択すると、対応するプロファイルが Prefab に即時適用されます。
VRChat へのアップロード時は ProfileApplier コンポーネントがビルドコピーから自動的に除去されるため、アップロード後のアバターには含まれません。

---

## License

MIT
