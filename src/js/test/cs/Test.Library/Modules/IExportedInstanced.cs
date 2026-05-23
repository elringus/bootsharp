using System;
using System.Threading.Tasks;

namespace Test.Library;

public interface IExportedInstanced
{
    event RecordChanged<IExportedInstanced> OnRecordChanged;

    Record? Record { get; set; }
    ExportedInnerInstanced Inner { get; }

    string GetInstanceArg ();
    Task<string> GetRecordIdAsync (Record record);
    Task<IBidirectional> GetBiAsync (Func<IBidirectional>? factory = null);
}
