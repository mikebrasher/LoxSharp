using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoxSharp
{
    class Resolver : Expr.IVisitor<object>, Stmt.IVisitor
    {
        private class Tracker
        {
            private readonly Token name;
            private bool initialized = false;
            private int count = 0;

            public Tracker(Token name)
            {
                this.name = name;
            }

            public Tracker(Token name, bool initialized, int count)
            {
                this.name = name;
                this.initialized = initialized;
                this.count = count;
            }

            public Token Name
            {
                get
                {
                    return name;
                }
            }

            public bool Initialized
            {
                get
                {
                    return initialized;
                }

                set
                {
                    initialized = value;
                }
            }

            public int Count
            {
                get
                {
                    return count;
                }
            }

            public void AddReference()
            {
                count++;
            }
        }

        private readonly Interpreter interpreter;
        private readonly Stack<Dictionary<string, Tracker>> scopes = new Stack<Dictionary<string, Tracker>>();

        private enum FunctionType { None, Function, Initializer, Method }
        private FunctionType currentFunction = FunctionType.None;
        
        private enum ClassType { None, Class, Subclass };
        private ClassType currentClass = ClassType.None;
        
        public Resolver(Interpreter interpreter)
        {
            this.interpreter = interpreter;
        }

        public void Resolve(List<Stmt> statements)
        {
            foreach (Stmt stmt in statements)
            {
                Resolve(stmt);
            }
        }

        private void BeginScope()
        {
            scopes.Push(new Dictionary<string, Tracker>());
        }

        private void EndScope()
        {
            Dictionary<string, Tracker> scope = scopes.Pop();

            // check for unused local variables
            foreach (Tracker reference in scope.Values)
            {
                if (reference.Count == 0)
                {
                    Lox.Error(reference.Name, "Variable '" + reference.Name.lexeme + "' is declared but never used.");
                }
            }
        }

        private void Declare(Token name)
        {
            if (scopes.Count > 0)
            {
                Dictionary<string, Tracker> scope = scopes.Peek();

                if (scope.ContainsKey(name.lexeme))
                {
                    Lox.Error(name, "Variable with this name already declared in this scope.");
                }

                scope.Add(name.lexeme, new Tracker(name));
            }
        }

        private void Define(Token name)
        {
            if (scopes.Count > 0)
            {
                scopes.Peek()[name.lexeme].Initialized = true;
            }
        }

        private void Resolve(Stmt stmt)
        {
            stmt.Accept(this);
        }

        private void Resolve(Expr expr)
        {
            expr.Accept(this);
        }

        private void ResolveLocal(Expr expr, Token name)
        {
            //for (int ii = scopes.Count - 1; ii >= 0; --ii)
            //{
            //    Dictionary<string, Tracker> currentScope = scopes.ElementAt(ii);
            //    if (currentScope.ContainsKey(name.lexeme))
            //    {
            //        currentScope[name.lexeme].AddReference();
            //        interpreter.Resolve(expr, scopes.Count - 1 - ii);
            //        return;
            //    }
            //}
            for (int ii = 0; ii < scopes.Count; ++ii)
            {
                Dictionary<string, Tracker> currentScope = scopes.ElementAt(ii);
                if (currentScope.ContainsKey(name.lexeme))
                {
                    currentScope[name.lexeme].AddReference();
                    interpreter.Resolve(expr, ii);
                    return;
                }
            }

            // not found. assume it is global
        }

        private void ResolveFunction(Stmt.Function function, FunctionType type)
        {
            FunctionType enclosingFunction = currentFunction;
            currentFunction = type;

            BeginScope();

            foreach (Token param in function.parameters)
            {
                Declare(param);
                Define(param);
            }

            Resolve(function.body);

            EndScope();

            currentFunction = enclosingFunction;
        }

        // Expr Visitors
        public object Visit(Expr.Variable expr)
        {
            if (scopes.Count > 0)
            {
                Dictionary<string, Tracker> scope = scopes.Peek();
                if (scope.ContainsKey(expr.name.lexeme) &&        // variable declared
                    scope[expr.name.lexeme].Initialized == false) // variable not initialized
                {
                    Lox.Error(expr.name, "Cannot read local variable in its own initializer.");
                }
            }

            ResolveLocal(expr, expr.name);

            return null;
        }

        public object Visit(Expr.Assign expr)
        {
            Resolve(expr.value);
            ResolveLocal(expr, expr.name);

            return null;
        }

        public object Visit(Expr.Binary expr)
        {
            Resolve(expr.left);
            Resolve(expr.right);

            return null;
        }

        public object Visit(Expr.Call expr)
        {
            Resolve(expr.callee);

            foreach (Expr arg in expr.arguments)
            {
                Resolve(arg);
            }

            return null;
        }

        public object Visit(Expr.Get expr)
        {
            Resolve(expr.obj);

            return null;
        }

        public object Visit(Expr.Grouping expr)
        {
            Resolve(expr.expression);

            return null;
        }

        public object Visit(Expr.Literal expr)
        {
            return null; // do nothing
        }

        public object Visit(Expr.Logical expr)
        {
            Resolve(expr.left);
            Resolve(expr.right);

            return null;
        }

        public object Visit(Expr.Set expr)
        {
            Resolve(expr.value);
            Resolve(expr.obj);
            
            return null;
        }

        public object Visit(Expr.Super expr)
        {
            if (currentClass == ClassType.None)
            {
                Lox.Error(expr.keyword, "Cannot use 'super' outside of a class.");
            }
            else if (currentClass != ClassType.Subclass)
            {
                Lox.Error(expr.keyword, "Cannot use 'super' in a class with no superclass.");
            }

            ResolveLocal(expr, expr.keyword);

            return null;
        }

        public object Visit(Expr.This expr)
        {
            if (currentClass == ClassType.None)
            {
                Lox.Error(expr.keyword, "Cannot use 'this' outside of a class.");
            }
            else
            {
                ResolveLocal(expr, expr.keyword);
            }
            
            return null;
        }

        public object Visit(Expr.Unary expr)
        {
            Resolve(expr.right);

            return null;
        }

        // Stmt Visitors
        public void Visit(Stmt.Block stmt)
        {
            BeginScope();
            Resolve(stmt.statements);
            EndScope();
        }

        public void Visit(Stmt.Class stmt)
        {
            Declare(stmt.name);
            Define(stmt.name);

            ClassType enclosingClass = currentClass;
            currentClass = ClassType.Class;

            if (stmt.superclass != null)
            {
                currentClass = ClassType.Subclass;
                Resolve(stmt.superclass);
                BeginScope();
                Token superToken = new Token(TokenType.Super, "super", null, stmt.name.line);
                scopes.Peek().Add("super", new Tracker(superToken, true, 1));
            }

            BeginScope();
            Token thisToken = new Token(TokenType.This, "this", null, stmt.name.line);
            scopes.Peek().Add("this", new Tracker(thisToken, true, 1)); // implicit reference in case 'this' is never used

            foreach (Stmt.Function method in stmt.methods)
            {
                FunctionType declaration = FunctionType.Method;
                if (method.name.lexeme == "init")
                {
                    declaration = FunctionType.Initializer;
                }

                ResolveFunction(method, declaration);
            }

            EndScope();

            if (stmt.superclass != null)
            {
                EndScope();
            }

            currentClass = enclosingClass;
        }

        public void Visit(Stmt.Function stmt)
        {
            Declare(stmt.name);
            Define(stmt.name);

            ResolveFunction(stmt, FunctionType.Function);
        }

        public void Visit(Stmt.Var stmt)
        {
            Declare(stmt.name);

            if (stmt.initializer != null)
            {
                Resolve(stmt.initializer);
            }

            Define(stmt.name);
        }

        public void Visit(Stmt.Expression stmt)
        {
            Resolve(stmt.expression);
        }

        public void Visit(Stmt.If stmt)
        {
            Resolve(stmt.condition);
            Resolve(stmt.thenBranch);
            if (stmt.elseBranch != null)
            {
                Resolve(stmt.elseBranch);
            }
        }

        public void Visit(Stmt.Print stmt)
        {
            Resolve(stmt.expression);
        }

        public void Visit(Stmt.Return stmt)
        {
            if (currentFunction == FunctionType.None)
            {
                Lox.Error(stmt.keyword, "Cannot return from top-level code.");
            }

            if (stmt.value != null)
            {
                if (currentFunction == FunctionType.Initializer)
                {
                    Lox.Error(stmt.keyword, "Cannot return a value from an initializer.");
                }

                Resolve(stmt.value);
            }
        }

        public void Visit(Stmt.While stmt)
        {
            Resolve(stmt.condition);
            Resolve(stmt.body);
        }
    }
}
