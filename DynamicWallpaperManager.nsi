; Use Unicode for proper Chinese character support
Unicode True

!define PRODUCT_NAME "DynamicWallpaperManager"
!define PRODUCT_VERSION "1.0.1"
!define PRODUCT_PUBLISHER "Jacky Zheng"

; Include Modern UI
!include "MUI2.nsh"
!include "nsDialogs.nsh"
!include "LogicLib.nsh"

; Function to kill running process
!macro KillProcess
    DetailPrint "正在检查并关闭运行中的程序..."
    nsExec::ExecToStack 'taskkill /F /IM "DynamicWallpaperManager.exe" /T'
    Pop $0 ; Return value
    Pop $1 ; Output
    ${If} $0 == 0
        DetailPrint "已关闭运行中的程序"
    ${Else}
        DetailPrint "未发现运行中的程序"
    ${EndIf}
    Sleep 500 ; Wait for process to fully terminate
!macroend

; General settings
Name "${PRODUCT_NAME} ${PRODUCT_VERSION}"
OutFile "${PRODUCT_NAME}-${PRODUCT_VERSION}.exe"
InstallDir "$APPDATA\DynamicWallpaperManager"
InstallDirRegKey HKCU "Software\${PRODUCT_NAME}" "InstallDir"
RequestExecutionLevel user

; Interface Settings
!define MUI_ABORTWARNING
!define MUI_ICON "${NSISDIR}\Contrib\Graphics\Icons\modern-install.ico"
!define MUI_UNICON "${NSISDIR}\Contrib\Graphics\Icons\modern-uninstall.ico"

; Welcome page
!define MUI_WELCOMEPAGE_TITLE "欢迎安装 ${PRODUCT_NAME}"
!define MUI_WELCOMEPAGE_TEXT "这将在您的计算机上安装 ${PRODUCT_NAME} ${PRODUCT_VERSION}。$\r$\n$\r$\n${PRODUCT_NAME} 是一个动态壁纸管理工具，让您的桌面更加生动。$\r$\n$\r$\n建议在继续之前关闭所有其他应用程序。$\r$\n$\r$\n点击 下一步 继续。"
!insertmacro MUI_PAGE_WELCOME

; Directory page
!insertmacro MUI_PAGE_DIRECTORY

; Instfiles page
!insertmacro MUI_PAGE_INSTFILES

; Finish page
!define MUI_FINISHPAGE_TITLE "安装完成"
!define MUI_FINISHPAGE_TEXT "${PRODUCT_NAME} 已成功安装到您的计算机。$\r$\n$\r$\n点击 完成 关闭此向导。"
!define MUI_FINISHPAGE_RUN "$INSTDIR\DynamicWallpaperManager.exe"
!define MUI_FINISHPAGE_RUN_TEXT "运行 ${PRODUCT_NAME}"
!define MUI_FINISHPAGE_SHOWREADME ""
!define MUI_FINISHPAGE_SHOWREADME_TEXT "在桌面创建快捷方式"
!define MUI_FINISHPAGE_SHOWREADME_FUNCTION CreateDesktopShortcut
!define MUI_FINISHPAGE_SHOWREADME_NOTCHECKED
!define MUI_FINISHPAGE_NOREBOOTSUPPORT
!insertmacro MUI_PAGE_FINISH

; Uninstaller pages
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

; Language
!insertmacro MUI_LANGUAGE "SimpChinese"

; Initialization function - kill process before installation
Function .onInit
    !insertmacro KillProcess
FunctionEnd

; Installation section
Section "主程序" SecMain
    SectionIn RO
    SetOutPath "$INSTDIR"
    File /r "bin\x86\Release\net8.0-windows\runtimes"
    File /r "bin\x86\Release\net8.0-windows\*.dll"
    File "bin\x86\Release\net8.0-windows\DynamicWallpaperManager.exe"
    File "bin\x86\Release\net8.0-windows\preview.jpg"
    File "bin\x86\Release\net8.0-windows\project.json"
    File /r "bin\x86\Release\net8.0-windows\assets\*.*"
    File "bin\x86\Release\net8.0-windows\DynamicWallpaperManager.dll.config"
    File "bin\x86\Release\net8.0-windows\DynamicWallpaperManager.deps.json"
    File "bin\x86\Release\net8.0-windows\DynamicWallpaperManager.runtimeconfig.json"
    File "bin\x86\Release\net8.0-windows\app.manifest"

    ; Write registry keys for uninstaller
    WriteRegStr HKCU "Software\${PRODUCT_NAME}" "InstallDir" "$INSTDIR"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}" "DisplayName" "${PRODUCT_NAME}"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}" "UninstallString" "$INSTDIR\Uninstall.exe"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}" "DisplayVersion" "${PRODUCT_VERSION}"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}" "Publisher" "${PRODUCT_PUBLISHER}"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}" "DisplayIcon" "$INSTDIR\DynamicWallpaperManager.exe"
    WriteUninstaller "$INSTDIR\Uninstall.exe"

    ; Create Start Menu shortcuts
    CreateDirectory "$SMPROGRAMS\${PRODUCT_NAME}"
    CreateShortcut "$SMPROGRAMS\${PRODUCT_NAME}\${PRODUCT_NAME}.lnk" "$INSTDIR\DynamicWallpaperManager.exe"
    CreateShortcut "$SMPROGRAMS\${PRODUCT_NAME}\卸载.lnk" "$INSTDIR\Uninstall.exe"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "${PRODUCT_NAME}" "$INSTDIR\DynamicWallpaperManager.exe -autostart"
SectionEnd

; Desktop shortcut function
Function CreateDesktopShortcut
    CreateShortcut "$DESKTOP\${PRODUCT_NAME}.lnk" "$INSTDIR\DynamicWallpaperManager.exe" \
                                    "-Show"
FunctionEnd

; Uninstaller section
Section "Uninstall"
    ; Kill running process before uninstallation
    !insertmacro KillProcess

    ; Remove files
    Delete "$INSTDIR\DynamicWallpaperManager.exe"
    Delete "$INSTDIR\*.dll"
    Delete "$INSTDIR\preview.jpg"
    Delete "$INSTDIR\project.json"
    Delete "$INSTDIR\assets\*.*"
    RMDir "$INSTDIR\assets"
    Delete "$INSTDIR\Uninstall.exe"
    Delete "$INSTDIR\app.manifest"
    Delete "$INSTDIR\runtimes\*.*"
    RMDir "$INSTDIR\runtimes"
    Delete "$INSTDIR\DynamicWallpaperManager.dll.config"
    Delete "$INSTDIR\DynamicWallpaperManager.deps.json"
    Delete "$INSTDIR\DynamicWallpaperManager.runtimeconfig.json"
    Delete "$INSTDIR\LOG\*.*"
    RMDir "$INSTDIR\LOG"
    RMDir "$INSTDIR"
    
    ; Remove shortcuts
    Delete "$DESKTOP\${PRODUCT_NAME}.lnk"
    Delete "$SMPROGRAMS\${PRODUCT_NAME}\${PRODUCT_NAME}.lnk"
    Delete "$SMPROGRAMS\${PRODUCT_NAME}\卸载.lnk"
    RMDir "$SMPROGRAMS\${PRODUCT_NAME}"

    ; Remove registry keys
    DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}"
    DeleteRegKey HKCU "Software\${PRODUCT_NAME}"
    DeleteRegValue HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "${PRODUCT_NAME}"
SectionEnd