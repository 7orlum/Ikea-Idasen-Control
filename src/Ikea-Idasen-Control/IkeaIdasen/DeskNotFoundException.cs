internal class DeskNotFoundException : Exception
{
    public DeskNotFoundException() 
    { }

    public DeskNotFoundException(string message)
        : base(message)
    { }

    public DeskNotFoundException(string message, Exception inner)
        : base(message, inner)
    { }
}