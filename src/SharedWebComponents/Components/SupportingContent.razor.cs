

using DocAssistant.Charty.Ai;

namespace SharedWebComponents.Components;

public sealed partial class SupportingContent
{
    [Parameter, EditorRequired] public required SupportingContentDto[] DataPoints { get; set; }

    [Parameter, EditorRequired] public SupportingImageRecord[] Images { get; set; }

    private ParsedSupportingContentItem[] _supportingContent = [];

    protected override void OnParametersSet()
    {
        if (DataPoints is { Length: > 0 })
        {
            _supportingContent =
                DataPoints.Select(ParseSupportingContent).ToArray();
        }

        base.OnParametersSet();
    }
}
