#!/bin/bash
cd `dirname $0`
rm -rf ./../dist
cd ../src/api/DiaryScraperCore
rm -rf ./bin 
dotnet publish -r osx-x64 --output bin/dist/osx
cd ../../app 
npm run tsc -- main.ts
npm run ng -- build --prod
npm run dist