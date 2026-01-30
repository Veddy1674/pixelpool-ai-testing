using Raylib_cs;
using System.Numerics;
using static ColorLog;
using static IPoolEnv;

static class EnvUtils
{
    public static readonly int ReccomendedThreadCount = Math.Max(Environment.ProcessorCount - 2, 1);

    public struct GameInfo(int ticks, int ballsFell, int wins, int losses)
    {
        public int
            ticks = ticks,
            ballsFell = ballsFell,
            wins = wins,
            losses = losses;
    }

    public struct GameStep(int ticks, int ballsFell, Vector2 direction)
    {
        public int
            ticks = ticks,
            ballsFell = ballsFell;
        public Vector2 direction = direction;
    }

    public class BallState(bool isActive = false, Vector2 position = default)
    {
        public bool IsActive = isActive;
        public Vector2 Position = position;
    }

    // executes a game instance at max speed with the default logic
    public static GameInfo RunGameInstance(IPoolEnv env, int steps, float ballLaunchForce = 1500f)
    {
        env.Reset(); // if it was just created it's not necessary, but just in case...

        // wins is pretty much impossible to get to > 0
        int wins = 0, losses = 0;
        int totTicks = 0, totBalls = 0;

        for (int i = 0; i < steps; i++)
        {
            // game logic
            env.SetBallVelocity(0, RandomDirection * ballLaunchForce);
            env.TickUntilBallsStop(out int ticks, out int ballsFell, out var ending);

            if (ending != Ending.Running)
                env.Reset();

            // update values
            totTicks += ticks;
            totBalls += ballsFell;

            if (ending == Ending.Loss)
                losses++;
            else if (ending == Ending.Victory)
                wins++;
        }

        return new(totTicks, totBalls, wins, losses);
    }

    // finds a random direction that causes ending to be == withEnd
    public static Vector2 FindADirection(PoolEnv env, Ending withEnd)
        => FindADirection(env, (ticks, ballsFell, ending) => ending == withEnd);

    public static Vector2 FindADirection(PoolEnv env, Func<int, int, Ending, bool> condition)
    {
        // env isn't copied, but rather rollback is used
        var sav = new SaveState(env);
        sav.Save();

        for (int i = 0; i < 6_000; i++)
        {
            var dir = RandomDirection;

            env.SetBallVelocity(0, dir * 1500f);
            env.TickUntilBallsStop(out int ticks, out int ballsFell, out var ending);

            sav.Load(); // rollback
            if (condition(ticks, ballsFell, ending))
                return dir;
        }
        throw new TimeoutException("Over 6.000 steps were simulated without finding a valid direction with FindADirection(...)");
    }

    #region save/load binary

    public static void SaveAsBinary(string name, EnvMode mode, Vector2[] vectors)
    {
        using var writer = new BinaryWriter(File.Open(name/* + ".bin"*/, FileMode.Create));

        writer.Write((byte)mode);
        foreach (var v in vectors)
        {
            writer.Write(v.X); // float (4 byte)
            writer.Write(v.Y); // float (4 byte)
        }
    }

    // SaveAsBinary_Incremental("test.bin", vecs) -> if test.bin exists, test1.bin is created, and if that one also already exists, test2.bin and so on
    public static void SaveAsBinary_Incremental(string fileName, EnvMode mode, Vector2[] vectors)
    {
        string directory = Path.GetDirectoryName(fileName) ?? Directory.GetCurrentDirectory();
        string baseName = Path.GetFileNameWithoutExtension(fileName);
        string extension = Path.GetExtension(fileName);

        Directory.CreateDirectory(directory); // if no exists

        int fileNumber = 1; // first will be fileName + 1, not fileName itself
        string fullPath;

        do
        {
            fullPath = Path.Combine(directory, $"{baseName}{fileNumber}{extension}");
            fileNumber++;
        }
        while (File.Exists(fullPath) && fileNumber < 10000);

        SaveAsBinary(fullPath, mode, vectors);
    }

    public static (EnvMode mode, Vector2[] vectors)? ReadBinary(string name)
    {
        try
        {
            using var reader = new BinaryReader(File.OpenRead(name));
            var vectors = new List<Vector2>();

            byte b = reader.ReadByte();
            var mode = (EnvMode)b;

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                float x = reader.ReadSingle();
                float y = reader.ReadSingle();
                vectors.Add(new Vector2(x, y));
            }

            return (mode, [.. vectors]);
        }
        catch { return null; }
    }

    public static IPoolEnv NewPoolEnv(EnvMode mode) => mode switch
    {
        EnvMode.Optimized => new PoolEnvMini(),
        _ => new PoolEnv(), // "normal"
    };

    public static ISaveState NewSaveState(this IPoolEnv env)
    {
        if (env is PoolEnv poolEnv)
            return new SaveState(poolEnv);
        else if (env is PoolEnvMini poolEnvMini)
            return new SaveStateStruct(poolEnvMini);
        else
            throw new Exception($"Invalid Environment of type {env.GetType()}");
    }

    #endregion

    public static void Playback(EnvMode mode, Vector2[] playbackDirections)
    {
        if (playbackDirections is null || playbackDirections.Length == 0) return;
        IPoolEnv env = NewPoolEnv(mode);

        // load whole video
        int videoLengthFrames;
        int[] stepEndFrames = new int[playbackDirections.Length]; // UI X of where each step ends (for the "step indicators" above the gray line)
        List<ISaveState> videoFrames = new(playbackDirections.Length * 450); // considering an average of 450 ticks per step
        {
            int frameCount = 0;
            for (int i = 0; i < playbackDirections.Length; i++)
            {
                env.SetBallVelocity(0, playbackDirections[i] * 1500f);

                Ending? ending;
                do
                {
                    videoFrames.Add(env.NewSaveState().Save());
                    ending = env.GameUpdate();

                    frameCount++;

                } while (ending == Ending.Running);

                stepEndFrames[i] = frameCount - 1; // last tick of the step

                // if there is another action next, but a loss/win already happened
                if (playbackDirections.Length > i + 1 && (ending == Ending.Loss || ending == Ending.Victory))
                {
                    Log("&cAn error occourred while loading a video, either a loss was found or actions after a win were found");

                    // no more needed because "env" type should always be correct, as the detection is automatic
                    //Log("&cThis can happen if you try to load a video with PoolEnv which was simulated with PoolEnvMini or viceversa");
                    return;
                }
            }

            Log($"&qSuccessfully loaded a video with {videoLengthFrames = videoFrames.Count} ticks/frames ({playbackDirections.Length} actions)");
        }

        // playback

        // this is an absolute mess
        var start = new Vector2(722, 790);
        var end = new Vector2(722 + 504, 790);

        var downStart = start + new Vector2(0, 20);
        var downEnd = end + new Vector2(0, 20);

        bool dragging = false;
        Vector2 circlePos = start;
        float dragOffsetX = 0;

        // playback stuff

        float circleIncrement = 504f / (float)videoLengthFrames;

        bool videoPlaying = false;

        int currentPlaybackIndex = 0;

        BallState[] currentBalls = videoFrames[0].GetBallsPositionAndActive();

        bool videoEnded()
            => !(currentPlaybackIndex < videoLengthFrames - 1);

        float tickToXPos(int tick)
        {
            float progress = (float)tick / (videoLengthFrames - 1);
            return start.X + progress * (end.X - start.X);
        }

        var grayThingiesDrawPosX = new float[playbackDirections.Length - 1];
        for (int i = 0; i < playbackDirections.Length - 1; i++)
        {
            grayThingiesDrawPosX[i] = tickToXPos(stepEndFrames[i]);
        }

        // forcing 50 fps instead of async simulation
        EnvRenderer.RenderVideo(getBallsPositionAndActive: ref currentBalls, fps: 50,
            postRendering: () =>
            {
                var mousePos = Raylib.GetMousePosition();

                var mouseBtn0Pressed = Raylib.IsMouseButtonPressed(0);
                var mouseBtn0Released = Raylib.IsMouseButtonReleased(0);

                EnvRenderer.Utils_ShowCoordsAtMouse(mousePos, mouseBtn0Pressed);

                Raylib.DrawLineEx(start, end, 5f, Color.White);

                Raylib.DrawLineEx(downStart, downEnd, 3.5f, Color.LightGray); // steps line

                // segments (each step start and end)
                // 0 - 504
                drawgraythingy((int)start.X);

                for (int i = 0; i < grayThingiesDrawPosX.Length; i++)
                    drawgraythingy((int)grayThingiesDrawPosX[i]);

                drawgraythingy((int)end.X);

                const float threshold = 1220f;
                float ticksOffset = Utils.Lerp(5f, 15f, Math.Clamp((circlePos.X - threshold) / (end.X - threshold), 0f, 1f));

                Raylib.DrawTextEx(Utils.BahnschriftFont, $"{videoLengthFrames} Ticks", end + new Vector2(ticksOffset, -5), 14f, 0, Color.White);
                Raylib.DrawTextEx(Utils.BahnschriftFont, $"{playbackDirections.Length} Steps", downEnd + new Vector2(5, -5), 14f, 0, Color.Gray);

                #region inputs

                if (mouseBtn0Pressed && Raylib.CheckCollisionPointCircle(mousePos, circlePos, 12f))
                {
                    dragging = true;
                    videoPlaying = false;
                    dragOffsetX = circlePos.X - mousePos.X;
                }
                else if (mouseBtn0Released)
                    dragging = false;

                if (dragging)
                {
                    // update circle pos
                    circlePos = new(Math.Clamp(mousePos.X + dragOffsetX, start.X, end.X), start.Y);

                    // update video pos
                    currentPlaybackIndex = Math.Clamp((int)((circlePos.X - start.X) / circleIncrement), 0, videoLengthFrames - 1);
                    currentBalls = videoFrames[currentPlaybackIndex++].GetBallsPositionAndActive();

                    videoPlaying = false; // just to make sure one doesn't drag and click play
                }

                if (Raylib.IsKeyPressed(KeyboardKey.Space))
                {
                    if (videoEnded())
                    {
                        currentPlaybackIndex = 0; // restart from beginning
                        currentBalls = videoFrames[currentPlaybackIndex++].GetBallsPositionAndActive();

                        circlePos = start;
                    }

                    videoPlaying = !videoPlaying; // apply in the next draw frame
                }
                else if (videoPlaying)
                {
                    if (videoEnded())
                    {
                        videoPlaying = false; // stop at the end
                    }
                    else
                    {
                        currentBalls = videoFrames[currentPlaybackIndex++].GetBallsPositionAndActive();
                        circlePos += Vector2.UnitX * circleIncrement;
                    }
                }

                Raylib.DrawCircleV(circlePos, 12f, Color.White);

                #endregion
            });

        void drawgraythingy(int x)
        {
            // RELATIVE
            //Raylib.DrawLineEx(
            //    new Vector2(downStart.X + x, downStart.Y - 5),
            //    new Vector2(downStart.X + x, downStart.Y + 5), 3.5f,
            //    (x == 0 || x == 504) ? Color.Gray : Color.LightGray
            //);

            // ABSOLUTE
            Raylib.DrawLineEx(
                new Vector2(x, downStart.Y - 5),
                new Vector2(x, downStart.Y + 5), 3.5f,
                x == (int)start.X || x == (int)end.X
                    ? Color.Gray : Color.LightGray
            );
        }
    }

    // in "frames"
    //public static int GetVideoLength(Vector2[] playbackDirections)
    //{
    //    var env = new PoolEnv();
    //    int frames = 0;

    //    for (int i = 0; i < playbackDirections.Length; i++)
    //    {
    //        env.SetBallVelocity(0, playbackDirections[i] * 1500f);
    //        env.TickUntilBallsStop(out int f, out _, out _);
    //        frames += f;
    //    }
    //    return frames;
    //}
}
