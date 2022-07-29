# Marionetta

![Marionetta](Images/Marionetta.100.png)

Marionetta - サンドボックス化されたアウトプロセスによって、レガシーライブラリを分割し、操作を容易にするライブラリ。

[![Project Status: WIP – Initial development is in progress, but there has not yet been a stable, usable release suitable for the public.](https://www.repostatus.org/badges/latest/wip.svg)](https://www.repostatus.org/#wip)

## NuGet

| Package  | NuGet                                                                                                                |
|:---------|:---------------------------------------------------------------------------------------------------------------------|
| Marionetta | [![NuGet Marionetta](https://img.shields.io/nuget/v/Marionetta.svg?style=flat)](https://www.nuget.org/packages/Marionetta) |

## CI

| main                                                                                                                                                                 | develop                                                                                                                                                                       |
|:---------------------------------------------------------------------------------------------------------------------------------------------------------------------|:------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| [![Marionetta CI build (main)](https://github.com/kekyo/Marionetta/workflows/.NET/badge.svg?branch=main)](https://github.com/kekyo/Marionetta/actions?query=branch%3Amain) | [![Marionetta CI build (develop)](https://github.com/kekyo/Marionetta/workflows/.NET/badge.svg?branch=develop)](https://github.com/kekyo/Marionetta/actions?query=branch%3Adevelop) |

----

[English language is here](https://github.com/kekyo/Marionetta)

## これは何？

Marionettaは、非常に排他的な（そして恐らくプロプライエタリである）.NETレガシーライブラリを、
プロセス分離されたサンドボックス上で簡単に実行できるようにするためのソリューションです。

例えば、`Any CPU`ではなく`x86`に固定され、`net35`の動作環境に限定しているレガシーライブラリを、
分離されたプロセスの元でロードし、リモートで呼び出すことができます。
概念的には.NET Remotingに似ていますが、ご存知の通り、.NET 6/5と.NET Coreでは廃止されています。

Marionettaはまだ透過的なプロキシをサポートしていません。
しかし、.NET Coreランタイム上での実行とリモートメソッド呼び出しは可能で、
十分に理解しやすいAPIで実行することができます。

Marionettaは、バックエンドのRPC転送の基礎として、[DupeNukem](https://github.com/kekyo/DupeNukem)のコアエンジンを使用しています。

### 動作環境

パッケージの対応プラットフォームは以下の通りです。
それぞれのバージョン毎に、個別のアセンブリが用意されています。
これは、動作環境に敏感なレガシーライブラリを考慮しての事です。

* .NET 6, 5
* .NET Core 3.1, 3.0, 2.2～2.0
* .NET Standard 2.1, 2.0, 1.6～1.3
* .NET Framework 4.8～4.0, 3.5

----

## 使用方法

Marionettaは、以下の両方のプロジェクトに、[NuGetパッケージ Marionetta](https://www.nuget.org/packages/Marionetta)をインストールして使用します:

|役割|使用するクラス|概要|
|:----|:----|:----|
|マスター|`Marionettist`|制御を行うアプリケーション側です。|
|スレーブ|`Puppet`|制御される側、つまりレガシーライブラリを含む側です。独立したプログラムで、子プロセスとして起動します。|

同一のプロセス内で、意図的にMarionettaを使う事も出来ます。その場合は、`MasterPuppet`と`Puppet`クラスを組で使用します。

### スレーブの構成方法

レガシーライブラリには、様々な動作条件が考えられます。例えば、.NETでは最も一般的な`AnyCPU`ではなく、
`x86`や`x64`などのプラットフォーム指定が必要であったり、WPFやWindows Formsで想定されるような、
`STAThread`かつ`Application`クラスによる、ウインドウメッセージポンプがメインスレッドの駆動に必要であるなどです。

従って、（コード量は少ないですが）カスタム実行コードの記述が必要になります。
以下は、WPFを利用するレガシーライブラリをホストするスレーブの構成方法の例です:

```csharp
using Marionetta;
using DupeNukem;

[STAThread]
public static void Main(string[] args)
{
    // Applicationクラスの初期化
    var app = new Application();

    // STAThreadを強制するため、明示的にSynchContextを割り当てる
    // (Puppetの生成時にキャプチャされる)
    var sc = new DispatcherSynchronizationContext();
    SynchronizationContext.SetSynchronizationContext(sc);

    // Puppetを生成する
    var arguments = DriverFactory.ParsePuppetArguments(args);
    using var puppet = DriverFactory.CreatePuppet(arguments);

    // リモート呼び出し可能なインスタンスを登録する
    // (DupeNukemと同様)
    var legacy = new LegacyController();
    puppet.RegisterObject("legacy", legacy);

    // マスターからシャットダウンの通知が発生
    puppet.ShutdownRequested += (s, e) =>
        // WPFアプリケーションの終了
        app.Shutdown();

    // Puppetを実行する（バックグラウンドで実行される）
    puppet.Start();
 
    // メッセージポンプの実行
    app.Run();
}
```

TODO:

----

## Marionettaの背景

大きなの動機の一つは、ASP.NETのインフラが大きくなりすぎるという問題です。
ASP.NETをRPC呼び出しのエンドポイント（関連するASP.NET WebAPI）とした場合、
依存するライブラリが膨大になり、かつ最新の環境を必要とするため、
レガシーライブラリとの共存が不可能になり得ます。

サンドボックスを実現するためのライブラリは他にもいくつかあり、
一般に「IPC」「RPC」と呼ばれる技術を使用しています。
Marionettaも同様に「IPC」「RPC」の延長線上にありますが、
出来るだけ他のライブラリへの依存を行わないように設計することで、
多くのライブラリにありがちな前提条件や制約、背景知識を排除しようとしました。

DupeNukemは、シリアライザーにJSON(おなじみの[NewtonSoft.Json](https://www.newtonsoft.com/json)を使用していますが、
NewtonSoft.Jsonもライブラリとしては十分練られており、独立していて、幅広いプラットフォームに対応していることから、
Marionettaが目標とする、プラットフォームの中立性を十分に担保出来ると考えています。

----

## License

Apache-v2.

----

## History

TODO:
