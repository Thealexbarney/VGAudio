using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using DspAdpcm.Lib.Adpcm.Formats;
using DspAdpcm.Uwp.ViewModels;

namespace DspAdpcm.Uwp.Views
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
            {
                return Visibility.Collapsed;
            }

            return (bool)value ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class DoubleToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
            {
                return Visibility.Collapsed;
            }

            return (double)value == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class TrackTypeEnumToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string param = parameter as string;
            if (param == null)
                return DependencyProperty.UnsetValue;
            if (Enum.IsDefined(value.GetType(), value) == false)
                return DependencyProperty.UnsetValue;


            object paramValue = Enum.Parse(value.GetType(), param);
            return paramValue.Equals(value);
        }


        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            string param = parameter as string;
            if (parameter == null)
                return DependencyProperty.UnsetValue;

            if (!(bool)value)
            {
                return DependencyProperty.UnsetValue;
            }
            return Enum.Parse(typeof(BrstmTrackType), param);
        }
    }

    public class SeekTableTypeEnumToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string param = parameter as string;
            if (param == null)
                return DependencyProperty.UnsetValue;
            if (Enum.IsDefined(value.GetType(), value) == false)
                return DependencyProperty.UnsetValue;


            object paramValue = Enum.Parse(value.GetType(), param);
            return paramValue.Equals(value);
        }


        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            string param = parameter as string;
            if (parameter == null)
                return DependencyProperty.UnsetValue;

            if (!(bool) value)
            {
                return DependencyProperty.UnsetValue;
            }
                return Enum.Parse(typeof(BrstmSeekTableType), param);
        }
    }

    public class BrstmToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
            {
                return Visibility.Collapsed;
            }

            return (MainViewModel.AdpcmTypes)value == MainViewModel.AdpcmTypes.Brstm ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class BcstmToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
            {
                return Visibility.Collapsed;
            }

            return (MainViewModel.AdpcmTypes)value == MainViewModel.AdpcmTypes.Bcstm ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
