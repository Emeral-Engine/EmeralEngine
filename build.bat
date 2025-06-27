set "dist=./EmeralEngine-dist"

if exist %dist% (
    rmdir /s /q "%dist%"
)

dotnet clean src -c Release
dotnet publish src -c Release -r win-x64 -p:PublishReadyToRun=true -o "%dist%"