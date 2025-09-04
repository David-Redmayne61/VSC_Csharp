[Setup]
AppName=First Project
AppVersion=1.1.0
WizardStyle=modern
DefaultDirName={autopf}\First Project
DefaultGroupName=First Project
OutputBaseFilename=FirstProjectSetup
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64
AppPublisher=David Redmayne
AppCopyright=© David Redmayne 2025

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\First Project"; Filename: "{app}\FirstProject.exe"
Name: "{commondesktop}\First Project"; Filename: "{app}\FirstProject.exe"

[Run]
Filename: "{app}\FirstProject.exe"; Description: "Launch First Project"; Flags: postinstall nowait