using Box2D.NET;
using Bridgaem.BaseEntity;
using Foster.Framework;
using FosterImGui;
using System.Numerics;

namespace Bridgaem;

public class Game : App
{
    public static Game Instance { get; private set; }
    public static Batcher Batch { get; private set; } = null!;
    private readonly Renderer imRenderer;

    private readonly B2WorldDef worldDef;
    public static B2WorldId WorldId { get; private set; }

    private static readonly List<Entity> entities = [];

    private const int fps = 60;
    private const float physicsDt = 1.0f / fps;
    private const int physicsSubsteps = 4;

    public static float Dt { get; private set; } = 1.0f / fps;

    public static Vector2 Camera { get; set; }
    public static float Zoom { get; set; } = 1.0f;
    public static float BaseZoom = 20.0f; // pixels/meter

    private Player player;
    private BridgePlatform leftPlat, rightPlat;

    public Game() : base(new AppConfig()
    {
        ApplicationName = "Bridgaem",
        WindowTitle = "WESH BRIDGE",
        Width = 1280,
        Height = 720,
        UpdateMode = UpdateMode.FixedStep(fps),
    })
    {
        Instance = this;
        Batch = new(GraphicsDevice);
        imRenderer = new(this);

        worldDef = B2Types.b2DefaultWorldDef();
        worldDef.gravity = new(0f, 9.81f);

        WorldId = B2Worlds.b2CreateWorld(worldDef);
    }

    protected override void Startup()
    {
        Atlas.Load(GraphicsDevice);

        {
            // bg
            const int layerCount = 6;
            const float layerYdisp = 8f;
            Subtexture[] backmountains = [Atlas.Get("bg/mountain_back01"), Atlas.Get("bg/mountain_back02")];
            Subtexture[] mountains = [Atlas.Get("bg/mountain01"), Atlas.Get("bg/mountain02"), Atlas.Get("bg/mountain03")];
            Subtexture[] clouds = [Atlas.Get("bg/cloud01"), Atlas.Get("bg/cloud02"), Atlas.Get("bg/cloud03")];
            Subtexture water = Atlas.Get("bg/water");

            for (float x = -1000; x <= 1000; x += 120)
            {
                int layer = layerCount;
                float factor = float.Pow(0.75f, layer);
                var tex = clouds[Random.Shared.Next(clouds.Length)];
                float scroll = (1 + layer) / 2f;
                Instantiate(new Parallax(factor, scroll, tex, Color.White * 0.4f) { X = x - layer * 20, Y = layer * layerYdisp - 20 });
            }

            for (float x = -1000; x <= 1000; x += 60f)
            {
                int layer = layerCount;
                float factor = float.Pow(0.75f, layer);
                var tex = backmountains[Random.Shared.Next(backmountains.Length)];
                Instantiate(new Parallax(factor, 0.0f, tex, Color.White) { X = x - layer * 20, Y = 30 - layer * layerYdisp });
            }

            for (int layer = layerCount - 1; layer >= 0; layer -= 1)
            {
                float factor = float.Pow(0.75f, layer);

                for (float x = -1000; x <= 1000; x += 120)
                {
                    var tex = clouds[Random.Shared.Next(clouds.Length)];
                    float scroll = (1 + layer) / 2f;
                    Instantiate(new Parallax(factor, scroll, tex, Color.White * 0.2f) { X = x - layer * 20, Y = 30 + layer * layerYdisp });
                }

                Color color = Color.White;
                color.R = (byte)(color.R * float.Pow(0.85f, layer));
                color.G = (byte)(color.G * float.Pow(0.85f, layer));
                color.B = (byte)(color.B * float.Pow(0.95f, layer));
                for (float x = -1000; x <= 1000; x += 60f)
                {
                    var tex = mountains[Random.Shared.Next(mountains.Length)];
                    Instantiate(new Parallax(factor, 0.0f, tex, color) { X = x - layer * 20, Y = 30 - layer * layerYdisp });
                }

                for (float x = -1000; x <= 1000; x += water.Width / 2f)
                {
                    Instantiate(new Parallax(factor, 0.0f, water, color) { X = x - layer * 50, Y = 76 - layer * layerYdisp });
                }
            }
        }

        // Instantiate(new GameBox(new Vector2(0, 10), 256, 5, Calc.DegToRad * 0, B2BodyType.b2_kinematicBody));
        // Instantiate(new GameBox(new Vector2(10, 10), 30, 5, Calc.DegToRad * -40, B2BodyType.b2_kinematicBody));

        Instantiate(player = new Player(new Vector2()));
        Instantiate(leftPlat = new BridgePlatform(new(0, 40)));
        Instantiate(rightPlat = new BridgePlatform(new(40, 40)));

        Instantiate(new Bridge(new(0, 0), new(40, 20), 25));

        Camera += Vector2.UnitX * 20;
        Camera += Vector2.UnitY * 30;
    }

    protected override void Shutdown()
    {
        imRenderer.Dispose();
    }

    public static void Instantiate(Entity entity)
    {
        entities.Add(entity);
    }

    public static void Destroy(Entity entity)
    {
        entity.Destroy();
        entities.Remove(entity);
    }

    protected override void Update()
    {
        imRenderer.BeginLayout();

        Dt = Time.Delta;

        if (player is not null)
        {
            float targetX = player.ChassisPos.X + 20;
            const float movementOffset = 8;
            if (Input.Keyboard.Down(Keys.A)) targetX -= movementOffset;
            else if (Input.Keyboard.Down(Keys.D)) targetX += movementOffset;
            float dist = targetX - Camera.X;
            Camera += Vector2.UnitX * dist * float.Exp(-200 * Dt);
        }

        if (Input.Keyboard.Down(Keys.O))
            Zoom *= float.Pow(0.5f, Dt);
        if (Input.Keyboard.Down(Keys.P))
            Zoom *= float.Pow(2f, Dt);

        if (Input.Keyboard.Down(Keys.Escape))
            Exit();

        foreach (Entity entity in entities)
            entity.Update();

        B2Worlds.b2World_Step(WorldId, physicsDt, physicsSubsteps);

        if (imRenderer.WantsTextInput)
            Window.StartTextInput();
        else
            Window.StopTextInput();

        imRenderer.EndLayout();
    }

    protected override void Render()
    {
        Window.Clear(Color.DeepSkyBlue);

        // need two matrices because scaling and translation are not commutative 
        Batch.PushMatrix(Window.Size / 2, Vector2.One * Zoom * BaseZoom, 0f);
        Batch.PushMatrix(-Camera, Vector2.One, 0f);
        {
            Color color = Color.FromHexStringRGB("236dc0");
            Batch.Rect(new(-1000, 50, 2000, 2000), color);

            foreach (Entity entity in entities)
                entity.Render();


        }
        Batch.PopMatrix();
        Batch.PopMatrix();

        Batch.Render(Window);
        Batch.Clear();

        imRenderer.Render();
    }
}