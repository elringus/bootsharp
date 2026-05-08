namespace Test.Library;

public delegate void RecordChanged<TCaller> (TCaller caller, Record? record);
