#define PRODUCT_NAME "EvilZombMapsLoader"
#define PRODUCT_PUBLISHER "CauMoH"
#define APPLICATION_VERSION "1.0"
#define PRODUCT_EXE "EvilZombMapsLoader.exe"
        
#define ReleaseDir "..\bin\Release"

#define NetFramework "ndp48-x86-x64-allos-enu.exe"

[Setup]
AppId={{2C545FC7-E6DB-45AE-9006-8EBFF73F9F82}}
AppName={#PRODUCT_NAME}
AppVersion={#APPLICATION_VERSION}                                    
AppPublisher={#PRODUCT_PUBLISHER}
DefaultDirName={pf}\{#PRODUCT_NAME}
DefaultGroupName={#PRODUCT_NAME} 
AllowNoIcons=no
OutputDir="..\Setup"
OutputBaseFilename={#PRODUCT_NAME}  вер. {#APPLICATION_VERSION}
Compression=lzma
SolidCompression=yes
LanguageDetectionMethod=none
UsePreviousTasks=no
PrivilegesRequired=admin
CloseApplications=yes
DisableWelcomePage=false  
UsePreviousLanguage=no  

[Languages]
Name: "ru"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}";

[Components]
Name: "main"; Description: Основные компоненты; Types: full compact custom; Flags: fixed

[Files]
Source: "{#ReleaseDir}\*"; Excludes: "*.pdb,*.xml"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs; Components: main

[Icons]
Name: "{group}\{#PRODUCT_NAME}"; Filename: "{app}\{#PRODUCT_EXE}"
Name: "{group}\{cm:UninstallProgram,{#PRODUCT_NAME}}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#PRODUCT_NAME}"; Filename: "{app}\{#PRODUCT_EXE}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#PRODUCT_EXE}"; Description: "{cm:LaunchProgram,{#StringChange(PRODUCT_NAME, '&', '&&')}}"; Flags: nowait postinstall skipifsilent                    

[UninstallRun]
Filename: "{cmd}"; Parameters: "/C ""taskkill /im {#PRODUCT_EXE} /f /t"  

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
     RegWriteStringValue(HKEY_CURRENT_USER, 'SOFTWARE\{#PRODUCT_PUBLISHER}\StandTvu', 'IsFirstRun', 'True');
     RegWriteStringValue(HKEY_CURRENT_USER, 'SOFTWARE\{#PRODUCT_PUBLISHER}\StandTvu', 'Language', ActiveLanguage);
  end;
end;

var
  IsNeedReboot: boolean; 

function IsDotNetDetected(version: string; service: cardinal): Boolean;
// Indicates whether the specified version and service pack of the .NET Framework is installed.
//
// version -- Specify one of these strings for the required .NET Framework version:
//    'v1.1'          .NET Framework 1.1
//    'v2.0'          .NET Framework 2.0
//    'v3.0'          .NET Framework 3.0
//    'v3.5'          .NET Framework 3.5
//    'v4\Client'     .NET Framework 4.0 Client Profile
//    'v4\Full'       .NET Framework 4.0 Full Installation
//    'v4.5'          .NET Framework 4.5
//    'v4.5.1'        .NET Framework 4.5.1
//    'v4.5.2'        .NET Framework 4.5.2
//    'v4.6'          .NET Framework 4.6
//    'v4.6.1'        .NET Framework 4.6.1
//
// service -- Specify any non-negative integer for the required service pack level:
//    0               No service packs required
//    1, 2, etc.      Service pack 1, 2, etc. required
var
    key, versionKey: string;
    install, release, serviceCount, versionRelease: cardinal;
    success: boolean;
begin
    versionKey := version;
    versionRelease := 0;

    // .NET 1.1 and 2.0 embed release number in version key
    if version = 'v1.1' then begin
        versionKey := 'v1.1.4322';
    end else if version = 'v2.0' then begin
        versionKey := 'v2.0.50727';
    end

    // .NET 4.5 and newer install as update to .NET 4.0 Full
    else if Pos('v4.', version) = 1 then begin
        versionKey := 'v4\Full';
        case version of
          'v4.5':   versionRelease := 378389;
          'v4.5.1': versionRelease := 378675; // or 378758 on Windows 8 and older
          'v4.5.2': versionRelease := 379893;
          'v4.6':   versionRelease := 393295; // or 393297 on Windows 8.1 and older
          'v4.6.1': versionRelease := 394254; // or 394271 on Windows 8.1 and older
          'v4.8': versionRelease := 528040;
        end;
    end;

    // installation key group for all .NET versions
    key := 'SOFTWARE\Microsoft\NET Framework Setup\NDP\' + versionKey;

    // .NET 3.0 uses value InstallSuccess in subkey Setup
    if Pos('v3.0', version) = 1 then begin
        success := RegQueryDWordValue(HKLM, key + '\Setup', 'InstallSuccess', install);
    end else begin
        success := RegQueryDWordValue(HKLM, key, 'Install', install);
    end;

    // .NET 4.0 and newer use value Servicing instead of SP
    if Pos('v4', version) = 1 then begin
        success := success and RegQueryDWordValue(HKLM, key, 'Servicing', serviceCount);
    end else begin
        success := success and RegQueryDWordValue(HKLM, key, 'SP', serviceCount);
    end;

    // .NET 4.5 and newer use additional value Release
    if versionRelease > 0 then begin
        success := success and RegQueryDWordValue(HKLM, key, 'Release', release);
        success := success and (release >= versionRelease);
    end;

    result := success and (install = 1) and (serviceCount >= service);
end;


procedure InstallFramework;
var
  StatusText: string;
  ResultCode: Integer;
begin
  StatusText := WizardForm.StatusLabel.Caption;
  WizardForm.StatusLabel.Caption := 'Установка .NET Framework 4.8. Это займет несколько минут...';
  WizardForm.ProgressGauge.Style := npbstMarquee;
  try
    if not Exec(ExpandConstant('{tmp}\{#NetFramework}'), '/passive /norestart', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
    begin
      MsgBox('.NET установка завершилась с ошибкой: ' + IntToStr(ResultCode) + '.', mbError, MB_OK);
    end;
  finally
	IsNeedReboot := True;
    WizardForm.StatusLabel.Caption := StatusText;
    WizardForm.ProgressGauge.Style := npbstNormal;

    DeleteFile(ExpandConstant('{tmp}\{#NetFramework}.exe'));
  end;
end;       

function CheckFramework() : Boolean; 
begin
  result := not IsDotNetDetected('v4.8', 0);
end;

// метод установки доп. софта
procedure InstallData(exepath, params: string);
var
    ResultCode: Integer;
begin
    Exec(ExpandConstant(exepath), ExpandConstant(params), '', SW_SHOW, ewWaitUntilTerminated, ResultCode)
end;

function IsWindowsVersionOrNewer(Major, Minor: Integer): Boolean;
var
  Version: TWindowsVersion;
begin
  GetWindowsVersionEx(Version);
  Result :=
    (Version.Major > Major) or
    ((Version.Major = Major) and (Version.Minor >= Minor));
end;

function IsWindowsXPOrNewer: Boolean;
begin
  Result := IsWindowsVersionOrNewer(5, 1);
end;

function IsWindowsVistaOrNewer: Boolean;
begin
  Result := IsWindowsVersionOrNewer(6, 0);
end;

function IsWindows7OrNewer: Boolean;
begin
  Result := IsWindowsVersionOrNewer(6, 1);
end;

function IsWindows8OrNewer: Boolean;
begin
  Result := IsWindowsVersionOrNewer(6, 2);
end;

function IsWindows10OrNewer: Boolean;
begin
  Result := IsWindowsVersionOrNewer(10, 0);
end;

// Проверка под какую архитектуру устанавливается программа
function IsX64: Boolean;
begin                                                                                       
  Result := ProcessorArchitecture = paX64;
end;


 