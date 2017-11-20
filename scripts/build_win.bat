rd .\..\dist /s /q
cd ../src/api/DiaryScraperCore
rd bin /s /q
dotnet publish -r win7-x64 --output bin/dist/win
cd ../../app 
npm run tsc -- main.ts
npm run ng -- build --prod
npm run dist