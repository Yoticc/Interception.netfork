namespace Interception;
public unsafe struct Mouse
{
    public Device Device;

    public void Send(MouseStroke* stroke) => Device.Send(stroke);
    public bool Receive(MouseStroke* stroke) => Device.Receive(stroke);
}
