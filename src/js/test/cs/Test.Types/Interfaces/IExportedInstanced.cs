using System.Threading.Tasks;

namespace Test.Types;

public interface IExportedInstanced
{
    event RecordChanged<IExportedInstanced> OnRecordChanged;

    Record? Record { get; set; }
    ExportedInnerInstanced Inner { get; }

    string GetInstanceArg ();
    Task<string> GetRecordIdAsync (Record record);
}
