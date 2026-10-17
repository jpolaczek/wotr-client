namespace Wotr.Client
{
    // MENU-12: how the game window fills the screen. Fullscreen takes the screen over (exclusive on Windows, the
    // system's full-screen space on macOS), Borderless is a window the size of the screen, Windowed has a frame.
    public enum WindowMode
    {
        Borderless = 0,
        Fullscreen = 1,
        Windowed = 2
    }
}
