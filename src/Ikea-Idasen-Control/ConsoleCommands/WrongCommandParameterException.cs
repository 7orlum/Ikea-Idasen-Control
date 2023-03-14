namespace IkeaIdasenControl.ConsoleCommands;

internal class WrongCommandParameterException : Exception
{
    public WrongCommandParameterException() 
    { }

    public WrongCommandParameterException(string message)
        : base(message)
    { }

    public WrongCommandParameterException(string message, Exception inner)
        : base(message, inner)
    { }
}