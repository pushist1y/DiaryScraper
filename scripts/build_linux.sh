#!/bin/bash
rm -rf ./../dist
cd ../src/api/DiaryScraperCore
rm -rf ./bin 
dotnet publish -r ubuntu.16.10-x64 --output bin/dist/linux
cd ../../app 
npm run dist