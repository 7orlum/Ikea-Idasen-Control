namespace IkeaIdasenControl.LinakDPGController;

using System.Net.NetworkInformation;

///<summary>
///Counts the height from the floor level in millimeters, 
///as opposed to the <see cref="DeskRaw"/> class, which counts the height from the lowest table level in tenths of a millimeter
///</summary>
public class Desk : IDisposable
{
    private DeskRaw _deskRaw = null!;

    public DeskCapabilities Capabilities => _deskRaw.Capabilities;

    public static async Task<Desk> ConnectAsync(PhysicalAddress bluetoothAddress)
    {
        return new Desk()
        {
            _deskRaw = await DeskRaw.ConnectAsync(bluetoothAddress)
        };
    }

    private Desk() {}
    
    public async Task<string> GetNameAsync() => await _deskRaw.GetNameAsync();
    public async Task SetNameAsync(string value) => await _deskRaw.SetNameAsync(value);
    public async Task<float> GetMinHeightAsync() => MmFromRaw(await _deskRaw.GetOffsetAsync());
    public async Task SetMinHeightAsync(float value) => await _deskRaw.SetOffsetAsync(RawFromMm(value));
    public async Task<float> GetHeightAsync() => MmFromRaw(await _deskRaw.GetOffsetAsync() + await _deskRaw.GetHeightAsync());
    public async Task SetHeightAsync(float value) => await _deskRaw.SetHeightAsync(RawFromMm(value - await GetMinHeightAsync()));
    public async Task<float> GetMemoryValueAsync(int cellNumber) => MmFromRaw(await _deskRaw.GetOffsetAsync() + await _deskRaw.GetMemoryValueAsync(cellNumber));
    public async Task SetMemoryValueAsync(int cellNumber, float value) => await _deskRaw.SetMemoryValueAsync(cellNumber, RawFromMm(value - await GetMinHeightAsync()));
    public void Dispose() => _deskRaw.Dispose();

    private float MmFromRaw(int raw)
    {
        return raw / 10f;
    }

    private ushort RawFromMm(float mm)
    {
        var result = mm * 10f;
        return result switch
        {
            <= 0 => 0,
            >= ushort.MaxValue - 1 => ushort.MaxValue - 1,
            _ => Convert.ToUInt16(result)
        };
    }
}