// Copyright (c) Microsoft. All rights reserved.

public interface IComputerVisionService
{
    public int Dimension { get; }

    Task<ImageEmbeddingResponse> VectorizeImage(string imagePathOrUrl, CancellationToken ct = default);
    Task<ImageEmbeddingResponse> VectorizeText(string text, CancellationToken ct = default);
}
