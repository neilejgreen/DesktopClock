using System.Text.RegularExpressions;

namespace DesktopClock.Utilities;

/// <summary>
/// Formatter that intelligently routes DateTime formatting based on the format string:
/// - "N" (with optional flags I, C, O) → Natural language formatter
///   - I = include "it's" prefix
///   - C = capitalize first letter
///   - O = use "o'clock" for exact hours
///   Examples: "N", "NICO", "NIC", "NO", "NI", "NC", etc.
/// - Contains "{...}" → Tokenizer
/// - Otherwise → Default system formatter
/// </summary>
public partial class DateTimeFormatter
{
    [GeneratedRegex( "{([^{}]+)}" )]
    private static partial Regex TokenizerRegex();

    private readonly StringComparison _ignoreCase = StringComparison.OrdinalIgnoreCase;

    private readonly NaturalLanguageTimeFormatter _naturalLanguageFormatter = new();

    public string Format( DateTime dateTime, string format )
    {
        try
        {
            // Handle null or empty format - use system default
            if ( string.IsNullOrWhiteSpace( format ) )
            {
                return dateTime.ToString();
            }

            // Route to natural language formatter
            if ( format.StartsWith( "N", _ignoreCase ) )
            {
                return FormatNaturalLanguage( dateTime, format );
            }

            // Route to tokenizer for formats with {...}
            if ( format.Contains( '{' ) && format.Contains( '}' ) )
            {
                return FormatWithTokenizer( dateTime, format );
            }

            // Default to system formatter for standard format strings
            return dateTime.ToString( format );
        }
        catch
        {
            // Fall back to the default format on any exception
            return "DateTime format not recognized.";
        }
    }


    private string FormatNaturalLanguage( DateTime dateTime, string format ) =>
        _naturalLanguageFormatter.Format( dateTime, new(
            Its: format.Contains( 'I', _ignoreCase ),
            CapitalizeFirst: format.Contains( 'U', _ignoreCase ),
            UseOClock: format.Contains( 'O', _ignoreCase )
        ) );

    private string FormatWithTokenizer( DateTime dateTime, string format ) =>
        TokenizerRegex().Replace( format, m =>
            dateTime.ToString( m.Value[ 1..^1 ] ) );
}
