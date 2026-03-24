# File-Backed Widget Rendering

This note captures the architecture for exported `.widget` files in `Incursa.OpenAI.ChatKit`.

## Scope

- Load a widget export from disk or a stream.
- Preserve the authoring metadata: template source, JSON schema, preview widget, and encoded widget payload.
- Validate supplied state against the export schema before rendering.
- Render the Jinja template with the validated state.
- Deserialize the rendered JSON into `WidgetRoot`.
- Normalize the exported leading-comma slice idiom and supply a `length` filter so the current `Incursa.Jinja` runtime can render the shipped export fixtures without changing their source files.

## Public surface

- `WidgetDefinition.Load(...)`
- `WidgetDefinition.LoadAsync(...)`
- `WidgetDefinition.DecodeEncodedWidget()`
- `WidgetDefinition.Build(object? state = null)`

## Boundary

This is an Incursa extension to the ChatKit .NET runtime. It does not add a generated widget-class system, a new template language, or any broader upstream parity claim beyond the behavior the repo now exercises.

## Acceptance link

- `LIB-CHATKIT-CORE-004`
