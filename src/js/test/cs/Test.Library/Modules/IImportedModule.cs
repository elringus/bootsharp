using System.Threading.Tasks;

namespace Test.Library;

public interface IImportedModule
{
    delegate void RecordChanged (Record? record);

    event RecordChanged OnRecordChanged;

    Record? Record { get; set; }

    Task<IImportedInstanced> GetInstanceAsync (string arg);
}
