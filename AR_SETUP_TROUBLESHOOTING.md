# Unity AR開発 環境構築・トラブルシューティングメモ
作成日: 2025/12/31
プロジェクト: yousei (Unity 2022.3.62f3)

ここまでの開発で遭遇した、AR環境構築における「つまづきポイント」と「解決策」をまとめました。今後の開発や、別のプロジェクト立ち上げ時の参考にしてください。

## 1. Input System の競合エラー (InvalidOperationException)
このプロジェクトで最も難航したエラーです。

*   **症状**:
    *   Consoleに `InvalidOperationException: You are trying to read Input using the UnityEngine.Input class...` というエラーがフレームごとに大量発生する。
    *   ログが埋め尽くされ、他の重要なARエラーが見えなくなる。
*   **原因**:
    *   Project Settings で `Active Input Handling` を `Both` (旧Inputと新Input Systemの両用) に設定していても、シーン内の `EventSystem` がデフォルトで旧式の `Standalone Input Module` を使い続けてしまうため、新Input System設定と衝突してエラーになる。
*   **解決策**:
    1.  Hierarchy ウィンドウで `EventSystem` オブジェクトを探して選択する。
    2.  Inspector ウィンドウで **Standalone Input Module** コンポーネントを **削除 (Remove Component)** する。
    3.  （UI操作が必要な場合）代わりに **Input System UI Input Module** コンポーネントを追加する。あるいは `Replace with InputSystemUIInputModule` ボタンが表示されていればそれを押す。
    *   **教訓**: Project Settingsを変えるだけでは不十分。シーン内のコンポーネントも手動で置き換える必要がある。

## 2. エディタでARが動かない (No active XR subsystems)
*   **症状**:
    *   `No active UnityEngine.XR.ARSubsystems.XRPlaneSubsystem is available` といった警告が出て、AR機能（平面検出など）が一切動かない。
*   **原因**:
    *   Unityエディタでの実行（Play Mode）は「PC/Mac/Linux Standalone」プラットフォーム扱いになるが、そこでARシミュレーション機能が有効化されていない。
*   **解決策**:
    1.  `Edit` > `Project Settings` > `XR Plug-in Management` を開く。
    2.  **デスクトップアイコン（PC, Mac & Linux Standalone）タブ** を選択する。
    3.  **XR Simulation** にチェックを入れる。
    *   **注意**: Androidタブの「ARCore」にチェックを入れるだけでは、エディタ上では動きません。

## 3. シミュレーションで平面が見つからない（妖精が出ない）
*   **症状**:
    *   エラーは消えたし `OnEnable` も呼ばれているが、いつまで経っても平面検出イベント (`OnPlanesChanged`) が発生せず、妖精が出現しない。
*   **原因**:
    *   AR Simulation環境では、実機と同じように**カメラを動かして空間をスキャン**しないと、シミュレーション空間内の床や壁が「認識済み」にならない。カメラを固定したままだと何も起きない。
*   **解決策（操作方法）**:
    1.  Unityの再生ボタンを押す。
    2.  Gameビューをクリックしてアクティブにする。
    3.  **マウス右クリック** を押したまま、キーボードの **W / A / S / D キー** を押して、3D空間内を歩き回る。
    4.  マウスを動かして視線を床や壁に向ける。グリッドが表示されれば認識成功。

## 4. 妖精（オブジェクト）の挙動設定
*   **工夫点**:
    *   **カメラ目線**: ARではオブジェクトが予期せぬ方向や角度で出現することがあるため、`Instantiate` 直後に `LookRotation` を使い、Y軸回転のみを制御して**「出現時にユーザーの方を向く」**ようにすると体験が良い。
    *   **パーツの自動検索**: 羽などのサブパーツをInspectorで設定し忘れるとエラーになるため、`Start()` 内で名前（`WingL`など）から自動検索して割り当てるフェイルセーフを入れると開発がスムーズ。

## 5. デバッグ環境の整備
AR開発は実機確認の手間がかかるため、エディタでの効率化が重要です。
*   **強制スポーンボタン**: 平面認識を待たずにモデルを表示させるUIボタン (`OnGUI` で実装) を作り、モデルの見た目やアニメーションだけ先に確認できるようにする。
*   **ステータス表示**: `ARPlaneManager.trackables.count`（認識した平面の数）を画面に表示し、ARシステムが死んでいるのか、単に床が見つかっていないだけなのかを判別できるようにする。
