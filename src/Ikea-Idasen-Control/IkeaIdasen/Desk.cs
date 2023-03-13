﻿using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

public class Desk : IDisposable
{
    private BluetoothLEDevice _device = null!;
    private DeskCapabilities _capabilities = null!;
    private GattDeviceService _nameService = null!;
    //private GattDeviceService _modelService = null!;
    private GattDeviceService _controlService = null!;
    private GattDeviceService _dpgService = null!;
    private GattDeviceService _heightSpeedSensorService = null!;
    private GattDeviceService _inputService = null!;
    private GattCharacteristic _nameCharacteristic = null!;
    //private GattCharacteristic _modelCharacteristic = null!;
    private GattCharacteristic _controlCharacteristic = null!;
    private GattCharacteristic _dpgCharacteristic = null!;
    private GattCharacteristic _heightSpeedSensorCharacteristic = null!;
    private GattCharacteristic _inputCharacteristic = null!;

    public DeskCapabilities Capabilities => _capabilities;

    public static async Task<Desk> ConnectAsync(BluetoothLEDevice device)
    {
        var result = new Desk();

        result._device = device;
        await result.ConnectAsync();
        await result.SetUserIdAsync(await result.GetUserIdAsync()); //SetMemoryPositionRawAsync doesn't work whithout it!
        result._capabilities = await result.GetCapabilities();
        
        return result;
    }

    private Desk() {}

    public async Task<string> GetNameAsync()
    {
        return await ReadStringAsync(_nameCharacteristic);
    }

    public async Task SetNameAsync(string value)
    {
        await WriteStringAsync(_nameCharacteristic, value);
    }

    public async Task<byte[]> GetUserIdAsync()
    {
        var result = await QueryBytesAsync(DPGCommand.UserID);
        if (result[2] != 1)
            throw new InvalidOperationException();
        return result[3..];
    }

    public async Task SetUserIdAsync(byte[] value)
    {
        var result = await QueryBytesAsync(DPGCommand.UserID, value);
        if (result[0] != 1 && result[1] != 0)
            throw new InvalidOperationException();
    }

    public async Task<ushort> GetOffsetRawAsync()
    {
        return await QueryUInt16Async(DPGCommand.DeskOffset);
    }

    public async Task SetOffsetRawAsync(ushort value)
    {
        var result = await QueryBytesAsync(DPGCommand.DeskOffset, value);
        if (result[0] != 1 && result[1] != 0)
            throw new InvalidOperationException();
    }

    public async Task<ushort> GetHeightRawAsync()
    {
        using var dataReader = await ReadDataAsync(_heightSpeedSensorCharacteristic);
        var height = dataReader.ReadUInt16();
        var speed = dataReader.ReadUInt16();
        return height;
    }

    public async Task<ushort> GetMemoryPositionRawAsync(int cellNumber)
    {
        if (cellNumber >= Capabilities.MemoryCells)
            throw new ArgumentOutOfRangeException(nameof(cellNumber), "There are no requested memory cells. Check Desk.Capabilities.MemoryCells to get the total number of memory cells");

        return await QueryUInt16Async(GetMemoryPositionCommand(cellNumber));
    }

    public async Task SetMemoryPositionRawAsync(int cellNumber, ushort heightRaw)
    {
        if (cellNumber >= Capabilities.MemoryCells)
            throw new ArgumentOutOfRangeException(nameof(cellNumber), "There are no requested memory cells. Check Desk.Capabilities.MemoryCells to get the total number of memory cells");

        await QueryBytesAsync(GetMemoryPositionCommand(cellNumber), heightRaw);
    }

    public async Task MoveToHeightRawAsync(ushort targetHeightRaw)
    {
        const int stopAfterAttempts = 5;

        await StartMovementAsync();

        var previousHeight = await GetHeightRawAsync();
        for (var attempts = 0; attempts < stopAfterAttempts; attempts++)
        {
            await WriteUInt16Async(_inputCharacteristic, targetHeightRaw);

            var currentHeight = await GetHeightRawAsync();
            var isMovementDetected = currentHeight != previousHeight;
            previousHeight = currentHeight;

            if (isMovementDetected)
                attempts = -1;
        }

        await FinishMovementAsync();
    }

    public float MmFromRaw(int raw)
    {
        return raw / 10f;
    }

    public ushort RawFromMmAsync(float mm)
    {
        var result = mm * 10f;
        return result switch
        {
            <= 0 => 0,
            >= ushort.MaxValue => ushort.MaxValue,
            _ => Convert.ToUInt16(result)
        };
    }

    public void Dispose()
    {
        Disconnect();
    }

    private async Task<DeskCapabilities> GetCapabilities()
    {
        var data = new byte[] { 0x7f, (byte)DPGCommand.Capabilities, 0x00 };
        await WriteBytesAsync(_dpgCharacteristic, data);
        var bytes = await ReadBytesAsync(_dpgCharacteristic);

        if (bytes.Length < 4)
            throw new InvalidOperationException();

        return new DeskCapabilities(
            MemoryCells: (byte)(bytes[2] & 0b0000_0111),
            AutoUp: (bytes[2] & 0b0000_1000) != 0,
            AutoDown: (bytes[2] & 0b0001_0000) != 0,
            BleAllowed: (bytes[2] & 0b0010_0000) != 0,
            HasDisplay: (bytes[2] & 0b0100_0000) != 0,
            HasLight: (bytes[2] & 0b1000_0000) != 0
        );
    }

    private DPGCommand GetMemoryPositionCommand(int cellNumber) => cellNumber switch
    {
        0 => DPGCommand.MemoryPosition1,
        1 => DPGCommand.MemoryPosition2,
        2 => DPGCommand.MemoryPosition3,
        3 => DPGCommand.MemoryPosition4,
        _ => throw new InvalidOperationException()
    };

    private async Task StartMovementAsync()
    {
        await WriteUInt16Async(_controlCharacteristic, 0xfe);
        await WriteUInt16Async(_controlCharacteristic, 0xff);
    }

    private async Task FinishMovementAsync()
    {
        await WriteUInt16Async(_controlCharacteristic, 0xff);
        await WriteUInt16Async(_inputCharacteristic, 0x8001);
    }

    private async Task<byte[]> QueryBytesAsync(DPGCommand command)
    {
        await WriteBytesAsync(_dpgCharacteristic, new byte[] { 0x7f, (byte)command, 0x00 });
        return await ReadBytesAsync(_dpgCharacteristic);
    }

    private async Task<ushort> QueryUInt16Async(DPGCommand command)
    {
        var bytes = await QueryBytesAsync(command);

        if (bytes[2] != 0x01)
            //the value is not defined
            return 0;

        using var dataReader = DataReader.FromBuffer(bytes.AsBuffer(3, 2));
        dataReader.ByteOrder = ByteOrder.LittleEndian;
        return dataReader.ReadUInt16();
    }

    private async Task<byte[]> QueryBytesAsync(DPGCommand command, byte[] value)
    {
        var data = new byte[] { 0x7f, (byte)command, 0x80, 0x01 }.Concat(value).ToArray();
        await WriteBytesAsync(_dpgCharacteristic, data);
        return await ReadBytesAsync(_dpgCharacteristic);
    }

    private async Task<byte[]> QueryBytesAsync(DPGCommand command, ushort value)
    {
        var data = BitConverter.GetBytes((ushort)value);
        if (!BitConverter.IsLittleEndian)
            Array.Reverse(data);
        return await QueryBytesAsync(command, data);
    }

    private async Task ConnectAsync()
    {
        Disconnect();

        _nameService = await GetServiceAsync(BLEConstants.NameServiceUUID);
        //_modelService = await GetServiceAsync(BLEConstants.ModelServiceUUID);
        _controlService = await GetServiceAsync(BLEConstants.ControlServiceUUID);
        _dpgService = await GetServiceAsync(BLEConstants.DPGServiceUUID);
        _heightSpeedSensorService = await GetServiceAsync(BLEConstants.HeightSpeedSensorServiceUUID);
        _inputService = await GetServiceAsync(BLEConstants.InputServiceUUID);

        _nameCharacteristic = await GetCharacteristicAsync(_nameService, BLEConstants.NameCharacteristicUUID);
        //_modelCharacteristic = await GetCharacteristicAsync(_modelService, BLEConstants.ModelCharacteristicUUID);
        _controlCharacteristic = await GetCharacteristicAsync(_controlService, BLEConstants.ControlCharacteristicUUID);
        _dpgCharacteristic = await GetCharacteristicAsync(_dpgService, BLEConstants.DPGCharacteristicUUID);
        _heightSpeedSensorCharacteristic = await GetCharacteristicAsync(_heightSpeedSensorService, BLEConstants.HeightSpeedSensorCharacteristicUUID);
        _inputCharacteristic = await GetCharacteristicAsync(_inputService, BLEConstants.InputCharacteristicUUID);
    }

    private void Disconnect()
    {
        _nameService?.Dispose();
        //_modelService?.Dispose();
        _controlService?.Dispose();
        _dpgService?.Dispose();
        _heightSpeedSensorService?.Dispose();
        _inputService?.Dispose();
    }

    private async Task<GattDeviceService> GetServiceAsync(string serviceUUID)
    {
        var result = await _device.GetGattServicesForUuidAsync(Guid.Parse(serviceUUID));
        if (GattCommunicationStatus.Success != result.Status)
            throw new ApplicationException(result.Status.ToString());
        return result.Services[0];
    }

    private async Task<GattCharacteristic> GetCharacteristicAsync(GattDeviceService service, string characteristicUUID)
    {
        var result = await service.GetCharacteristicsForUuidAsync(Guid.Parse(characteristicUUID));
        if (GattCommunicationStatus.Success != result.Status)
            throw new ApplicationException(result.Status.ToString());
        return result.Characteristics[0];
    }

    private async Task<string> ReadStringAsync(GattCharacteristic characteristic)
    {
        var result = await characteristic.ReadValueAsync(BluetoothCacheMode.Uncached);

        if (GattCommunicationStatus.Success != result.Status)
            throw new ApplicationException(result.Status.ToString());

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
            throw new ApplicationException(result.Status.ToString());

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
            throw new ApplicationException(result.Status.ToString());

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
            throw new ApplicationException(result.Status.ToString());

        var dataReader = DataReader.FromBuffer(result.Value);
        dataReader.ByteOrder = ByteOrder.LittleEndian;
        return dataReader;
    }

    private async Task WriteStringAsync(GattCharacteristic characteristic, string value)
    {
        var bytes = System.Text.Encoding.ASCII.GetBytes(value);
        var result = await characteristic.WriteValueAsync(bytes.AsBuffer());
        if (GattCommunicationStatus.Success != result)
            throw new ApplicationException(result.ToString());
    }

    private async Task WriteUInt16Async(GattCharacteristic characteristic, ushort value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (!BitConverter.IsLittleEndian)
            Array.Reverse(bytes);

        var result = await characteristic.WriteValueAsync(bytes.AsBuffer());
        if (GattCommunicationStatus.Success != result)
            throw new ApplicationException(result.ToString());
    }

    private async Task WriteBytesAsync(GattCharacteristic characteristic, byte[] value)
    {
        var result = await characteristic.WriteValueAsync(value.AsBuffer());
        if (GattCommunicationStatus.Success != result)
            throw new ApplicationException(result.ToString());
    }
}