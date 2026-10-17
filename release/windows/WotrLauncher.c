// DIST-06: replacement for Unity's Wotr.exe in the Windows installer (spike S2). Velopack runs the main executable
// with --veloapp-install|updated|obsolete|uninstall <version> and waits for exit code 0; Unity's own executable would
// open the game window instead. This one answers those hooks at once without loading UnityPlayer.dll and otherwise
// does what Unity's WindowsPlayer Main.cpp does: hands the command line to UnityMain. The executable keeps Unity's
// name, because the player finds its data folder (Wotr_Data) by it. Built by release/windows/build-launcher.sh.
#include <windows.h>
#include <shellapi.h>
#include <wchar.h>

// Unity's Main.cpp exports these so that hybrid-GPU laptops start the game on the discrete GPU.
__declspec(dllexport) DWORD NvOptimusEnablement = 0x00000001;
__declspec(dllexport) int AmdPowerXpressRequestHighPerformance = 1;

typedef int (*UnityMainFunction)(HINSTANCE, HINSTANCE, LPWSTR, int);

static int IsVelopackHook(void)
{
    int count = 0;
    LPWSTR *arguments = CommandLineToArgvW(GetCommandLineW(), &count);
    int hook = 0;
    if (arguments == NULL)
        return 0;
    for (int i = 1; i < count && !hook; i++)
        hook = wcsncmp(arguments[i], L"--veloapp-", 10) == 0;
    LocalFree(arguments);
    return hook;
}

int WINAPI wWinMain(HINSTANCE instance, HINSTANCE previous, LPWSTR commandLine, int show)
{
    if (IsVelopackHook())
        return 0;
    HMODULE player = LoadLibraryW(L"UnityPlayer.dll");
    // Through void (*)(void): the generic function pointer type that FARPROC may be cast from without a warning.
    UnityMainFunction unityMain = player ? (UnityMainFunction)(void (*)(void))GetProcAddress(player, "UnityMain") : NULL;
    if (unityMain == NULL)
    {
        MessageBoxW(NULL, L"UnityPlayer.dll is missing or damaged. Reinstall War of the Ring Online.",
            L"War of the Ring Online", MB_OK | MB_ICONERROR);
        return 1;
    }
    return unityMain(instance, previous, commandLine, show);
}
