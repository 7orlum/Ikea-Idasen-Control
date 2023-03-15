namespace IkeaIdasenControl.ConsoleCommands;

using IkeaIdasenControl;
using IkeaIdasenControl.LinakDPGController;
using ManyConsole;
using System.Net.NetworkInformation;
using System.Text;

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


    protected static async Task<string> GetStateAndSettingsReport(MyDesk desk, ReportSection sections)
    {
        var result = new StringBuilder();

        if (sections.HasFlag(ReportSection.Name))
            result.AppendLine($"Name {await desk.GetNameAsync(),21}");

        if (sections.HasFlag(ReportSection.Height))
            result.AppendLine($"Current height    {await desk.GetHeightAsync(),5:0} mm");
        
        if (sections.HasFlag(ReportSection.MinHeight))
            result.AppendLine($"Minimum height    {await desk.GetMinHeightAsync(),5:0} mm");

        if (sections.HasFlag(ReportSection.Memory))
            for (var memoryCellNumber = 1; memoryCellNumber <= desk.NumberOfMemoryCells; memoryCellNumber++)
            {
                var memoryValue = await desk.GetMemoryValueAsync(memoryCellNumber);
                if (memoryValue is null)
                    result.AppendLine($"Memory position {memoryCellNumber}");
                else
                    result.AppendLine($"Memory position {memoryCellNumber} {memoryValue,5:0} mm");
            }

        return result.ToString();
    }
}
