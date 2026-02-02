using System.Numerics;
using static IPoolEnv;

static partial class Algorithm
{
    // random directions until end state is victory
    public static Vector2 FindSingleWin(in IPoolEnv env, in int maxSteps = 10000)
    {
        Vector2 dir;
        var currentState = env.NewSaveState().Save();

        for (int i = 0; i < maxSteps; i++)
        {
            dir = RandomDirection;

            env.SetBallVelocity(0, dir * 1500f);
            env.TickUntilBallsStop(out _, out _, out var ending);
            currentState.Load(); // restore

            if (ending == Ending.Victory)
                return dir;
        }

        throw new TimeoutException("No winning direction found after " + maxSteps + " steps");
    }
}
