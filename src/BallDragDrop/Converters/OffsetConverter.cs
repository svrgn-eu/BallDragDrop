using System;
using System.Globalization;
using System.Windows.Data;

namespace BallDragDrop.Converters
{
    /// <summary>
    /// Simple converter to offset a value by a specified amount
    /// </summary>
    public class OffsetConverter : IValueConverter
    {
        #region Properties

        /// <summary>
        /// Singleton instance of the OffsetConverter
        /// </summary>
        public static readonly OffsetConverter Instance = new OffsetConverter();

        #endregion Properties

        #region Methods

        /// <summary>
        /// Converts a value by adding the specified offset
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <param name="targetType">The target type</param>
        /// <param name="parameter">The offset amount as a string</param>
        /// <param name="culture">The culture info</param>
        /// <returns>The value plus the offset, or the original value if conversion fails</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue && parameter is string parameterString)
            {
                // Use invariant culture to avoid locale-specific parsing issues
                if (double.TryParse(parameterString, NumberStyles.Float, CultureInfo.InvariantCulture, out double offset))
                {
                    return doubleValue + offset;
                }
            }
            return value;
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