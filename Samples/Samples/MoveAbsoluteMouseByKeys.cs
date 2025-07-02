using Interceptions;
// Move mouse by absolute coordinations when key presses
class MoveAbsoluteMouseByKeys : ISample
{
    public static void Start() => Interception.OnKeyUp += OnKeyUp;

    public static void Stop() => Interception.OnKeyUp -= OnKeyUp;

    static void OnKeyUp(Key key)
    {
        if (key == Key.Q)
            Interception.SetMouse(100, 100);
        else if (key == Key.W)
            Interception.SetMouse(200, 200);
        else if (key == Key.E)
            Interception.SetMouse(300, 300);
        else if (key == Key.R)
            Interception.SetMouse(400, 400);
    }
}