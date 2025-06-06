using Cysharp.Threading.Tasks;
using Hikari;
using Hikari.UI;
using System;
using System.Diagnostics;

namespace CannonCape;

public sealed class Scenario
{
    private readonly UIElement _uiOverlay;
    private readonly UIElement _gameUIRoot;

    private enum ScenarioState
    {
        Home,
        Play,
        Quit,
    }

    public Scenario()
    {
        CreateSky();
        App.Screen.UITree.SetRoot(new Panel
        {
            Background = Brush.Transparent,
            Children =
            [
                _gameUIRoot = new Panel
                {
                    Background = Brush.Transparent,
                },
                _uiOverlay = new Panel
                {
                    Background = Brush.Transparent,
                },
            ],
        });
    }

    public async UniTask Start()
    {
        var state = ScenarioState.Home;
        while(true) {
            switch(state) {
                default:
                case ScenarioState.Home: {
                    state = await HomeScene();
                    continue;
                }
                case ScenarioState.Play:
                    await MainPlayScene.Start(this);
                    break;
                case ScenarioState.Quit: {
                    return;
                }
            }
        }
    }

    public async UniTask FadeIn(float seconds = 0.6f)
    {
        var span = TimeSpan.FromSeconds(seconds);
        var start = App.CurrentTime;
        while(true) {
            var elapsed = App.CurrentTime - start;
            if(elapsed >= span) {
                _uiOverlay.Background = Brush.Transparent;
                return;
            }
            var alpha = (float)elapsed.Ticks / span.Ticks;
            _uiOverlay.Background = Brush.Solid(0, 0, 0, 1f - alpha);
            await App.Screen.Update.Switch();
        }
    }

    public async UniTask FadeOut(float seconds = 0.6f)
    {
        var span = TimeSpan.FromSeconds(seconds);
        var start = App.CurrentTime;
        while(true) {
            var elapsed = App.CurrentTime - start;
            if(elapsed >= span) {
                _uiOverlay.Background = Brush.Black;
                return;
            }
            var alpha = (float)elapsed.Ticks / span.Ticks;
            _uiOverlay.Background = Brush.Solid(0, 0, 0, alpha);
            await App.Screen.Update.Switch();
        }
    }

    private async UniTask<ScenarioState> HomeScene()
    {
        var tcs = new UniTaskCompletionSource<ScenarioState>();
        var ui = HomeUI(state => tcs.TrySetResult(state));
        _gameUIRoot.Children.Add(ui);
        var nextState = await tcs.Task;
        ui.Remove();
        await FadeOut();
        return nextState;
    }

    private static void CreateSky()
    {
        var screen = App.Screen;
        var shader = SkyShader.Create(screen).DisposeOn(screen.Closed);
        var material = SkyMaterial.Create(shader).DisposeOn(screen.Closed);
        var mesh = PrimitiveShapes.SkySphere(screen, false).DisposeOn(screen.Closed);
        var sky = new FrameObject(mesh, material)
        {
            Name = "sky",
            Scale = new Vector3(1200),
        };
    }

    private static Panel HomeUI(Action<ScenarioState> onStateChanged)
    {
        Panel homeUI = null!;
        homeUI = new Panel
        {
            Name = "HomeUI",
            Background = Brush.Transparent,
            Width = LayoutLength.Proportion(0.6f),
            Height = LayoutLength.Length(360),
            Flow = new Flow(FlowDirection.Column, FlowWrapMode.NoWrap),
            Children =
            [
                HomeUIButton(b =>
                {
                    b.Name = "ゲーム開始";
                    b.Text = "ゲーム開始";
                    b.Background = Brush.Red;
                    b.Clicked.Subscribe(_ =>
                    {
                        onStateChanged.Invoke(ScenarioState.Play);
                        return;
                    });
                }),
                HomeUIButton(b =>
                {
                    b.Text = "遊び方";
                    b.Background = Brush.Green;
                    b.Clicked.Subscribe(_ =>
                    {
                    });
                }),
                HomeUIButton(b =>
                {
                    b.Text = "ゲーム終了";
                    b.Background = Brush.Blue;
                    b.Clicked.Subscribe(_ =>
                    {
                        onStateChanged.Invoke(ScenarioState.Quit);
                        App.Screen.RequestClose();
                    });
                }),
            ],
        };
        return homeUI;
    }

    private static Button HomeUIButton(Action<Button>? modify = null)
    {
        var button = new Button
        {
            Typeface = Typeface.FromFile(Resources.Path("mplus-1p-regular.otf")),
            FontSize = 30,
            Width = LayoutLength.Length(600),
            Height = LayoutLength.Length(100),
            Margin = new Thickness(10),
            BorderRadius = new CornerRadius(20f),
            BorderWidth = new Thickness(2),
            BorderColor = Brush.White,
            Background = Brush.Red,
            HoverProps = new()
            {
            },
        };
        modify?.Invoke(button);
        return button;
    }
}



public sealed class MainPlayScene
{
    private readonly Cannon? _cannon;
    private readonly Ground? _ground;
    private readonly CameraController _cameraController;

    private MainPlayScene(Cannon cannon, Ground ground)
    {
        _cannon = cannon;
        _ground = ground;
        _cameraController = new CameraController(cannon);
    }


    public static async UniTask Start(Scenario scenario)
    {
        MainPlayScene scene;
        try {
            scene = await LoadScene();
            scene._cameraController.Start();
        }
        finally {
            await scenario.FadeIn();
        }
        // TODO:
        await UniTask.Never(default);
    }

    private static async UniTask<MainPlayScene> LoadScene()
    {
        var (cannon, ground) = await UniTask.WhenAll(Cannon.Load(), Ground.Load());
        return new MainPlayScene(cannon, ground);
    }
}
