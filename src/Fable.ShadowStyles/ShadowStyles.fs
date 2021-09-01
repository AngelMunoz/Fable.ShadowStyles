module ShadowStyles

open Feliz
open Browser.Css
open Browser.Types
open Fable.Core
open Fable.Core.JsInterop

module Types =
    [<AutoOpen>]
    module Extensions =

        [<Emit("Array.from(document.styleSheets)")>]
        let private stylesheets: CSSStyleSheet array = jsNative

        [<Emit("Array.from($0.cssRules)")>]
        let private getRules (sheet: CSSStyleSheet) : Browser.Types.CSSRule array = jsNative

        type CSSStyleSheet with
            /// <summary>
            /// replaces the content of the stylesheet with the content passed into it.
            /// The method returns a promise that resolves with a CSSStyleSheet object.
            /// </summary>
            /// <remarks>
            /// The replaceSync() and CSSStyleSheet.replace() methods can only be used on a stylesheet created with the CSSStyleSheet() constructor.
            /// </remarks>
            [<Emit("$0.replace($1)")>]
            member __.replace(value: string) : JS.Promise<CSSStyleSheet> = jsNative

            /// <summary>
            /// synchronously replaces the content of the stylesheet with the content passed into it.
            /// </summary>
            /// <remarks>
            /// The replaceSync() and CSSStyleSheet.replace() methods can only be used on a stylesheet created with the CSSStyleSheet() constructor.
            /// </remarks>
            [<Emit("$0.replaceSync($1)")>]
            member __.replaceSync(value: string) : unit = jsNative

            /// <summary>
            /// creates a Constructable CSSStyleSheet object with css rules passed as a string.
            /// </summary>
            static member FromString(css: string) : CSSStyleSheet =
                let sheet = CSSStyleSheet.Create()
                sheet.replaceSync (css)
                sheet

            /// <summary>
            /// creates a construtable CSSStyleSheet array from the stylesheets in the document.
            /// </summary>
            /// <remarks>
            /// This method should be called once per application.
            /// </remarks>
            static member FromDocument() =
                lazy
                    (seq {
                        for sheet in stylesheets do
                            let textRules =
                                getRules sheet
                                |> Array.fold (fun curr (next: Browser.Types.CSSRule) -> $"{curr}\n{next.cssText}") ""

                            let sheet = CSSStyleSheet.Create()
                            sheet.replaceSync (textRules)
                            sheet
                     })

        type Element with
            /// returns this DocumentOrShadow adopted stylesheets or sets them.
            /// https://wicg.github.io/construct-stylesheets/#using-constructed-stylesheets
            [<Emit("$0.adoptedStyleSheets{{=$1}}")>]
            member __.adoptedStyleSheets
                with get (): CSSStyleSheet array = jsNative
                and set (v: CSSStyleSheet array): unit = jsNative

        type Document with
            /// returns this DocumentOrShadow adopted stylesheets or sets them.
            /// https://wicg.github.io/construct-stylesheets/#using-constructed-stylesheets
            [<Emit("$0.adoptedStyleSheets{{=$1}}")>]
            member __.adoptedStyleSheets
                with get (): CSSStyleSheet array = jsNative
                and set (v: CSSStyleSheet array): unit = jsNative



    type CssProperty =
        private
        | CssProperty of property: string * value: string

        member this.Value =
            match this with
            | CssProperty (key, value) -> (key, value)

        member this.AsString: string =
            let prop, value = this.Value
            $"{prop}: {value};"

        static member Create (key: string) (value: string) = CssProperty(key, value)

    type CSSRule =
        | CssRule of rule: string * CssProperty seq

        member this.Value =
            match this with
            | CssRule (rule, props) -> (rule, props)

        member this.AsString: string =
            let rule, props = this.Value

            let propsStr =
                Seq.fold (fun next (curr: CssProperty) -> $"{curr.AsString}{next}") "" props

            $"{rule} {{ {propsStr} }}"

open Types

/// <summary>
/// Scss is the Engine from [Feliz.Engine](https://github.com/alfonsogarciacaro/Feliz.Engine) with a custom type for CSS properties.
/// </summary>
let SCss = CssEngine CssProperty.Create

module Operators =
    /// <summary>
    /// Adds a CSS property to a CSS rule.
    /// </summary>
    let (=>) (ruleName: string) (props: CssProperty seq) = CssRule(ruleName, props)

/// <summary>
/// a static class that helps us to expose two convenience methods to adopt a CSS stylesheet or stylesheets.
/// </summary>
type ShadowStyles() =
    /// <summary>
    /// Adds a single stylesheet to the shadow root of the given node.
    /// </summary>
    /// <remarks>
    ///  If `inShadow` is false the stylesheet will be added to the node itself.
    /// </remarks>
    /// <param name="host">the element to which the stylesheet will be added</param>
    /// <param name="rules">the stylesheet to add</param>
    /// <param name="inShadow">
    /// If true, the stylesheet will be added to the shadow root of the given element,
    /// otherwise it will be added to the provided node
    /// </param>
    static member adoptStyleSheet(host: Node, rules: CSSRule seq, ?inShadow: bool) : unit =
        let inShadow = defaultArg inShadow true

        let sheetStr =
            Seq.fold (fun next (curr: CSSRule) -> $"{next}\n{curr.AsString}") "" rules

        let sheet = CSSStyleSheet.Create()
        sheet.replaceSync (sheetStr)

        if inShadow then
            host?shadowRoot?adoptedStyleSheets <- [| sheet |]
        else
            host?adoptedStyleSheets <- [| sheet |]
    /// <summary>
    /// Adds multiple CSSStyleSheet's to the shadow root of the given node.
    /// Optionally you can pass an extra set of CSSRule's to complement the stylesheets.
    /// </summary>
    /// <remarks>
    ///  If `inShadow` is false the stylesheet will be added to the node itself.
    /// </remarks>
    /// <param name="host">the element to which the stylesheets will be added</param>
    /// <param name="sheets">Existing instances of CSSStyleSheet that will be added</param>
    /// <param name="extraRules">An extra set of CSSRule's to complement the stylesheets. Usually these extra rules are local styles</param>
    /// <param name="inShadow">
    /// If true, the stylesheet will be added to the shadow root of the given element,
    /// otherwise it will be added to the provided node
    /// </param>
    static member adoptStyleSheets
        (
            host: HTMLElement,
            sheets: CSSStyleSheet seq,
            ?extraRules: CSSRule seq,
            ?inShadow: bool
        ) : unit =
        let getRulesAsString (rules: CSSRule seq) : CSSStyleSheet seq =
            seq {
                for rule in rules do
                    let ruleStr = rule.AsString
                    let sheet = CSSStyleSheet.Create()
                    sheet.replaceSync (ruleStr)
                    sheet
            }

        let inShadow = defaultArg inShadow true

        let extraRules =
            defaultArg extraRules Seq.empty
            |> getRulesAsString

        if inShadow then
            host?shadowRoot?adoptedStyleSheets <- [| yield! sheets; yield! extraRules |]
        else
            host?adoptedStyleSheets <- [| yield! sheets; yield! extraRules |]
