rd .\..\dist /s /q
cd ../src/app
call npm run tsc -- main.ts
call npm run ng -- build --prod

cd ../api/DiaryScraperCore
rd bin /s /q
dotnet publish -r win7-x86 --output bin/dist/win
cd ../../app
call npm run electron-builder -- . --win --ia32
cd ../../dist
for %%a in (*.exe) do ren "%%~a" "%%~na-ia32%%~xa"
cd ../src/app

cd ../api/DiaryScraperCore
rd bin /s /q
dotnet publish -r win7-x64 --output bin/dist/win
cd ../../app
call npm run electron-builder -- . --win --x64

cd ../api/DiaryScraperCore
rd bin /s /q
dotnet publish -r ubuntu.16.10-x64 --output bin/dist/linux
cd ../../app
call npm run electron-builder -- . --linux --x64
