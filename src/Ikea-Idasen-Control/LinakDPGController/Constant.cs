namespace IkeaIdasenControl.LinakDPGController;

public static class Constant
{
    public const string NameServiceUUID = "00001800-0000-1000-8000-00805F9B34FB";
    public const string NameCharacteristicUUID = "00002A00-0000-1000-8000-00805F9B34FB";
    public const string ModelServiceUUID = "00001800-0000-1000-8000-00805F9B34FB";
    public const string ModelCharacteristicUUID = "00002A24-0000-1000-8000-00805F9B34FB";
    public const string ControlServiceUUID = "99fa0001-338a-1024-8a49-009c0215f78a";
    public const string ControlCharacteristicUUID = "99fa0002-338a-1024-8a49-009c0215f78a";
    public const string DPGServiceUUID = "99fa0010-338a-1024-8a49-009c0215f78a";
    public const string DPGCharacteristicUUID = "99fa0011-338a-1024-8a49-009c0215f78a";
    public const string HeightSpeedSensorServiceUUID = "99fa0020-338a-1024-8a49-009c0215f78a";
    public const string HeightSpeedSensorCharacteristicUUID = "99fa0021-338a-1024-8a49-009c0215f78a";
    public const string InputServiceUUID = "99fa0030-338a-1024-8a49-009c0215f78a";
    public const string InputCharacteristicUUID = "99fa0031-338a-1024-8a49-009c0215f78a";


    public enum Command : byte
    {
        ProductInfo = 0x08,
        Name = 0x26,
        Capabilities = 0x80,
        DeskOffset = 0x81,
        UserID = 0x86,
        ReminderSetting = 0x88,
        MemoryPosition1 = 0x89,
        MemoryPosition2 = 0x8a,
        MemoryPosition3 = 0x8b,
        MemoryPosition4 = 0x8c,
        LogEntry = 0x90,
    }
}
