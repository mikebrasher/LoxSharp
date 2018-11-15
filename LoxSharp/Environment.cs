using System;
using System.Collections.Generic;

namespace LoxSharp
{
    class Environment
    {
        public readonly Environment enclosing;
        private readonly Dictionary<string, object> values = new Dictionary<string, object>();

        public Environment()
        {
            enclosing = null;
        }

        public Environment(Environment enclosing)
        {
            this.enclosing = enclosing;
        }

        public void Define(string name, object value)
        {
            if (values.ContainsKey(name))
            {
                // overwrite value instead of throwing exception
                values[name] = value;
            }
            else
            {
                values.Add(name, value);
            }
        }

        public void AssignAt(int distance, Token name, object value)
        {
            Ancestor(distance).values[name.lexeme] = value;
        }

        public void Assign(Token name, object value)
        {
            if (values.ContainsKey(name.lexeme))
            {
                values[name.lexeme] = value;
            }
            else if (enclosing != null)
            {
                enclosing.Assign(name, value);
            }
            else
            {
                throw new RuntimeError(name, "Undefined variable '" + name.lexeme + "'.");
            }
        }

        private Environment Ancestor(int distance)
        {
            Environment environment = this;

            for (int ii = 0; ii < distance; ++ii)
            {
                environment = environment.enclosing;
            }

            return environment;
        }

        public object GetAt(int distance, string name)
        {
            return Ancestor(distance).values[name];
        }

        //public object GetAt(int distance, Token name)
        //{
        //    Environment ancestor = Ancestor(distance);

        //    if (ancestor.values.ContainsKey(name.lexeme))
        //    {
        //        return ancestor.values[name.lexeme];
        //    }

        //    throw new RuntimeError(name, "Variable '" + name.lexeme + "' does not exist in this scope.");
        //}

        public object Get(Token name)
        {
            object ret = null;

            if (values.ContainsKey(name.lexeme))
            {
                ret = values[name.lexeme];
            }
            else if (enclosing != null)
            {
                ret = enclosing.Get(name);
            }
            else
            {
                throw new RuntimeError(name, "Undefined variable '" + name.lexeme + "'.");
            }

            return ret;
        }
    }
}
