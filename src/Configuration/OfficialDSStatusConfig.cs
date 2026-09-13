namespace OfficialDSStatus.Configuration;

public class OfficialDSStatusConfig
{
    public bool Enabled { get; set; } = true;
    public bool SetValveDSOnRoundStart { get; set; } = true;
    public bool SetValveDSOnMapLoad { get; set; } = true;
    public bool DebugLog { get; set; } = true;
}
