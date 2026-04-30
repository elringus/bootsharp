using System.Threading.Tasks;

namespace Test.Types;

public interface IExportedStatic
{
    delegate void RecordChanged (Record? record);

    event RecordChanged OnRecordChanged;

    Record? Record { get; set; }

    Task<IExportedInstanced> GetInstanceAsync (string arg);
}
