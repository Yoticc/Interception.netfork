using Interceptions;

// Swaps mouse move axis
class XYReverse : ISample
{
    public static void Start() => Interception.CancelableOnMouseMove += OnMouseMove;

    public static void Stop() => Interception.CancelableOnMouseMove -= OnMouseMove;

    static bool OnMouseMove(int x, int y)
    {
        Interception.MoveMouse(y, x);
        return false;
    }
}