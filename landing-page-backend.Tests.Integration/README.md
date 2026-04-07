# 整合測試 (Integration Tests)

這個專案包含 landing-page-backend 的整合測試，使用 ASP.NET Core 的 `WebApplicationFactory` 來測試完整的 API 端點。

## 測試內容

### SitePageController 測試
- ✅ GetAll - 取得所有頁面
- ✅ GetAll with Keyword - 使用關鍵字過濾
- ✅ Create - 建立新頁面（有效和無效資料）
- ✅ GetById - 取得指定頁面（存在和不存在的 ID）
- ✅ Update - 更新頁面（存在和不存在的 ID）
- ✅ Delete - 刪除頁面（存在和不存在的 ID）

### MediaFileController 測試
- ✅ GetAll - 取得所有媒體檔案
- ✅ GetAll with Keyword/Hash - 使用關鍵字和雜湊值過濾
- ✅ Upload - 上傳圖片（有效圖片和無檔案情況）
- ✅ Upload Duplicate - 測試重複檔案處理
- ✅ GetById - 取得指定檔案（存在和不存在的 ID）
- ✅ UpdateDescription - 更新描述（存在和不存在的 ID）
- ✅ Delete - 刪除檔案（存在和不存在的 ID）

## 運行測試

```bash
# 運行所有整合測試
dotnet test

# 運行特定測試類別
dotnet test --filter "FullyQualifiedName~SitePageControllerIntegrationTests"

# 顯示詳細輸出
dotnet test --logger "console;verbosity=detailed"
```

## 技術細節

- 使用 **SQLite in-memory** 資料庫進行測試（支援事務和 ExecuteDeleteAsync）
- 使用 `CustomWebApplicationFactory` 配置測試環境
- 每個測試類別使用 `IClassFixture` 共享測試伺服器實例
- 測試涵蓋成功和失敗的情境

## 與單元測試的差異

- **單元測試** (`landing-page-backend.Tests`): 測試個別服務邏輯，使用 Mock 隔離依賴
- **整合測試** (`landing-page-backend.Tests.Integration`): 測試完整的 HTTP 請求/回應流程，包含路由、控制器、服務和資料庫
