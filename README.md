# Hikari

これは作者がゲームエンジンの勉強のために作った、実験的な自作ゲームエンジンです。 実用性を考慮していません。Pull Request や issue は受け付けていません。

C# (.NET9) + Rust + WebGPU です。.NET9 がインストールされていれば、ビルドや実行に特にセットアップは必要ありません。

Rust で WebGPU を使用し、それを C# 側から呼び出しています。Rust のネイティブコードは WebGPU の API を呼び出すグルーコードのみで、ゲームエンジンの実装は C# 側にあります。

SampleApp.csproj を実行すればサンプルが動きます。Windows のみ。C#, Rust 側ともクロスプラットフォームに動作するように作成していますが、Windows 以外では試していません。(Windows 用以外のネイティブライブラリのビルドと配置はしていません)

```
> cd Hikari/SampleApp
> dotnet run -c Release
```

Forward Rendering, Deferred Rendering, Cascaded Shadow, PBR など

![scene-image](./img/image.gif)
