using System;
using System.Text;

namespace LoxSharp
{
    class ExpressionPrinter  : Expr.IVisitor<string>, Stmt.IVisitor<string>
    {
        public string Print(Expr expr)
        {
            return expr.Accept(this);
        }

        private string Parentherize(string name, params Expr[] exprs)
        {
            StringBuilder builder = new StringBuilder();

            builder.Append("(").Append(name);

            foreach (Expr e in exprs)
            {
                builder.Append(" ");
                builder.Append(e.Accept(this));
            }

            builder.Append(")");

            return builder.ToString();
        }

        private string Parentherize2(string name, params object[] parts)
        {
            StringBuilder builder = new StringBuilder();

            builder.Append("(").Append(name);

            foreach (object part in parts)
            {
                builder.Append(" ");

                if (part is Expr)
                {
                    builder.Append(((Expr)part).Accept(this));
                }
                else if (part is Stmt)
                {
                    //builder.Append(((Stmt)part).Accept(this));
                }
                else if (part is Token)
                {
                    builder.Append(((Token)part).lexeme);
                }
                else
                {
                    builder.Append(part);
                }
            }

            builder.Append(")");

            return builder.ToString();
        }

        // Expr Visitors
        public string Visit(Expr.Assign assign)
        {
            //return Parentherize2("=", assign.name.lexeme, assign.value);
            return "";
        }

        public string Visit(Expr.Binary binary)
        {
            return Parentherize(binary.oper.lexeme, binary.left, binary.right);
        }

        public string Visit(Expr.Grouping grouping)
        {
            return Parentherize("group", grouping.expression);
        }

        public string Visit(Expr.Literal literal)
        {
            string ret = "nil";

            if (literal.value != null)
            {
                ret = literal.value.ToString();
            }

            return ret;
        }

        public string Visit(Expr.Unary unary)
        {
            return Parentherize(unary.oper.lexeme, unary.right);
        }

        public string Visit(Expr.Variable variable)
        {
            return variable.name.lexeme;
        }

        // Stmt Visitors
        public void Visit(Stmt.Block block)
        {
            // do nothing
        }

        public void Visit(Stmt.Expression expression)
        {
            // do nothing
        }

        public void Visit(Stmt.Print print)
        {
            // do nothing
        }

        public void Visit(Stmt.Var var)
        {
            // do nothing
        }
    }
}
