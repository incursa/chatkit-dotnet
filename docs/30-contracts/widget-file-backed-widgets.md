# Widget File-Backed Rendering Contract

This contract defines how exported `.widget` files are verified in the repository.

## Verified behavior

- The two acceptance fixtures load successfully from the test fixture directory.
- [`WidgetDefinition.DecodeEncodedWidget()`](../../src/Incursa.OpenAI.ChatKit/WidgetDefinitions.cs) exposes the embedded default state.
- [`WidgetDefinition.Build(...)`](../../src/Incursa.OpenAI.ChatKit/WidgetDefinitions.cs) validates that state against the exported JSON schema.
- Valid state renders through the Jinja template and deserializes into a [`WidgetRoot`](../../src/Incursa.OpenAI.ChatKit/Widgets.cs).
- Invalid state fails before a widget root is produced.
- Exported templates may be normalized for the current `Incursa.Jinja` runtime where the export uses the leading-comma slice idiom and `length` filter.

## Fixtures

- [`tests/Incursa.OpenAI.ChatKit.Tests/Fixtures/Email Summary.widget`](../../tests/Incursa.OpenAI.ChatKit.Tests/Fixtures/Email%20Summary.widget)
- [`tests/Incursa.OpenAI.ChatKit.Tests/Fixtures/EmailListId.widget`](../../tests/Incursa.OpenAI.ChatKit.Tests/Fixtures/EmailListId.widget)

## Trace link

- [`LIB-CHATKIT-CORE-004`](../../specs/libraries/library-conformance-matrix.md)
