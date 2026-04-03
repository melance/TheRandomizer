#define MyAppName "The Randomizer CLI"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Lance Boudreaux"
#define MyAppExeName "TheRandomizer.CLI.exe"
#define MyPublishDir "..\..\src\TheRandomizer.CLI\bin\Release\net10.0\publish"

[Setup]
AppId={{8E7E9C4D-6E5E-4B4B-A6D6-6F3B44B1A001}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\The Randomizer CLI
DefaultGroupName=The Randomizer
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputDir=Output
OutputBaseFilename=TheRandomizerCLI-Setup-{#MyAppVersion}
Compression=lzma
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
ChangesEnvironment=yes

[Files]
Source: "{#MyPublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\The Randomizer CLI"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall The Randomizer CLI"; Filename: "{uninstallexe}"

[Tasks]
Name: "addtopath"; Description: "Add The Randomizer CLI to PATH"; Flags: unchecked

[Registry]
Root: HKLM; Subkey: "SYSTEM\CurrentControlSet\Control\Session Manager\Environment"; \
    ValueType: expandsz; ValueName: "Path"; ValueData: "{olddata};{app}"; \
    Tasks: addtopath; Check: NeedsAddPath(ExpandConstant('{app}')); Flags: preservestringtype


[Code]
function NeedsAddPath(Dir: string): Boolean;
var
  PathValue: string;
begin
  if RegQueryStringValue(HKCU, 'Environment', 'Path', PathValue) then
    Result := Pos(';' + Uppercase(Dir) + ';', ';' + Uppercase(PathValue) + ';') = 0
  else
    Result := True;
end;

procedure RemovePath(Dir: string);
var
  PathValue: string;
  P: Integer;
begin
  if RegQueryStringValue(HKCU, 'Environment', 'Path', PathValue) then
  begin
    P := Pos(';' + Uppercase(Dir) + ';', ';' + Uppercase(PathValue) + ';');
    if P > 0 then
    begin
      Delete(PathValue, P - 1, Length(Dir) + 1);
      RegWriteExpandStringValue(HKCU, 'Environment', 'Path', PathValue);
    end;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
    RemovePath(ExpandConstant('{app}'));
end;