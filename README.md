# Hikari

## 概要

C# (.NET9) + Rust + WebGPU で作った自作ゲームエンジンです。Pull Request や issue は受け付けていません。

Windows (x64) と Mac (arm64) で動作します。Rust の実装部分は WebGPU の API を呼び出すグルーコードのみで、ゲームエンジンの実装のほとんどは C# 側にあります。

Forward Rendering, Deferred Rendering, Cascaded Shadow, PBR, UI フレームワークなどを実装。

async/await による非同期処理やマルチスレッド環境での使用などモダンな C# に合った記述が可能で、ガベージによる GC への負荷を抑えたハイパフォーマンスでありながらも、C# らしい使い方が可能なゲームエンジンを目指しています。NativeAOT に対応しています。

![scene-image](./img/image.gif)

## ビルド

.NET9 と Rust のビルドツールが必要です。

### Windows (x64)

```
> cargo build --manifest-path .\corehikari\Cargo.toml --release --target x86_64-pc-windows-msvc
> dotnet build .\HikariEngine\HikariEngine.sln -c Release
```

### Mac (arm64)

Mac では、下記のコマンドの後、ビルドされた Rust のネイティブライブラリ (`corehikari/target/libcorehikari.dylib`) を、実行したい C# 側の実行バイナリ (例えば `SampleApp` など) の出力ディレクトリに手動でコピーしてください。

```
$ cargo build --manifest-path ./corehikari/Cargo.toml --release --target aarch64-apple-darwin
$ dotnet build ./HikariEngine/HikariEngine.sln -c Release
```

## サンプル

### CannonCape

このゲームエンジンを使用して作成したゲームです。(`HikariEngine/CannonCape.csproj`)

音声再生に使用しているライブラリが Windows のみ対応しているため、Mac では音が出ません。
ゲーム内の 3D モデルは自作、音声ファイルは権利フリーの物と[魔王魂](https://maou.audio/)の物を使用しています。

![cannon-cape](./img/cannon_cape.gif)

### SampleApp

簡単な3Dモデルが配置されたシーンです。(`HikariEngine/SampleApp.csproj`)
