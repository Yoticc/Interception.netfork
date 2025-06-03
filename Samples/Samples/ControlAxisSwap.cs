using Interceptions;
// Reverse the x y coordinates when left control is pressed 
class ControlAxisSwap : ISample
{
    public static void Start() => Interception.CancelableOnMouseMove += OnMouseMove;

    public static void Stop() => Interception.CancelableOnMouseMove -= OnMouseMove;

    static bool OnMouseMove(int x, int y)
    {
        if (Interception.IsKeyDown(Key.LControl))
        {
            Interception.MoveMouse(-x, -y);

            return false;
        }

        return true;
    }
}