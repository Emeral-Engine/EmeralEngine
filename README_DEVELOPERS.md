# コードの改変について
## プロジェクトを出力
プログラムが書ける人は、もしかすると自分で書きたい実装があるかもしれません。
そんなときは「出力」で「プロジェクト」を選んでください。

.NETプロジェクトを出力することができ、自由に生成物を改変することができます。


## VisualStudioを使う際の注意点
ただし、デバッグをする際に注意点があります。

VisualStudioでデバッグ実行すると、そのままでは失敗します。これはリソース情報を含む「datas」フォルダを上手く参照できていないためです。

そのため、datasフォルダを、csprojと同じ階層にある「bin/Debug/net9.0-windows」の中にコピーしてください。

## ビルドについて
生成物の.exeと同じ階層にdatasフォルダが置かれるようにしてください。

## 各種変数
コードを見てください。
汚いですが、弄りたい値は大体コントロールだと思うので、xamlのNameを見てください。

# EmeralEngineのビルド方法
```bash
$ git clone https://github.com/Emeral-Engine/EmeralEngine
$ cd EmeralEngine
$ dotnet build
```
