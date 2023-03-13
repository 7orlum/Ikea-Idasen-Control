internal class ShowCommand : DeskConsoleCommand
{
    public ShowCommand() : base()
    {
        IsCommand("Show", "shows the current state of the Idasen desk");
        HasLongDescription("The desks must already be paired to the computer.");
    }

    public override int Run(string[] remainingArguments)
    {
        return RunWithExceptionCatching(async () => await ShowDeskStateAsync());
    }

    private async Task ShowDeskStateAsync()
    {
        using var desk = await GetDeskAsync();

        Console.WriteLine($"Name {await desk.GetNameAsync(),21}");
        Console.WriteLine($"Current height    {await desk.GetHeightAsync(),5:0} mm");
        Console.WriteLine($"Minimum height    {await desk.GetMinHeightAsync(),5:0} mm");
        for (var memoryCellNumber = 1; memoryCellNumber <= desk.Capabilities.NumberOfMemoryCells; memoryCellNumber++)
            Console.WriteLine($"Memory position {memoryCellNumber} {await desk.GetMemoryValueAsync(memoryCellNumber),5:0} mm");
    }
}