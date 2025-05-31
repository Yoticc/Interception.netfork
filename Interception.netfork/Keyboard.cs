namespace Interception;
public unsafe struct Keyboard
{
    public Device Device;

    public void Send(KeyStroke* stroke) => Device.Send(stroke);
    public bool Receive(KeyStroke* stroke) => Device.Receive(stroke);
}