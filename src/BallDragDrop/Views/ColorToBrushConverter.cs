using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace BallDragDrop.Views
{
    /// <summary>
    /// Converter to convert Color to SolidColorBrush for XAML binding
    /// </summary>
    public class ColorToBrushConverter : IValueConverter
    {
        #region Properties

        /// <summary>
        /// Singleton instance of the ColorToBrushConverter
        /// </summary>
        public static readonly ColorToBrushConverter Instance = new ColorToBrushConverter();

        #endregion Properties

        #region Construction

        #endregion Construction

        #region Methods

        /// <summary>
        /// Converts a Color to a SolidColorBrush
        /// </summary>
        /// <param name="value">The Color value to convert</param>
        /// <param name="targetType">The target type</param>
        /// <param name="parameter">The parameter (not used)</param>
        /// <param name="culture">The culture info</param>
        /// <returns>A SolidColorBrush with the specified color</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color color)
            {
                return new SolidColorBrush(color);
            }
            return new SolidColorBrush(Colors.Transparent);
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