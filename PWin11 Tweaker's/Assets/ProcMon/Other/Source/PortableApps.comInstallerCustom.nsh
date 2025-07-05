!macro CustomCodePreInstall
	${If} ${FileExists} "$INSTDIR\App\ProcessMonitor-Modern"
		Rename "$INSTDIR\App\ProcessMonitor" "$INSTDIR\App\ProcessMonitor-Legacy"
		Rename "$INSTDIR\App\ProcessMonitor-Modern" "$INSTDIR\App\ProcessMonitor"
	${EndIf}
!macroend
