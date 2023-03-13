using ManyConsole;
using System.Net.NetworkInformation;
using Windows.Devices.Bluetooth;

internal class MoveCommand : ConsoleCommand
{
    public string Address { get; set; } = null!;

    public MoveCommand()
    {
        IsCommand("Move", "moves the Idasen desk to the specified height");
        HasLongDescription("The desks must already be paired to the computer.");
        HasRequiredOption("a|address=", "address of the desk like ec:02:09:df:8e:d8. You can get your desk address calling the program with the parameter List", address => Address = address ?? string.Empty);
        HasAdditionalArguments(1, "the target height in millimeters or a memory position in the format: m{number of memory position}.");
    }

    public override int Run(string[] remainingArguments)
    {
        return MoveIdasenDeskToTargetHeightAsync(remainingArguments[0]).Result ? 0 : 1;
    }

    private async Task<bool> MoveIdasenDeskToTargetHeightAsync(string value)
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

        float heightMm;
        if (TryParseMemoryCellNumber(value, out byte memoryCell))
        {
            try
            {
                heightMm = await desk.GetMemoryValueAsync(memoryCell - 1);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
        else if (!TryParseHeight(value, out heightMm))
        {
            Console.WriteLine($"Height {value} is wrong. It must be like '183' if you define it in millimeters, or like 'm1' if you define it as number of memory position");
            return false;
        }

        Console.WriteLine($"Moving the desk to {heightMm:0} mm");

        await desk.SetHeightAsync(heightMm);

        Console.WriteLine($"Current height is {await desk.GetHeightAsync():0} mm");

        return true;
    }

    private bool TryParseMemoryCellNumber(string value, out byte memoryCell)
    {
        memoryCell = default;

        if (value.StartsWith("m", StringComparison.InvariantCultureIgnoreCase))
            return byte.TryParse(value[1..], out memoryCell);
        else
            return false;
    }

    private bool TryParseHeight(string value, out float height)
    {
        return float.TryParse(value, out height);
    }

    private ulong ToUInt64(PhysicalAddress address) =>
        BitConverter.ToUInt64(address.GetAddressBytes().Reverse().Concat(new byte[] { 0, 0 }).ToArray());
}