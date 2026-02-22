using System.Numerics;

// a class to easily setup different environments for testing, benchmarking, algorithms, AI training
static partial class EnvSetup
{
    public static float[] Normal_GetState(in PoolEnvMini env)
    {
        // (cueX, cueY), for each ball (distX, distY, isActive) where dist is relative to cue
        // it is supposed to automatically understand collisions and holes
        float[] state = new float[2 + 15 * 3]; // 47

        state[0] = env.BallsBody[0].Position.X;
        state[1] = env.BallsBody[0].Position.Y;

        for (int i = 1; i < 16; i++)
        {
            int baseIndex = 2 + (i - 1) * 3;

            state[baseIndex] = env.BallsBody[i].Position.X - env.BallsBody[0].Position.X;
            state[baseIndex + 1] = env.BallsBody[i].Position.Y - env.BallsBody[0].Position.Y;
            state[baseIndex + 2] = env.BallsBody[i].IsActive ? 1f : 0f;
        }

        return state;
    }

    public static float Normal_GetReward(int ballsFell, Ending ending)
    {
        if (ending == Ending.Loss) return -1f;
        if (ending == Ending.Victory) return 20f + (ballsFell * 3);

        return (ballsFell * 3) - 0.01f;

        // multiplying by a value > 1 is NECESSARY because with a "-1" reward when loss and "0.99" when one ball falls,
        // the AI learns to do nothing to stay safe
    }

    public static bool Normal_IsDone(Ending ending)
        => ending != Ending.Running;
}