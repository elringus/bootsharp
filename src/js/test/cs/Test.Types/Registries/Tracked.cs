namespace Test.Types;

public record Tracked : Vehicle
{
    public TrackType TrackType { get; set; }
}
