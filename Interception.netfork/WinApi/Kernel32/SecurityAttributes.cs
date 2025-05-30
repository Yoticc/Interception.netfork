unsafe struct SecurityAttributes
{
    public int Length;
    public void* SecurityDescriptor;
    public bool InheritHandle;
}