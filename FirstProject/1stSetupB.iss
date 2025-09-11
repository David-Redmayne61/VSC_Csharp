[Setup]
AppName=FirstProject Customer Call Management System
AppVersion=2.0.1
AppPublisher=David Redmayne
AppPublisherURL=
AppSupportURL=
AppUpdatesURL=
DefaultDirName={autopf}\FirstProject
DefaultGroupName=FirstProject
AllowNoIcons=yes
OutputDir=.
OutputBaseFilename=FirstProjectSetup_v2.0.1
Compression=lzma
SolidCompression=yes
SetupIconFile=
UninstallDisplayIcon={app}\FirstProject.exe
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "C:\Users\David.Redmayne\VSC\VSC_Csharp\FirstProject\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\FirstProject"; Filename: "{app}\FirstProject.exe"; WorkingDir: "{app}"
Name: "{group}\{cm:UninstallProgram,FirstProject}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\FirstProject"; Filename: "{app}\FirstProject.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\FirstProject.exe"; Description: "{cm:LaunchProgram,FirstProject}"; Flags: nowait postinstall skipifsilent
Filename: "explorer.exe"; Parameters: "{app}"; Description: "Open install folder"; Flags: postinstall skipifsilent unchecked

