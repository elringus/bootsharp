namespace Test.Types;

public delegate void RecordChanged<TCaller> (TCaller caller, Record? record);
