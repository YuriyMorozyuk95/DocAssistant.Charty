using System.ComponentModel;

namespace Shared.Models;

public enum Intent
{
    [Description(@"This intent serves as a fallback or default state. It indicates that no specific operation has been identified or requested.
                Typically used for initialization or error handling when the intent cannot be determined.")]
    Default = 0,

    [Description(@"Query: This intent is used for querying or retrieving data from the database.
                It encompasses operations such as SELECT statements, searching for records, and fetching specific data based on certain criteria.")]
    Query = 1,

    [Description(@"Insert: This intent signifies the action of adding new data into an existing table within the database. It is commonly associated with INSERT statements,
where new records or rows are added to a specified table.")]
    Insert = 2,

    [Description(@"Create Table: This intent is related to the creation of new database structures, such as tables. It involves defining the schema, columns, data types,
and constraints for a new table and executing the necessary SQL statements to create it within the database.")]
    Create = 3,
}
