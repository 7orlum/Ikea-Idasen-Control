namespace IkeaIdasenControl.ConsoleCommands;

using IkeaIdasenControl;
using IkeaIdasenControl.LinakDPGController;
using ManyConsole;
using System.Net.NetworkInformation;

public abstract class DeskConsoleCommand : ConsoleCommand
{
    public string Address { get; set; } = null!;

    public DeskConsoleCommand()
    {
        HasRequiredOption("a|address=", "address of the desk like ec:02:09:df:8e:d8. You can get your desk address calling the program with the parameter List", address => Address = address ?? string.Empty);
    }

    protected async Task<MyDesk> GetDeskAsync()
    {
        if (!PhysicalAddress.TryParse(Address, out var physicalAddress))
            throw new WrongCommandParameterException($"Address {Address} is wrong. It must be like 'ec:02:09:df:8e:d8'. You can get your desk address calling the program with the parameter List");

        return await MyDesk.ConnectAsync(physicalAddress);
    }

    protected bool TryParseHeight(string value, out float height)
    {
        return float.TryParse(value, out height);
    }

    protected bool TryParseCurrent(string value)
    {
        return value.Equals("current", StringComparison.InvariantCultureIgnoreCase);
    }

    protected bool TryParseMemoryCellNumber(string value, out byte memoryCellNumber)
    {
        memoryCellNumber = default;

        if (value.StartsWith("m", StringComparison.InvariantCultureIgnoreCase))
            return byte.TryParse(value[1..], out memoryCellNumber);
        else
            return false;
    }

    protected int RunWithExceptionCatching(Func<Task> action)
    {
        try
        {
            action().Wait();
            return 0;
        }
        catch (AggregateException ex)
        {
            foreach(var e in ex.Flatten().InnerExceptions)
            {
                if (e is DeskNotFoundException ||
                    e is GattCommunicationException ||
                    e is WrongMemoryCellNumberException ||
                    e is WrongCommandParameterException)
                {
                    Console.WriteLine(e.Message);
                }
                else if (e is System.Runtime.InteropServices.COMException)
                {
                    Console.WriteLine($"Desk connection lost");
                }
                else
                    throw;
            };
            return 1;
        }
    }
}