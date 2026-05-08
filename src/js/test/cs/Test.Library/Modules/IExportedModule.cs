using System.Threading.Tasks;

namespace Test.Library;

public interface IExportedModule
{
    delegate void RecordChanged (Record? record);

    event RecordChanged OnRecordChanged;

    Record? Record { get; set; }

    Task<IExportedInstanced> GetInstanceAsync (string arg);
}
