using Box2D.NET;
using Bridgaem.BaseEntity;
using Foster.Framework;
using FosterImGui;
using System.Numerics;

namespace Bridgaem;

public class Game : App
{
    public static Batcher Batch { get; private set; } = null!;
    private readonly Renderer imRenderer;

    private readonly B2WorldDef worldDef;
    public static B2WorldId WorldId { get; private set; }

    private static List<Entity> entities = new();

    private const int fps = 60;
    private const float dt = 1.0f / fps;
    private const int substeps = 4;


    public Game() : base(new AppConfig()
    {
        ApplicationName = "Bridgaem",
        WindowTitle = "WESH BRIDGE",
        Width = 1280,
        Height = 720,
        UpdateMode = UpdateMode.FixedStep(fps), // 60 fps
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
        Instantiate(new GameBox(new Vector2(0, 0), 256, 5, Calc.DegToRad * 45, B2BodyType.b2_kinematicBody));
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
        if (Input.Keyboard.Down(Keys.Escape))
            Exit();

        foreach (Entity entity in entities)
            entity.Update();

        B2Worlds.b2World_Step(WorldId, dt, substeps);

        UpdateImGui();
    }

    protected override void Render()
    {
        Window.Clear(Color.Black);

        Batch.PushMatrix(new(1280 / 2, 720 / 2), new(5, 5), 0f);
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