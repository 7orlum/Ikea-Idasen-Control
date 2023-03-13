internal class MoveCommand : DeskConsoleCommand
{
    public MoveCommand() : base()
    {
        IsCommand("Move", "moves the Idasen desk to the specified height");
        HasLongDescription("The desks must already be paired to the computer.");
        HasAdditionalArguments(1, "the target height in millimeters or a memory position in the format: m{number of memory position}.");
    }

    public override int Run(string[] remainingArguments)
    {
        return MoveIdasenDeskToTargetHeightAsync(remainingArguments[0]).Result ? 0 : 1;
    }

    private async Task<bool> MoveIdasenDeskToTargetHeightAsync(string value)
    {
        using var desk = await GetDeskAsync();

        float height;
        if (TryParseMemoryCellNumber(value, out byte memoryCellNumber))
        {
            try
            {
                height = await desk.GetMemoryValueAsync(memoryCellNumber);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
        else if (!TryParseHeight(value, out height))
        {
            Console.WriteLine($"Height {value} is wrong. It must be like '183' if you define it in millimeters, or like 'm1' if you define it as number of memory position");
            return false;
        }

        Console.WriteLine($"Moving the desk to {height:0} mm");

        await desk.SetHeightAsync(height);

        Console.WriteLine($"Current height is {await desk.GetHeightAsync():0} mm");

        return true;
    }
}