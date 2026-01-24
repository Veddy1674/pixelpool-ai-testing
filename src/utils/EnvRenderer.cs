using Raylib_cs;
using System.Numerics;
using System.Runtime.InteropServices;
using static EnvUtils;

static class EnvRenderer
{
    public const int SCALE = 8; // global scale factor

    private static readonly Color backgroundColor = new(18, 18, 18);

    private static readonly Color[] ballColors = // precalc
        [
            Color.White,
            new Color(29, 29, 29), new Color(255, 255, 57), new Color(59, 59, 255),
            new Color(255, 59, 59), new Color(255, 121, 255), new Color(16, 171, 0),
            new Color(185, 113, 25), new Color(255, 113, 0), new Color(255, 239, 141),
            new Color(0, 138, 255), new Color(255, 117, 114), new Color(255, 165, 235),
            new Color(255, 181, 97), new Color(0, 233, 34), new Color(185, 149, 83),
        ];

    public static bool ShowHitboxes { get; set; } = false;
    public static bool ShowCoordinates { get; set; } = false;

    [DllImport("user32.dll")]
    static extern int GetSystemMetrics(int nIndex);

    private static readonly int ScreenWidth;
    private static readonly int ScreenHeight;

    static EnvRenderer()
    {
        ScreenWidth = GetSystemMetrics(0);
        ScreenHeight = GetSystemMetrics(1);
    }

    public static void EnableRendering(IPoolEnv env, Action? preRendering = null, Action? postRendering = null)
    {
        // init renderer, resizing is allowed but not meant
        Raylib.SetConfigFlags(ConfigFlags.MaximizedWindow | ConfigFlags.ResizableWindow | ConfigFlags.AlwaysRunWindow | ConfigFlags.VSyncHint);

        // vsync on but it doesn't matter, game logic isn't processed here!

        Raylib.InitWindow(ScreenWidth, ScreenHeight, "Pixel Pool");
        Raylib.MaximizeWindow();

        #region draw init

        var screenCenter = Raylib.GetScreenCenter();

        // pool table
        var poolTable = new Sprite(Utils.LoadTextureER("assets.pooltable1.png").WithSize(129, 64));

        // balls
        var balls = new Sprite[ballColors.Length];
        var ballTexture = Utils.LoadTextureER("assets.ball.png").WithSize(4, 4); // shared texture (texture size is also shared)

        for (int i = 0; i < balls.Length; i++)
            balls[i] = new Sprite(ballTexture, color: ballColors[i]);

        #endregion

        #region draw loop

        Utils.LoadFonts();

        while (!Raylib.WindowShouldClose())
        {
            preRendering?.Invoke(); // usually input related

            Raylib.BeginDrawing();

            // background
            Raylib.ClearBackground(backgroundColor);

            // draw table
            poolTable.DrawCentered(screenCenter);

            if (ShowHitboxes)
                Raylib.DrawRectangleLinesEx(IPoolEnv.poolTableCollision, 3f, Color.Lime); // draw table outline

            DrawBalls(env, env.GetBallsPosition(), balls);

            // draw holes hitbox
            if (ShowHitboxes)
            {
                foreach (var (position, radius) in IPoolEnv.ballHoles)
                    Raylib.DrawCircleLinesV(position, radius, Color.Yellow);
            }

            postRendering?.Invoke();

            Raylib.EndDrawing();
        }
        #endregion

        Raylib.CloseWindow();
    }

    /* RenderVideo with IPoolEnv
    public static void RenderVideo(IPoolEnv env, Action? preRendering = null, Action? postRendering = null, int fps = -1)
    {
        // init renderer, resizing is allowed but not meant
        var flags = ConfigFlags.MaximizedWindow | ConfigFlags.ResizableWindow | ConfigFlags.AlwaysRunWindow;
        if (fps == -1)
            flags |= ConfigFlags.VSyncHint;
        else
            Raylib.SetTargetFPS(fps);

        Raylib.SetConfigFlags(flags);
        // vsync on but it doesn't matter, game logic isn't processed here!

        Raylib.InitWindow(ScreenWidth, ScreenHeight, "Pixel Pool");
        Raylib.MaximizeWindow();

        #region draw init

        var screenCenter = Raylib.GetScreenCenter();

        // pool table
        var poolTable = new Sprite(Utils.LoadTextureER("assets.pooltable1.png").WithSize(129, 64));

        // balls
        var balls = new Sprite[ballColors.Length];
        var ballTexture = Utils.LoadTextureER("assets.ball.png").WithSize(4, 4); // shared texture (texture size is also shared)

        for (int i = 0; i < balls.Length; i++)
            balls[i] = new Sprite(ballTexture, color: ballColors[i]);

        #endregion

        #region draw loop

        Utils.LoadFonts();

        while (!Raylib.WindowShouldClose())
        {
            preRendering?.Invoke(); // usually input related

            Raylib.BeginDrawing();

            // background
            Raylib.ClearBackground(backgroundColor);

            // draw table
            poolTable.DrawCentered(screenCenter);

            if (ShowHitboxes)
                Raylib.DrawRectangleLinesEx(IPoolEnv.poolTableCollision, 3f, Color.Lime); // draw table outline

            DrawBalls(env, env.GetBallsPosition(), balls);

            // draw holes hitbox
            if (ShowHitboxes)
            {
                foreach (var (position, radius) in IPoolEnv.ballHoles)
                    Raylib.DrawCircleLinesV(position, radius, Color.Yellow);
            }

            postRendering?.Invoke();

            Raylib.EndDrawing();
        }
        #endregion

        Raylib.CloseWindow();
    }
    */

    // render video without env
    // getBallsPositionAndActive is intended to be modified inside preRendering or postRendering (or async)
    public static void RenderVideo(ref BallState[] getBallsPositionAndActive, Action? preRendering = null, Action? postRendering = null, int fps = -1)
    {
        // init renderer, resizing is allowed but not meant
        var flags = ConfigFlags.MaximizedWindow | ConfigFlags.ResizableWindow | ConfigFlags.AlwaysRunWindow;
        if (fps == -1)
            flags |= ConfigFlags.VSyncHint;
        else
            Raylib.SetTargetFPS(fps);

        Raylib.SetConfigFlags(flags);
        // vsync on but it doesn't matter, game logic isn't processed here!

        Raylib.InitWindow(ScreenWidth, ScreenHeight, "Pixel Pool");
        Raylib.MaximizeWindow();

        #region draw init

        var screenCenter = Raylib.GetScreenCenter();

        // pool table
        var poolTable = new Sprite(Utils.LoadTextureER("assets.pooltable1.png").WithSize(129, 64));

        // balls
        var balls = new Sprite[ballColors.Length];
        var ballTexture = Utils.LoadTextureER("assets.ball.png").WithSize(4, 4); // shared texture (texture size is also shared)

        for (int i = 0; i < balls.Length; i++)
            balls[i] = new Sprite(ballTexture, color: ballColors[i]);

        #endregion

        #region draw loop

        Utils.LoadFonts();

        while (!Raylib.WindowShouldClose())
        {
            preRendering?.Invoke(); // usually input related

            Raylib.BeginDrawing();

            // background
            Raylib.ClearBackground(backgroundColor);

            // draw table
            poolTable.DrawCentered(screenCenter);

            if (ShowHitboxes)
                Raylib.DrawRectangleLinesEx(IPoolEnv.poolTableCollision, 3f, Color.Lime); // draw table outline

            #region Draw Balls

            const float defaultBallRadius = 15f; // PoolEnv.ballRadius is private

            if (getBallsPositionAndActive[0].IsActive)
            {
                balls[0].DrawCentered((Vector2)getBallsPositionAndActive[0].Position); // cue ball
                if (ShowHitboxes)
                    Raylib.DrawCircleLinesV((Vector2)getBallsPositionAndActive[0].Position, defaultBallRadius, Color.Orange);
            }

            for (int i = 1; i < balls.Length; i++)
            {
                if (!getBallsPositionAndActive[i].IsActive) continue;

                balls[i].DrawCentered((Vector2)getBallsPositionAndActive[i].Position);

                if (ShowHitboxes)
                    Raylib.DrawCircleLinesV((Vector2)getBallsPositionAndActive[i].Position, defaultBallRadius, Color.Red);
            }

            #endregion

            // draw holes hitbox
            if (ShowHitboxes)
            {
                foreach (var (position, radius) in IPoolEnv.ballHoles)
                    Raylib.DrawCircleLinesV(position, radius, Color.Yellow);
            }

            postRendering?.Invoke();

            Raylib.EndDrawing();
        }
        #endregion

        Raylib.CloseWindow();
    }

    private static void DrawBalls(IPoolEnv env, Vector2[] positions, Sprite[] balls)
    {
        // logic is fundamentally the same, but a general "BallsBody" type cannot exist because one is class and the other a struct
        if (env is PoolEnv poolEnv)
        {
            if (poolEnv.BallsBody[0].IsActive)
            {
                balls[0].DrawCentered(positions[0]); // cue ball
                if (ShowHitboxes)
                    Raylib.DrawCircleLinesV(positions[0], poolEnv.BallsBody[0].RadiusCollider, Color.Orange);
            }

            for (int i = 1; i < balls.Length; i++)
            {
                if (!poolEnv.BallsBody[i].IsActive) continue;

                balls[i].DrawCentered(positions[i]);

                if (ShowHitboxes)
                    Raylib.DrawCircleLinesV(positions[i], poolEnv.BallsBody[i].RadiusCollider, Color.Red);
            }
        }
        else if (env is PoolEnvMini poolEnvMini)
        {
            if (poolEnvMini.BallsBody[0].IsActive)
            {
                balls[0].DrawCentered(positions[0]); // cue ball
                if (ShowHitboxes)
                    Raylib.DrawCircleLinesV(positions[0], IPoolEnv.ballRadius, Color.Orange);
            }

            for (int i = 1; i < balls.Length; i++)
            {
                if (!poolEnvMini.BallsBody[i].IsActive) continue;

                balls[i].DrawCentered(positions[i]);

                if (ShowHitboxes)
                    Raylib.DrawCircleLinesV(positions[i], IPoolEnv.ballRadius, Color.Red);
            }
        }
    }

    private static bool clipBoardCopied = false;
    public static void Utils_ShowCoordsAtMouse(Vector2 mousePosition, bool isMouseBtn0Down)
    {
        var mp = mousePosition + new Vector2(20, -20);
        string s = clipBoardCopied ? "Copied to clipboard!" : $"x{mp.X} y{mp.Y}";

        Raylib.DrawText(s, (int)mp.X, (int)mp.Y, 20, Color.White);

        if (isMouseBtn0Down)
        {
            Raylib.SetClipboardText($"{mp.X}, {mp.Y}");
            new Thread(() =>
            {
                clipBoardCopied = true;
                Thread.Sleep(500);
                clipBoardCopied = false;
            }).Start();
        }
    }

    public static void Utils_ShowCoordsAtMouse()
        => Utils_ShowCoordsAtMouse(Raylib.GetMousePosition(), Raylib.IsMouseButtonPressed(0));
}