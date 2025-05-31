using InterceptionInternal;

class SwapShiftAndCapslock : ISample
{
    public static void Start()
    {
        Interception.CancelableOnKeyDown += OnKeyDown;
        Interception.CancelableOnKeyUp += OnKeyUp;
    }

    public static void Stop()
    {
        Interception.CancelableOnKeyDown -= OnKeyDown;
        Interception.CancelableOnKeyUp -= OnKeyUp;
    }

    static bool OnKeyDown(Key key)
    {
        if (key == Key.LShift)
        {
            Interception.KeyDown(Key.CapsLock);
            return false;
        }

        if (key == Key.CapsLock)
        {
            Interception.KeyDown(Key.LShift);
            return false;
        }

        return true;
    }

    static bool OnKeyUp(Key key)
    {
        if (key == Key.LShift)
        {
            Interception.KeyUp(Key.CapsLock);
            return false;
        }

        if (key == Key.CapsLock)
        {
            Interception.KeyUp(Key.LShift);
            return false;
        }

        return true;
    }
}