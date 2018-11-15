using System;
using System.Collections.Generic;

namespace LoxSharp
{
    public abstract class Stmt
    {
        public interface IVisitor
        {
            void Visit(Block stmt);
            void Visit(Class stmt);
            void Visit(Expression stmt);
            void Visit(Function stmt);
            void Visit(If stmt);
            void Visit(Print stmt);
            void Visit(Return stmt);
            void Visit(Var stmt);
            void Visit(While stmt);
        }

        public class Block : Stmt
        {
            public readonly List<Stmt> statements;

            public Block(List<Stmt> statements)
            {
                this.statements = statements;
            }

            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
        }

        public class Class : Stmt
        {
            public readonly Token name;
            public readonly Expr superclass;
            public readonly List<Stmt.Function> methods;

            public Class(Token name, Expr superclass, List<Stmt.Function> methods)
            {
                this.name = name;
                this.superclass = superclass;
                this.methods = methods;
            }

            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
        }

        public class Expression : Stmt
        {
            public readonly Expr expression;

            public Expression(Expr expression)
            {
                this.expression = expression;
            }

            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
        }

        public class Function : Stmt
        {
            public readonly Token name;
            public readonly List<Token> parameters;
            public readonly List<Stmt> body;

            public Function(Token name, List<Token> parameters, List<Stmt> body)
            {
                this.name = name;
                this.parameters = parameters;
                this.body = body;
            }

            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
        }

        public class If : Stmt
        {
            public readonly Expr condition;
            public readonly Stmt thenBranch;
            public readonly Stmt elseBranch;

            public If(Expr condition, Stmt thenBranch, Stmt elseBranch)
            {
                this.condition = condition;
                this.thenBranch = thenBranch;
                this.elseBranch = elseBranch;
            }

            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
        }

        public class Print : Stmt
        {
            public readonly Expr expression;

            public Print(Expr expression)
            {
                this.expression = expression;
            }

            public override void Accept(Stmt.IVisitor visitor)
            {
                visitor.Visit(this);
            }
        }

        public class Return : Stmt
        {
            public readonly Token keyword;
            public readonly Expr value;

            public Return(Token keyword, Expr value)
            {
                this.keyword = keyword;
                this.value = value;
            }

            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
        }

        public class Var : Stmt
        {
            public readonly Token name;
            public readonly Expr initializer;

            public Var(Token name, Expr initializer)
            {
                this.name = name;
                this.initializer = initializer;
            }

            public override void Accept(Stmt.IVisitor visitor)
            {
                visitor.Visit(this);
            }
        }

        public class While : Stmt
        {
            public readonly Expr condition;
            public readonly Stmt body;

            public While(Expr condition, Stmt body)
            {
                this.condition = condition;
                this.body = body;
            }

            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
        }

        public abstract void Accept(IVisitor visitor);
    }
}
