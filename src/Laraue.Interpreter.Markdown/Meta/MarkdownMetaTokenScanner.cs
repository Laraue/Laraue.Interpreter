using Laraue.Interpreter.Scanning;

namespace Laraue.Interpreter.Markdown.Meta;

public class MarkdownMetaTokenScanner(string input)
    : TokenScanner<MarkdownMetaTokenType>(input.AsMemory())
{
    protected override MarkdownMetaTokenType NewLineTokenType => MarkdownMetaTokenType.NewLine;
    protected override bool AddNewLineTokens => true;

    private bool _metaStarted;
    private bool _metaFinished;

    protected override bool TryProcessNextCharInternal(char nextChar)
    {
        switch (nextChar)
        {
            case ' ' when _metaStarted && !_metaFinished:
                AddToken(MarkdownMetaTokenType.WhiteSpace);
                return true;
            case ' ':
                // Outside of the meta section a space is the start of the content (indentation),
                // not a separator between meta tokens.
                ReadContent();
                return true;
            default:
                HandleNonWhitespaceChar(nextChar);
                return true;
        }
    }

    private void HandleNonWhitespaceChar(char nextChar)
    {
        // Try read metadata. Once the meta is finished '-' is a part of the content (a list, a rule).
        if (nextChar == '-' && !_metaFinished)
        {
            ReadMetaDelimiter();
            return;
        }
        
        // If any char consumed but the meta is not started, there is no metadata in file
        if (!_metaStarted)
        {
            ReadContent();
            return;
        }
        
        // If meta is finished just read the whole content remained
        if (_metaFinished)
        {
            ReadContent();
            return;
        }
        
        switch (nextChar)
        {
            case ':':
                AddToken(MarkdownMetaTokenType.Delimiter);
                return;
            case '[':
                AddToken(MarkdownMetaTokenType.ArrayStart);
                return;
            case ']':
                AddToken(MarkdownMetaTokenType.ArrayEnd);
                return;
            case ',':
                AddToken(MarkdownMetaTokenType.Comma);
                return;
            default:
                AddWordOrNumber();
                return;
        }
    }

    private void ReadMetaDelimiter()
    {
        if (Check(0, '-') && Check(1, '-'))
        {
            AddToken(MarkdownMetaTokenType.MetaDelimiter);
            Advance(2);
            
            if (!_metaStarted)
                _metaStarted = true;
            else
                _metaFinished = true;
            return;
        }

        // A single '-' or '--' before any meta is the beginning of the content, e.g. a list item.
        if (!_metaStarted)
        {
            ReadContent();
            return;
        }

        AddWordOrNumber();
    }

    private void ReadContent()
    {
        while (PopNextCharIf(_ => true));

        // No explicit Literal here: MarkdownMetaTokenParser reads this token's Lexeme,
        // so passing a separately-allocated copy of the same text would be wasted work
        // for what can be the whole remainder of a large document.
        AddToken(MarkdownMetaTokenType.Content);
    }
    
    private readonly char[] _nonWordsChar = [',', '\r', '\n', ':', '[', ']'];
    
    private void AddWordOrNumber()
    {
        // Check if only whitespaces remained then finish
        while (PopNextCharIf(ch => !_nonWordsChar.Contains(ch)));
        
        var text = GetCurrentScanValue().Trim();
        AddToken(MarkdownMetaTokenType.Word, text.ToString());
    }
}