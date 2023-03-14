namespace IkeaIdasenControl.ConsoleCommands;

internal class Move : DeskConsoleCommand
{
    public Move() : base()
    {
        IsCommand("Move", "moves the Idasen desk to the specified height");
        HasLongDescription("The desks must already be paired to the computer.");
        HasAdditionalArguments(1, "the target height in millimeters or a memory position in the format: m{number of memory position}.");
    }

    public override int Run(string[] remainingArguments)
    {
        return RunWithExceptionCatching(async () => await SetHeightAsync(remainingArguments[0]));
    }

    private async Task SetHeightAsync(string heightString)
    {
        using var desk = await GetDeskAsync();

        float height;
        if (TryParseMemoryCellNumber(heightString, out byte memoryCellNumber))
            height = await desk.GetMemoryValueAsync(memoryCellNumber);
        else if (!TryParseHeight(heightString, out height))
            throw new WrongCommandParameterException($"Height {heightString} is wrong. It must be like '183' if you define it in millimeters, or like 'm1' if you define it as number of memory position");

        Console.WriteLine($"Moving the desk to {height:0} mm");
        await desk.SetHeightAsync(height);
        Console.WriteLine($"Current height is {await desk.GetHeightAsync():0} mm");
    }
}