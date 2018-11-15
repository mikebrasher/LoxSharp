using System;
using System.Collections.Generic;

namespace LoxSharp
{
    public abstract class Expr
    {
        public interface IVisitor<T>
        {
            T Visit(Assign expr);
            T Visit(Binary expr);
            T Visit(Call expr);
            T Visit(Get expr);
            T Visit(Grouping expr);
            T Visit(Literal expr);
            T Visit(Logical expr);
            T Visit(Set expr);
            T Visit(Super expr);
            T Visit(This expr);
            T Visit(Unary expr);
            T Visit(Variable expr);
        }

        public class Assign : Expr
        {
            public readonly Token name;
            public readonly Expr value;

            public Assign(Token name, Expr value)
            {
                this.name = name;
                this.value = value;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class Binary : Expr
        {
            public readonly Expr left;
            public readonly Token oper;
            public readonly Expr right;

            public Binary(Expr left, Token oper, Expr right)
            {
                this.left = left;
                this.oper = oper;
                this.right = right;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class Call : Expr
        {
            public readonly Expr callee;
            public readonly Token paren;
            public readonly List<Expr> arguments;

            public Call(Expr callee, Token paren, List<Expr> arguments)
            {
                this.callee = callee;
                this.paren = paren;
                this.arguments = arguments;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class Get : Expr
        {
            public readonly Expr obj;
            public readonly Token name;

            public Get(Expr obj, Token name)
            {
                this.obj = obj;
                this.name = name;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class Grouping : Expr
        {
            public readonly Expr expression;

            public Grouping(Expr expression)
            {
                this.expression = expression;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class Literal : Expr
        {
            public readonly object value;

            public Literal(object value)
            {
                this.value = value;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class Logical : Expr
        {
            public readonly Expr left;
            public readonly Token oper;
            public readonly Expr right;

            public Logical(Expr left, Token oper, Expr right)
            {
                this.left = left;
                this.oper = oper;
                this.right = right;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class Set : Expr
        {
            public readonly Expr obj;
            public readonly Token name;
            public readonly Expr value;

            public Set(Expr obj, Token name, Expr value)
            {
                this.obj = obj;
                this.name = name;
                this.value = value;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class Super : Expr
        {
            public readonly Token keyword;
            public readonly Token method;

            public Super(Token keyword, Token method)
            {
                this.keyword = keyword;
                this.method = method;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class This : Expr
        {
            public readonly Token keyword;

            public This(Token keyword)
            {
                this.keyword = keyword;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class Unary : Expr
        {
            public readonly Token oper;
            public readonly Expr right;

            public Unary(Token oper, Expr right)
            {
                this.oper = oper;
                this.right = right;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class Variable : Expr
        {
            public readonly Token name;

            public Variable(Token name)
            {
                this.name = name;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public abstract T Accept<T>(IVisitor<T> visitor);
    }
}
