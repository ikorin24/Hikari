using Cysharp.Threading.Tasks;
using Hikari;
using Hikari.UI;
using System;

namespace CannonCape;

public static class HomeScene
{
    public static async UniTask<ScenarioState> Run()
    {
        App.Camera.LookAt(new Vector3(0, 0, -100), new Vector3(0, 10, 0));

        var tcs = new UniTaskCompletionSource<ScenarioState>();

        UIElement uiOverlay;
        App.Screen.UITree.SetRoot(new Panel
        {
            Name = "root",
            Background = Brush.Transparent,
            Children =
            [
                new Panel
                {
                    Name = "gameUIRoot",
                    Background = Brush.Transparent,
                    Children =
                    [
                        HomeUI(state => tcs.TrySetResult(state)),
                    ],
                },
                uiOverlay = new Panel
                {
                    Name = "uiOverlay",
                    Background = Brush.Transparent,
                },
            ],
        });
        await FadeHelper.FadeIn(uiOverlay);
        using var player = AudioPlayer.Play(Resources.Path("タイトル画面BGM.wav"));
        var nextState = await tcs.Task;
        await FadeHelper.FadeOut(uiOverlay);
        return nextState;
    }

    private static Panel HomeUI(Action<ScenarioState> onStateChanged)
    {
        Panel homeUI = null!;
        homeUI = new Panel
        {
            Name = "homeUI",
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
                    Typeface = Typeface.FromFile(Resources.Path("07にくまるフォント.otf")),
                    VerticalAlignment = VerticalAlignment.Center,
                    Width = LayoutLength.Length(900),
                    Height = LayoutLength.Length(150),
                    BorderRadius = new CornerRadius(3),
                    Margin = new Thickness(0, 0, 200, 0),
                },
                new Panel
                {
                    Name = "Buttons Panel",
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
                                onStateChanged.Invoke(ScenarioState.MainPlay);
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
