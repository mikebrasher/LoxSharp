using System;
using System.Collections.Generic;

namespace LoxSharp
{
    class Interpreter : Expr.IVisitor<object>, Stmt.IVisitor
    {
        public readonly Environment globals;

        private Environment environment = new Environment();
        private readonly Dictionary<Expr, int> locals = new Dictionary<Expr, int>();

        public Interpreter()
        {
            globals = environment;

            globals.Define("clock", new Clock());
        }

        private class Clock : ILoxCallable
        {
            private static DateTime Jan1st1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            public int Arity
            {
                get
                {
                    return 0;
                }
            }

            public object Call(Interpreter interpreter, List<object> arguments)
            {
                return (DateTime.UtcNow - Jan1st1970).TotalMilliseconds / 1000.0;
            }
        }

        public void Interpret(List<Stmt> statements)
        {
            try
            {
                foreach ( Stmt stmt in statements)
                {
                    Execute(stmt);
                }
            }
            catch(RuntimeError error)
            {
                Lox.RuntimeError(error);
            }
        }

        private string Stringify(object obj)
        {
            string ret = "nil";

            if (obj != null)
            {
                ret = obj.ToString();
            }

            return ret;
        }

        private object Evaluate(Expr expr)
        {
            return expr.Accept(this);
        }

        private void Execute(Stmt stmt)
        {
            stmt.Accept(this);
        }

        public void Resolve(Expr expr, int depth)
        {
            locals.Add(expr, depth);
        }

        private bool IsTruthy(object obj)
        {
            bool ret = true;

            if (obj == null)
            {
                ret = false;
            }
            else if (obj is bool)
            {
                ret = (bool)obj;
            }

            return ret;
        }

        private bool IsEqual(object left, object right)
        {
            bool ret = left.Equals(right);

            if (left == null)
            {
                if (right == null)
                {
                    // nil is only equal to nil
                    ret = true;
                }
                else
                {
                    ret = false;
                }
            }

            return ret;
        }

        private void CheckNumberOperand(Token oper, object right)
        {
            if (right is double) return;
            throw new RuntimeError(oper, "Operand must be a number.");
        }

        private void CheckNumberOperands(Token oper, object left, object right)
        {
            if (left is double && right is double) return;
            throw new RuntimeError(oper, "Operands must be a numbers.");
        }

        private void CheckNonzeroOperand(Token oper, double right)
        {
            if (right != 0.0) return;
            throw new RuntimeError(oper, "Division by zero.");
        }

        public void ExecuteBlock(List<Stmt> statements, Environment environment)
        {
            Environment previous = this.environment;
            
            try
            {
                this.environment = environment;

                foreach (Stmt statement in statements)
                {
                    Execute(statement);
                }
            }
            finally
            {
                this.environment = previous;
            }
        }

        private object LookUpVariable(Token name, Expr expr)
        {
            object ret = null;
            
            if (locals.ContainsKey(expr))
            {
                int distance = locals[expr];
                ret = environment.GetAt(distance, name.lexeme);
            }
            else
            {
                ret = globals.Get(name);
            }

            return ret;
        }

        // Expr Visitors
        public object Visit(Expr.Assign expr)
        {
            object value = Evaluate(expr.value);

            if (locals.ContainsKey(expr))
            {
                int distance = locals[expr];
                environment.AssignAt(distance, expr.name, value);
            }
            else
            {
                globals.Assign(expr.name, value);
            }

            return value;
        }

        public object Visit(Expr.Call expr)
        {
            object callee = Evaluate(expr.callee);

            List<object> arguments = new List<object>();
            foreach (Expr arg in expr.arguments)
            {
                arguments.Add(Evaluate(arg));
            }

            if (!(callee is ILoxCallable))
            {
                throw new RuntimeError(expr.paren, "Can only call functions and classes.");
            }

            ILoxCallable function = (ILoxCallable)callee;

            if (arguments.Count != function.Arity)
            {
                throw new RuntimeError(expr.paren, "Expected " + function.Arity
                    + " arguments but got " + arguments.Count + ".");
            }

            return function.Call(this, arguments);
        }

        public object Visit(Expr.Get expr)
        {
            object obj = Evaluate(expr.obj);

            if (obj is LoxInstance)
            {
                return ((LoxInstance)obj).Get(expr.name);
            }

            throw new RuntimeError(expr.name, "Only instances have properties.");
        }

        public object Visit(Expr.Grouping expr)
        {
            return Evaluate(expr.expression);
        }

        public object Visit(Expr.Literal expr)
        {
            return expr.value;
        }

        public object Visit(Expr.Logical expr)
        {
            object ret = null;

            object left = Evaluate(expr.left);

            if (expr.oper.type == TokenType.Or)
            {
                if (IsTruthy(left))
                {
                    // short-circuit (true  || ...)
                    ret = left;
                }
                else
                {
                    // lazy evaluate if necessary
                    ret = Evaluate(expr.right);
                }
            }
            else
            {
                if (IsTruthy(left))
                {
                    // lazy evaluate if necessary
                    ret = Evaluate(expr.right);
                }
                else
                {
                    // short-circuit (false && ...)
                    ret = left;
                }
            }

            return ret;
        }

        public object Visit(Expr.Set expr)
        {
            object obj = Evaluate(expr.obj);

            if (!(obj is LoxInstance))
            {
                throw new RuntimeError(expr.name, "Only instances have fields.");
            }

            object value = Evaluate(expr.value);
            ((LoxInstance)obj).Set(expr.name, value);

            return value;
        }

        public object Visit(Expr.Super expr)
        {
            int distance = locals[expr];
            LoxClass superclass = (LoxClass)environment.GetAt(distance, "super");

            // "this" is always one level nearer than "super"'s environment;
            LoxInstance obj = (LoxInstance)environment.GetAt(distance - 1, "this");

            LoxFunction method = superclass.FindMethod(obj, expr.method.lexeme);

            if (method == null)
            {
                throw new RuntimeError(expr.method, "Undefined property '" + expr.method.lexeme + ".");
            }

            return method;
        }

        public object Visit(Expr.This expr)
        {
            return LookUpVariable(expr.keyword, expr);
        }

        public object Visit(Expr.Unary expr)
        {
            object ret = null;

            object right = Evaluate(expr.right);

            switch (expr.oper.type)
            {
                case TokenType.Minus:
                    CheckNumberOperand(expr.oper, right);
                    ret = -(double)right;
                    break;

                case TokenType.Bang:
                    ret = !IsTruthy(right);
                    break;

                default:
                    // do nothing
                    break;
            }

            return ret;
        }

        public object Visit(Expr.Binary expr)
        {
            object ret = null;

            object left = Evaluate(expr.left);
            object right = Evaluate(expr.right);

            switch(expr.oper.type)
            {
                case TokenType.BangEqual:
                    ret = !IsEqual(left, right);
                    break;

                case TokenType.EqualEqual:
                    ret = IsEqual(left, right);
                    break;

                case TokenType.Greater:
                    CheckNumberOperands(expr.oper, left, right);
                    ret = (double)left > (double)right;
                    break;

                case TokenType.GreaterEqual:
                    CheckNumberOperands(expr.oper, left, right);
                    ret = (double)left >= (double)right;
                    break;

                case TokenType.Less:
                    CheckNumberOperands(expr.oper, left, right);
                    ret = (double)left < (double)right;
                    break;

                case TokenType.LessEqual:
                    CheckNumberOperands(expr.oper, left, right);
                    ret = (double)left <= (double)right;
                    break;

                case TokenType.Minus:
                    CheckNumberOperands(expr.oper, left, right);
                    ret = (double)left - (double)right;
                    break;

                case TokenType.Plus:
                    if (left is double && right is double)
                    {
                        ret = (double)left + (double)right;
                    }
                    else if (left is string && right is string)
                    {
                        ret = (string)left + (string)right;
                    }
                    else
                    {
                        throw new RuntimeError(expr.oper, "Operands must be two numbers or two strings.");
                    }
                    break;

                case TokenType.Slash:
                    CheckNumberOperands(expr.oper, left, right);
                    CheckNonzeroOperand(expr.oper, (double)right);
                    ret = (double)left / (double)right;
                    break;

                case TokenType.Star:
                    CheckNumberOperands(expr.oper, left, right);
                    ret = (double)left * (double)right;
                    break;

                default:
                    // do nothing
                    break;
            }

            return ret;
        }

        public object Visit(Expr.Variable expr)
        {
            return LookUpVariable(expr.name, expr);
        }

        // Stmt Visitors
        public void Visit(Stmt.Block stmt)
        {
            ExecuteBlock(stmt.statements, new Environment(environment));
        }

        public void Visit(Stmt.Class stmt)
        {
            environment.Define(stmt.name.lexeme, null);

            object superclass = null;
            if (stmt.superclass != null)
            {
                superclass = Evaluate(stmt.superclass);
                if (!(superclass is LoxClass))
                {
                    throw new RuntimeError(stmt.name, "Superclass must be a class.");
                }

                environment = new Environment(environment);
                environment.Define("super", superclass);
            }

            Dictionary<string, LoxFunction> methods = new Dictionary<string, LoxFunction>();
            foreach (Stmt.Function method in stmt.methods)
            {
                LoxFunction function = new LoxFunction(method, environment, method.name.lexeme == "init");
                methods.Add(method.name.lexeme, function);
            }

            LoxClass klass = new LoxClass(stmt.name.lexeme, (LoxClass) superclass, methods);

            if (superclass != null)
            {
                environment = environment.enclosing;
            }

            environment.Assign(stmt.name, klass);
        }

        public void Visit(Stmt.Expression stmt)
        {
            Evaluate(stmt.expression);
        }

        public void Visit(Stmt.Function stmt)
        {
            LoxFunction function = new LoxFunction(stmt, environment, false);
            environment.Define(stmt.name.lexeme, function);
        }

        public void Visit(Stmt.If stmt)
        {
            if (IsTruthy(Evaluate(stmt.condition)))
            {
                Execute(stmt.thenBranch);
            }
            else if (stmt.elseBranch != null)
            {
                Execute(stmt.elseBranch);
            }
        }

        public void Visit(Stmt.Print stmt)
        {
            object value = Evaluate(stmt.expression);
            Console.WriteLine(Stringify(value));
        }

        public void Visit(Stmt.Return stmt)
        {
            object value = null;
            if (stmt.value != null)
            {
                value = Evaluate(stmt.value);
            }

            throw new Return(value);
        }

        public void Visit(Stmt.Var stmt)
        {
            object value = null;

            if (stmt.initializer != null)
            {
                value = Evaluate(stmt.initializer);
            }

            environment.Define(stmt.name.lexeme, value);
        }

        public void Visit(Stmt.While stmt)
        {
            while (IsTruthy(Evaluate(stmt.condition)))
            {
                Execute(stmt.body);
            }
        }
    }
}
