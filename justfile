set shell := ["powershell.exe", "-c"]

default:
    just --fmt --unstable 2> $null
    just --list --unsorted
    just fmt

# 執行專案
run:
    dotnet run --project landing-page-backend/landing-page-backend.csproj

# 建構專案
build:
    dotnet build landing-page-backend/landing-page-backend.csproj

# 重新整理相依套件
refresh:
    dotnet restore landing-page-backend/landing-page-backend.csproj

# 執行測試
test:
    dotnet test landing-page-backend.Tests/landing-page-backend.Tests.csproj

# 執行 Integration Tests
test-integration:
    dotnet test landing-page-backend.Tests.Integration/landing-page-backend.Tests.Integration.csproj

# 清除建構產物
clean:
    dotnet clean landing-page-backend/landing-page-backend.csproj

# 格式化程式碼
fmt:
    dotnet format landing-page-backend.sln

# 檢查程式碼格式是否符合規範
fmt-check:
    dotnet format --verify-no-changes landing-page-backend.sln

deploy-staging:
    & .\landing-page-backend\iis_deploy_staging.ps1

# CI 流程：重新整理相依套件、建構專案、檢查程式碼格式、執行測試
ci:
    just refresh
    just build
    just fmt-check
    just test
    just test-integration

# 停止本機執行中的後端程序（避免 EF/build 檔案鎖定）
api-stop:
    -Stop-Process -Name landing-page-backend -Force -ErrorAction SilentlyContinue

# ===== Database / Migration（MSSQL + PostgreSQL）=====

# 新增 MSSQL migration（名稱: just mig-add-mssql InitName）
mig-add-mssql name: api-stop
    dotnet ef migrations add {{ name }} --project landing-page-backend/landing-page-backend.csproj --output-dir Migrations/MsSql -- --provider=mssql

# 新增 PostgreSQL migration（名稱: just mig-add-pg InitName）
mig-add-pg name: api-stop
    dotnet ef migrations add {{ name }} --project landing-page-backend/landing-page-backend.csproj --output-dir Migrations/PostgreSql -- --provider=postgresql

# 套用 MSSQL migration 到資料庫
db-update-mssql: api-stop
    dotnet ef database update --project landing-page-backend/landing-page-backend.csproj -- --provider=mssql

# 套用 PostgreSQL migration 到資料庫
db-update-pg: api-stop
    dotnet ef database update --project landing-page-backend/landing-page-backend.csproj -- --provider=postgresql
