namespace IkeaIdasenControl.ConsoleCommands;

internal class Clear : DeskConsoleCommand
{
    public Clear() : base()
    {
        IsCommand("Clean", "cleans the Idasen desk memory position");
        HasLongDescription("The desks must already be paired to the computer.");
        HasAdditionalArguments(1, "memory cell in the format: m{number of memory cell}");
    }

    public override int Run(string[] remainingArguments)
    {
        return RunWithExceptionCatching(async () => await SetMemoryValueAsync(remainingArguments[0]));
    }

    private async Task SetMemoryValueAsync(string memoryString)
    {
        using var desk = await GetDeskAsync();

        byte memoryCellNumber;
        if (!TryParseMemoryCellNumber(memoryString, out memoryCellNumber))
            throw new WrongCommandParameterException($"Memory cell number {memoryString} is wrong. It must be like 'm1'");

        Console.WriteLine($"Clean memory cell {memoryCellNumber}");
        await desk.ClearMemoryValueAsync(memoryCellNumber);
        Console.WriteLine(await GetStateAndSettingsReport(desk, ReportSection.MinHeight | ReportSection.Memory));
    }
}