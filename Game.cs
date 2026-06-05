using Foster.Framework;
using FosterImGui;
using ImGuiNET;
using System.Numerics;

namespace Bridgaem;

public class Game : App
{
    private readonly Batcher batch;
    private readonly Texture image;
    private readonly Renderer imRenderer;

    public Game() : base(new AppConfig()
    {
        ApplicationName = "Bridgaem",
        WindowTitle = "WESH BRIDGE",
        Width = 1280,
        Height = 720,
    })
    {
        batch = new(GraphicsDevice);
        //image = new Texture(GraphicsDevice, new Image("button.png"));
        imRenderer = new(this);
    }

    protected override void Startup()
    {

    }

    protected override void Shutdown()
    {
        imRenderer.Dispose();
    }

    protected override void Update()
    {
        if (Input.Keyboard.Down(Keys.Escape))
            Exit();

        imRenderer.BeginLayout();

        // toggle text input if ImGui wants it
        if (imRenderer.WantsTextInput)
            Window.StartTextInput();
        else
            Window.StopTextInput();

        ImGui.SetNextWindowSize(new Vector2(400, 300), ImGuiCond.Appearing);
        if (ImGui.Begin("Hello Foster x Dear ImGui"))
        {
            // show an Image button
            /*var imageId = imRenderer.GetTextureID(image);
            if (ImGui.ImageButton("Image", imageId, new Vector2(32, 32)))
                ImGui.OpenPopup("Image Button");*/

            // image buttton popup
            if (ImGui.BeginPopup("Image Button"))
            {
                ImGui.Text("You pressed the Image Button!");
                ImGui.EndPopup();
            }

            // custom sprite batcher inside imgui window
            ImGui.Text("Some Foster Sprite Batching:");
            var size = new Vector2(ImGui.GetContentRegionAvail().X, 200);
            if (imRenderer.BeginBatch(size, out var batch, out var bounds))
            {
                batch.CheckeredPattern(bounds, 16, 16, Color.DarkGray, Color.Gray);
                batch.Circle(bounds.Center, 32, 16, Color.Red);
            }
            imRenderer.EndBatch();

            ImGui.Text("That weas pretty cool!");
        }
        ImGui.End();

        ImGui.ShowDemoWindow();

        imRenderer.EndLayout();
    }

    protected override void Render()
    {
        Window.Clear(Color.Black);

        batch.Render(Window);
        batch.Clear();

        imRenderer.Render();
    }
}