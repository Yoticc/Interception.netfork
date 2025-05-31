using System.Runtime.InteropServices;

// Disables software mouse acceleration in windows, if it is disabled, it has no effect
unsafe class DisableMouseAcceleration : ISample
{
    public static void Start() => Interception.CancelableOnMouseMove += OnMouseMove;

    public static void Stop() => Interception.CancelableOnMouseMove -= OnMouseMove;

    static bool OnMouseMove(int x, int y)
    {
        var cursor = stackalloc int[2];
        GetCursorPos(cursor);
        SetCursorPos(cursor[0] + x, cursor[1] + y);

        return false;
    }

    [DllImport("user32")] public static extern bool GetCursorPos(int* point);
    [DllImport("user32")] public static extern bool SetCursorPos(int x, int y);
}