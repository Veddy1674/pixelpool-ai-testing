using System.Numerics;
using static EnvUtils;

interface IPhysicsBody {
    //public Vector2 Position { get; set; }
}

interface ISaveState {
    public ISaveState Save();
    public bool Load();
    public BallState[] GetBallsPositionAndActive();

    public static bool CopyTo(ISaveState source, ISaveState target)
    {
        if (source is SaveState s && target is SaveState t)
            return SaveState.CopyTo(s, t);

        else if (source is SaveStateStruct s1 && target is SaveStateStruct t1)
            return SaveStateStruct.CopyTo(s1, t1);

        throw new Exception("ISaveState.CopyTo: Incompatible types");
    }
}