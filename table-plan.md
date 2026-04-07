# Table 規劃：圖片與網站 JSON 渲染

## 需求對應

1. 圖片可上傳到伺服器，檔名改成 Hash 儲存
2. 可對外提供圖片 URL
3. 可描述圖片用途
4. 網站資料以 JSON 儲存給前端渲染
5. 頁面可綁定目前使用中的圖片
6. 只接受 JPG、PNG

## 1. media_files（圖片主表）

用途：管理上傳後圖片檔案與對外 URL

- Id: `uniqueidentifier` / `bigint`（PK）
- StoragePath: `nvarchar(500)` NOT NULL（實體路徑或 object key）
- PublicUrl: `nvarchar(1000)` NOT NULL（對外可用 URL）
- Description: `nvarchar(500)` NULL
- Hash: `char(64)` NOT NULL（SHA-256 Hash）

## 2. site_pages（網站頁面資料）

用途：存放頁面資料，讓前端依 `ContentJson` 渲染

如果使用 PostgreSQL，可以考慮使用 JSONB 型別來儲存 `ContentJson`，以提升查詢效能。

- Id: `uniqueidentifier` / `bigint`（PK）
- ContentJson: `nvarchar(max)` NOT NULL（或資料庫 JSON 型別）
- Name: `nvarchar(200)` NOT NULL（頁面命名，方便管理與查找）
- IsPublished: `bit` NOT NULL（是否公開）
