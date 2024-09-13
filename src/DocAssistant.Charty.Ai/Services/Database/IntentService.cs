using System.ComponentModel;
using System.Reflection;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Memory;

using Shared.Models;

namespace DocAssistant.Charty.Ai.Services.Database;

public interface IIntentService
{
    Task<Intent> CheckIntent(string input);
}
public class IntentService : IIntentService
{
    private readonly Dictionary<Intent, string> _intentDescriptions;
    private readonly VolatileMemoryStore _memoryStore;
    private readonly ITextEmbeddingGenerationService _embeddingGenerator;
    private readonly string _collectionName = "intentCollection";

    private bool _initialized;

    public IntentService(Kernel kernel)
    {
        _intentDescriptions = new Dictionary<Intent, string>();
        _memoryStore = new VolatileMemoryStore();
        _embeddingGenerator = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
    }

    private async Task TryInitialize()
    {
        if (!_initialized)
        {
            await _memoryStore.CreateCollectionAsync(_collectionName);

            foreach (Intent intent in Enum.GetValues(typeof(Intent)))
            {
                var description = GetEnumDescription(intent);

                _intentDescriptions.Add(intent, description);

                var embedding = await _embeddingGenerator.GenerateEmbeddingAsync(description);

                var memoryRecord = MemoryRecord.LocalRecord(
                    id: intent.ToString(),
                    text: description,
                    description: description,
                    embedding: embedding
                );

                await _memoryStore.UpsertAsync(_collectionName, memoryRecord);
            }

            _initialized = true;
        }
    }

    private string GetEnumDescription(Enum value)
    {
        FieldInfo fi = value.GetType().GetField(value.ToString());
        DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
        return (attributes.Length > 0) ? attributes[0].Description : value.ToString();
    }

    public async Task<Intent> CheckIntent(string input)
    {
        await TryInitialize();

        var inputEmbedding = await _embeddingGenerator.GenerateEmbeddingAsync(input);

        var topMatch = await _memoryStore.GetNearestMatchAsync(_collectionName, inputEmbedding, minRelevanceScore: 0.0, withEmbedding: true);

        if (topMatch.HasValue)
        {
            var matchedId = topMatch.Value.Item1.Metadata.Id;
            return Enum.Parse<Intent>(matchedId);
        }

        return Intent.Default;
    }
}
