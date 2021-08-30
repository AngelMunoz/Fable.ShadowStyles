# ShadowStyles

[constructable stylesheets]: https://developers.google.com/web/updates/2019/02/constructable-stylesheets

[Fable.Haunted] https://github.com/AngelMunoz/Fable.Haunted
[lit-html] https://github.com/alfonsogarciacaro/Fable.Lit
[Sutil] https://github.com/davedawkins/Sutil
[css in js]: https://medium.com/dailyjs/what-is-actually-css-in-js-f2f529a2757
[Feliz.Engine]: https://github.com/alfonsogarciacaro/Feliz.Engine

> Powered by [Feliz.Engine]

Shadow Styles is a [constructable stylesheets] F# wrapper that enables you to add styles to your Fable apps that generate Custom Elements or even Web components like [lit-html] + [Fable.Haunted] and in the future [Sutil] + [Fable.Haunted].

# CSS in F#

constructable stylesheets (along with css module imports spec) try to solve the [css in js] dilema, while shadow DOM solves encapsulation, it makes it hard to share styles hence why we'd like to use something like constructable stylesheets

# Usage Example

Here we'll use [Fable.Haunted] and [lit-html] to define a web component (i.e a custom element with a shadow root)
we'll add a stylesheet to the current document and a local stylesheet to the `flit-app` web component

> **_NOTE_**: For Safari and Firefox a polyfill is required
>
> ```html
> <script src="https://unpkg.com/construct-style-sheets-polyfill"></script>
> ```

```fsharp
module Main

open Browser.Dom
open Lit
open Haunted
open Fable.Core.JsInterop

open ShadowStyles
open ShadowStyles.Types
open ShadowStyles.Operators

// note we use SCss to prevent colisions with things like Fable.Lit Css

let globalStyles =
  [ "html, body"
    => [ SCss.padding 0
         SCss.margin 0
         SCss.fontFamily
           "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, 'Open Sans', 'Helvetica Neue', sans-serif" ]
    "body" => [ SCss.color "blue" ] ]

/// the `=>` operator is a proxy for: `CSSRule(selector, cssPropertySequence)`

// these get applied to the whole document (false means we're not using shadowDOM)
ShadowStyles.adoptStyleSheet (document, globalStyles, false)

let myComponent () =
  // with Haunted, the functions are binded to the HTML element they are assigned
  let host = jsThis
  // which helps us get a reference to the element's shadow root

  let localStyles =
    // while classes are not required because inside
    // shadow DOM element styles are isolated
    // we'll use it for ilustration purposes
    [ ".my-rule"
      => [ SCss.displayFlex
           SCss.alignContentCenter
           SCss.color "red" ] ]

  // the following function call applies multiple (and local) styles to the host even in shadowDOM
  // let existingGlobalStyles = document?adoptedStyleSheets
  // ShadowStyles.adoptStyleSheets (host, existingGlobalStyles, localStyles)

  // this applies a single style to the host in shadowDOM
  ShadowStyles.adoptStyleSheet (host, localStyles)

  html
    $"""
    <div class="my-rule">
      <h1>Hello, World!</h1>
    </div>
  """

defineComponent
  "flit-app"
  (Haunted.Component(myComponent, {| useShadowDOM = true |}))
```

```html
<!DOCTYPE html>
<html>
  <head>
    <title>Lit + Fable Template</title>
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <link rel="shortcut icon" href="fable.ico" />
    <!-- The polyfill is required for Safari and Firefox untill they enable the feature natively
        blink browsers already support this natively (chrome, edge, opera, etc...) -->
    <script src="https://unpkg.com/construct-style-sheets-polyfill"></script>
  </head>
  <body>
    <div>outside flit-app!</div>
    <flit-app></flit-app>
    <script type="module" src="/dist/Main.fs.js"></script>
  </body>
</html>
```
