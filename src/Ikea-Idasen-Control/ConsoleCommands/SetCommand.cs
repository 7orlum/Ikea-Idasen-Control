using ManyConsole;
using System.Net.NetworkInformation;
using Windows.Devices.Bluetooth;

internal class SetCommand : ConsoleCommand
{
    public string Address { get; set; } = null!;

    public SetCommand()
    {
        IsCommand("Set", "sets the Idasen desk memory position to the specified height");
        HasLongDescription("The desks must already be paired to the computer.");
        HasRequiredOption("a|address=", "address of the desk like ec:02:09:df:8e:d8. You can get your desk address calling the program with the parameter List", address => Address = address ?? string.Empty);
        HasAdditionalArguments(2, "memory cell in the format: m{number of memory cell} and the target height in millimeters which be written to the memory cell or 'current' to write current height.");
    }

    public override int Run(string[] remainingArguments)
    {
        return RememberHeightToMemoryAsync(remainingArguments[0], remainingArguments[1]).Result ? 0 : 1;
    }

    private async Task<bool> RememberHeightToMemoryAsync(string memoryString, string heightString)
    {
        if (!PhysicalAddress.TryParse(Address, out var physicalAddress))
        {
            Console.WriteLine($"Address {Address} is wrong. It must be like 'ec:02:09:df:8e:d8'. You can get your desk address calling the program with the parameter List");
            return false;
        }

        using var device = await BluetoothLEDevice.FromBluetoothAddressAsync(ToUInt64(physicalAddress));
        if (device == null)
        {
            Console.WriteLine($"Idasen desk {Address} not found");
            return false;
        }

        using var desk = await Desk.ConnectAsync(device);

        if (!TryParseMemoryCellNumber(memoryString, out byte cellNumber))
        {
            Console.WriteLine($"Memory cell number {memoryString} is wrong. It must be like 'm1'");
            return false;
        }

        float heightMm;
        if (TryParseCurrent(heightString))
        {
            heightMm = await desk.GetHeightAsync();
        }
        else if (!TryParseHeight(heightString, out heightMm))
        {
            Console.WriteLine($"Height {heightString} is wrong. It must be like '183' if you define it in millimeters, or 'current' if you want to get the current height");
            return false;
        }

        Console.WriteLine($"Writing {heightMm:0} mm into memory cell {cellNumber}");

        await desk.SetMemoryValueAsync(cellNumber, heightMm);

        for (cellNumber = 1; cellNumber <= desk.Capabilities.NumberOfMemoryCells; cellNumber++)
            Console.WriteLine($"Memory position {cellNumber} {await desk.GetMemoryValueAsync(cellNumber),5:0} mm");
        Console.WriteLine($"Minimum height    {await desk.GetMinHeightAsync(),5:0} mm");

        return true;
    }

    private bool TryParseMemoryCellNumber(string value, out byte cellNumber)
    {
        cellNumber = default;

        if (value.StartsWith("m", StringComparison.InvariantCultureIgnoreCase))
            return byte.TryParse(value[1..], out cellNumber);
        else
            return false;
    }

    private bool TryParseCurrent(string value)
    {
        return value.Equals("current", StringComparison.InvariantCultureIgnoreCase);
    }

    private bool TryParseHeight(string value, out float height)
    {
        return float.TryParse(value, out height);
    }

    private ulong ToUInt64(PhysicalAddress address) =>
        BitConverter.ToUInt64(address.GetAddressBytes().Reverse().Concat(new byte[] { 0, 0 }).ToArray());
}