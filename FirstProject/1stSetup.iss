; -- FirstProject.iss --
[Setup]
AppName=FirstProject
AppVersion=1.0
DefaultDirName={pf}\FirstProject
DefaultGroupName=FirstProject
OutputDir=.
OutputBaseFilename=FirstProjectSetup
Compression=lzma
SolidCompression=yes

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\FirstProject"; Filename: "{app}\FirstProject.exe"

[Run]
Filename: "explorer.exe"; Parameters: "{app}"; Description: "Open install folder"; Flags: postinstall skipifsilent