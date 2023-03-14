namespace IkeaIdasenControl;

using ManyConsole;

public class Program
{
    public static int Main(string[] args)
    {
        var commands = GetCommands();
        return ConsoleCommandDispatcher.DispatchCommand(commands, args, Console.Out);
    }

    public static IEnumerable<ConsoleCommand> GetCommands()
    {
        return ConsoleCommandDispatcher.FindCommandsInSameAssemblyAs(typeof(Program));
    }
}