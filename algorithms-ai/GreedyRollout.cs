using System.Numerics;
using static EnvUtils;
using static ColorLog;
using static IPoolEnv;

static partial class Algorithm
{
    public static Vector2[] GreedyRollout(in IPoolEnv env, in int samples, in bool logging = false)
    {
        if (env is PoolEnv poolEnv)
            return GreedyRollout(poolEnv, samples, logging);
        else if (env is PoolEnvMini poolEnvMini)
            return GreedyRollout(poolEnvMini, samples, logging);
        else
            throw new Exception($"Invalid Environment of type {env.GetType()}");
    }

    // i guess duplicated code works if the alternative is hell

    public static Vector2[] GreedyRollout(in PoolEnv env, in int samples, in bool logging = false)
    {
        List<GameStep> bestSteps = new(20);
        SaveState currentState = new(env);
        currentState.Save();

        while (true)
        {
            GameStep bestMove = new(int.MaxValue, -1, Vector2.Zero); // worst possible
            SaveState bestMoveSave = new(env);

            for (int i = 0; i < samples; i++)
            {
                var dir = RandomDirection;

                env.SetBallVelocity(0, dir * 1500f);
                env.TickUntilBallsStop(out int ticks, out int ballsFell, out var ending);

                // 1. victory, return right away
                if (ending == Ending.Victory)
                {
                    bestSteps.Add(new(ticks, ballsFell, dir));

                    if (logging)
                    {
                        Log($"&aStep {bestSteps.Count} &f- &aBalls: &d{ballsFell} (15/15)&a, Ticks: &s{ticks}");
                        Log("&tVictory found in &y" + bestSteps.Count + " &tsteps!");
                    }

                    goto victory;
                }
                // 2. loss, skip completely
                else if (ending == Ending.Loss)
                    goto reset;

                // 3. else: still running, prioritize most balls and least ticks

                // replace if better than last best move
                if (ballsFell > bestMove.ballsFell || (ballsFell == bestMove.ballsFell && ticks < bestMove.ticks))
                {
                    bestMove = new(ticks, ballsFell, dir);
                    bestMoveSave.Save();
                }

            reset:
                currentState.Load(); // rollback
            }

            if (bestMove.ballsFell == 0)
            {
                throw new Exception("&cGreedy Rollout Algorithm: No balls fell after " + samples + " samples, something went wrong...");
            }

            bestSteps.Add(bestMove);
            if (!bestMoveSave.CopyTo(currentState)) throw new Exception("yeah, this should never happen");

            if (logging)
                Log($"&aStep {bestSteps.Count} &f- &aBalls: &s{bestMove.ballsFell} ({bestSteps.Sum(s => s.ballsFell)}/15)&a, Ticks: &s{bestMove.ticks}");
        }

    victory:
        return [.. bestSteps.Select(s => s.direction)];
    }

    public static Vector2[] GreedyRollout(in PoolEnvMini env, in int samples, in bool logging = false)
    {
        List<GameStep> bestSteps = new(20);
        SaveStateStruct currentState = new(env);
        currentState.Save();

        while (true)
        {
            GameStep bestMove = new(int.MaxValue, -1, Vector2.Zero); // worst possible
            SaveStateStruct bestMoveSave = new(env);

            for (int i = 0; i < samples; i++)
            {
                var dir = RandomDirection;

                env.SetBallVelocity(0, dir * 1500f);
                env.TickUntilBallsStop(out int ticks, out int ballsFell, out var ending);

                // 1. victory, return right away
                if (ending == Ending.Victory)
                {
                    bestSteps.Add(new(ticks, ballsFell, dir));

                    if (logging)
                    {
                        Log($"&aStep {bestSteps.Count} &f- &aBalls: &d{ballsFell} (15/15)&a, Ticks: &s{ticks}");
                        Log("&tVictory found in &y" + bestSteps.Count + " &tsteps!");
                    }

                    goto victory;
                }
                // 2. loss, skip completely
                else if (ending == Ending.Loss)
                    goto reset;

                // 3. else: still running, prioritize most balls and least ticks

                // replace if better than last best move
                if (ballsFell > bestMove.ballsFell || (ballsFell == bestMove.ballsFell && ticks < bestMove.ticks))
                {
                    bestMove = new(ticks, ballsFell, dir);
                    bestMoveSave.Save();
                }

            reset:
                currentState.Load(); // rollback
            }

            if (bestMove.ballsFell == 0)
            {
                throw new Exception("&cGreedy Rollout Algorithm: No balls fell after " + samples + " samples, something went wrong...");
            }

            bestSteps.Add(bestMove);
            if (!bestMoveSave.CopyTo(currentState)) throw new Exception("yeah, this should never happen");

            if (logging)
                Log($"&aStep {bestSteps.Count} &f- &aBalls: &s{bestMove.ballsFell} ({bestSteps.Sum(s => s.ballsFell)}/15)&a, Ticks: &s{bestMove.ticks}");
        }

    victory:
        return [.. bestSteps.Select(s => s.direction)];
    }
}
