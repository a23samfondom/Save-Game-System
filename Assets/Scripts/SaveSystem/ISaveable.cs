public interface ISaveable
{
    string SaveId { get; }
    SaveData CaptureState();
    void RestoreState(SaveData data);
}