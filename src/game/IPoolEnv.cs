using Raylib_cs;
using System.Numerics;

interface IPoolEnv
{
    //public static readonly float defaultBallLaunchForce = 1500f;

    public const float physicsDeltaTime = 0.02f; // 50 fps

    public const float ballRadius = 15f; // should be 16, but balls collide with eachother on game start
    public const float ballMass = 1.0f;

    #region precalculated data

    public static readonly Vector2[] ballPositions = // precalc
        [
            new(716+5, 524-20), new(1164+15, 524-20), new(1112+15, 524-20), new(1138+15, 506-20),
            new(1138+15, 542-20), new(1164+15, 488-20), new(1164+15, 560-20), new(1190+15, 470-20),
            new(1190+15, 506-20), new(1190+15, 542-20), new(1190+15, 578-20), new(1216+15, 452-20),
            new(1216+15, 488-20), new(1216+15, 524-20), new(1216+15, 560-20), new(1216+15, 596-20)
        ];

    public static readonly (Vector2 position, float radius)[] ballHoles =
        [
            (new(482, 286), 20), (new(1438, 286), 20), (new(483, 722), 20), (new(1437, 722), 20), // corners
            (new(960, 285), 20), (new(960, 723), 20) // middles up and down
        ];

    public static readonly Rectangle poolTableCollision = Utils.RecFromLTRB(484, 289, 1436, 719); // precalc

    #endregion

    #region utils (static)

    public static Vector2 RandomDirection
    {
        get
        {
            float x = Random.Shared.NextSingle() * 2f - 1f; // -1..1
            float y = Random.Shared.NextSingle() * 2f - 1f; // -1..1

            Vector2 dir = new(x, y);

            // avoid vec zero
            if (dir.LengthSquared() < 0.001f)
                return Vector2.UnitX; // right

            return dir.Normalized();
        }
    }

    // x from IPoolEnv.poolTableCollision.X + safeMargin TO IPoolEnv.poolTableCollision.X + IPoolEnv.poolTableCollision.Width - safeMargin
    // y from IPoolEnv.poolTableCollision.Y + safeMargin TO IPoolEnv.poolTableCollision.Y + IPoolEnv.poolTableCollision.Height - safeMargin
    const float safeMargin = 28f; // 28 seems to be the minimum, 32 is also fine
    public static Vector2 RandomSafePosition => new(
        poolTableCollision.X + safeMargin + Random.Shared.NextSingle() * (poolTableCollision.Width - (safeMargin * 2f)),
        poolTableCollision.Y + safeMargin + Random.Shared.NextSingle() * (poolTableCollision.Height - (safeMargin * 2f))
    );

    #endregion

    public bool BallsStill { get; } // needs to be manually set to false when balls are moved (velocities applied)

    public void TickAll(out int ballsFell, out Ending ending);
    public bool BallsAreStill();
    public void TickUntilBallsStop(out int ticks, out int ballsFell, out Ending ending);
    public void Reset(int seed = -1);
    public bool CheckVictory();
    public Ending? GameUpdate(Action? preReset = null);

    public Vector2[] GetBallsPosition();
    public void SetBallVelocity(int index, Vector2 velocity);

    /// <summary>
    /// 0 ->        (1, 0)
    /// 0.5 ->      (0, 1)
    /// 1 = -1 ->   (-1, 0)
    /// -0.5 ->     (0, -1)
    /// </summary>
    public void SetAction(float angle, float strength = 1500f) // angle -1 to 1
    {
        // rescale angle from (-1, 1) to (-pi, pi)
        angle *= (float)Math.PI;

        SetBallVelocity(0, new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * strength);
    }
}

internal enum Ending
{
    Running, Victory, Loss
}