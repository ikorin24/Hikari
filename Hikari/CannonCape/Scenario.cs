using Cysharp.Threading.Tasks;
using Hikari;
using Hikari.Mathematics;
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
        App.Camera.LookAt(new Vector3(0, 0, -100), new Vector3(0, 10, 0));
        CreateSea();
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
        using var player = AudioPlayer.Play(Resources.Path("タイトル画面BGM.wav"));
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

    private static void CreateSea()
    {
        var screen = App.Screen;
        var albedo = Texture2D.Create1x1Rgba8UnormSrgb(screen, TextureUsages.TextureBinding, new ColorByte(45, 55, 110, 255)).DisposeOn(screen.Closed);
        var metallicRoughness = Texture2D.Create1x1Rgba8Unorm(screen, TextureUsages.TextureBinding, new ColorByte(0, 127, 0, 0)).DisposeOn(screen.Closed);
        var normal = Texture2D.Create1x1Rgba8Unorm(screen, TextureUsages.TextureBinding, new ColorByte(127, 127, 255, 255)).DisposeOn(screen.Closed);
        var material = PbrMaterial.Create(App.PbrShader, albedo, metallicRoughness, normal).DisposeOn(screen.Closed);
        var mesh = PrimitiveShapes.Plane(screen, true).DisposeOn(screen.Closed);
        var sea = new FrameObject(mesh, material)
        {
            Name = "sea",
            Rotation = Quaternion.FromAxisAngle(Vector3.UnitX, -90.ToRadian()),
            Scale = new Vector3(1200),
        };
    }

    private static Panel HomeUI(Action<ScenarioState> onStateChanged)
    {
        Panel homeUI = null!;
        homeUI = new Panel
        {
            Background = Brush.Transparent,
            Children =
            [
                new Label
                {
                    Name = "title",
                    Text = "Cannon Cape",
                    Color = Color4.White,
                    Background = Brush.LinearGradient(0,
                    [
                        new(Color4.FromHexCode("#8006"), 0.04f),
                        new(Color4.FromHexCode("#0000"), 0.4f),
                    ]),
                    FontSize = 120,
                    Typeface = Typeface.FromFile(@"D:\private\data\taiho\font\NikumaruFont\07にくまるフォント.otf"),
                    VerticalAlignment = VerticalAlignment.Center,
                    Width = LayoutLength.Length(900),
                    Height = LayoutLength.Length(150),
                    BorderRadius = new CornerRadius(3),
                    Margin = new Thickness(0, 0, 200, 0),
                },
                new Panel
                {
                    Name = "HomeUI",
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(300, 0, 0, 0),
                    Height = LayoutLength.Length(160),
                    Background = Brush.Transparent,
                    Flow = new Flow(FlowDirection.Column, FlowWrapMode.NoWrap),
                    Children =
                    [
                        HomeUIButton(b =>
                        {
                            b.Name = "ゲーム開始";
                            b.Text = "ゲーム開始";
                            b.Background = Brush.LinearGradient(0,
                            [
                                new(Color4.FromHexCode("#FF606E"), 0.72f),
                                new(Color4.FromHexCode("#FF9BA7"), 0.9f),
                            ]);
                            b.HoverProps = new()
                            {
                                Background = Brush.LinearGradient(0,
                                [
                                    new(Color4.FromHexCode("#FF606E"), 0.72f),
                                    new(Color4.FromHexCode("#F45368"), 0.9f),
                                ]),
                                BorderColor = Brush.Solid(Color4.FromHexCode("#FBB")),
                                Color = Color4.FromHexCode("#FFF3F3"),
                            };
                            b.ActiveProps = new()
                            {
                                Background = Brush.Solid(Color4.FromHexCode("#C16A86")),
                                Color = Color4.FromHexCode("#F3EEEE"),
                            };
                            b.Clicked.Subscribe(_ =>
                            {
                                AudioPlayer.Play(Resources.Path("ゲーム開始.wav"));
                                onStateChanged.Invoke(ScenarioState.Play);
                                return;
                            });
                        }),
                        HomeUIButton(b =>
                        {
                            b.Name = "終了";
                            b.Text = "終了";
                            b.Background = Brush.LinearGradient(0,
                            [
                                new(Color4.FromHexCode("#363BD1"), 0.72f),
                                new(Color4.FromHexCode("#6164CE"), 0.9f),
                            ]);
                            b.HoverProps = new()
                            {
                                Background = Brush.LinearGradient(0,
                                [
                                    new(Color4.FromHexCode("#363BD1"), 0.72f),
                                    new(Color4.FromHexCode("#4A4D9B"), 0.9f),
                                ]),
                                BorderColor = Brush.Solid(Color4.FromHexCode("#BBF")),
                                Color = Color4.FromHexCode("#F3F3FF"),
                            };
                            b.ActiveProps = new()
                            {
                                Background = Brush.Solid(Color4.FromHexCode("#22269E")),
                            };
                            b.Clicked.Subscribe(_ =>
                            {
                                onStateChanged.Invoke(ScenarioState.Quit);
                                App.Screen.RequestClose();
                            });
                        }),
                    ],
                }
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
            Color = Color4.White,
            Width = LayoutLength.Length(400),
            Height = LayoutLength.Length(60),
            Margin = new Thickness(10),
            BorderRadius = new CornerRadius(30f),
            BorderWidth = new Thickness(3),
            BorderColor = Brush.White,
        };
        modify?.Invoke(button);
        return button;
    }
}
