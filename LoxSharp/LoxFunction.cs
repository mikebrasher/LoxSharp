using System;
using System.Collections.Generic;

namespace LoxSharp
{
    class LoxFunction : ILoxCallable
    {
        private readonly Stmt.Function declaration;
        private readonly Environment closure;
        private readonly bool isInitializer;

        public LoxFunction(Stmt.Function declaration, Environment closure, bool isInitializer)
        {
            this.declaration = declaration;
            this.closure = closure;
            this.isInitializer = isInitializer;
        }

        public LoxFunction Bind(LoxInstance instance)
        {
            Environment environment = new Environment(closure);
            environment.Define("this", instance);
            return new LoxFunction(declaration, environment, isInitializer);
        }

        public int Arity
        {
            get
            {
                return declaration.parameters.Count;
            }
        }

        public object Call(Interpreter interpreter, List<object> arguments)
        {
            object ret = null;

            Environment environment = new Environment(closure);

            for (int ii = 0; ii < declaration.parameters.Count; ++ii)
            {
                environment.Define(declaration.parameters[ii].lexeme, arguments[ii]);
            }

            try
            {
                interpreter.ExecuteBlock(declaration.body, environment);
            }
            catch (Return returnValue)
            {
                ret = returnValue.value;
            }

            if (isInitializer)
            {
                ret = closure.GetAt(0, "this");
            }

            return ret;
        }

        public override string ToString()
        {
            return "<fn " + declaration.name.lexeme + ">";
        }
    }
}
