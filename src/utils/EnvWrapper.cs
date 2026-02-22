class EnvWrapper
{
    private readonly PoolEnvMini env;
    public int EnvType = 0; // set in python
    // 0 = Normal, 1 = OneBall

    public EnvWrapper()
    {
        env = new PoolEnvMini();
    }

    public void Reset(int seed = -1)
    {
        env.Reset(seed);

        // custom reset
        if (EnvType == 1)
            EnvSetup.OneBallShot_Reset(env, minDistance: 100f);
    }

    public float[] GetState()
    {
        if (EnvType == 1)
            return EnvSetup.OneBallShot_GetState(env);
        else
            return EnvSetup.Normal_GetState(env);
    }

    public float GetReward(int ballsFell, Ending ending)
    {
        if (EnvType == 1)
            return EnvSetup.OneBallShot_GetReward(ending);
        else
            return EnvSetup.Normal_GetReward(ballsFell, ending);
    }

    public bool GetDoneCondition(Ending ending)
    {
        if (EnvType == 1)
            return EnvSetup.OneBallShot_IsDone(); // always true
        else
            return EnvSetup.Normal_IsDone(ending);
    }

    public void SetAction(float angle, float strength = 1500f)
        => env.SetAction(angle, strength);

    public StepResult Step(float angle)
    {
        SetAction(angle, strength: 1500f);
        env.TickUntilBallsStop(out int ticks, out int ballsFell, out var ending);

        return new StepResult
        {
            State = GetState(),
            Reward = GetReward(ballsFell, ending),
            Done = GetDoneCondition(ending),
            BallsFell = ballsFell,
            Ticks = ticks,
            Ending = ending
        };
    }

    public struct StepResult
    {
        public float[] State;
        public float Reward;
        public bool Done;
        public int BallsFell;
        public int Ticks;
        public Ending Ending; // 0 = Running, 1 = Victory, 2 = Loss
    }
}