# Fairy Mission AR (Unity + AR Foundation)

3Dの妖精が1日1回のお題を出してくれるARカメラアプリの骨組みです。Unity 2022 LTS + AR Foundation 5.x + URP を前提にしています。

## セットアップ
- Unity 2022 LTS（URPテンプレート推奨）でプロジェクトを作成。
- Package Manager から以下を導入:
  - AR Foundation 5.x
  - ARCore XR Plugin 5.x（Android向け）
  - Mobile Notifications (報酬通知用)
- Build Settings: Android / Minimum API Level 24+ / Target API Level 33+。Graphics API は Vulkan/Metal ではなく OpenGLES3 を優先すると互換性が高いです。
- Player Settings: XR Plug-in Management で ARCore を有効化。

## シーン構成（推奨）
- `MissionScene`：今日のお題カードと開始ボタン。`MissionManager` をアタッチ。
- `ARCameraScene`：
  - `AR Session`, `AR Session Origin`
  - `AR Camera`（URP Camera）に `AR Camera Manager`, `AR Occlusion Manager`（任意）を追加
  - `AR Plane Manager`（水平面のみ）、`AR Raycast Manager`
  - 空オブジェクトに `ARFairySpawner`（妖精プレハブとレイキャスト参照を割当）
  - `ColorMissionCondition` などミッション判定用のコンポーネント
  - UI Canvas にミッション表示、進捗バー、撮影ボタン（`CaptureAndShare`）
- `WardrobeScene`：報酬の羽や衣装の着せ替え（後日拡張）。

## フロー
1. アプリ起動 → `MissionManager` が毎日ミッションを決定し PlayerPrefs に保存。
2. ユーザーが「開始」で AR カメラへ。最初に検出した水平面へ妖精を自動スポーン。
3. `ColorMissionCondition` などがカメラ画像を解析し、条件達成で撮影ボタンが解放。
4. 撮影 → `FairyAvatar` が喜びアニメと VFX を再生 → `RewardPopup` で報酬表示。
5. 撮影画像はギャラリーへ保存（`CaptureAndShare`）。

## 含まれるスクリプト
- `Assets/Scripts/Missions/MissionManager.cs`  
  - ミッション定義と毎日のローテーション、状態保存を担当。
- `Assets/Scripts/Missions/ColorMissionCondition.cs`  
  - ARカメラのフレームから HSV 判定で色占有率をチェック。
- `Assets/Scripts/AR/ARFairySpawner.cs`  
  - 平面検出後に妖精プレハブをスポーンしアンカーを維持。
- `Assets/Scripts/Fairy/FairyAvatar.cs`  
  - Animator へのトリガー送信（Idle/Happy/Thanks）。演出シーケンスの起点。
- `Assets/Scripts/Camera/CaptureAndShare.cs`  
  - スクリーンショットを RenderTexture 経由で保存し、ネイティブ共有を呼び出すフック。

## 使い方（ざっくり）
1. `Assets/Scripts/` をプロジェクトへ追加。
2. 妖精モデル（Humanoidリグ、複数アニメ付き）と Animator Controller を用意し、`FairyAvatar` のパラメータ名 (`Idle`, `Happy`, `Thanks`) に合わせてトリガーを設定。
3. ARカメラに `AR Camera Manager` を付与し、`ColorMissionCondition` に参照を割り当て。
4. UI の進捗バーやテキストは `MissionManager` のイベントに購読して更新。
5. ビルド → 実機で ARCore 許可を受けてテスト。

## 判定の拡張
- 笑顔判定: ML Kit Face Detection を Android ネイティブで行い、結果を Unity 側へ JNI で渡す（`MissionCondition` の `ReportProgress` を呼ぶ形）。
- 円形判定: OpenCV for Unity または軽量な Hough 代替を ComputeShader で実装。
- 歩数連動: Health Connect / Google Fit から歩数を取得し、ミッションパラメータに応じて合否を返す。

## 注意
- モバイルの熱とバッテリーに注意し、VFX の最大パーティクル数と Bloom 強度を端末スペックで可変にしてください。
- `ColorMissionCondition` はダウンサンプリングした CPU イメージを Burst + Jobs で走らせていますが、URP の解像度スケールと組み合わせて負荷を抑えると安定します。
