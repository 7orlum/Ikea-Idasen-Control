namespace IkeaIdasenControl.ConsoleCommands;

internal class Set : DeskConsoleCommand
{
    public Set() : base()
    {
        IsCommand("Set", "sets the Idasen desk memory position to the specified height");
        HasLongDescription("The desks must already be paired to the computer.");
        HasAdditionalArguments(2, "memory cell in the format: m{number of memory cell} and the target height in millimeters which be written to the memory cell or 'current' to write current height.");
    }

    public override int Run(string[] remainingArguments)
    {
        return RunWithExceptionCatching(async () => await SetMemoryValueAsync(remainingArguments[0], remainingArguments[1]));
    }

    private async Task SetMemoryValueAsync(string memoryString, string heightString)
    {
        using var desk = await GetDeskAsync();

        byte memoryCellNumber;
        if (!TryParseMemoryCellNumber(memoryString, out memoryCellNumber))
            throw new WrongCommandParameterException($"Memory cell number {memoryString} is wrong. It must be like 'm1'");

        float height;
        if (TryParseCurrent(heightString))
            height = await desk.GetHeightAsync();
        else if (!TryParseHeight(heightString, out height))
            throw new WrongCommandParameterException($"Height {heightString} is wrong. It must be like '183' if you define it in millimeters, or 'current' if you want to get the current height");

        Console.WriteLine($"Writing {height:0} mm into memory cell {memoryCellNumber}");
        await desk.SetMemoryValueAsync(memoryCellNumber, height);
        for (memoryCellNumber = 1; memoryCellNumber <= desk.Capabilities.NumberOfMemoryCells; memoryCellNumber++)
            Console.WriteLine($"Memory position {memoryCellNumber} {await desk.GetMemoryValueAsync(memoryCellNumber),5:0} mm");
        Console.WriteLine($"Minimum height    {await desk.GetMinHeightAsync(),5:0} mm");
    }
}