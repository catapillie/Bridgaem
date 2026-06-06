using Box2D.NET;
using Bridgaem.BaseEntity;
using Foster.Framework;
using FosterImGui;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;

namespace Bridgaem;

public class Game : App
{
    public static Batcher Batch { get; private set; } = null!;
    private readonly Renderer imRenderer;

    private readonly B2WorldDef worldDef;
    public static B2WorldId WorldId { get; private set; }

    private static readonly List<Entity> entities = [];

    private const int fps = 60;
    private const float dt = 1.0f / fps;
    private const int substeps = 4;

    public static float Dt { get; private set; } = 1.0f / fps;



    public Game() : base(new AppConfig()
    {
        ApplicationName = "Bridgaem",
        WindowTitle = "WESH BRIDGE",
        Width = 1280,
        Height = 720,
        UpdateMode = UpdateMode.FixedStep(fps),
    })
    {
        Batch = new(GraphicsDevice);
        imRenderer = new(this);

        worldDef = B2Types.b2DefaultWorldDef();
        worldDef.gravity = new(0f, 9.81f);

        WorldId = B2Worlds.b2CreateWorld(worldDef);
    }

    protected override void Startup()
    {
        Atlas.Load(GraphicsDevice);

        {// bg
            const int layerCount = 6;
            Subtexture[] backmountains = [Atlas.Get("bg/mountain_back01"), Atlas.Get("bg/mountain_back02")];
            Subtexture[] mountains = [Atlas.Get("bg/mountain01"), Atlas.Get("bg/mountain02"), Atlas.Get("bg/mountain03")];
            Subtexture[] clouds = [Atlas.Get("bg/cloud01"), Atlas.Get("bg/cloud02"), Atlas.Get("bg/cloud03")];

            for (float x = -1000; x <= 1000; x += 60f)
            {
                int layer = layerCount;
                var tex = backmountains[Random.Shared.Next(backmountains.Length)];
                Instantiate(new Parallax(0.5f, 0.0f, tex, Color.White) { X = x - layer * 20, Y = 30 - layer * 5 });
            }

            for (int layer = layerCount - 1; layer >= 0; layer -= 1)
            {
                for (float x = -1000; x <= 1000; x += 120)
                {
                    var tex = clouds[Random.Shared.Next(clouds.Length)];
                    float scroll = (1 + layer) / 2f;
                    Instantiate(new Parallax(0.5f, scroll, tex, Color.White * 0.2f) { X = x - layer * 20, Y = 30 + layer * 5 });
                }

                Color color = Color.White;
                color.R = (byte)(color.R * float.Pow(0.8f, layer));
                color.G = (byte)(color.G * float.Pow(0.8f, layer));
                color.B = (byte)(color.B * float.Pow(0.9f, layer));
                for (float x = -1000; x <= 1000; x += 60f)
                {
                    var tex = mountains[Random.Shared.Next(mountains.Length)];
                    Instantiate(new Parallax(0.5f, 0.0f, tex, color) { X = x - layer * 20, Y = 30 - layer * 5 });
                }

            }
        }

        Instantiate(new GameBox(new Vector2(0, 10), 256, 5, Calc.DegToRad * 40, B2BodyType.b2_kinematicBody));
        for (int i = 1; i <= 10; i++)
            Instantiate(new GameBox(new Vector2(0, -10), 2, 2, 0, B2BodyType.b2_dynamicBody));
    }

    protected override void Shutdown()
    {
        imRenderer.Dispose();
    }

    private void UpdateImGui()
    {
        imRenderer.BeginLayout();

        if (imRenderer.WantsTextInput)
            Window.StartTextInput();
        else
            Window.StopTextInput();

        imRenderer.EndLayout();
    }

    public static void Instantiate(Entity entity)
    {
        entities.Add(entity);
    }

    public static void Destroy(Entity entity)
    {
        entities.Remove(entity);
    }

    protected override void Update()
    {
        Dt = Time.Delta;

        if (Input.Keyboard.Down(Keys.Escape))
            Exit();

        foreach (Entity entity in entities)
            entity.Update();

        B2Worlds.b2World_Step(WorldId, dt, substeps);

        UpdateImGui();
    }

    protected override void Render()
    {
        Window.Clear(Color.SkyBlue);

        float t = (float)Time.Elapsed.TotalSeconds / 2f;
        System.Console.WriteLine(t);
        float scale = 1 + Ease.Bounce.Out(float.Abs(2 * (t % 1f) - 1)) * 10;
        Batch.PushMatrix(new(1280 / 2, 720 / 2), Vector2.One * 5 * scale, 0f);
        {
            foreach (Entity entity in entities)
                entity.Render();
        }
        Batch.PopMatrix();

        Batch.Render(Window);
        Batch.Clear();

        imRenderer.Render();
    }
}