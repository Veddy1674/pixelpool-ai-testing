using System.Numerics;
using static EnvUtils;
using static IPoolEnv;

// optimized PoolEnv variant, which uses PhysicsBodyStruct and other optimization techniques, although more hardcoded
class PoolEnvMini : IPoolEnv
{
    public PhysicsBodyStruct[] BallsBody { get; } = new PhysicsBodyStruct[16];

    public bool BallsStill { get; private set; } = true; // needs to be manually set to false when balls are moved (velocities applied)

    // init
    public PoolEnvMini(PhysicsBodyStruct[]? presetBallsBody = null)
    {
        if (presetBallsBody == null)
            // create bodies
            for (int i = 0; i < BallsBody.Length; i++)
                BallsBody[i] = new PhysicsBodyStruct(ballPositions[i]);
        else
            presetBallsBody.CopyTo(BallsBody, 0);

        //Reset(); // ???
    }

    #region Getters & Setters

    private readonly Vector2[] ballPositionsCache = new Vector2[ballPositions.Length];
    public Vector2[] GetBallsPosition()
    {
        // optimized rather than: [.. BallsBody.Select(b => b.Position)];

        for (int i = 0; i < ballPositionsCache.Length; i++)
            ballPositionsCache[i] = BallsBody[i].Position;

        return ballPositionsCache;
    }

    public void SetBallVelocity(int index, Vector2 velocity)
    {
        BallsBody[index].SetVelocity(velocity);
        BallsStill = false;
    }

    #endregion

    #region Game Logic

    public void TickAll(out int ballsFell, out Ending ending)
    {
        ballsFell = 0;
        ending = Ending.Running;

        // physics update
        for (int i = 0; i < 16; i++)
        {
            BallsBody[i].Update(physicsDeltaTime);

            // ball-hole collisions
            if (BallsBody[i].CheckFall(ballHoles))
            {
                if (i == 0)
                {
                    ending = Ending.Loss;
                    return;
                }
                else
                {
                    BallsBody[i].Reset();
                    BallsBody[i].IsActive = false;
                    ballsFell++;

                    /* minor theorical bug:
                     * if cue ball and any other ball fall at the same tick,
                     * ending is set to loss and ballsFell isn't incremented for the other ball
                    */

                    if (CheckVictory())
                    {
                        ending = Ending.Victory;
                        return;
                    }
                }
            }
        }

        // ball-ball collisions
        for (int i = 0; i < 16; i++)
        {
            for (int j = i + 1; j < 16; j++)
            {
                if (BallsBody[i].CheckCollision(BallsBody[j]))
                    BallsBody[i].ResolveCollision(ref BallsBody[j]);
            }
        }

        // wall collisions
        for (int i = 0; i < 16; i++)
        {
            BallsBody[i].ResolveWallCollisions();
        }
    }

    public bool BallsAreStill()
    {
        for (int i = 0; i < 16; i++)
            if (BallsBody[i].IsMoving)
                return false;

        return true;
    }

    public void TickUntilBallsStop(out int ticks, out int ballsFell, out Ending ending)
    {
        ballsFell = 0;
        ending = Ending.Running;
        ticks = 0;

        while (!BallsAreStill())
        {
            TickAll(out int _ballsFell, out var _ending);
            ballsFell += _ballsFell;
            ticks++;

            if (_ending != Ending.Running)
            {
                //ballsFell = 0;
                ending = _ending;
                break;
            }
        }

        BallsStill = true;
    }

    public void Reset(int seed = -1)
    {
        var random = seed == -1 ? new Random(4477) : new Random(seed);
        // randomizes ball positions except cue and 8 ball (0 and 1)
        var positions = new int[16];

        for (int i = 0; i < 16; i++)
            positions[i] = i;

        for (int i = 15; i >= 0; i--)
        {
            if (i == 0 || i == 1) continue;

            int j = random.Next(i + 1);

            if (j == 0 || j == 1) continue;
            (positions[i], positions[j]) = (positions[j], positions[i]);
        }

        positions[0] = 0;
        positions[1] = 1;

        for (int i = 0; i < 16; i++)
        {
            BallsBody[i].Reset();
            BallsBody[i].Position = ballPositions[positions[i]];
        }
    }

    public bool CheckVictory()
    {
        for (int i = 1; i < 16; i++)
            if (BallsBody[i].IsActive)
                return false;

        return true;
    }

    // default logic, one tick simulation
    public Ending? GameUpdate(Action? preReset = null)
    {
        if (!BallsStill)
        {
            TickAll(out _, out var ending);

            if (ending != Ending.Running)
            {
                preReset?.Invoke();
                Reset();
            }
            else
                BallsStill = BallsAreStill();

            return ending;
        }
        return null;
    }

    #endregion

    #region GetState(), SetAction(), GetReward()

    public float[] GetState()
    {
        // pos xy of cue ball, xy dist to all balls
        // it is supposed to automatically understand collisions and holes
        float[] state = new float[2 + 15 * 2]; // 32

        state[0] = BallsBody[0].Position.X;
        state[1] = BallsBody[0].Position.Y;

        for (int i = 1; i < 15; i++)
        {
            state[i * 2] = BallsBody[i].Position.X;
            state[i * 2 + 1] = BallsBody[i].Position.Y;
        }

        return state;
    }

    public void SetAction(float angle, float strength = 1500f) // angle -1 to 1
    {
        // rescale angle from (-1, 1) to (-pi, pi)
        angle *= (float)Math.PI;

        SetBallVelocity(0, new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * strength);
    }

    public float GetReward(int ballsFell, Ending ending)
    {
        if (ending == Ending.Loss) return -1f;
        if (ending == Ending.Victory) return 20f + (ballsFell * 3);

        return (ballsFell * 3) - 0.01f;

        // multiplying by a value > 1 is NECESSARY because with a "-1" reward when loss and "0.99" when one ball falls,
        // the AI learns to do nothing to stay safe
    }

    #endregion
}

internal class SaveStateStruct(PhysicsBodyStruct[] ballsRef) : ISaveState
{
    public readonly PhysicsBodyStruct[] ballsReference = ballsRef;
    private readonly bool[] ballsActive = new bool[16];
    private readonly Vector2[] ballsPosition = new Vector2[16];
    private bool saved;

    public SaveStateStruct(PoolEnvMini env) : this(env.BallsBody) { }

    public ISaveState Save()
    {
        for (int i = 0; i < 16; i++)
        {
            ballsActive[i] = ballsReference[i].IsActive;
            ballsPosition[i] = ballsReference[i].Position;
        }
        saved = true;
        return this;
    }

    public bool Load()
    {
        if (!saved) return false;

        for (int i = 0; i < 16; i++)
        {
            ballsReference[i].Reset();
            ballsReference[i].IsActive = ballsActive[i];
            ballsReference[i].Position = ballsPosition[i];
        }
        return true;
    }

    public static bool CopyTo(SaveStateStruct source, SaveStateStruct target)
    {
        if (source.ballsReference != target.ballsReference) return false;

        for (int i = 0; i < 16; i++)
        {
            target.ballsActive[i] = source.ballsActive[i];
            target.ballsPosition[i] = source.ballsPosition[i];
            target.saved = true;
        }
        return true;
    }

    public BallState[] GetBallsPositionAndActive()
    {
        var list = new BallState[16];

        for (int i = 0; i < 16; i++)
            list[i] = new(ballsActive[i], ballsPosition[i]);

        return list;
    }
}