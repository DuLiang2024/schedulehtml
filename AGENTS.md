# AGENTS.md — VibeCoding運用ガイド（Codex / .NET 10 / Azure Web App 自動デプロイ）

## 目的
- ローカルで実装 → Git に push → CI/CD により Azure Web App へ自動反映、を最短で回す。
- 画面仕様は「JSON を編集すると UI がリアルタイム更新」するタイムライン型ダッシュボード（長い横棒、日/時間切替）。

---

## 前提（既に整っていること）
- Azure Web App は作成済み
- Git push をトリガーに自動デプロイされるパイプライン（GitHub Actions もしくは Azure DevOps）が設定済み
- Web App の Runtime Stack: **Dotnet - v10.0**

---

## エージェントの原則（必ず守る）
1. **Secrets をコミットしない**
   - `appsettings.*.json` の本番用値、接続文字列、証明書、publish profile 等はリポジトリに入れない。
2. **小さく刻む**
   - 1PR/1変更目的。差分を小さくして push → デプロイ確認を高速化。
3. **壊さない**
   - 変更前に現状確認、変更後に `build/test` を通してから push。
4. **JSON が単一ソース**
   - UI 表示は JSON から生成される。JSON と表示の乖離を作らない。

---

## 想定リポジトリ構成（例）
- `src/`
  - `Web/`（ASP.NET Core / Blazor / MVC 等の実体）
- `tests/`
- `docs/`
- `AGENTS.md`（このファイル）
- `README.md`

※実際の構成が異なる場合は、**構成に合わせて以降のコマンドパスを調整**する。

---

## ローカル開発の基本手順（人間 + Codex）
### 0) 環境確認
- `.NET SDK 10` がインストール済みであること
- `dotnet --info` で SDK 10 系が見えること

### 1) 初回セットアップ（Codex CLI）
- リポジトリ直下で実行（既に /init 済みなら不要）
  - `codex /init`
- 以降、作業は「小タスク単位」で Codex に依頼し、実装→テスト→push までを一気通貫させる。

### 2) ローカル起動
- 代表例（実プロジェクトに合わせて変更）
  - `dotnet restore`
  - `dotnet build -c Release`
  - `dotnet test -c Release`
  - `dotnet run --project src/Web`

### 3) ブランチ運用（推奨）
- `main`（デプロイ対象）
- `feature/<topic>`（開発用）
- PR マージで `main` に反映 → 自動デプロイ

※「push したら即反映」運用の場合は、`main` 直 push の代わりに **必ず build/test を通す**。

---

## デプロイ連携（自動反映の前提整理）
- 自動反映のトリガーは通常いずれか：
  1) GitHub Actions（Azure Web Apps Deploy）
  2) Azure DevOps Pipeline
- エージェントは **パイプライン定義を勝手に破壊しない**。
- 変更が必要な場合は、まず現行 YAML / 設定を読み、影響範囲を説明した上で最小変更で行う。

---

## タスク分解テンプレ（Codex に渡す指示例）
以下の形式で Codex に依頼する（要点が揃うので失敗しにくい）：

- 目的：
- 受け入れ条件（UI/動作）：
- 変更範囲（ファイル/機能）：
- 実装方針：
- テスト方針：
- 影響（破壊的変更の有無）：

---

## ダッシュボード仕様（最小合意）
### UI
- タイムライン型（レーン×横棒）
- 粒度切替：日 / 時間
- JSON 編集 → リアルタイム反映（Parse/Validate 成功時のみ）

### データ（JSON）
- `view.mode`: `day` or `hour`
- `view.range`: start/end
- `lanes[]`
- `items[]`（start/end or date/durationDays を許容）
- JSON 変更時：`parse → validate → normalize → render`
- 失敗時：エラー表示し、UI は直前の正常状態を維持

---

## コーディング規約（最低限）
- 変更は「既存の構造・命名」に合わせる（新規ルールを勝手に増やさない）
- 例外・エラー時の挙動は明示する（JSON不正時など）
- ログは過剰に出さない（必要箇所に限定）

---

## テスト・品質ゲート（push 前に必須）
- `dotnet build -c Release`
- `dotnet test -c Release`
- UI がある場合：最低限のスモーク（JSON を1つ壊しても落ちないこと等）

---

## 変更時チェックリスト（PR/Push前）
- [ ] Secrets / 接続文字列を追加していない
- [ ] `build/test` が通る
- [ ] JSON のバリデーション失敗時の挙動が安全
- [ ] 日/時間切替で表示が破綻しない
- [ ] `main` 反映後に Azure 側で確認すべき観点が明記されている

---

## トラブルシュート（よくある）
### デプロイされない
- パイプライン実行履歴（GitHub Actions / Azure DevOps）で失敗点を特定
- Web App の「デプロイセンター」設定を確認（連携先リポ/ブランチ）
- `main` に push されているか（トリガーブランチ違い）

### 動くが更新が反映されない
- キャッシュ（ブラウザ/Service Worker）を疑う
- 生成物が古い可能性：CI のビルド成果物が更新されているか確認

### .NET 10 互換問題
- SDK/TargetFramework の不一致を確認
- ローカルと CI の SDK バージョン差を確認（global.json 有無）

---

## ロールバック方針（最短）
- `main` を直前のコミットへ戻して push（=自動デプロイで戻る）
- 重要：ロールバック理由と対象コミットを記録する

---

## 依頼の仕方（このAGENTS.mdを読んだ上で）
- 「何を作るか」「受け入れ条件」「JSON例」「日/時間切替の期待」を短く箇条書きで渡す
- 実装→テスト→push までを1セットで依頼する
