using System.Threading.Tasks;

namespace Test.Types;

public interface IImportedStatic
{
    delegate void RecordChanged (Record? record);

    event RecordChanged OnRecordChanged;

    Record? Record { get; set; }

    Task<IImportedInstanced> GetInstanceAsync (string arg);
}
