using System;
using System.Collections.Generic;

namespace LoxSharp
{
    class LoxClass : ILoxCallable
    {
        public readonly string name;
        public readonly LoxClass superclass;
        private readonly Dictionary<string, LoxFunction> methods;

        public LoxClass(string name, LoxClass superclass, Dictionary<string, LoxFunction> methods)
        {
            this.name = name;
            this.superclass = superclass;
            this.methods = methods;
        }

        public LoxFunction FindMethod(LoxInstance instance, string name)
        {
            LoxFunction ret = null;

            if (methods.ContainsKey(name))
            {
                ret = methods[name].Bind(instance);
            }
            else if (superclass != null)
            {
                ret = superclass.FindMethod(instance, name);
            }

            return ret;
        }

        public int Arity
        {
            get
            {
                int ret = 0;

                if (methods.ContainsKey("init"))
                {
                    ret = methods["init"].Arity;
                }

                return ret;
            }
        }

        public object Call(Interpreter interpreter, List<object> arguments)
        {
            LoxInstance instance = new LoxInstance(this);

            if (methods.ContainsKey("init"))
            {
                methods["init"].Bind(instance).Call(interpreter, arguments);
            }

            return instance;
        }

        public override string ToString()
        {
            return name;
        }
    }
}
