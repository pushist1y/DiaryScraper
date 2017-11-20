rd .\..\dist /s /q
cd ../src/api/DiaryScraperCore
rd bin /s /q
dotnet publish -r win7-x64 --output bin/dist/win
cd ../../app 
call npm run tsc -- main.ts
call npm run ng -- build --prod
call npm run dist