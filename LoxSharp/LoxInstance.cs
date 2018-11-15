using System.Collections.Generic;

namespace LoxSharp
{
    class LoxInstance
    {
        private LoxClass klass;
        private readonly Dictionary<string, object> fields = new Dictionary<string, object>();

        public LoxInstance(LoxClass klass)
        {
            this.klass = klass;
        }

        public object Get(Token name)
        {
            if (fields.ContainsKey(name.lexeme))
            {
                return fields[name.lexeme];
            }

            LoxFunction method = klass.FindMethod(this, name.lexeme);
            if (method != null)
            {
                return method;
            }

            throw new RuntimeError(name, "Undefined property '" + name.lexeme + "'.");
        }

        public void Set(Token name, object value)
        {
            if (fields.ContainsKey(name.lexeme))
            {
                fields[name.lexeme] = value;
            }
            else
            {
                fields.Add(name.lexeme, value);
            }
        }

        public override string ToString()
        {
            return klass.name + " instance";
        }
    }
}
