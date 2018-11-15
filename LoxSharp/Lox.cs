using System;
using System.IO;
using System.Collections.Generic;

namespace LoxSharp
{
    class Lox
    {
        private static Interpreter interpreter = new Interpreter();

        private static bool hadError = false;
        private static bool hadRuntimeError = false;

        static void Main(string[] args)
        {
            if (args.Length > 1)
            {
                Console.WriteLine("Usage: cslox [script]");
            }
            else if (args.Length == 1)
            {
                RunFile(args[0]);
            }
            else
            {
                RunPrompt();
            }
        }

        private static void RunFile(string path)
        {
            string text = File.ReadAllText(Path.GetFullPath(path));
            Run(text, false);

            // Indicate an error in the exit code.
            if (hadError)
            {
                System.Environment.Exit(65);
            }

            if (hadRuntimeError)
            {
                System.Environment.Exit(70);
            }
        }

        private static void RunPrompt()
        {
            BufferedStream stream = new BufferedStream(Console.OpenStandardInput());
            StreamReader reader = new StreamReader(stream);

            while(true)
            {
                Console.Write(">> ");
                Run(reader.ReadLine(), true);

                hadError = false;
            }
        }

        private static void Run(string source, bool repl)
        {
            Scanner scanner = new Scanner(source);
            List<Token> tokens = scanner.ScanTokens();

            Parser parser = new Parser(tokens, repl);
            List<Stmt> statements = parser.Parse();

            // Stop if there was a syntax error
            if (hadError) return;

            Resolver resolver = new Resolver(interpreter);
            resolver.Resolve(statements);
            
            // Stop if there was a resolution error
            if (hadError) return;

            interpreter.Interpret(statements);
        }

        public static void Error(int line, string message)
        {
            Report(line, "", message);
        }

        public static void Error(Token token, string message)
        {
            if (token.type == TokenType.Eof)
            {
                Report(token.line, " at end", message);
            }
            else
            {
                Report(token.line, " at '" + token.lexeme + "'", message);
            }
        }

        public static void RuntimeError(RuntimeError error)
        {
            Console.Error.WriteLine(error.Message + "\n[line " + error.token.line + "]");
            hadRuntimeError = true;
        }

        static private void Report(int line, string where, string message)
        {
            Console.Error.WriteLine("[line " + line + "] Error" + where + ": " + message);
            hadError = true;
        }
    }
}
