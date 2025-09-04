[Setup]
AppName=FirstProject
AppVersion={#GetFileVersion("publish\FirstProject.exe")}
DefaultDirName={autopf}\FirstProject
DefaultGroupName=FirstProject
OutputDir=.
OutputBaseFilename=FirstProjectSetup
Compression=lzma
SolidCompression=yes

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\FirstProject"; Filename: "{app}\FirstProject.exe"; WorkingDir: "{app}"; Flags: runminimized

[Run]
Filename: "explorer.exe"; Parameters: "{app}"; Description: "Open install folder"; Flags: postinstall skipifsilent
