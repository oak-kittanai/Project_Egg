using Fusion;

public interface IClimbable
{
    void ClimbHandle(NetworkInputData input);
    void StartClimbing();
    void StopClimbing();
}