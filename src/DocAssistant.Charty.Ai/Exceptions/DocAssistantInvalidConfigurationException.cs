

namespace DocAssistant.Charty.Ai.Exceptions;

public class DocAssistantInvalidConfigurationException : Exception
{
    public DocAssistantInvalidConfigurationException()
    {
    }

    public DocAssistantInvalidConfigurationException(string message)
        : base(message)
    {
    }

    public DocAssistantInvalidConfigurationException(string message, System.Exception innerException)
        : base(message, innerException)
    {
    }
}
