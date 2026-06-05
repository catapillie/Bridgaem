using System.Numerics;
using Box2D.NET;
using Foster.Framework;
using FosterImGui;

namespace Bridgaem;

public class Game : App
{
    private readonly Batcher batch;
    private readonly Renderer imRenderer;

    private readonly B2WorldDef worldDef;
    private readonly B2WorldId worldId;

    private B2BodyId dynBodyId;

    private const int fps = 60;
    private const float dt = 1.0f / fps;


    public Game() : base(new AppConfig()
    {
        ApplicationName = "Bridgaem",
        WindowTitle = "WESH BRIDGE",
        Width = 1280,
        Height = 720,
        UpdateMode = UpdateMode.FixedStep(fps), // 60 fps
    })
    {
        batch = new(GraphicsDevice);
        imRenderer = new(this);

        worldDef = B2Types.b2DefaultWorldDef();
        worldDef.gravity = new(0f, -1f);

        worldId = B2Worlds.b2CreateWorld(worldDef);
    }

    protected override void Startup()
    {
        // ground
        B2BodyDef groundBodyDef = B2Types.b2DefaultBodyDef();
        groundBodyDef.position = new(0, -10);
        B2BodyId groundBodyId = B2Bodies.b2CreateBody(worldId, groundBodyDef);
        B2Polygon groundBox = B2Geometries.b2MakeBox(50, 5);
        B2ShapeDef groundShapeDef = B2Types.b2DefaultShapeDef();
        B2Shapes.b2CreatePolygonShape(groundBodyId, groundShapeDef, groundBox);

        // shape
        B2BodyDef bodyDef = B2Types.b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = new(0f, 4f);
        bodyDef.rotation = B2MathFunction.b2MakeRot(float.Pi / 8);
        B2BodyId bodyId = B2Bodies.b2CreateBody(worldId, bodyDef);
        B2Polygon dynamicBox = B2Geometries.b2MakeBox(1.0f, 1.0f);
        B2ShapeDef shapeDef = B2Types.b2DefaultShapeDef();
        shapeDef.density = 1.0f;
        shapeDef.material.friction = 0.3f;
        B2Shapes.b2CreatePolygonShape(bodyId, shapeDef, dynamicBox);
        dynBodyId = bodyId;
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

    protected override void Update()
    {
        if (Input.Keyboard.Down(Keys.Escape))
            Exit();

        const int substeps = 4;
        B2Worlds.b2World_Step(worldId, dt, substeps);
        var bodyPos = B2Bodies.b2Body_GetPosition(dynBodyId);
        var bodyRot = B2Bodies.b2Body_GetRotation(dynBodyId);
        float bodyAngle = float.Atan2(bodyRot.c, bodyRot.s);
        Console.WriteLine($"{bodyPos.X} {bodyPos.Y} {bodyAngle}");

        UpdateImGui();
    }

    protected override void Render()
    {
        Window.Clear(Color.Black);

        batch.PushMatrix(new(1280 / 2, 720 / 2), new(50, -50), 0f);
        {
            var bodyPos = B2Bodies.b2Body_GetPosition(dynBodyId);
            var bodyRot = B2Bodies.b2Body_GetRotation(dynBodyId);
            float bodyAngle = float.Atan2(bodyRot.c, bodyRot.s);
            batch.PushMatrix(new(bodyPos.X, bodyPos.Y), Vector2.One, bodyAngle);
            batch.Rect(0, 0, 2, 2, Color.Red);
            batch.PopMatrix();
        }
        batch.PopMatrix();

        batch.Render(Window);
        batch.Clear();

        imRenderer.Render();
    }
}