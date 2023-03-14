namespace IkeaIdasenControl.LinakDPGController;

public record DeskCapabilities (
    int NumberOfMemoryCells, 
    bool AutoUp,
    bool AutoDown,
    bool BleAllowed,
    bool HasDisplay,
    bool HasLight
);
