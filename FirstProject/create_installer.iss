[Setup]
AppName=First Project
AppVersion=1.0.0
WizardStyle=modern
DefaultDirName={autopf}\First Project
DefaultGroupName=First Project
OutputBaseFilename=FirstProjectSetup
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64

[Files]
Source: "bin\Release\net8.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\First Project"; Filename: "{app}\FirstProject.exe"
Name: "{commondesktop}\First Project"; Filename: "{app}\FirstProject.exe"

[Run]
Filename: "{app}\FirstProject.exe"; Description: "Launch First Project"; Flags: postinstall nowait