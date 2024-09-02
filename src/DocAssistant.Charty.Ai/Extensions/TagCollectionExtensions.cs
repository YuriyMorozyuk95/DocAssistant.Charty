// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;

namespace DocAssistant.Charty.Ai.Extensions;

public static class TagCollectionExtensions  
{  
    public static string GetTagValue(this TagCollection tags, string key)  
    {
        return tags.TryGetValue(key, out var value) ? value.FirstOrDefault() : null;
    }  
}
