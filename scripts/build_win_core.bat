cd ../src/api/DiaryScraperCore
rd bin /s /q
dotnet publish -r win7-x64 --output bin/dist/win
