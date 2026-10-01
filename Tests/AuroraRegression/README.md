# Aurora regression harness

Links production Config, renderer, textures, bridge and terminal handler. Game,
UI, logging and configuration-save boundaries are stubbed; FakeAnomaly is a
separate assembly so reflection uses the real bridge path. Uses SharpDX and a
real D3D11 hardware device for texture creation. Does not install the plugin.

From the Aurora repository:

```powershell
dotnet run --project Tests/AuroraRegression/AuroraRegression.csproj -c Release -f net10.0
dotnet build Tests/AuroraRegression/AuroraRegression.csproj -c Release -f net48
./Tests/AuroraRegression/bin/Release/net48/AuroraRegression.exe
```

Expected: 105 assertions per runtime. An intentional null-device setup failure
logs once while testing fail-closed recovery. The harness tests nested device-loss
classification with an exception, not an actual hardware reset. Requires the
installed game at the Bin64 path specified in the project.
