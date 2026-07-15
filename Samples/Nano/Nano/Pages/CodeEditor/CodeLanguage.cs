using Rayo.Rendering;

namespace Nano.Pages.CodeEditor;

/// <summary>Defines the tokenization rules used by a <see cref="CodeEditor"/>.</summary>
public interface ICodeLanguage
{
    IEnumerable<CodeToken> Tokenize(string line);
}

public readonly record struct CodeToken(string Text, CodeTokenKind Kind);

public enum CodeTokenKind
{
    Plain,
    Keyword,
    String,
    Number,
    Comment,
    Builtin
}

/// <summary>Initial Lua syntax definition. Add another <see cref="ICodeLanguage"/> for a new language.</summary>
public sealed class LuaCodeLanguage : ICodeLanguage
{
    private static readonly HashSet<string> s_keywords =
    [
        "and", "break", "do", "else", "elseif", "end", "false", "for", "function", "goto",
        "if", "in", "local", "nil", "not", "or", "repeat", "return", "then", "true", "until", "while"
    ];

    private static readonly HashSet<string> s_builtins =
    ["assert", "ipairs", "pairs", "print", "require", "table", "tonumber", "tostring", "type"];

    public IEnumerable<CodeToken> Tokenize(string line)
    {
        for (var index = 0; index < line.Length;)
        {
            if (index + 1 < line.Length && line[index] == '-' && line[index + 1] == '-')
            {
                yield return new CodeToken(line[index..], CodeTokenKind.Comment);
                yield break;
            }

            if (line[index] is '\'' or '"')
            {
                var quote = line[index++];
                var start = index - 1;
                while (index < line.Length && line[index] != quote)
                {
                    index += line[index] == '\\' && index + 1 < line.Length ? 2 : 1;
                }

                if (index < line.Length) index++;
                yield return new CodeToken(line[start..index], CodeTokenKind.String);
                continue;
            }

            if (char.IsLetter(line[index]) || line[index] == '_')
            {
                var start = index++;
                while (index < line.Length && (char.IsLetterOrDigit(line[index]) || line[index] == '_')) index++;
                var word = line[start..index];
                yield return new CodeToken(word, s_keywords.Contains(word)
                    ? CodeTokenKind.Keyword
                    : s_builtins.Contains(word) ? CodeTokenKind.Builtin : CodeTokenKind.Plain);
                continue;
            }

            if (char.IsDigit(line[index]))
            {
                var start = index++;
                while (index < line.Length && (char.IsDigit(line[index]) || line[index] == '.')) index++;
                yield return new CodeToken(line[start..index], CodeTokenKind.Number);
                continue;
            }

            yield return new CodeToken(line[index++].ToString(), CodeTokenKind.Plain);
        }
    }
}
