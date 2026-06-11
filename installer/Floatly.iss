#define MyAppName "Floatly"
#define MyAppDisplayName "Floatly（浮岛）"
#ifndef MyAppVersion
#define MyAppVersion "2.0.9"
#endif
#define MyAppPublisher "cass-2003"
#define MyAppURL "https://github.com/cass-2003/Floatly"
#define MyAppExeName "Floatly.exe"

[Setup]
AppId={{A8F3C2E1-9B4D-4A7E-8F1C-2D5E6A9B0C3D}
AppName={#MyAppDisplayName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppDisplayName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppDisplayName}
AllowNoIcons=yes
OutputDir=..\release
OutputBaseFilename=Floatly-Setup-{#MyAppVersion}
SetupIconFile=..\release\Floatly\app.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
DisableProgramGroupPage=yes
ShowLanguageDialog=no

[Languages]
Name: "chinesesimp"; MessagesFile: "ChineseSimplified.isl"

[CustomMessages]
chinesesimp.LaunchProgram=安装完成后运行 {#MyAppDisplayName}

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加任务："; Flags: unchecked

[Files]
Source: "..\release\Floatly\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppDisplayName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\卸载 {#MyAppDisplayName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppDisplayName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram}"; Flags: nowait postinstall skipifsilent

[Code]
function IsDotNet8DesktopInstalled: Boolean;
var
  Names: TArrayOfString;
  I: Integer;
begin
  Result := False;
  if RegGetSubkeyNames(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedframework\Microsoft.WindowsDesktop.App', Names) then
  begin
    for I := 0 to GetArrayLength(Names) - 1 do
      if Copy(Names[I], 1, 2) = '8.' then
      begin
        Result := True;
        Exit;
      end;
  end;
  if RegGetSubkeyNames(HKLM, 'SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\sharedframework\Microsoft.WindowsDesktop.App', Names) then
  begin
    for I := 0 to GetArrayLength(Names) - 1 do
      if Copy(Names[I], 1, 2) = '8.' then
      begin
        Result := True;
        Exit;
      end;
  end;
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
  if not IsDotNet8DesktopInstalled then
  begin
    if MsgBox('浮岛（Floatly）需要安装 .NET 8 桌面运行时（x64）。' + #13#10 + #13#10 +
      '下载地址：https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0' + #13#10 + #13#10 +
      '是否仍要继续安装？',
      mbConfirmation, MB_YESNO) = IDNO then
      Result := False;
  end;
end;
