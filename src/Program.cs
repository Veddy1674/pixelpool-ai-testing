using Raylib_cs;
using System.Numerics;
using static Ending;
using static Raylib_cs.KeyboardKey;

class Program
{
    public static readonly string Path_GreedyRollout = "saved/greedy_rollout/";
    public static readonly string Path_Test = "saved/test/";

    static void Main(string[] args)
    {
        // disable logging
        Raylib.SetTraceLogLevel(TraceLogLevel.None);

        // create folders for saving stuff (harcoded)
        Directory.CreateDirectory(Path_GreedyRollout);
        Directory.CreateDirectory(Path_Test);

        ConfigUtils.CreateIfNotExists(); // saved/config.json

        if (args.Contains("--headless"))
        {
            bool noargs = ProgramCLI.Init(); // returns wheter to run the program normally

            PythonUtils.ShutdownPython(); // shut if was running

            if (!noargs) return;
        }
        // TODO hide command prompt?

        // logic to run one game instance, rendered, not meant for AI training, more like viewing results and debugging
        var env = new PoolEnv();

        Ending lastGameState = Running;
        bool isPaused = false;
        ManualResetEventSlim pauseEvent = new(true);

        void GameUpdate()
        {
            var gameState = env.GameUpdate();

            if (gameState != null && gameState != lastGameState) // if game state changed
                lastGameState = (Ending)gameState;
        }

        void SetPaused(bool paused = true)
        {
            isPaused = paused;

            if (isPaused)
                pauseEvent.Reset();
            else
                pauseEvent.Set();
        }

        var gameLogic = Task.Run(async () =>
        {
            while (true)
            {
                pauseEvent.Wait();

                GameUpdate();

                await Task.Delay(20); // 50 fps
            }
        });

        EnvRenderer.EnableRendering(env, // sync
            preRendering: () =>
        {
            // inputs, pre-rendering
            if (KeyPressing(R))
            {
                env.Reset();
            }
            else if (KeyPressing(W))
            {
                var dir = (Raylib.GetMousePosition() - env.BallsBody[0].Position).Normalized();

                env.SetBallVelocity(0, dir * 1500f);
            }
            else if (KeyPressing(P))
            {
                SetPaused(!isPaused);
            }
            else if (KeyPressing(H))
            {
                EnvRenderer.ShowHitboxes = !EnvRenderer.ShowHitboxes;
                EnvRenderer.ShowCoordinates = !EnvRenderer.ShowCoordinates;
            }
            else if (KeyPressing(Grave)) // backslash for europeans
            {
                if (isPaused)
                {
                    // frame advance
                    GameUpdate();
                }
                else
                    SetPaused(true);
            }

        },
            postRendering: () =>
        {
            if (EnvRenderer.ShowCoordinates)
                EnvRenderer.Utils_ShowCoordsAtMouse();

            Raylib.DrawFPS(10, 10);

            // other:
            Raylib.DrawText("Paused: ", 495, 221, 20, Color.White);

            Raylib.DrawText(isPaused ? "TRUE" : "FALSE", 584, 221, 20, isPaused ? Color.Green : Color.Red);

            Raylib.DrawText("Last State: ", 495, 198, 20, Color.White);

            Raylib.DrawText(lastGameState == Victory ? "Victory" : lastGameState == Loss ? "Loss" : "Running", 620, 198, 20, lastGameState == Victory ? Color.Yellow : lastGameState == Loss ? Color.Red : Color.Green);

            // show all keybinds
            float y = 20;
            Raylib.DrawTextEx(Utils.BahnschriftFont, "R: Resets the environment", new(10, y += 20), 20, 0, Color.White);
            Raylib.DrawTextEx(Utils.BahnschriftFont, "P: Pauses the game", new(10, y += 20), 20, 0, Color.White);
            Raylib.DrawTextEx(Utils.BahnschriftFont, "W: Launches the ball", new(10, y += 20), 20, 0, Color.White);
            Raylib.DrawTextEx(Utils.BahnschriftFont, "H: Toggle debug mode", new(10, y += 20), 20, 0, Color.White);
            Raylib.DrawTextEx(Utils.BahnschriftFont, "Grave: Frame advance", new(10, y += 20), 20, 0, Color.White);
        });
        
        // end and cleanup
        gameLogic.Wait();
        pauseEvent.Dispose();
    }

    // alias
    private static bool KeyPressing(KeyboardKey key)
        => Raylib.IsKeyPressed(key);
}