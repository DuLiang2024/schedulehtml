# schedulehtml

JSON を単一ソースにしたタイムライン型ダッシュボードです。`view` / `lanes` / `items` を編集すると、右側のプレビューがリアルタイムで更新されます。

## スタック

- ASP.NET Core Razor Pages (.NET 10)
- Vanilla JS + CSS (no framework)
- GitHub Actions → Azure Web App 自動デプロイ

## 構成

```
.
├── global.json                # .NET 10.0.103 を固定
├── schedulehtml.sln
├── src
│   ├── Core                   # JSON モデル + バリデーター
│   └── Web                    # Razor UI / 静的アセット
│       └── wwwroot/data
│           └── default-schedule.json
└── tests
    └── ScheduleHtml.Core.Tests
```

## JSON 仕様（最小）

```jsonc
{
  "view": {
    "mode": "day" | "hour",
    "range": { "start": "<ISO8601>", "end": "<ISO8601>" }
  },
  "lanes": [
    { "id": "design", "label": "Design", "color": "#884EA0" }
  ],
  "items": [
    {
      "id": "wireframes",
      "laneId": "design",
      "label": "Wireframes",
      "start": "2026-02-17T12:00:00-08:00",
      "end": "2026-02-18T12:00:00-08:00",
      "status": "in-progress",
      "description": "Optional notes",
      "durationDays": 1.5 // end がなければ start + durationDays
    }
  ]
}
```

> JSON → Parse → Validate → Normalize → Render。失敗時は最後の正常レンダーを維持し、エラーを表示します。

## ローカル開発

1. **SDK**: .NET 10.0.103 以上 (`global.json` で固定)。インストール済みなら `dotnet --info` に表示されます。未インストールの場合は `dotnet-install` で `./.dotnet` に配置し、`./.dotnet/dotnet` コマンドを使ってください。
2. **復元 + ビルド + テスト**
   ```powershell
   .\.dotnet\dotnet restore
   .\.dotnet\dotnet build -c Release
   .\.dotnet\dotnet test -c Release
   ```
3. **起動**
   ```powershell
   .\.dotnet\dotnet run --project src/Web/ScheduleHtml.Web.csproj
   ```
   既定で <https://localhost:5001> へバインドされます。

## テスト

`tests/ScheduleHtml.Core.Tests` で JSON バリデーションのサンプル/異常系をカバーしています。CI では `dotnet test -c Release` を必須にしてください。

## 運用メモ

- Secrets（接続文字列、publish profile 等）は**絶対に**コミットしない。
- 1PR = 1目的。変更後は `build/test` を通してから push。
- 日/時間切替は `view.mode` を書き換えるボタン経由で行い、UI と JSON の乖離を防ぐ。
- ロールバックは `main` を直前コミットに戻すのが最速です。理由を必ず残すこと。
