namespace IkeaIdasenControl.LinakDPGController;

internal class GattCommunicationException : Exception
{
    public GattCommunicationException() 
    { }

    public GattCommunicationException(string message)
        : base(message)
    { }

    public GattCommunicationException(string message, Exception inner)
        : base(message, inner)
    { }
}