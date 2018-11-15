using System;

namespace LoxSharp
{
    class Return : Exception
    {
        public readonly object value;

        public Return(object value)
        {
            this.value = value;
        }
    }
}
