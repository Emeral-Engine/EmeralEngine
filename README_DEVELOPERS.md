# コードの改変について
## プロジェクトを出力
プログラムが書ける人は、もしかすると自分で書きたい実装があるかもしれません。
そんなときは「出力」で「プロジェクト」を選んでください。

.NETプロジェクトを出力することができ、自由に生成物を改変することができます。


## VisualStudioを使う際の注意点
ただし、デバッグをする際に注意点があります。

VisualStudioでデバッグ実行すると、そのままでは失敗します。これはリソース情報を含む`datas`フォルダ、`runtime`フォルダを上手く参照できていないためです。

そのため、`datas`フォルダ、`runtime`フォルダを、csprojと同じ階層にある`bin/Debug/net9.0-windows`の中にコピーしてください。

## ビルドについて
生成物の.exeと同じ階層に`datas`、`runtime`フォルダが置かれるようにしてください。

## 各種変数
コードを見てください。
汚いですが、弄りたい値は大体コントロールだと思うので、xamlのNameを見てください。

# EmeralEngineのビルドについて
## 依存関係
- .NET SDK 10
- Go
- [hato](https://github.com/midry3/hato) (自作のタスクランナーです)

## コマンド
```sh
$ git clone https://github.com/Emeral-Engine/EmeralEngine
$ cd EmeralEngine
$ hato build
```
