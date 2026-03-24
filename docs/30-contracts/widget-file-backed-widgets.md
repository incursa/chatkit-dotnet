# Widget File-Backed Rendering Contract

This contract defines how exported `.widget` files are verified in the repository.

## Verified behavior

- The two acceptance fixtures load successfully from the test fixture directory.
- `WidgetDefinition.DecodeEncodedWidget()` exposes the embedded default state.
- `WidgetDefinition.Build(...)` validates that state against the exported JSON schema.
- Valid state renders through the Jinja template and deserializes into a `WidgetRoot`.
- Invalid state fails before a widget root is produced.
- Exported templates may be normalized for the current `Incursa.Jinja` runtime where the export uses the leading-comma slice idiom and `length` filter.

## Fixtures

- `tests/Incursa.OpenAI.ChatKit.Tests/Fixtures/Email Summary.widget`
- `tests/Incursa.OpenAI.ChatKit.Tests/Fixtures/EmailListId.widget`

## Trace link

- `LIB-CHATKIT-CORE-004`
