using System.Numerics;
using static ColorLog;
using static EnvUtils;
using static IPoolEnv;

static partial class Algorithm
{
    public static Vector2[] GreedyRollout(in IPoolEnv env, in int samples, in bool logging = false)
    {
        List<GameStep> bestSteps = new(20);
        ISaveState currentState = env.NewSaveState();
        currentState.Save();

        while (true)
        {
            GameStep bestMove = new(int.MaxValue, -1, Vector2.Zero); // worst possible
            ISaveState bestMoveSave = env.NewSaveState();

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
                throw new Exception("&cGreedy Rollout Algorithm: No balls fell after " + samples + " samples (perhaps too few?)...");

            #region local search

            var oldStep = bestMove; // for logging

            // local search
            currentState.Load();
            var improvedDir = LocalSearch(env, bestMove.direction, increment: 0.01f, times: 10);

            bool localSearchImproved = !improvedDir.Equals(bestMove.direction);

            var finalStep = bestMove;

            if (localSearchImproved)
            {
                // re-simulate to get new stats
                env.SetBallVelocity(0, improvedDir * 1500f);
                env.TickUntilBallsStop(out int newticks, out int newballsfell, out _);

                finalStep = new(newticks, newballsfell, improvedDir);

                bestMoveSave.Save();
            }

            #endregion

            bestSteps.Add(finalStep);
            bestMoveSave.CopyTo(currentState);

            if (logging)
            {
                int totalBalls = bestSteps.Sum(s => s.ballsFell);
                if (localSearchImproved)
                {
                    Log($"&sOLD: &aStep {bestSteps.Count} &f- &aBalls: &s{oldStep.ballsFell} ({totalBalls - finalStep.ballsFell + oldStep.ballsFell}/15)&a, Ticks: &s{oldStep.ticks}");
                }
                Log($"&aStep {bestSteps.Count} &f- &aBalls: &s{finalStep.ballsFell} ({totalBalls}/15)&a, Ticks: &s{finalStep.ticks}");
            }
        }

    victory:
        return [.. bestSteps.Select(s => s.direction)];
    }

    // sorta of hill-climbing to fine-tune a direction (see if smaller increments yield better results)
    private static Vector2[] DeltasOf(float increment)
        => [new(increment, 0), new(0, increment), new(-increment, 0), new(0, -increment)];

    public static Vector2 LocalSearch(in IPoolEnv env, in Vector2 direction, float increment = 0.01f, int times = 10)
    {
        // total iterations is (times / increment) * 4

        var bestDir = direction;

        ISaveState state = env.NewSaveState();
        state.Save();

        env.SetBallVelocity(0, bestDir * 1500f); // get current best
        env.TickUntilBallsStop(out int leastTicks, out int mostBallsFell, out _);

        var deltas = DeltasOf(increment); // directions (4)

        for (int i = 0; i < times; i++)
        {
            //bool improved = false;

            foreach (var delta in deltas)
            {
                state.Load();

                Vector2 newDir = Vector2.Normalize(bestDir + delta);

                env.SetBallVelocity(0, newDir * 1500f);
                env.TickUntilBallsStop(out int ticks, out int balls, out var ending);

                if (ending == Ending.Loss) continue; // ignore
                if (ending == Ending.Victory) return newDir; // priority

                if (balls > mostBallsFell || (balls == mostBallsFell && ticks < leastTicks))
                {
                    bestDir = newDir;
                    leastTicks = ticks;
                    mostBallsFell = balls;
                    //improved = true;
                }
            }

            // here you can return if improvement was/wasn't found
            //if (!improved) break;
        }

        state.Load();
        return bestDir;
    }
}
