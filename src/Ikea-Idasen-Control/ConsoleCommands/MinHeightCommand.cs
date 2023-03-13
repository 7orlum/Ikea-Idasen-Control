using ManyConsole;
using System.Net.NetworkInformation;
using Windows.Devices.Bluetooth;

internal class MinHeightCommand : ConsoleCommand
{
    public string Address { get; set; } = null!;

    public MinHeightCommand()
    {
        IsCommand("MinHeight", "defines the Idasen desk height in the lowest position");
        HasLongDescription("The desks must already be paired to the computer.");
        HasRequiredOption("a|address=", "address of the desk like ec:02:09:df:8e:d8. You can get your desk address calling the program with the parameter List", address => Address = address ?? string.Empty);
        HasAdditionalArguments(1, "the desk's height in millimeters measured in the desk's the lowest position.");
    }

    public override int Run(string[] remainingArguments)
    {
        return SetMinHeightAsync(remainingArguments[0]).Result ? 0 : 1;
    }

    private async Task<bool> SetMinHeightAsync(string heightString)
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

        if (!TryParseHeight(heightString, out float heightMm))
        {
            Console.WriteLine($"Height {heightString} is wrong. It must be like '183'");
            return false;
        }

        Console.WriteLine($"Writing {heightMm:0} mm as the desk's height in the the lowest position");

        ushort heightRaw = desk.RawFromMmAsync(heightMm - desk.MmFromRaw(await desk.GetOffsetRawAsync()));
        await desk.SetOffsetRawAsync(heightRaw);

        for (var cellNumber = 0; cellNumber < desk.Capabilities.MemoryCells; cellNumber++)
            Console.WriteLine($"Memory position {cellNumber + 1} {desk.MmFromRaw(await desk.GetOffsetRawAsync() + await desk.GetMemoryPositionRawAsync(cellNumber)),5:0} mm");
        Console.WriteLine($"Minimum height    {desk.MmFromRaw(await desk.GetOffsetRawAsync()),5:0} mm");

        return true;
    }

    private bool TryParseHeight(string value, out float height)
    {
        return float.TryParse(value, out height);
    }

    private ulong ToUInt64(PhysicalAddress address) =>
        BitConverter.ToUInt64(address.GetAddressBytes().Reverse().Concat(new byte[] { 0, 0 }).ToArray());
}