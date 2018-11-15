using System;

namespace LoxSharp
{
    class RuntimeError : Exception
    {
        public readonly Token token;

        public RuntimeError(Token token, string message) : base(message)
        {
            this.token = token;
        }
    }
}
