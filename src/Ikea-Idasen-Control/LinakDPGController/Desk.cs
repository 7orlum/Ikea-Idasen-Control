namespace IkeaIdasenControl.LinakDPGController;

using System;
using System.Buffers.Binary;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

///<summary>
///Counts the height from the lowest table level in tenths of a millimeter
///</summary>
public class Desk : IDisposable
{
    private BluetoothLEDevice _device = null!;
    private DeskCapabilities _capabilities = null!;
    private GattCharacteristic _name = null!;
    //private GattCharacteristic _model = null!;
    private GattCharacteristic _control = null!;
    private GattCharacteristic _dpg = null!;
    private GattCharacteristic _heightSpeedSensor = null!;
    private GattCharacteristic _input = null!;

    public DeskCapabilities Capabilities => _capabilities;

    public static async Task<Desk> ConnectAsync(PhysicalAddress bluetoothAddress)
    {
        var result = new Desk()
        {
            _device =
                await BluetoothLEDevice.FromBluetoothAddressAsync(PhysicalAddressToUInt64(bluetoothAddress)) ??
                throw new DeskNotFoundException("Desk not found")
        };
        
        await result.ConnectAsync();
        result._capabilities = await result.GetCapabilities();
        
        return result;
    }

    private Desk() {}

    public async Task<string> GetNameAsync()
    {
        return await ReadStringAsync(_name);
    }

    public async Task SetNameAsync(string value)
    {
        await WriteStringAsync(_name, value);
    }

    public async Task<byte[]> GetUserIdAsync()
    {
        return (await QueryDPGAsync(Command.UserID))[1..];
    }

    public async Task SetUserIdAsync(byte[] value)
    {
        _ = await QueryDPGAsync(Command.UserID, value);
    }

    public async Task<ushort> GetOffsetAsync()
    {
        var responce = await QueryDPGAsync(Command.DeskOffset);

        //responce format is invalid
        if (responce.Length < 1)
            throw new InvalidDataException();

        //offset value is not set
        if (responce[0] == 0)
            return 0;

        //responce format is invalid
        if (responce.Length < 3)
            throw new InvalidDataException();

        return ReadUInt16LittleEndian(responce, 1);
    }

    public async Task SetOffsetAsync(ushort value)
    {
        _ = await QueryDPGAsync(Command.DeskOffset, UInt16AsLittleEndian(value));
    }

    public async Task<ushort> GetHeightAsync()
    {
        using var dataReader = await ReadDataAsync(_heightSpeedSensor);
        var height = dataReader.ReadUInt16();
        var speed = dataReader.ReadUInt16();
        return height;
    }

    public async Task SetHeightAsync(ushort targetHeightRaw)
    {
        const int stopAfterAttempts = 5;

        await StartMovementAsync();

        var previousHeight = await GetHeightAsync();
        for (var attempts = 0; attempts < stopAfterAttempts; attempts++)
        {
            await WriteUInt16Async(_input, targetHeightRaw);

            var currentHeight = await GetHeightAsync();
            var isMovementDetected = currentHeight != previousHeight;
            previousHeight = currentHeight;

            if (isMovementDetected)
                attempts = -1;
        }

        await FinishMovementAsync();
    }

    public async Task<ushort?> GetMemoryValueAsync(int cellNumber)
    {
        var responce = await QueryDPGAsync(GetMemoryPositionCommand(cellNumber));

        //responce format is invalid
        if (responce.Length < 1)
            throw new InvalidDataException();

        //memory value is not set
        if (responce[0] == 0)
            return null;

        //responce format is invalid
        if (responce.Length < 3)
            throw new InvalidDataException();

        return ReadUInt16LittleEndian(responce, 1);
    }

    public async Task SetMemoryValueAsync(int cellNumber, ushort value)
    {
        _ = await QueryDPGAsync(GetMemoryPositionCommand(cellNumber), UInt16AsLittleEndian(value).ToArray());
    }

    public async Task ClearMemoryValueAsync(int cellNumber)
    {
        _ = await QueryDPGAsync(GetMemoryPositionCommand(cellNumber), new byte[] { 0xFF, 0xFF });
    }

    public void Dispose()
    {
        Disconnect();
    }

    private async Task ConnectAsync()
    {
        _name = await GetCharacteristicAsync(ServiceUUID.Name, CharacteristicUUID.Name);
        //_model = await GetCharacteristicAsync(ServiceUUID.Model, CharacteristicUUID.Model);
        _control = await GetCharacteristicAsync(ServiceUUID.Control, CharacteristicUUID.Control);
        _dpg = await GetCharacteristicAsync(ServiceUUID.DPG, CharacteristicUUID.DPG);
        _heightSpeedSensor = await GetCharacteristicAsync(ServiceUUID.HeightSpeedSensor, CharacteristicUUID.HeightSpeedSensor);
        _input = await GetCharacteristicAsync(ServiceUUID.Input, CharacteristicUUID.Input);
    }

    private void Disconnect()
    {
        _device?.Dispose();
        _name?.Service?.Dispose();
        //_model?.Service?.Dispose();
        _control?.Service?.Dispose();
        _dpg?.Service?.Dispose();
        _heightSpeedSensor?.Service?.Dispose();
        _input?.Service?.Dispose();
    }

    private async Task<DeskCapabilities> GetCapabilities()
    {
        var responce = await QueryDPGAsync(Command.Capabilities);

        //responce format is invalid
        if (responce.Length < 1)
            throw new InvalidDataException();

        return new DeskCapabilities(
            NumberOfMemoryCells: (responce[0] & 0b0000_0111),
            AutoUp: (responce[0] & 0b0000_1000) != 0,
            AutoDown: (responce[0] & 0b0001_0000) != 0,
            BleAllowed: (responce[0] & 0b0010_0000) != 0,
            HasDisplay: (responce[0] & 0b0100_0000) != 0,
            HasLight: (responce[0] & 0b1000_0000) != 0
        );
    }

    private Command GetMemoryPositionCommand(int cellNumber) => cellNumber switch
    {
        <= 0 => throw new WrongMemoryCellNumberException("Invalid memory cell number. Must be greater that zero"),
        1 => Command.MemoryPosition1,
        2 => Command.MemoryPosition2,
        3 => Command.MemoryPosition3,
        4 => Command.MemoryPosition4,
        _ => throw new WrongMemoryCellNumberException(
            $"Invalid memory cell number. Check {nameof(Desk)}.{nameof(Capabilities)}.{nameof(Capabilities.NumberOfMemoryCells)} to get the total number of memory cells. Memory cells numbering starts with one")
    };

    private async Task StartMovementAsync()
    {
        await WriteUInt16Async(_control, 0xfe);
        await WriteUInt16Async(_control, 0xff);
    }

    private async Task FinishMovementAsync()
    {
        await WriteUInt16Async(_control, 0xff);
        await WriteUInt16Async(_input, 0x8001);
    }

    private async Task<byte[]> QueryDPGAsync(Command command, byte[]? value = null)
    {
        if (value == null)
            await WriteBytesAsync(_dpg, new byte[] { 0x7f, command, 0x00 });
        else
            await WriteBytesAsync(_dpg, new byte[] { 0x7f, command, 0x80, 0x01 }.Concat(value).ToArray());

        var result = await ReadBytesAsync(_dpg);

        //responce format is invalid
        if (result.Length < 2)
            throw new InvalidDataException();

        //query failed
        if (result[0] != 0x01)
            throw new InvalidOperationException();

        return result[2..];
    }

    private byte[] UInt16AsLittleEndian(ushort value)
    {
        var result = new byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(new Span<byte>(result), value);
        return result;
    }

    private ushort ReadUInt16LittleEndian(byte[] data, int offset)
    {
        return BinaryPrimitives.ReadUInt16LittleEndian(new ReadOnlySpan<byte>(data, offset, 2));
    }

    private static ulong PhysicalAddressToUInt64(PhysicalAddress address) =>
        BitConverter.ToUInt64(address.GetAddressBytes().Reverse().Concat(new byte[] { 0, 0 }).ToArray());

    private async Task<GattCharacteristic> GetCharacteristicAsync(Guid serviceUUID, Guid characteristicUUID)
    {
        GattDeviceService service;
        service = await GetServiceAsync(serviceUUID);
        try
        {
            var result = await service.GetCharacteristicsForUuidAsync(characteristicUUID, BluetoothCacheMode.Uncached);
            if (GattCommunicationStatus.Success != result.Status)
                throw new GattCommunicationException(result.Status.ToString());
            if (result.Characteristics.Count() == 0)
                throw new GattCommunicationException("GATT characteristic not found");
            return result.Characteristics[0];
        }
        catch
        {
            service.Dispose();
            throw;
        }
    }

    private async Task<GattDeviceService> GetServiceAsync(Guid UUID)
    {
        var result = await _device.GetGattServicesForUuidAsync(UUID);
        if (GattCommunicationStatus.Success != result.Status)
            throw new GattCommunicationException(result.Status.ToString());
        if (result.Services.Count() == 0)
            throw new GattCommunicationException("GATT service not found");
        return result.Services[0];
    }

    private async Task<string> ReadStringAsync(GattCharacteristic characteristic)
    {
        var result = await characteristic.ReadValueAsync(BluetoothCacheMode.Uncached);

        if (GattCommunicationStatus.Success != result.Status)
            throw new GattCommunicationException(result.Status.ToString());

        if (result.Value.Length == 0)
            return String.Empty;

        using var dataReader = DataReader.FromBuffer(result.Value);
        dataReader.ByteOrder = ByteOrder.LittleEndian;
        return dataReader.ReadString(result.Value.Length);
    }

    private async Task<ushort> ReadUInt16Async(GattCharacteristic characteristic)
    {
        var result = await characteristic.ReadValueAsync(BluetoothCacheMode.Uncached);

        if (GattCommunicationStatus.Success != result.Status)
            throw new GattCommunicationException(result.Status.ToString());

        if (result.Value.Length == 0)
            return default;

        using var dataReader = DataReader.FromBuffer(result.Value);
        dataReader.ByteOrder = ByteOrder.LittleEndian;
        return dataReader.ReadUInt16();
    }

    private async Task<byte[]> ReadBytesAsync(GattCharacteristic characteristic)
    {
        var result = await characteristic.ReadValueAsync(BluetoothCacheMode.Uncached);

        if (GattCommunicationStatus.Success != result.Status)
            throw new GattCommunicationException(result.Status.ToString());

        if (result.Value.Length == 0)
            return Array.Empty<byte>();

        using var dataReader = DataReader.FromBuffer(result.Value);
        dataReader.ByteOrder = ByteOrder.LittleEndian;
        var bytes = new byte[result.Value.Length];
        dataReader.ReadBytes(bytes);
        return bytes;
    }

    private async Task<DataReader> ReadDataAsync(GattCharacteristic characteristic)
    {
        var result = await characteristic.ReadValueAsync(BluetoothCacheMode.Uncached);

        if (GattCommunicationStatus.Success != result.Status)
            throw new GattCommunicationException(result.Status.ToString());

        var dataReader = DataReader.FromBuffer(result.Value);
        dataReader.ByteOrder = ByteOrder.LittleEndian;
        return dataReader;
    }

    private async Task WriteStringAsync(GattCharacteristic characteristic, string value)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        var result = await characteristic.WriteValueAsync(bytes.AsBuffer());
        if (GattCommunicationStatus.Success != result)
            throw new GattCommunicationException(result.ToString());
    }

    private async Task WriteUInt16Async(GattCharacteristic characteristic, ushort value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (!BitConverter.IsLittleEndian)
            Array.Reverse(bytes);

        var result = await characteristic.WriteValueAsync(bytes.AsBuffer());
        if (GattCommunicationStatus.Success != result)
            throw new GattCommunicationException(result.ToString());
    }

    private async Task WriteBytesAsync(GattCharacteristic characteristic, byte[] value)
    {
        var result = await characteristic.WriteValueAsync(value.AsBuffer());
        if (GattCommunicationStatus.Success != result)
            throw new GattCommunicationException(result.ToString());
    }
}