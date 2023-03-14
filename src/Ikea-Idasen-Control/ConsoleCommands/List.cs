namespace IkeaIdasenControl.ConsoleCommands;

using ManyConsole;
using Windows.Devices.Enumeration;

internal class List : ConsoleCommand
{
    public List()
    {
        IsCommand("List", "shows the list of Idasen desks");
        HasLongDescription("The desks must already be paired to the computer to appear in the list.");
    }

    public override int Run(string[] remainingArguments)
    {
        Console.WriteLine("Please wait, the list of devices is forming");
        Console.WriteLine();

        var devices = DeskListAsync().Result;
        Console.WriteLine("Address\t\t\tName\t\tStatus");
        foreach (var device in devices)
            Console.WriteLine($"{device.Address}\t{device.Name}\t{device.Status}");

        return 0;
    }

    private async Task<IEnumerable<Device>> DeskListAsync()
    {
        var result = new List<Device>();

        //ProtocolId == Bluetooh LE and IsPaired == True
        var deviceFilter = "System.Devices.Aep.ProtocolId:= \"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\""; // AND System.Devices.Aep.IsPaired:=System.StructuredQueryType.Boolean#True";
        var deviceKind = DeviceInformationKind.AssociationEndpoint;
        var additionalProperties = new []{ "System.Devices.Aep.DeviceAddress" };
        foreach (var device in await DeviceInformation.FindAllAsync(deviceFilter, additionalProperties, deviceKind))
            result.Add(new Device(
                Address: (string)device.Properties["System.Devices.Aep.DeviceAddress"],
                Name: device.Name,
                Status: (bool)device.Properties["System.Devices.Aep.IsPaired"] ? "Paired" : ""
                ));
        
        return result
            .OrderBy(device => device.Name)
            .ThenBy(device => device.Address)
            .ToArray();
    }

    private record Device(string Address, string Name, string Status);
}