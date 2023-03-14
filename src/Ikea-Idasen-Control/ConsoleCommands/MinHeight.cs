namespace IkeaIdasenControl.ConsoleCommands;

internal class MinHeight : DeskConsoleCommand
{
    public MinHeight() : base()
    {
        IsCommand("MinHeight", "defines the Idasen desk height in the lowest position");
        HasLongDescription("The desks must already be paired to the computer.");
        HasAdditionalArguments(1, "the desk's height in millimeters measured in the desk's the lowest position.");
    }

    public override int Run(string[] remainingArguments)
    {
        return RunWithExceptionCatching(async () => await SetMinHeightAsync(remainingArguments[0]));
    }

    private async Task SetMinHeightAsync(string heightString)
    {
        using var desk = await GetDeskAsync();

        float height;
        if (!TryParseHeight(heightString, out height))
            throw new WrongCommandParameterException($"Height {heightString} is wrong. It must be like '183'");

        Console.WriteLine($"Writing {height:0} mm as the desk's height in the the lowest position");
        await desk.SetMinHeightAsync(height);
        for (var memoryCellNumber = 1; memoryCellNumber <= desk.Capabilities.NumberOfMemoryCells; memoryCellNumber++)
            Console.WriteLine($"Memory position {memoryCellNumber} {desk.GetMemoryValueAsync(memoryCellNumber),5:0} mm");
        Console.WriteLine($"Minimum height    {desk.GetMinHeightAsync(),5:0} mm");
    }
}