using ManyConsole;
using System.Net.NetworkInformation;
using Windows.Devices.Bluetooth;

internal class ShowCommand : ConsoleCommand
{
    public string Address { get; set; } = null!;

    public ShowCommand()
    {
        IsCommand("Show", "shows the current state of the Idasen desk");
        HasLongDescription("The desks must already be paired to the computer.");
        HasRequiredOption("a|address=", "address of the desk like ec:02:09:df:8e:d8. You can get your desk address calling the program with the parameter List", address => Address = address?? string.Empty);
    }

    public override int Run(string[] remainingArguments)
    {
        return ShowIdasenDeskStatusAsync().Result ? 0 : 1;
    }

    private async Task<bool> ShowIdasenDeskStatusAsync()
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
        Console.WriteLine($"Name {await desk.GetNameAsync(),21}");
        Console.WriteLine($"Current height    {await desk.GetHeightAsync(),5:0} mm");
        Console.WriteLine($"Minimum height    {await desk.GetMinHeightAsync(),5:0} mm");
        for (var cellNumber = 0; cellNumber < desk.Capabilities.MemoryCells; cellNumber++)
            Console.WriteLine($"Memory position {cellNumber + 1} {await desk.GetMemoryValueAsync(cellNumber),5:0} mm");

        return true;
    }

    private ulong ToUInt64(PhysicalAddress address) =>
        BitConverter.ToUInt64(address.GetAddressBytes().Reverse().Concat(new byte[] { 0, 0 }).ToArray());
}