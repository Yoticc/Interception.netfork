using Interceptions;

// Opens task manager to the M key
class OpenTaskManager : ISample
{
    public static void Start() => Interception.CancelableOnKeyDown += OnKeyDown;

    public static void Stop() => Interception.CancelableOnKeyDown -= OnKeyDown;

    static Key[] TaskManagerShortcut = [Key.LControl, Key.LShift, Key.Esc];
    static bool OnKeyDown(Key key)
    {
        if (key == Key.M)
        {            
            Interception.KeyDown(TaskManagerShortcut);
            Interception.KeyUp(TaskManagerShortcut);
            return false;
        }

        return true;
    }
}