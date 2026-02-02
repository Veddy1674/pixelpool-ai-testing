static class CustomEnvs
{
    //public static EnvConfig Normal = new(
    //    getState: NormalGetState,
    //    getReward: NormalGetReward
    //);

    //public static EnvConfig OneBallShot = new(
    //    getState: balls =>
    //    [
    //        balls[0].Position.X,
    //        balls[0].Position.Y,
    //        balls[1].Position.X - balls[0].Position.X,
    //        balls[1].Position.Y - balls[0].Position.Y
    //    ],
    //    getReward: (ballsFell, ending) => 1 // TODO TODO TODO TODO
    //);

    #region methods

    public static float[] NormalGetState(PhysicsBodyStruct[] BallsBody)
    {
        // (cueX, cueY), for each ball (distX, distY, isActive) where dist is relative to cue
        // it is supposed to automatically understand collisions and holes
        float[] state = new float[2 + 15 * 3]; // 47

        state[0] = BallsBody[0].Position.X;
        state[1] = BallsBody[0].Position.Y;

        for (int i = 1; i < 16; i++)
        {
            int baseIndex = 2 + (i - 1) * 3;

            state[baseIndex] = BallsBody[i].Position.X - BallsBody[0].Position.X;
            state[baseIndex + 1] = BallsBody[i].Position.Y - BallsBody[0].Position.Y;
            state[baseIndex + 2] = BallsBody[i].IsActive ? 1f : 0f;
        }

        return state;
    }

    public static float NormalGetReward(int ballsFell, Ending ending)
    {
        if (ending == Ending.Loss) return -1f;
        if (ending == Ending.Victory) return 20f + (ballsFell * 3);

        return (ballsFell * 3) - 0.01f;

        // multiplying by a value > 1 is NECESSARY because with a "-1" reward when loss and "0.99" when one ball falls,
        // the AI learns to do nothing to stay safe
    }

    #endregion
}

//class EnvConfig(Func<PhysicsBodyStruct[], float[]> getState, Func<int, Ending, float> getReward)
//{
//    public readonly Func<PhysicsBodyStruct[], float[]> GetState = getState;
//    public readonly Func<int, Ending, float> GetReward = getReward;
//}