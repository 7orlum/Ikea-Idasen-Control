namespace IkeaIdasenControl;

using IkeaIdasenControl.LinakDPGController;
using System.Net.NetworkInformation;

///<summary>
///Counts the height from the floor level in millimeters, 
///as opposed to the <see cref="Desk"/> class, which counts the height from the lowest table level in tenths of a millimeter
///</summary>
public class MyDesk : IDisposable
{
    private Desk _desk = null!;

    public DeskCapabilities Capabilities => _desk.Capabilities;

    public static async Task<MyDesk> ConnectAsync(PhysicalAddress bluetoothAddress)
    {
        return new MyDesk()
        {
            _desk = await Desk.ConnectAsync(bluetoothAddress)
        };
    }

    private MyDesk() { }

    public async Task<string> GetNameAsync() => await _desk.GetNameAsync();
    public async Task SetNameAsync(string value) => await _desk.SetNameAsync(value);
    public async Task<float> GetMinHeightAsync() => MmFromRaw(await _desk.GetOffsetAsync());
    public async Task SetMinHeightAsync(float value) => await _desk.SetOffsetAsync(RawFromMm(value));
    public async Task<float> GetHeightAsync() => MmFromRaw(await _desk.GetOffsetAsync() + await _desk.GetHeightAsync());
    public async Task SetHeightAsync(float value) => await _desk.SetHeightAsync(RawFromMm(value - await GetMinHeightAsync()));
    public async Task<float> GetMemoryValueAsync(int cellNumber) => MmFromRaw(await _desk.GetOffsetAsync() + await _desk.GetMemoryValueAsync(cellNumber));
    public async Task SetMemoryValueAsync(int cellNumber, float value) => await _desk.SetMemoryValueAsync(cellNumber, RawFromMm(value - await GetMinHeightAsync()));
    public void Dispose() => _desk.Dispose();

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