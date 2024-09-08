

using Shared.Models;

namespace DocAssistant.Charty.Ai.Extensions;

public static class ChatTurnExtensions
{
    public static string GetQuestionFromHistory(this ChatMessage[] history)
    {
        var question = history.LastOrDefault(x => x.IsUser);
        return question.Content;
    }
	
    public static ChatMessage MergeLastQuestionWithSystemResponse(this ChatMessage[] history, string botResponse, List<SupportingContentDto> supportingContent)
    {
        var lastChatTurn = history.LastOrDefault();

        //lastChatTurn.SupportingContent = supportingContent;
        //lastChatTurn.Bot = botResponse;

        return lastChatTurn;
    }
}
