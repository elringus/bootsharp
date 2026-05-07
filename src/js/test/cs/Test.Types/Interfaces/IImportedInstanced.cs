using System.Threading.Tasks;

namespace Test.Types;

public interface IImportedInstanced
{
    event RecordChanged<IImportedInstanced> OnRecordChanged;

    Record? Record { get; set; }
    IImportedInnerInstanced Inner { get; }

    string GetInstanceArg ();
    Task<string> GetRecordIdAsync (Record record);
}
