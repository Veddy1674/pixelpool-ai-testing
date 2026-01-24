using static EnvUtils;

interface IPhysicsBody { } // empty to avoid complexity (attempted)
interface ISaveState {
    public ISaveState Save();
    public bool Load();
    public BallState[] GetBallsPositionAndActive();
}