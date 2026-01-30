using System.Diagnostics;
using System.Numerics;
using static ColorLog;
using static EnvUtils;

static class ProgramCLI
{
    const string asciiArt = @"
&z______ _          _  &x______           _ 
&z| ___ (_)        | | &x| ___ \         | |
&z| |_/ /___  _____| | &x| |_/ /__   ___ | |
&z|  __/| \ \/ / _ \ | &x|  __/ _ \ / _ \| |
&z| |   | |>  <  __/ | &x| | | (_) | (_) | |
&z\_|   |_/_/\_\___|_| &x\_|  \___/ \___/|_|
&1v1.0
";

    // returns wheter to run a rendered game instance normally
    public static bool Init()
    {
        Console.Title = "Pixel Pool - CLI";

        Log(asciiArt);
        Log("Type a command or 'help' for a list of commands.");

        while (true)
        {
            Log("&a> &0", newLine: false);
            string? input = Console.ReadLine();

            if (string.IsNullOrEmpty(input))
            {
                Log("Type a command or 'help' for a list of commands.");
                continue;
            }

            string[] args = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // case sensitive on purpose, because yes
            switch (args.GetOrDefault(0))
            {
                case "benchmark":
                    HandleBenchmark(args);
                    break;

                case "algoritm" or "algo":
                    HandleAlgo(args);
                    break;

#if true // debugging purpose, an empty case which i can easily toggle on and off, always empty because i remove the code after the test
                case "test":
                    Playback(EnvMode.Optimized, [new(1, 0)]);
                    break;
#endif

                case "playback":
                    // load from file
                    string binPath = args.GetOrDefault(1);
                    if (binPath.EndsWith(".bin"))
                    {
                        var content = ReadBinary(binPath);
                        if (content is null)
                        {
                            Log($"&cFailed to load content from '{binPath}'! (Perhaps file corrupted or not found)");
                            break;
                        }

                        PlaybackDirections = content.Value.vectors;
                        PlayBackMode = content.Value.mode;

                        Log($"&aLoading a video (mode: {PlayBackMode}) from '{binPath}'...");
                    }

                    // load from latest
                    if (PlaybackDirections.Length == 0)
                        Log("&cNo actions found to playback!");
                    else
                        Playback(PlayBackMode, PlaybackDirections);
                    break;

                #region Special cases (such as "help", "exit")

                case "noargs":
                    return true;

                case "help":
                    Log("Available commands: help, clear, exit");
                    break;

                case "cls":
                    Console.Clear();
                    Log(asciiArt);
                    Log("Type a command or 'help' for a list of commands.");
                    break;

                case "exit":
                    Log("&qExiting...");
                    return false;

                #endregion

                default:
                    Log($"Unknown command: {input}");
                    break;
            }
        }
    }

    #region Command Handlers

    static readonly Lock logLock = new(); // avoid race conditions
    static Vector2[] PlaybackDirections = []; // used in EnvUtils.Playback(here)
    static EnvMode PlayBackMode = EnvMode.Normal;

    static void HandleBenchmark(string[] args)
    {
        int threads = 1;
        int steps = 10_000;

        var mode = GetEnvMode(args);

        if (args.TryGetValueAfter("--steps", out var str_steps) && int.TryParse(str_steps, out steps))
            steps = Math.Max(steps, 1);

        if (args.TryGetValueAfter("--threads", out var str_threads) && int.TryParse(str_threads, out threads))
            threads = Math.Max(threads, 1); //Math.Clamp(threads, 1, Environment.ProcessorCount);

        Log($"\n&aBenchmark running... (mode: {mode}, {steps} steps, {threads} threads)\n");

        // TODO: Estimate time
        BenchmarkNormal(mode, threads, steps);
    }

    private static void BenchmarkNormal(EnvMode mode, int threads, int steps)
    {
        if (threads == 1)
        {
            var sw = Stopwatch.StartNew();

            var info = RunGameInstance(NewPoolEnv(mode), steps);

            sw.Stop();

            Log($"&qBanchmark ended succesfully in {sw.ElapsedMilliseconds}ms:");
            Log($"&sTicks: &d{info.ticks.WithSeparatorDots()}&s, BallsFell: &d{info.ballsFell}&s, Wins: &d{info.wins}&s, Losses: &d{info.losses}");

            var tickMs = (double)info.ticks / sw.ElapsedMilliseconds;
            var stepsMs = (double)steps / sw.ElapsedMilliseconds;
            Log($"&sTicks/ms: &d{tickMs:F2} &s(&d{((int)(tickMs * 1000)).WithSeparatorDots()} &sticks/s)");
            Log($"&sSteps/ms: &d{stepsMs:F2} &s(&d{((int)(stepsMs * 1000))/*.WithSeparatorDots()*/} &ssteps/s)");
        }
        else
        {
            // parallel benchmark
            var sw = Stopwatch.StartNew();

            int stepsPerThread = steps / threads;
            int remainder = steps % threads;

            var results = new GameInfo[threads];

            Parallel.For(0, threads, threadId =>
            {
                int threadSteps = stepsPerThread + (threadId < remainder ? 1 : 0);
                results[threadId] = RunGameInstance(NewPoolEnv(mode), threadSteps);

                // no order guarantee
                lock (logLock)
                    Log($"&sThread {threadId + 1}/{threads} simulated {threadSteps} steps in {sw.ElapsedMilliseconds}ms");
            });

            // merge results
            var total = new GameInfo(
                results.Sum(r => r.ticks),
                results.Sum(r => r.ballsFell),
                results.Sum(r => r.wins),
                results.Sum(r => r.losses)
            );

            sw.Stop();
            Log($"&qBenchmark ended successfully in {sw.ElapsedMilliseconds}ms ({threads} threads):");
            Log($"&sTicks: &d{total.ticks.WithSeparatorDots()}&s, BallsFell: &d{total.ballsFell}&s, Wins: {(total.wins == 0 ? "&d" : "&t")}{total.wins}&s, Losses: &d{total.losses}");

            var tickMs = (double)total.ticks / sw.ElapsedMilliseconds;
            var stepsMs = (double)steps / sw.ElapsedMilliseconds;
            Log($"&sTicks/ms: &d{tickMs:F2} &s(&d{((int)(tickMs * 1000)).WithSeparatorDots()} &sticks/s)");
            Log($"&sSteps/ms: &d{stepsMs:F2} &s(&d{((int)(stepsMs * 1000))/*.WithSeparatorDots()*/} &ssteps/s)");
        }
    }

    static void HandleAlgo(string[] args)
    {
        var mode = GetEnvMode(args);

        // 1. Attempt random directions, update the "best direction" and create a savestate every time a better one is found
        // Based on the priority: 1. victory found (instantly return), 2. most balls fell and 2. fewest ticks
        // 2. Roll back and repeat 1. for "samples" amount of times.
        // 3. Use the savestate of the best direction as the new "base state" and repeat 1.

        if (args.GetOrDefault(1) is "greedy_rollout" or "gr") // greedy rollout with random sampling
        {
            // TODO: allow parallelization
            int threads = 1;
            int samples = 10_000; // each state tries this amount of random directions and chooses the best

            if (args.TryGetValueAfter("--threads", out var str_threads) && int.TryParse(str_threads, out threads))
                threads = Math.Max(threads, 1);

            if (args.TryGetValueAfter("--samples", out var str_samples) && int.TryParse(str_samples, out samples))
                samples = Math.Max(samples, 1);

            // start
            Log($"\n&aGreedy Rollout Algorithm running... (mode: {mode}, {samples} samples, {threads} threads)\n");

            var sw = Stopwatch.StartNew();

            PlaybackDirections = Algorithm.GreedyRollout(NewPoolEnv(mode), samples, logging: true);
            PlayBackMode = mode;

            sw.Stop();

            Log($"&qSimulated {samples * PlaybackDirections.Length} steps successfully in {sw.ElapsedMilliseconds}ms");
            Log("&qType &w'playback' &qto visually see the best moves found.");

            // save in binary
            SaveAsBinary_Incremental($"{Program.Path_GreedyRollout}results.bin", PlayBackMode, PlaybackDirections);
        }
    }

    #endregion
    
    // returns the chosen env mode from args (check flags such as --optimized)
    private static EnvMode GetEnvMode(string[] args)
        => args.Contains("--optimized") ? EnvMode.Optimized : EnvMode.Normal;
}

enum EnvMode
{
    Normal = 0, Optimized = 1
}