using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoxSharp
{
    class Parser
    {
        private readonly bool repl = false;

        private class ParseError : Exception { };
        private readonly List<Token> tokens;
        private int current = 0;

        public Parser(List<Token> tokens)
        {
            this.tokens = tokens;
        }

        public Parser(List<Token> tokens, bool repl)
        {
            this.tokens = tokens;
            this.repl = repl;
        }

        public List<Stmt> Parse()
        {
            List<Stmt> statements = new List<Stmt>();

            while (!IsAtEnd())
            {
                statements.Add(Declaration());
            }

            return statements;
        }

        private Stmt Declaration()
        {
            Stmt stmt = null;

            try
            {
                if (Match(TokenType.Class))
                {
                    stmt = ClassDeclaration();
                }
                else if (Match(TokenType.Fun))
                {
                    stmt = Function("function");
                }
                else if (Match(TokenType.Var))
                {
                    stmt = VarDeclaration();
                }
                else
                {
                    stmt = Statement();
                }
            }
            catch (ParseError error)
            {
                Synchronize();
            }

            return stmt;
        }

        private Stmt ClassDeclaration()
        {
            Token name = Consume(TokenType.Identifier, "Expect class name.");

            Expr superclass = null;
            if (Match(TokenType.Less))
            {
                Consume(TokenType.Identifier, "Expect superclass name.");
                superclass = new Expr.Variable(Previous());
            }

            Consume(TokenType.LeftBrace, "Expect '{' before class body.");

            List<Stmt.Function> methods = new List<Stmt.Function>();
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                methods.Add(Function("method"));
            }

            Consume(TokenType.RightBrace, "Expect '}' after class body.");

            return new Stmt.Class(name, superclass, methods);
        }

        private Stmt VarDeclaration()
        {
            Token name = Consume(TokenType.Identifier, "Expect variable name.");

            Expr initializer = null;
            if (Match(TokenType.Equal))
            {
                initializer = Expression();
            }

            Consume(TokenType.Semicolon, "Expect ';' after variable declaration.");

            return new Stmt.Var(name, initializer);
        }

        private Stmt Statement()
        {
            Stmt stmt = null;

            if (Match(TokenType.For))
            {
                stmt = ForStatement();
            }
            else if (Match(TokenType.If))
            {
                stmt = IfStatement();
            }
            else if (Match(TokenType.Print))
            {
                stmt = PrintStatement();
            }
            else if (Match(TokenType.Return))
            {
                stmt = ReturnStatement();
            }
            else if (Match(TokenType.While))
            {
                stmt = WhileStatement();
            }
            else if (Match(TokenType.LeftBrace))
            {
                stmt = BlockStatement();
            }
            else
            {
                stmt = ExpressionStatement();
            }

            return stmt;
        }

        private Stmt ForStatement()
        {
            Consume(TokenType.LeftParen, "Expect '(' after 'for'");

            // initializer
            Stmt initializer = null;
            if (Match(TokenType.Semicolon))
            {
                initializer = null;
            }
            else if (Match(TokenType.Var))
            {
                initializer = VarDeclaration();
            }
            else
            {
                initializer = ExpressionStatement();
            }

            // condition
            Expr condition = null;
            if (!Check(TokenType.Semicolon))
            {
                condition = Expression();
            }
            Consume(TokenType.Semicolon, "Expect ';' after loop condition.");

            // increment
            Expr increment = null;
            if (!Check(TokenType.RightParen))
            {
                increment = Expression();
            }
            Consume(TokenType.RightParen, "Expect ')' after for clauses.");

            // body
            Stmt body = Statement();

            // desugar
            if (increment != null)
            {
                List<Stmt> statements = new List<Stmt>();
                statements.Add(body);
                statements.Add(new Stmt.Expression(increment));

                body = new Stmt.Block(statements);
            }

            if (condition == null)
            {
                condition = new Expr.Literal(true);
            }
            body = new Stmt.While(condition, body);

            if (initializer != null)
            {
                List<Stmt> statements = new List<Stmt>();
                statements.Add(initializer);
                statements.Add(body);

                body = new Stmt.Block(statements);
            }

            return body;
        }

        private Stmt IfStatement()
        {
            // condition
            Consume(TokenType.LeftParen, "Expect '(' after 'if'.");
            Expr condition = Expression();
            Consume(TokenType.RightParen, "Expect ')' after condition.");

            // then
            Stmt thenBranch = Statement();

            // else
            Stmt elseBranch = null;
            if (Match(TokenType.Else))
            {
                elseBranch = Statement();
            }

            return new Stmt.If(condition, thenBranch, elseBranch);
        }

        private Stmt PrintStatement()
        {
            Expr value = Expression();
            Consume(TokenType.Semicolon, "Expect ';' after value.");
            return new Stmt.Print(value);
        }

        private Stmt ReturnStatement()
        {
            Token keyword = Previous();

            Expr value = null;
            if (!Check(TokenType.Semicolon))
            {
                value = Expression();
            }

            Consume(TokenType.Semicolon, "Expect ';' after return value.");

            return new Stmt.Return(keyword, value);
        }
        
        private Stmt WhileStatement()
        {
            // condition
            Consume(TokenType.LeftParen, "Expect '(' after 'while'.");
            Expr condition = Expression();
            Consume(TokenType.RightParen, "Expect ')' after condition.");

            // body
            Stmt body = Statement();

            return new Stmt.While(condition, body);
        }

        private Stmt BlockStatement()
        {
            List<Stmt> statements = new List<Stmt>();

            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                statements.Add(Declaration());
            }

            Consume(TokenType.RightBrace, "Expect '}' after block.");

            return new Stmt.Block(statements);
        }

        private Stmt ExpressionStatement()
        {
            Stmt ret = null;

            Expr expr = Expression();

            if (repl)
            {
                if(Check(TokenType.Semicolon))
                {
                    Consume(TokenType.Semicolon, "Expect ';' after value.");
                    ret = new Stmt.Expression(expr);
                }
                else
                {
                    ret = new Stmt.Print(expr);
                }
            }
            else
            {
                Consume(TokenType.Semicolon, "Expect ';' after value.");
                ret = new Stmt.Expression(expr);
            }

            return ret;
        }

        private Stmt.Function Function(string kind)
        {
            // name
            //Token name = null;
            //if (Match(TokenType.LeftParen))
            //{
            //    Token paren = Previous();
            //    name = new Token(TokenType.Identifier, "lambda", "lambda", paren.line);
            //}
            //else
            //{
            //    name = Consume(TokenType.Identifier, "Expect " + kind + " name.");
            //    Consume(TokenType.LeftParen, "Expect '(' after " + kind + " name.");
            //}

            Token name = Consume(TokenType.Identifier, "Expect " + kind + " name.");
            Consume(TokenType.LeftParen, "Expect '(' after " + kind + " name.");

            // parameters
            List<Token> parameters = new List<Token>();
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    if (parameters.Count > 7)
                    {
                        Error(Peek(), "Cannot have more than 8 parameters.");
                    }

                    parameters.Add(Consume(TokenType.Identifier, "Expect parameter name."));
                } while (Match(TokenType.Comma));
            }

            Consume(TokenType.RightParen, "Expect ')' after paremeters.");

            // body
            Consume(TokenType.LeftBrace, "Expect '{' before " + kind + " body.");
            Stmt block = BlockStatement();

            List<Stmt> body = new List<Stmt>();
            if (block is Stmt.Block)
            {
                body = ((Stmt.Block)block).statements;
            }
            else
            {
                Error(name, "Invalid function body for " + name + ".");
            }

            return new Stmt.Function(name, parameters, body);
        }

        private Expr Expression()
        {
            return Assignment();
        }

        private Expr Assignment()
        {
            Expr expr = Or();

            if (Match(TokenType.Equal))
            {
                Token equals = Previous();
                Expr value = Assignment();

                if (expr is Expr.Variable)
                {
                    Token name = ((Expr.Variable)expr).name;
                    expr = new Expr.Assign(name, value);
                }
                else if (expr is Expr.Get)
                {
                    Expr.Get get = (Expr.Get)expr;
                    expr = new Expr.Set(get.obj, get.name, value);
                }
                else
                {
                    Error(equals, "Invalid assignment target.");
                }
            }

            return expr;
        }

        private Expr Or()
        {
            Expr expr = And();

            while (Match(TokenType.Or))
            {
                Token oper = Previous();
                Expr right = And();
                expr = new Expr.Logical(expr, oper, right);
            }

            return expr;
        }

        private Expr And()
        {
            Expr expr = Equality();

            while(Match(TokenType.And))
            {
                Token oper = Previous();
                Expr right = Equality();
                expr = new Expr.Logical(expr, oper, right);
            }

            return expr;
        }

        private Expr Equality()
        {
            Expr expr = Comparison();

            while (Match(TokenType.BangEqual, TokenType.EqualEqual))
            {
                Token oper = Previous();
                Expr right = Comparison();
                expr = new Expr.Binary(expr, oper, right);
            }

            return expr;
        }

        private Expr Comparison()
        {
            Expr expr = Addition();

            while(Match(TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual))
            {
                Token oper = Previous();
                Expr right = Addition();
                expr = new Expr.Binary(expr, oper, right);
            }

            return expr;
        }

        private Expr Addition()
        {
            Expr expr = Multiplication();

            while(Match(TokenType.Minus, TokenType.Plus))
            {
                Token oper = Previous();
                Expr right = Multiplication();
                expr = new Expr.Binary(expr, oper, right);
            }

            return expr;
        }

        private Expr Multiplication()
        {
            Expr expr = Unary();

            while (Match(TokenType.Slash, TokenType.Star))
            {
                Token oper = Previous();
                Expr right = Unary();
                expr = new Expr.Binary(expr, oper, right);
            }

            return expr;
        }

        private Expr Unary()
        {
            Expr expr = null;

            if (Match(TokenType.Bang, TokenType.Minus))
            {
                Token oper = Previous();
                Expr right = Unary();
                expr = new Expr.Unary(oper, right);
            }
            else
            {
                expr = Call();
            }

            return expr;
        }

        private Expr Call()
        {
            Expr expr = Primary();

            while (true)
            {
                if (Match(TokenType.LeftParen))
                {
                    expr = FinishCall(expr);
                }
                else if (Match(TokenType.Dot))
                {
                    Token name = Consume(TokenType.Identifier, "Expect property name after '.'.");
                    expr = new Expr.Get(expr, name);
                }
                else
                {
                    break;
                }
            }

            return expr;
        }

        private Expr FinishCall(Expr callee)
        {
            List<Expr> arguments = new List<Expr>();
            if (!Check(TokenType.RightParen))
            {
                // do-while loop ensures we parse at least one argument
                do
                {
                    if (arguments.Count > 7)
                    {
                        Error(Peek(), "Cannot have more than 8 arguments.");
                    }

                    arguments.Add(Expression());
                } while (Match(TokenType.Comma));
            }

            Token paren = Consume(TokenType.RightParen, "Expect ')' after arguments.");

            return new Expr.Call(callee, paren, arguments);
        }

        private Expr Primary()
        {
            Expr expr = null;

            if (Match(TokenType.False))
            {
                expr = new Expr.Literal(false);
            }
            else if (Match(TokenType.True))
            {
                expr = new Expr.Literal(true);
            }
            else if (Match(TokenType.Nil))
            {
                expr = new Expr.Literal(null);
            }
            else if (Match(TokenType.Number, TokenType.String))
            {
                expr = new Expr.Literal(Previous().literal);
            }
            else if (Match(TokenType.Super))
            {
                Token keyword = Previous();
                Consume(TokenType.Dot, "Expect '.' after 'super'.");
                Token method = Consume(TokenType.Identifier, "Expect superclass method name.");
                expr = new Expr.Super(keyword, method);
            }
            else if (Match(TokenType.This))
            {
                expr = new Expr.This(Previous());
            }
            else if (Match(TokenType.Identifier))
            {
                expr = new Expr.Variable(Previous());
            }
            else if (Match(TokenType.LeftParen))
            {
                expr = Expression();
                Consume(TokenType.RightParen, "Expect ')' after expression.");
                expr = new Expr.Grouping(expr);
            }
            else
            {
                throw Error(Peek(), "Expect expression.");
            }

            return expr;
        }

        private bool Match(params TokenType[] types)
        {
            bool ret = false;

            foreach (TokenType t in types)
            {
                if (Check(t))
                {
                    Advance();
                    ret = true;
                    break;
                }
            }

            return ret;
        }

        private Token Consume(TokenType type, string message)
        {
            if (Check(type))
            {
                return Advance();
            }

            throw Error(Peek(), message);
        }

        private ParseError Error(Token token, string message)
        {
            Lox.Error(token, message);
            return new ParseError();
        }

        private void Synchronize()
        {
            Advance();

            while (!IsAtEnd() && !StatementBoundary())
            {
                Advance();
            }
        }

        private bool StatementBoundary()
        {
            TokenType peek = Peek().type;

            return ((Previous().type == TokenType.Semicolon) ||
                (peek == TokenType.Class ) ||
                (peek == TokenType.Fun   ) ||
                (peek == TokenType.Var   ) ||
                (peek == TokenType.For   ) ||
                (peek == TokenType.If    ) ||
                (peek == TokenType.While ) ||
                (peek == TokenType.Print ) ||
                (peek == TokenType.Return));
        }

        private bool Check(TokenType tokenType)
        {
            bool ret = false;
            
            if (!IsAtEnd())
            {
                ret = (Peek().type == tokenType);
            }

            return ret;
        }

        private Token Advance()
        {
            if (!IsAtEnd())
            {
                current++;
            }

            return Previous();
        }

        private bool IsAtEnd()
        {
            return Peek().type == TokenType.Eof;
        }

        private Token Peek()
        {
            return tokens[current];
        }
        
        private Token Previous()
        {
            return tokens[current - 1];
        }
    }
}
