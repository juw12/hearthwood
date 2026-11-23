; Hearthwood for Windows - per-user installer (no admin rights). build.py compiles it with:
;   iscc /DAppVersion=1.0.0 /DSourceDir=<dist\Hearthwood> /DOutputDir=<dist> /DIconFile=<build\hearthwood.ico> hearthwood.iss
; Installs to %LOCALAPPDATA%\Programs\Hearthwood, adds Hearthwood to the Start menu (and optionally the desktop) and lists it
; in Settings > Apps. The window keeps its data (what you saved in it) in %LOCALAPPDATA%\Hearthwood, which
; uninstalling leaves in place so a reinstall finds it again.

#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#ifndef SourceDir
  #define SourceDir "dist\Hearthwood"
#endif
#ifndef OutputDir
  #define OutputDir "dist"
#endif
#ifndef IconFile
  #define IconFile "build\hearthwood.ico"
#endif

[Setup]
AppId={{E81247BA-698B-569E-A6DE-E1E1C366EEFB}
AppName=Hearthwood
AppVersion={#AppVersion}
AppVerName=Hearthwood {#AppVersion}
AppPublisher=Juwen Lie
AppPublisherURL=https://juw12.github.io/hearthwood/preview.html
AppSupportURL=https://github.com/juw12/hearthwood
DefaultDirName={autopf}\Hearthwood
DisableDirPage=yes
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir={#OutputDir}
OutputBaseFilename=Hearthwood-Windows-Setup
SetupIconFile={#IconFile}
UninstallDisplayIcon={app}\Hearthwood.exe
UninstallDisplayName=Hearthwood
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=no
SetupLogging=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{userprograms}\Hearthwood"; Filename: "{app}\Hearthwood.exe"; Comment: "A medieval village prototype you can walk around in"
Name: "{userdesktop}\Hearthwood"; Filename: "{app}\Hearthwood.exe"; Comment: "A medieval village prototype you can walk around in"; Tasks: desktopicon

[Run]
Filename: "{app}\Hearthwood.exe"; Description: "{cm:LaunchProgram,Hearthwood}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{app}\Hearthwood.exe"; Parameters: "--stop"; Flags: runhidden waituntilterminated; RunOnceId: "StopHearthwood"

[Code]
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  Result := '';
  { Updating: close an open Hearthwood window first so its files can be replaced. }
  if FileExists(ExpandConstant('{app}\Hearthwood.exe')) then
    Exec(ExpandConstant('{app}\Hearthwood.exe'), '--stop', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;
