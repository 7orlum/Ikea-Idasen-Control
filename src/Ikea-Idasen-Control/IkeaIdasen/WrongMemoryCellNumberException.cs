internal class WrongMemoryCellNumberException : Exception
{
    public WrongMemoryCellNumberException() 
    { }

    public WrongMemoryCellNumberException(string message)
        : base(message)
    { }

    public WrongMemoryCellNumberException(string message, Exception inner)
        : base(message, inner)
    { }
}