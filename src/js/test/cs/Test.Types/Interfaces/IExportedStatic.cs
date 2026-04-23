using System;
using System.Threading.Tasks;

namespace Test.Types;

public interface IExportedStatic
{
    event Action<Record?> OnRecordChanged;

    Record? Record { get; set; }

    Task<IExportedInstanced> GetInstanceAsync (string arg);
}
