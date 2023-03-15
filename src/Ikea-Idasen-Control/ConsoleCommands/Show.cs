namespace IkeaIdasenControl.ConsoleCommands;

internal class Show : DeskConsoleCommand
{
    public Show() : base()
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
        Console.WriteLine(await GetStateAndSettingsReport(desk, ReportSection.Name | ReportSection.Height | ReportSection.MinHeight | ReportSection.Memory));
    }
}