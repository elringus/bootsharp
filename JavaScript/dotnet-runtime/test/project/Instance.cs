using Microsoft.JSInterop;

namespace Test
{
    public class Instance
    {
        private string var;

        [JSInvokable]
        public static DotNetObjectReference<Instance> CreateInstance ()
        {
            return DotNetObjectReference.Create(new Instance());
        }

        [JSInvokable]
        public void SetVar (string value) => var = value;

        [JSInvokable]
        public string GetVar () => var;

        [JSInvokable]
        public string SetFromOther (DotNetObjectReference<Instance> objRef) => var = objRef.Value.var;
    }
}
