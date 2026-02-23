using System.Numerics;

// a class to easily setup different environments for testing, benchmarking, algorithms, AI training
static partial class EnvSetup
{
    /// <summary>
    /// setup an environment with a cue ball and a ball both in random safe positions
    /// </summary>
    public static void OneBallShot_Reset(in PoolEnvMini env, float minDistance = 100f)
    {
        //int chosen = Random.Shared.Next(1, 16); // random ball
        // now using "1" directly

        // disable all except balls[i])
        for (int i = 1; i < 16; i++)
            env.BallsBody[i].IsActive = false;

        env.BallsBody[1].IsActive = true;

        // randomize cue ball pos
        env.BallsBody[0].Position = IPoolEnv.RandomSafePosition;

        Vector2 chosenBallPos;
        minDistance *= minDistance; // squared

        do
            chosenBallPos = IPoolEnv.RandomSafePosition;
        while
            ((chosenBallPos - env.BallsBody[0].Position).LengthSquared() < minDistance);

        env.BallsBody[1].Position = chosenBallPos;
    }

    public static float[] OneBallShot_GetState(in PoolEnvMini env)
        => [
            // minX = minX = 484 + 28 + 0 * (952 - 56) = 512
            // maxX = 484 + 28 + 0.999 * (952 - 56) ≈ 512 + 896 * 0.999 = 512 + 895.1 = ~1408

            // minY = 289 + 28 + 0 * (430 - 56) = 317
            // maxY = 289 + 28 + 0.999 * (430 - 56) ≈ 317 + 374 * 0.999 = 317 + 373.6 = ~691

            // min dist X and Y = minDistance from reset, so 100f default
            // max dist X = 1407.1 - 512 = ~895.1
            // max dist Y = 690.6 - 317 = ~373.6

            env.BallsBody[0].Position.X / 1440f,
            env.BallsBody[0].Position.Y / 700f,
            (env.BallsBody[1].Position.X - env.BallsBody[0].Position.X) / 910f,
            (env.BallsBody[1].Position.Y - env.BallsBody[0].Position.Y) / 390f
        ];

    public static float OneBallShot_GetReward(Ending ending)
        => (ending == Ending.Victory) ? 1f : -1f;

    public static bool OneBallShot_IsDone()
        => true;
}