namespace IkeaIdasenControl.LinakDPGController;

public static class ServiceUUID
{
    public static Guid Name => new Guid("00001800-0000-1000-8000-00805F9B34FB");
    public static Guid Model => new Guid("00001800-0000-1000-8000-00805F9B34FB");
    public static Guid Control => new Guid("99fa0001-338a-1024-8a49-009c0215f78a");
    public static Guid DPG => new Guid("99fa0010-338a-1024-8a49-009c0215f78a");
    public static Guid HeightSpeedSensor => new Guid("99fa0020-338a-1024-8a49-009c0215f78a");
    public static Guid Input => new Guid("99fa0030-338a-1024-8a49-009c0215f78a");
}

public static class CharacteristicUUID
{
    public static Guid Name => new Guid("00002A00-0000-1000-8000-00805F9B34FB");
    public static Guid Model => new Guid("00002A24-0000-1000-8000-00805F9B34FB");
    public static Guid Control => new Guid("99fa0002-338a-1024-8a49-009c0215f78a");
    public static Guid DPG => new Guid("99fa0011-338a-1024-8a49-009c0215f78a");
    public static Guid HeightSpeedSensor => new Guid("99fa0021-338a-1024-8a49-009c0215f78a");
    public static Guid Input => new Guid("99fa0031-338a-1024-8a49-009c0215f78a");
}

public class Command
{
    byte _code;

    public static Command ProductInfo => new Command(0x08);
    public static Command Name => new Command(0x26);
    public static Command Capabilities => new Command(0x80);
    public static Command DeskOffset => new Command(0x81);
    public static Command UserID => new Command(0x86);
    public static Command ReminderSetting => new Command(0x88);
    public static Command MemoryPosition1 => new Command(0x89);
    public static Command MemoryPosition2 => new Command(0x8a);
    public static Command MemoryPosition3 => new Command(0x8b);
    public static Command MemoryPosition4 => new Command(0x8c);
    public static Command LogEntry => new Command(0x90);

    private Command(byte code) => _code = code;

    public static implicit operator byte(Command command)
    {
        return command._code;
    }
}
