using Fusion;

public interface IClimbable
{
    bool TryStartClimb(MovementCharacter player);
    void OnClimbTick(MovementCharacter player, NetworkInputData input);
    void OnStopClimb(MovementCharacter player);
}