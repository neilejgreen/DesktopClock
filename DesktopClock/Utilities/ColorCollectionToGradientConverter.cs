using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DesktopClock.Utilities;

/// <summary>
/// Converts a collection of colors to a LinearGradientBrush with equal spacing.
/// Returns transparent brush if collection is empty.
/// </summary>
public class ColorCollectionToGradientConverter : IValueConverter
{
    public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
    {
        // If no gradient colors are set, use transparent
        if ( value is not ObservableCollection<Color> colors || colors.Count == 0 )
        {
            return new SolidColorBrush( Colors.Transparent );
        }

        // If only one color, return a solid brush
        if ( colors.Count == 1 )
        {
            return new SolidColorBrush( colors[ 0 ] );
        }

        // Create a linear gradient from left to right with equal spacing
        var gradientBrush = new LinearGradientBrush {
            StartPoint = new( 0, 0.5 ),
            EndPoint = new( 1, 0.5 )
        };

        // Calculate equal offsets for each color
        double step = 1.0 / ( colors.Count - 1 );
        for ( int i = 0; i < colors.Count; i++ )
        {
            gradientBrush.GradientStops.Add( new GradientStop( colors[ i ], i * step ) );
        }

        return gradientBrush;
    }

    public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
    {
        throw new NotImplementedException();
    }
}
