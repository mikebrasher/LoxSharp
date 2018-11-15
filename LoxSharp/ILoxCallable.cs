using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoxSharp
{
    interface ILoxCallable
    {
        int Arity { get; }
        object Call(Interpreter interpreter, List<object> arguments);
    }
}
