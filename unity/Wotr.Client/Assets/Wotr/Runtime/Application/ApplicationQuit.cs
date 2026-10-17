namespace Wotr.Client
{
    // MENU-14: the one place the game closes itself on the player's request. A player build quits; the editor
    // leaves Play mode.
    public static class ApplicationQuit
    {
        public static void Exit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            UnityEngine.Application.Quit();
#endif
        }
    }
}
