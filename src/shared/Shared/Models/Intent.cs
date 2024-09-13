using System.ComponentModel;

namespace Shared.Models;

public enum Intent
{
    [Description(@"default")]
    Default = 0,

    [Description(@"Query data")]
    Query = 1,

    [Description(@"Add data")]
    Insert = 2,

    [Description(@"Create Table")]
    Create = 3,
}
