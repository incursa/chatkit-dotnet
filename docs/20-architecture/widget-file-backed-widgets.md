# File-Backed Widget Rendering

This note captures the architecture for exported `.widget` files in [`Incursa.OpenAI.ChatKit`](../../src/Incursa.OpenAI.ChatKit/README.md).

## Scope

- Load a widget export from disk or a stream.
- Preserve the authoring metadata: template source, JSON schema, preview widget, and encoded widget payload.
- Validate supplied state against the export schema before rendering.
- Render the Jinja template with the validated state.
- Deserialize the rendered JSON into [`WidgetRoot`](../../src/Incursa.OpenAI.ChatKit/Widgets.cs).
- Normalize the exported leading-comma slice idiom and supply a `length` filter so the current `Incursa.Jinja` runtime can render the shipped export fixtures without changing their source files.

## Public surface

- [`WidgetDefinition.Load(...)`](../../src/Incursa.OpenAI.ChatKit/WidgetDefinitions.cs)
- [`WidgetDefinition.LoadAsync(...)`](../../src/Incursa.OpenAI.ChatKit/WidgetDefinitions.cs)
- [`WidgetDefinition.DecodeEncodedWidget()`](../../src/Incursa.OpenAI.ChatKit/WidgetDefinitions.cs)
- [`WidgetDefinition.Build(object? state = null)`](../../src/Incursa.OpenAI.ChatKit/WidgetDefinitions.cs)

## Boundary

This is an Incursa extension to the ChatKit .NET runtime. It does not add a generated widget-class system, a new template language, or any broader upstream parity claim beyond the behavior the repo now exercises.

## Acceptance link

- [`LIB-CHATKIT-CORE-004`](../../specs/libraries/library-conformance-matrix.md)
