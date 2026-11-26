[Setup]
AppId={{3B7D5E21-6A4C-4F19-9D08-5C1E8A730033}
AppName=Hudhud Unlocker
AppVersion=1.0.3
AppPublisher=Hudhud Apps
AppPublisherURL=https://hudhudapps.com
DefaultDirName={autopf}\Hudhud Unlocker
DefaultGroupName=Hudhud Unlocker
OutputDir=installer_output
OutputBaseFilename=HudhudUnlocker_Setup_v1.0.3
SetupIconFile=app.ico
UninstallDisplayIcon={app}\HudhudUnlocker.exe
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=admin
WizardStyle=modern

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"

[CustomMessages]
turkish.PinTaskbar=Görev çubuğuna sabitle
english.PinTaskbar=Pin to taskbar
german.PinTaskbar=An Taskleiste anheften
french.PinTaskbar=Épingler à la barre des tâches
spanish.PinTaskbar=Anclar a la barra de tareas
italian.PinTaskbar=Aggiungi alla barra delle applicazioni
turkish.CtxMenu=Hudhud Unlocker ile kilidi aç
english.CtxMenu=Unlock with Hudhud Unlocker
german.CtxMenu=Mit Hudhud Unlocker entsperren
french.CtxMenu=Déverrouiller avec Hudhud Unlocker
spanish.CtxMenu=Desbloquear con Hudhud Unlocker
italian.CtxMenu=Sblocca con Hudhud Unlocker
turkish.CtxTask=Sağ tık menüsüne "Hudhud Unlocker ile kilidi aç" ekle
english.CtxTask=Add "Unlock with Hudhud Unlocker" to the right-click menu
german.CtxTask="Mit Hudhud Unlocker entsperren" zum Kontextmenü hinzufügen
french.CtxTask=Ajouter « Déverrouiller avec Hudhud Unlocker » au menu contextuel
spanish.CtxTask=Añadir «Desbloquear con Hudhud Unlocker» al menú contextual
italian.CtxTask=Aggiungi «Sblocca con Hudhud Unlocker» al menu contestuale
turkish.OtherTasks=Diğer görevler:
turkish.UacBypass=Uygulamayı başlatmayı kolaylaştır: UAC onayını atlamak için Windows Görev Zamanlayıcı kullan
english.OtherTasks=Other tasks:
english.UacBypass=Simplify app launch by using Windows Task Scheduler to bypass User Account Control confirmation
german.OtherTasks=Weitere Aufgaben:
german.UacBypass=Start vereinfachen: Windows-Aufgabenplanung nutzen, um die UAC-Bestätigung zu umgehen
french.OtherTasks=Autres tâches :
french.UacBypass=Simplifier le lancement : utiliser le Planificateur de tâches pour contourner la confirmation UAC
spanish.OtherTasks=Otras tareas:
spanish.UacBypass=Simplificar el inicio: usar el Programador de tareas para omitir la confirmación de UAC
italian.OtherTasks=Altre attività:
italian.UacBypass=Semplifica l'avvio: usa Utilità di pianificazione per evitare la conferma UAC

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "taskbarpin"; Description: "{cm:PinTaskbar}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "ctxmenu"; Description: "{cm:CtxTask}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "uacbypass"; Description: "{cm:UacBypass}"; GroupDescription: "{cm:OtherTasks}"

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Hudhud Unlocker"; Filename: "{app}\HudhudUnlocker.exe"; IconFilename: "{app}\HudhudUnlocker.exe"; Tasks: not uacbypass
Name: "{group}\Hudhud Unlocker"; Filename: "{sys}\schtasks.exe"; Parameters: "/run /tn ""HudhudUnlocker"""; IconFilename: "{app}\HudhudUnlocker.exe"; Flags: runminimized; Tasks: uacbypass
Name: "{autodesktop}\Hudhud Unlocker"; Filename: "{app}\HudhudUnlocker.exe"; IconFilename: "{app}\HudhudUnlocker.exe"; Tasks: desktopicon and not uacbypass
Name: "{autodesktop}\Hudhud Unlocker"; Filename: "{sys}\schtasks.exe"; Parameters: "/run /tn ""HudhudUnlocker"""; IconFilename: "{app}\HudhudUnlocker.exe"; Flags: runminimized; Tasks: desktopicon and uacbypass
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar\Hudhud Unlocker"; Filename: "{app}\HudhudUnlocker.exe"; IconFilename: "{app}\HudhudUnlocker.exe"; Tasks: taskbarpin and not uacbypass
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar\Hudhud Unlocker"; Filename: "{sys}\schtasks.exe"; Parameters: "/run /tn ""HudhudUnlocker"""; IconFilename: "{app}\HudhudUnlocker.exe"; Flags: runminimized; Tasks: taskbarpin and uacbypass

[Registry]
Root: HKCR; Subkey: "*\\shell\\HudhudUnlocker"; ValueType: string; ValueName: ""; ValueData: "{cm:CtxMenu}"; Flags: uninsdeletekey; Tasks: ctxmenu
Root: HKCR; Subkey: "*\\shell\\HudhudUnlocker"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\\HudhudUnlocker.exe"; Tasks: ctxmenu
Root: HKCR; Subkey: "*\\shell\\HudhudUnlocker\\command"; ValueType: string; ValueName: ""; ValueData: """{app}\\HudhudUnlocker.exe"" ""%1"""; Tasks: ctxmenu
Root: HKCR; Subkey: "Directory\\shell\\HudhudUnlocker"; ValueType: string; ValueName: ""; ValueData: "{cm:CtxMenu}"; Flags: uninsdeletekey; Tasks: ctxmenu
Root: HKCR; Subkey: "Directory\\shell\\HudhudUnlocker"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\\HudhudUnlocker.exe"; Tasks: ctxmenu
Root: HKCR; Subkey: "Directory\\shell\\HudhudUnlocker\\command"; ValueType: string; ValueName: ""; ValueData: """{app}\\HudhudUnlocker.exe"" ""%1"""; Tasks: ctxmenu

[Run]
Filename: "{sys}\schtasks.exe"; Parameters: "/Create /TN ""HudhudUnlocker"" /TR ""\""{app}\HudhudUnlocker.exe\"""" /SC ONCE /ST 00:00 /RL HIGHEST /F"; Flags: runhidden; Tasks: uacbypass
Filename: "{app}\HudhudUnlocker.exe"; Description: "{cm:LaunchProgram,Hudhud Unlocker}"; Flags: shellexec postinstall skipifsilent

[UninstallRun]
Filename: "{sys}\schtasks.exe"; Parameters: "/Delete /TN ""HudhudUnlocker"" /F"; Flags: runhidden; RunOnceId: "DelTask"
