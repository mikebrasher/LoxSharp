using System;
using System.Collections.Generic;

namespace LoxSharp
{
    class Scanner
    {
        private readonly string source;
        private readonly List<Token> tokens = new List<Token>();
        private static readonly Dictionary<string, TokenType> Keywords;

        private int start = 0;
        private int current = 0;
        private int line = 1;

        public Scanner(string source)
        {
            this.source = source;
        }

        static Scanner()
        {
            Keywords = new Dictionary<string, TokenType>
            {
                {"and", TokenType.And},
                {"class", TokenType.Class},
                {"else", TokenType.Else},
                {"false", TokenType.False},
                {"for", TokenType.For},
                {"fun", TokenType.Fun},
                {"if", TokenType.If},
                {"nil", TokenType.Nil},
                {"or", TokenType.Or},
                {"print", TokenType.Print},
                {"return", TokenType.Return},
                {"super", TokenType.Super},
                {"this", TokenType.This},
                {"true", TokenType.True},
                {"var", TokenType.Var},
                {"while", TokenType.While}
            };            
        }

        public List<Token> ScanTokens()
        {
            while (!IsAtEnd())
            {
                // We are at the beginning of the next lexeme
                start = current;
                ScanNextToken();
            }

            tokens.Add(new LoxSharp.Token(TokenType.Eof, "", null, line));

            return tokens;
        }

        private bool IsAtEnd()
        {
            return current >= source.Length;
        }

        private bool IsDigit(char c)
        {
            return ((c >= '0') && (c <= '9'));
        }

        private bool IsAlpha(char c)
        {
            bool lower = (c >= 'a') && (c <= 'z');
            bool upper = (c >= 'A') && (c <= 'Z');
            bool under = c == '_';

            return (lower || upper || under);
        }

        private bool IsAlphaNumeric(char c)
        {
            return (IsAlpha(c) || IsDigit(c));
        }

        private void ScanNextToken()
        {
            char c = Advance();
            switch(c)
            {
                case '(':
                    AddToken(TokenType.LeftParen);
                    break;

                case ')':
                    AddToken(TokenType.RightParen);
                    break;

                case '{':
                    AddToken(TokenType.LeftBrace);
                    break;

                case '}':
                    AddToken(TokenType.RightBrace);
                    break;

                case ',':
                    AddToken(TokenType.Comma);
                    break;

                case '.':
                    AddToken(TokenType.Dot);
                    break;

                case '-':
                    AddToken(TokenType.Minus);
                    break;

                case '+':
                    AddToken(TokenType.Plus);
                    break;

                case ';':
                    AddToken(TokenType.Semicolon);
                    break;

                case '*':
                    AddToken(TokenType.Star);
                    break;

                case '!':
                    AddToken(Match('=') ? TokenType.BangEqual : TokenType.Bang);
                    break;

                case '=':
                    AddToken(Match('=') ? TokenType.EqualEqual : TokenType.Equal);
                    break;

                case '<':
                    AddToken(Match('=') ? TokenType.LessEqual : TokenType.Less);
                    break;

                case '>':
                    AddToken(Match('=') ? TokenType.GreaterEqual : TokenType.Greater);
                    break;

                case '/':
                    if (Match('/'))
                    {
                        // A comment goes until the end of the line
                        while ((Peek() != '\n') && !IsAtEnd())
                        {
                            Advance();
                        }
                    }
                    else if (Match('*'))
                    {
                        ParseBlockComment();
                    }
                    else
                    {
                        AddToken(TokenType.Slash);
                    }
                    break;

                case ' ':
                case '\r':
                case '\t':
                    //ignore whitespace
                    break;

                case '\n':
                    line++;
                    break;

                case '"':
                    ParseString();
                    break;

                default:
                    if (IsDigit(c))
                    {
                        ParseNumber();
                    }
                    else if (IsAlpha(c))
                    {
                        ParseIdentifier();
                    }
                    else
                    {
                        Lox.Error(line, "Unexpected character.");
                    }
                    break;
            }
        }

        private char Advance()
        {
            current++;
            return source[current-1];
        }

        private void AddToken(TokenType type)
        {
            AddToken(type, null);
        }

        private void AddToken(TokenType type, object literal)
        {
            string text = source.Substring(start, current - start);
            tokens.Add(new Token(type, text, literal, line));
        }

        private bool Match(char expected)
        {
            bool ret = false;

            if (!IsAtEnd())
            {
                if (source[current] == expected)
                {
                    current++;
                    ret = true;
                }
            }

            return ret;
        }

        private char Peek()
        {
            char ret = '\0';

            if (current < source.Length)
            {
                ret = source[current];
            }

            return ret;
        }

        private char PeekNext()
        {
            char ret = '\0';

            if ( current + 1 < source.Length)
            {
                ret = source[current + 1];
            }

            return ret;
        }

        private void ParseBlockComment()
        {
            while (true)
            {
                if (IsAtEnd())
                {
                    Lox.Error(line, "Unterminated block comment");
                    break;
                }
                else
                {
                    char c = Advance();

                    if ( c == '*')
                    {
                        if (Peek() == '/')
                        {
                            Advance(); // consume trailing slash
                            break;
                        }
                    }
                    else if ( c == '\n')
                    {
                        line++;
                    }
                }
            }
        }

        private void ParseString()
        {
            while (true)
            {
                if (IsAtEnd())
                {
                    Lox.Error(line, "Unterminated string.");
                    break;
                }
                else
                {
                    char c = Advance();

                    if (c == '"') // The closing "
                    {
                        // Trim the surrounding quotes
                        string value = source.Substring(start + 1, current - start - 2);
                        AddToken(TokenType.String, value);
                        break;
                    }
                    else if (c == '\n')
                    {
                        line++;
                    }
                }
            }
        }

        private void ParseNumber()
        {
            while (IsDigit(Peek()))
            {
                Advance();
            }

            // Look for the fractional part
            if ((Peek() == '.') && IsDigit(PeekNext()))
            {
                Advance(); // Consume the '.'

                while (IsDigit(Peek()))
                {
                    Advance();
                }
            }

            AddToken(TokenType.Number, double.Parse(source.Substring(start, current - start)));
        }

        private void ParseIdentifier()
        {
            while (IsAlphaNumeric(Peek()))
            {
                Advance();
            }

            // See if the identifier is a reserved word
            string text = source.Substring(start, current - start);

            TokenType type = Keywords.ContainsKey(text) ? Keywords[text] : TokenType.Identifier;
            AddToken(type);
        }
    }

    public class Token
    {
        public readonly TokenType type;
        public readonly string lexeme;
        public readonly object literal;
        public readonly int line;

        public Token(TokenType type, string lexeme, object literal, int line)
        {
            this.type = type;
            this.lexeme = lexeme;
            this.literal = literal;
            this.line = line;
        }

        public override string ToString()
        {
            return type + " " + lexeme + " " + literal;
        }
    }

    public enum TokenType
    {
        // Single-character tokens.
        LeftParen, RightParen, LeftBrace, RightBrace,
        Comma, Dot, Minus, Plus, Semicolon, Slash, Star,

        // One or two character tokens.
        Bang, BangEqual,
        Equal, EqualEqual,
        Greater, GreaterEqual,
        Less, LessEqual,

        // Literals.
        Identifier, String, Number,

        // Keywords.
        And, Class, Else, False, Fun, For, If, Nil, Or,
        Print, Return, Super, This, True, Var, While,

        Eof
    }
}
