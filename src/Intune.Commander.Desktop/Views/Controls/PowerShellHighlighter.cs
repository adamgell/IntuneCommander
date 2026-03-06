using System;
using System.Collections.Generic;
using Avalonia.Media;

namespace Intune.Commander.Desktop.Views.Controls;

/// <summary>
/// Simple PowerShell tokenizer that produces colored tokens for syntax highlighting.
/// Uses VS Code Dark+ theme colors.
/// </summary>
public static class PowerShellHighlighter
{
    public enum TokenType
    {
        Default,
        Comment,
        String,
        Variable,
        Keyword,
        Cmdlet,
        Number,
        Parameter,
        Type
    }

    public readonly record struct Token(string Text, TokenType Type);

    // VS Code Dark+ theme colors
    private static readonly Color CommentColor = Color.Parse("#6A9955");
    private static readonly Color StringColor = Color.Parse("#CE9178");
    private static readonly Color VariableColor = Color.Parse("#9CDCFE");
    private static readonly Color KeywordColor = Color.Parse("#569CD6");
    private static readonly Color CmdletColor = Color.Parse("#DCDCAA");
    private static readonly Color NumberColor = Color.Parse("#B5CEA8");
    private static readonly Color ParameterColor = Color.Parse("#9CDCFE");
    private static readonly Color TypeColor = Color.Parse("#4EC9B0");
    private static readonly Color DefaultColor = Color.Parse("#D4D4D4");

    private static readonly HashSet<string> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "if", "else", "elseif", "foreach", "for", "while", "do",
        "switch", "break", "continue", "return", "function", "param",
        "begin", "process", "end", "try", "catch", "finally", "throw",
        "exit", "trap", "filter", "class", "enum", "using", "in",
        "where", "select", "true", "false", "null", "not"
    };

    private static readonly HashSet<string> Operators = new(StringComparer.OrdinalIgnoreCase)
    {
        "-eq", "-ne", "-lt", "-gt", "-ge", "-le",
        "-like", "-notlike", "-match", "-notmatch",
        "-contains", "-notcontains", "-in", "-notin",
        "-not", "-and", "-or", "-band", "-bor", "-bnot", "-bxor",
        "-is", "-isnot", "-as", "-replace", "-split", "-join",
        "-f"
    };

    public static Color GetColor(TokenType type) => type switch
    {
        TokenType.Comment => CommentColor,
        TokenType.String => StringColor,
        TokenType.Variable => VariableColor,
        TokenType.Keyword => KeywordColor,
        TokenType.Cmdlet => CmdletColor,
        TokenType.Number => NumberColor,
        TokenType.Parameter => ParameterColor,
        TokenType.Type => TypeColor,
        _ => DefaultColor,
    };

    public static List<Token> Tokenize(string code)
    {
        var tokens = new List<Token>();
        if (string.IsNullOrEmpty(code)) return tokens;

        int i = 0;
        int len = code.Length;

        while (i < len)
        {
            char c = code[i];

            // Block comment: <# ... #>
            if (c == '<' && i + 1 < len && code[i + 1] == '#')
            {
                int end = code.IndexOf("#>", i + 2, StringComparison.Ordinal);
                end = end < 0 ? len : end + 2;
                tokens.Add(new Token(code[i..end], TokenType.Comment));
                i = end;
                continue;
            }

            // Single-line comment: # to end of line
            if (c == '#')
            {
                int end = code.IndexOf('\n', i);
                if (end < 0) end = len;
                tokens.Add(new Token(code[i..end], TokenType.Comment));
                i = end;
                continue;
            }

            // Double-quoted string (supports backtick escapes)
            if (c == '"')
            {
                int end = i + 1;
                while (end < len)
                {
                    if (code[end] == '`' && end + 1 < len) { end += 2; continue; }
                    if (code[end] == '"') { end++; break; }
                    end++;
                }
                if (end > len) end = len;
                tokens.Add(new Token(code[i..end], TokenType.String));
                i = end;
                continue;
            }

            // Single-quoted string ('' is the escape for a literal quote)
            if (c == '\'')
            {
                int end = i + 1;
                while (end < len)
                {
                    if (code[end] == '\'' && end + 1 < len && code[end + 1] == '\'') { end += 2; continue; }
                    if (code[end] == '\'') { end++; break; }
                    end++;
                }
                if (end > len) end = len;
                tokens.Add(new Token(code[i..end], TokenType.String));
                i = end;
                continue;
            }

            // Variable: $identifier or $env:NAME etc.
            if (c == '$')
            {
                int end = i + 1;
                while (end < len && (char.IsLetterOrDigit(code[end]) || code[end] == '_' || code[end] == ':'))
                    end++;
                tokens.Add(new Token(code[i..end], TokenType.Variable));
                i = end;
                continue;
            }

            // Type literal: [TypeName] or [System.String]
            if (c == '[')
            {
                int close = code.IndexOf(']', i + 1);
                if (close > 0 && close - i < 60 && !code[i..(close + 1)].Contains('\n'))
                {
                    tokens.Add(new Token(code[i..(close + 1)], TokenType.Type));
                    i = close + 1;
                    continue;
                }
                tokens.Add(new Token("[", TokenType.Default));
                i++;
                continue;
            }

            // Parameter or comparison operator: -Name, -eq, etc.
            if (c == '-' && i + 1 < len && char.IsLetter(code[i + 1]))
            {
                int end = i + 1;
                while (end < len && (char.IsLetterOrDigit(code[end]) || code[end] == '_'))
                    end++;
                string word = code[i..end];
                tokens.Add(new Token(word, Operators.Contains(word) ? TokenType.Keyword : TokenType.Parameter));
                i = end;
                continue;
            }

            // Word: keyword, cmdlet, or identifier
            if (char.IsLetter(c) || c == '_')
            {
                int end = i + 1;
                while (end < len && (char.IsLetterOrDigit(code[end]) || code[end] == '_' || code[end] == '-'))
                    end++;
                string word = code[i..end];

                if (Keywords.Contains(word))
                    tokens.Add(new Token(word, TokenType.Keyword));
                else if (word.Contains('-') && word.Length > 3)
                    tokens.Add(new Token(word, TokenType.Cmdlet));
                else
                    tokens.Add(new Token(word, TokenType.Default));

                i = end;
                continue;
            }

            // Number (integer, decimal, with optional KB/MB/GB suffix)
            if (char.IsDigit(c))
            {
                int end = i + 1;
                while (end < len && (char.IsDigit(code[end]) || code[end] == '.'))
                    end++;
                // PowerShell size suffixes
                if (end < len && code[end] is 'K' or 'M' or 'G' or 'T' or 'P')
                {
                    end++;
                    if (end < len && code[end] == 'B') end++;
                }
                tokens.Add(new Token(code[i..end], TokenType.Number));
                i = end;
                continue;
            }

            // Everything else: operators, punctuation, whitespace
            tokens.Add(new Token(c.ToString(), TokenType.Default));
            i++;
        }

        return tokens;
    }
}
