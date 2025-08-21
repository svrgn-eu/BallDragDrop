using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace BallDragDrop.Converters
{
    /// <summary>
    /// Converter to convert Color to Color for XAML binding (identity converter)
    /// </summary>
    public class ColorToColorConverter : IValueConverter
    {
        #region Properties

        /// <summary>
        /// Singleton instance of the ColorToColorConverter
        /// </summary>
        public static readonly ColorToColorConverter Instance = new ColorToColorConverter();

        #endregion Properties

        #region Methods

        /// <summary>
        /// Converts a Color to a Color (identity conversion)
        /// </summary>
        /// <param name="value">The Color value to convert</param>
        /// <param name="targetType">The target type</param>
        /// <param name="parameter">The parameter (not used)</param>
        /// <param name="culture">The culture info</param>
        /// <returns>The same Color value</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color color)
            {
                return color;
            }
            return Colors.Transparent;
        }

        /// <summary>
        /// Converts back (not implemented)
        /// </summary>
        /// <param name="value">The value to convert back</param>
        /// <param name="targetType">The target type</param>
        /// <param name="parameter">The parameter</param>
        /// <param name="culture">The culture info</param>
        /// <returns>Not implemented</returns>
        /// <exception cref="NotImplementedException">This method is not implemented</exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion Methods
    }
}