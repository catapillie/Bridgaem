using Box2D.NET;
using Bridgaem.BaseEntity;
using Bridgaem.Utility;
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

    public Player Player { get; private set; }
    private BridgePlatform leftPlat, rightPlat;

    public enum Direction
    {
        Right,
        Left,
    }

    public Direction CurrentDirection { get; private set; } = Direction.Right;

    private const float SafeTime = 2f;
    private float crossedTimer = 0.0f;
    private bool hasCrossed = false;


    enum PlacementKind
    {
        None,
        Bridge,
        Fan,
        UpdownPlank,
        TurnPlank,
        Slingshot,
    }
    private PlacementKind placementKind = PlacementKind.None;

    private readonly Dictionary<PlacementKind, int> inventory = [];

    public static SpriteFont Font { get; private set; } = null!;
    private const float iconScale = 3f;

    private uint Score = 0;
    private float scoreLerp = 0f;

    public enum State
    {
        Playing,
        Editing,
    }

    public State CurrentState { get; set; } = State.Editing;


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

        Font = new SpriteFont(GraphicsDevice,
            new Font("assets/font/archivo_black.ttf"), 200f);
    }

    private void GrantPlacement(PlacementKind kind, int count)
    {
        if (inventory.ContainsKey(kind))
            inventory[kind] += count;
        else
            inventory.Add(kind, count);
    }

    private void UsePlacement(PlacementKind kind)
    {
        if (inventory.ContainsKey(kind) && inventory[kind] > 0)
            inventory[kind]--;
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
        Instantiate(leftPlat = new BridgePlatform(new(0, 40)));
        Instantiate(rightPlat = new BridgePlatform(new(40, 40)));

        Vector2 playerPosition = B2Bodies.b2Body_GetPosition(leftPlat.BodyId).ToVector2() - new Vector2(0, leftPlat.Height / 2 + 5);
        Instantiate(Player = new Player(playerPosition));

        Camera += Vector2.UnitX * 20;
        Camera += Vector2.UnitY * 30;

        GrantPlacement(PlacementKind.Bridge, 1);
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

    private Vector2 bridgePlacementLeft, bridgePlacementRight;
    private bool isPlacingBridge = false;
    private void BridgePlacement()
    {
        if (isPlacingBridge)
        {
            const float maxLength = 30f;
            bridgePlacementRight = ScreenToWorld(Input.Mouse.Position);
            Vector2 diff = bridgePlacementRight - bridgePlacementLeft;
            float d = float.Clamp(diff.Length(), 0.0f, maxLength);
            if (d > 0.1f)
            {
                Vector2 dir = diff.Normalized();
                bridgePlacementRight = bridgePlacementLeft + dir * d;
            }


            if (!Input.Mouse.LeftDown)
            {
                float len = Vector2.Distance(bridgePlacementLeft, bridgePlacementRight);
                int tileCount = 1 + (int)len * 2;
                Instantiate(new Bridge(bridgePlacementLeft, bridgePlacementRight, tileCount));
                isPlacingBridge = false;
                UsePlacement(PlacementKind.Bridge);
                return;
            }

        }
        else if (Input.Mouse.LeftPressed)
        {
            bridgePlacementLeft = ScreenToWorld(Input.Mouse.Position);
            isPlacingBridge = true;
        }
    }

    private void FanPlacement()
    {
        if (Input.Mouse.LeftPressed)
        {
            Vector2 pos = ScreenToWorld(Input.Mouse.Position);
            Instantiate(new Fan(pos, 20f, 1f, 200f));
            UsePlacement(PlacementKind.Fan);
        }
    }

    private void UpdownPlankPlacement()
    {
        if (Input.Mouse.LeftPressed)
        {
            Vector2 pos = ScreenToWorld(Input.Mouse.Position);
            Instantiate(new Plank([
                pos,
                pos - Vector2.UnitY * 20f
            ], 20f, 1.4f, 0f, 0f, 5));
            UsePlacement(PlacementKind.UpdownPlank);
        }
    }


    private void TurnPlankPlacement()
    {
        if (Input.Mouse.LeftPressed)
        {
            Vector2 pos = ScreenToWorld(Input.Mouse.Position);
            Instantiate(new Plank([pos], 20f, 1.4f, 0f, 2.5f, 5));
            UsePlacement(PlacementKind.TurnPlank);
        }
    }



    private void SlingshotPlacement()
    {
        if (Input.Mouse.LeftPressed)
        {
            Vector2 pos = ScreenToWorld(Input.Mouse.Position);
            Instantiate(new LaunchPad(pos, 20f, 1.4f));
            UsePlacement(PlacementKind.Slingshot);
        }
    }

    private void HandlePlacements()
    {
        {
            if (inventory.TryGetValue(placementKind, out int count) && count <= 0)
                placementKind = PlacementKind.None;
        }

        {
            Subtexture slotTex = Atlas.Get("icon_slot");
            Vector2 iconPos = Vector2.Zero;
            Rect bounds;
            foreach (var (k, count) in inventory)
            {
                bounds = new(iconPos, slotTex.Width * iconScale, slotTex.Height * iconScale);
                if (Input.Mouse.LeftPressed && bounds.Contains(Input.Mouse.Position) && count > 0)
                {
                    placementKind = k;
                    return;
                }
                iconPos += Vector2.UnitY * slotTex.Height * iconScale;
            }

            //switch to playing
            iconPos = new Vector2(Window.Width, Window.Height) - slotTex.Size * iconScale;
            bounds = new(iconPos, slotTex.Width * iconScale, slotTex.Height * iconScale);
            if (Input.Mouse.LeftPressed && bounds.Contains(Input.Mouse.Position))
            {
                CurrentState = State.Playing;
                Player.Respawn();
                return;
            }
        }

        switch (placementKind)
        {
            case PlacementKind.Bridge:
                BridgePlacement();
                return;
            case PlacementKind.Fan:
                FanPlacement();
                return;
            case PlacementKind.UpdownPlank:
                UpdownPlankPlacement();
                return;
            case PlacementKind.TurnPlank:
                TurnPlankPlacement();
                return;
            case PlacementKind.Slingshot:
                SlingshotPlacement();
                return;

            case PlacementKind.None:
            default: break;
        }
    }

    private void UpdateCamera()
    {
        float targetZoom = 0.6f;
        if (CurrentState is State.Editing)
        {
            targetZoom = 0.4f;
            const float panSpeed = 60;
            if (Input.Keyboard.Down(Keys.Left))
                Camera -= Vector2.UnitX * panSpeed * Dt;
            if (Input.Keyboard.Down(Keys.Right))
                Camera += Vector2.UnitX * panSpeed * Dt;
            if (Input.Keyboard.Down(Keys.Up))
                Camera -= Vector2.UnitY * panSpeed * Dt;
            if (Input.Keyboard.Down(Keys.Down))
                Camera += Vector2.UnitY * panSpeed * Dt;
        }
        Zoom += (targetZoom - Zoom) * float.Exp(-Dt * 100);

        if (CurrentState is State.Playing)
        {
            if (Player is not null)
            {
                float dirOffset = hasCrossed ? 0 : CurrentDirection switch
                {
                    Direction.Right => +1,
                    Direction.Left => -1,
                    _ => 0f
                };
                float targetX = Player.ChassisPos.X + dirOffset * 20;
                float distX = targetX - Camera.X;
                Camera += Vector2.UnitX * distX * float.Exp(-200 * Dt);


                float targetY = Player.ChassisPos.Y - 10;
                float distY = targetY - Camera.Y;
                Camera += Vector2.UnitY * distY * float.Exp(-200 * Dt);
            }
        }
    }

    protected override void Update()
    {
        imRenderer.BeginLayout();

        Dt = Time.Delta;

        UpdateCamera();

        scoreLerp = Calc.Approach(scoreLerp, 0f, Dt);

#if DEBUG
        if (Input.Keyboard.Down(Keys.Escape))
            Exit();
#endif

        if (CurrentState is State.Editing)
        {
            // placement  
            HandlePlacements();
        }
        else
        {
            {
                //back
                Subtexture slotTex = Atlas.Get("icon_slot");
                Vector2 iconPos = new Vector2(Window.Width, Window.Height) - slotTex.Size * iconScale;
                Rect bounds = new(iconPos, slotTex.Width * iconScale, slotTex.Height * iconScale);
                if (Input.Mouse.LeftPressed && bounds.Contains(Input.Mouse.Position))
                {
                    CurrentState = State.Editing;
                    Player.Respawn();
                }
            }
        }

        foreach (Entity entity in entities)
            entity.Update();

        // gameplay loop
        {
            if (Player is not null)
            {
                hasCrossed = CurrentDirection switch
                {
                    Direction.Right => rightPlat.IsDetected(Player),
                    Direction.Left => leftPlat.IsDetected(Player),
                    _ => false,
                };

                if (hasCrossed)
                {
                    crossedTimer += Dt;
                    if (crossedTimer >= SafeTime)
                    {
                        // level up
                        crossedTimer = 0f;
                        hasCrossed = false;
                        CurrentState = State.Editing;
                        Score++;
                        scoreLerp = 1f;


                        PlacementKind[] availableObjects = [
                            PlacementKind.Bridge, PlacementKind.Bridge, PlacementKind.Bridge,
                            PlacementKind.Fan, PlacementKind.Fan, PlacementKind.Fan,
                            PlacementKind.UpdownPlank, PlacementKind.UpdownPlank, PlacementKind.UpdownPlank,
                            PlacementKind.TurnPlank,
                            PlacementKind.Slingshot,
                        ];

                        // two new obj
                        GrantPlacement(availableObjects[Random.Shared.Next(availableObjects.Length)], 1);
                        GrantPlacement(availableObjects[Random.Shared.Next(availableObjects.Length)], 1);
                        if (Score <= 10)
                            GrantPlacement(availableObjects[Random.Shared.Next(availableObjects.Length)], 1);


                        switch (CurrentDirection)
                        {
                            case Direction.Right:
                                CurrentDirection = Direction.Left;
                                leftPlat.TargetPos -= Vector2.UnitX * 40;
                                Player.RespawnPos = B2Bodies.b2Body_GetPosition(rightPlat.BodyId).ToVector2() - new Vector2(0, rightPlat.Height / 2 + 5);
                                break;
                            case Direction.Left:
                                CurrentDirection = Direction.Right;
                                rightPlat.TargetPos += Vector2.UnitX * 40;
                                Player.RespawnPos = B2Bodies.b2Body_GetPosition(leftPlat.BodyId).ToVector2() - new Vector2(0, leftPlat.Height / 2 + 5);
                                break;
                            default: break;
                        }
                    }
                }
                else
                {
                    crossedTimer = 0f;
                }
            }


            B2Worlds.b2World_Step(WorldId, physicsDt, physicsSubsteps);

            if (imRenderer.WantsTextInput)
                Window.StartTextInput();
            else
                Window.StopTextInput();

            imRenderer.EndLayout();
        }
    }

    private Vector2 ScreenToWorld(Vector2 pos)
    {
        pos -= Window.Size / 2;
        pos /= Zoom * BaseZoom;
        pos += Camera;
        return pos;
    }

    private string GetPlacementIconName(PlacementKind k)
     => k switch
     {
         PlacementKind.Bridge => "icons/bridge",
         PlacementKind.Fan => "icons/fan",
         PlacementKind.UpdownPlank => "icons/updownplank",
         PlacementKind.TurnPlank => "icons/turnplank",
         PlacementKind.Slingshot => "icons/sling",
         PlacementKind.None => "icons/none",
         _ => "icons/none",
     };

    private string GetPlacementName(PlacementKind k)
     => k switch
     {
         PlacementKind.Bridge => "Bridge",
         PlacementKind.Fan => "Fan",
         PlacementKind.UpdownPlank => "Plank (up-down)",
         PlacementKind.TurnPlank => "Plank (turn)",
         PlacementKind.Slingshot => "Launchpad",
         PlacementKind.None => "None",
         _ => "None",
     };

    private void RenderBridgePlacement()
    {
        if (!isPlacingBridge)
            return;

        float offset = (float)Time.Elapsed.TotalSeconds * 5f % 1f;
        Batch.LineDashed(bridgePlacementLeft, bridgePlacementRight, 0.2f, Color.White, 1f, offset);
    }

    private void RenderBridgePlacementGizmo()
    {
        Subtexture iconTexture = Atlas.Get(GetPlacementIconName(PlacementKind.Bridge));
        Batch.Image(iconTexture, Input.Mouse.Position, Vector2.Zero, Vector2.One * 2, 0f, Color.White);
    }

    private void RenderFanPlacement()
    {
        Subtexture iconTexture = Atlas.Get("fan/up1");
        Batch.ImageJustified(iconTexture, ScreenToWorld(Input.Mouse.Position), new(.5f, .5f), 0.3f, Color.White * 0.5f);
    }

    private void RenderFanPlacementGizmo()
    {

    }

    private void RenderUpdownPlankPlacement()
    {
        Batch.ImageJustified(
            Atlas.Get("plank"), ScreenToWorld(Input.Mouse.Position),
            Vector2.One * 0.5f, 0.15f, Color.White * 0.7f);
        Batch.ImageJustified(
            Atlas.Get("plank"), ScreenToWorld(Input.Mouse.Position) - Vector2.UnitY * 20,
            Vector2.One * 0.5f, 0.15f, Color.White * 0.7f);
        Batch.LineDashed(
            ScreenToWorld(Input.Mouse.Position),
            ScreenToWorld(Input.Mouse.Position) - Vector2.UnitY * 20, 0.2f, Color.White * 0.7f, 1f, 0f);
    }

    private void RenderUpdownPlankPlacementGizmo()
    {
        Subtexture iconTexture = Atlas.Get(GetPlacementIconName(PlacementKind.UpdownPlank));
        Batch.Image(iconTexture, Input.Mouse.Position, Vector2.Zero, Vector2.One * 2, 0f, Color.White);
    }

    private void RenderTurnPlankPlacement()
    {
        Batch.ImageJustified(
            Atlas.Get("plank"), ScreenToWorld(Input.Mouse.Position),
            Vector2.One * 0.5f, 0.15f, Color.White * 0.7f);
        float offset = (float)Time.Elapsed.TotalSeconds * 5f % 1f;
        Batch.CircleDashed(new Circle(ScreenToWorld(Input.Mouse.Position), 10f), 0.2f, 32, Color.White * 0.7f, 1f, offset);
    }

    private void RenderTurnPlankPlacementGizmo()
    {
        Subtexture iconTexture = Atlas.Get(GetPlacementIconName(PlacementKind.TurnPlank));
        Batch.Image(iconTexture, Input.Mouse.Position, Vector2.Zero, Vector2.One * 2, 0f, Color.White);
    }

    private void RenderSlingshotPlacement()
    {
        Batch.ImageJustified(
            Atlas.Get("sling"), ScreenToWorld(Input.Mouse.Position),
            Vector2.One * 0.5f, 0.15f, Color.White * 0.7f);

        float offset = (float)Time.Elapsed.TotalSeconds * 5f % 1f;
        Batch.LineDashed(ScreenToWorld(Input.Mouse.Position), ScreenToWorld(Input.Mouse.Position) - Vector2.UnitY * 8, 0.2f, Color.White, 1f, offset);
    }

    private void RenderSlingshotPlacementGizmo()
    {
        Subtexture iconTexture = Atlas.Get(GetPlacementIconName(PlacementKind.Slingshot));
        Batch.Image(iconTexture, Input.Mouse.Position, Vector2.Zero, Vector2.One * 2, 0f, Color.White);
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

            // world gizmos for placements
            if (CurrentState is State.Editing)
            {
                switch (placementKind)
                {
                    case PlacementKind.Bridge:
                        RenderBridgePlacement(); break;
                    case PlacementKind.Fan:
                        RenderFanPlacement(); break;
                    case PlacementKind.UpdownPlank:
                        RenderUpdownPlankPlacement(); break;
                    case PlacementKind.TurnPlank:
                        RenderTurnPlankPlacement(); break;
                    case PlacementKind.Slingshot:
                        RenderSlingshotPlacement(); break;

                    case PlacementKind.None:
                    default: break;
                }
            }
        }
        Batch.PopMatrix();
        Batch.PopMatrix();

        Subtexture slotTex = Atlas.Get("icon_slot");
        Vector2 iconPos;
        if (CurrentState is State.Playing)
        {
            // back icon
            {
                iconPos = new Vector2(Window.Width, Window.Height) - slotTex.Size * iconScale;
                Rect bounds = new(iconPos, slotTex.Width * iconScale, slotTex.Height * iconScale);
                Color color = Color.White * 0.8f;
                if (bounds.Contains(Input.Mouse.Position))
                    color = Color.White;
                Subtexture iconTexture = Atlas.Get("icons/back");
                Batch.Image(slotTex, iconPos, Vector2.Zero, Vector2.One * iconScale, 0f, color);
                Batch.Image(iconTexture, iconPos, Vector2.Zero, Vector2.One * iconScale, 0f, color);
            }
        }

        // ui placement
        if (CurrentState is State.Editing)
        {
            iconPos = Vector2.Zero;
            foreach (var (k, count) in inventory)
            {
                Rect bounds = new(iconPos, slotTex.Width * iconScale, slotTex.Height * iconScale);
                Color color = count > 0 ? (Color.White * 0.8f) : (Color.Red * 0.5f);
                if (bounds.Contains(Input.Mouse.Position) && count > 0 || placementKind == k)
                {
                    color = Color.White;
                }

                Subtexture iconTexture = Atlas.Get(GetPlacementIconName(k));
                Batch.Image(slotTex, iconPos, Vector2.Zero, Vector2.One * iconScale, 0f, color);
                Batch.Image(iconTexture, iconPos, Vector2.Zero, Vector2.One * iconScale, 0f, color);
                Font.Draw(Batch, count.ToString(), iconPos + Vector2.UnitX * slotTex.Width * iconScale, 30f, color);
                if (placementKind == k)
                    Font.Draw(Batch, GetPlacementName(placementKind),
                        iconPos + new Vector2(slotTex.Width * iconScale, slotTex.Height * iconScale * 0.5f),
                        30f, color);
                iconPos += Vector2.UnitY * slotTex.Height * iconScale;
            }

            // go icon
            {
                iconPos = new Vector2(Window.Width, Window.Height) - slotTex.Size * iconScale;
                Rect bounds = new(iconPos, slotTex.Width * iconScale, slotTex.Height * iconScale);
                Color color = Color.White * 0.8f;
                if (bounds.Contains(Input.Mouse.Position))
                    color = Color.White;
                Subtexture iconTexture = Atlas.Get("icons/go");
                Batch.Image(slotTex, iconPos, Vector2.Zero, Vector2.One * iconScale, 0f, color);
                Batch.Image(iconTexture, iconPos, Vector2.Zero, Vector2.One * iconScale, 0f, color);
            }

            {
                switch (placementKind)
                {
                    case PlacementKind.Bridge:
                        RenderBridgePlacementGizmo(); break;
                    case PlacementKind.Fan:
                        RenderFanPlacementGizmo(); break;
                    case PlacementKind.UpdownPlank:
                        RenderUpdownPlankPlacementGizmo(); break;
                    case PlacementKind.TurnPlank:
                        RenderTurnPlankPlacementGizmo(); break;
                    case PlacementKind.Slingshot:
                        RenderSlingshotPlacementGizmo(); break;

                    case PlacementKind.None:
                    default: break;
                }
            }
        }

        if (hasCrossed)
        {
            Vector2 pos = new(Window.Width / 2, Window.Height * .25f);
            string text = string.Format("{0:0.00}", Calc.Clamp(crossedTimer, 0.0f, SafeTime));
            float t = Calc.Clamp(crossedTimer, 0.0f, SafeTime) / SafeTime;
            float scale = Ease.Expo.Out(t);
            Color color = Color.Lerp(Color.White, Color.Red, t);
            Font.Draw(Batch, text, pos, new(.5f, .5f), 100 * scale * 1.2f, color * 0.25f);
            Font.Draw(Batch, text, pos, new(.5f, .5f), 100 * scale * 1.1f, color * 0.5f);
            Font.Draw(Batch, text, pos, new(.5f, .5f), 100 * scale, color);
        }

        //Score
        {

            Vector2 pos = new(Window.Width * 0.9f, Window.Height * .1f);
            string text = Score.ToString();
            Font.Draw(Batch, text, pos, new(.5f, .5f), 100 * (1 + Ease.Cube.In(scoreLerp)), Color.White);
        }

        Batch.Render(Window);
        Batch.Clear();

        imRenderer.Render();
    }
}