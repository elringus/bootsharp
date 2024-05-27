using System.Collections.Generic;

namespace Bench;

public record RecordA (List<RecordB> List, IReadOnlyList<RecordB> RoList, IDictionary<string, RecordB> Dict);
