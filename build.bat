set "dist=%CD%/EmeralEngine-dist"

if exist %dist% (
    rmdir /s /q "%dist%"
)

git submodule update --remote
cd Runtime
call ./build.bat "%dist%/gameruntime/emeral.dll"
rm "%dist%/gameruntime/emeral.h"
cd ..
dotnet clean src -c Release
dotnet publish src -c Release -r win-x64 -p:PublishReadyToRun=true -o "%dist%"