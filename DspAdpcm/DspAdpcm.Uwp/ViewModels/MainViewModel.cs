using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using DspAdpcm.Encode.Adpcm;
using DspAdpcm.Encode.Formats;
using DspAdpcm.Encode.Wave;
using GalaSoft.MvvmLight.Command;
using PropertyChanged;

namespace DspAdpcm.Uwp.ViewModels
{
    [ImplementPropertyChanged]
    public class MainViewModel
    {
        private string _inPath;
        private string _outPath;
        public double Time { get; set; }

        public string InPath
        {
            get { return _inPath; }
            set
            {
                _inPath = value;
                EncodeCommand.RaiseCanExecuteChanged();
            }
        }

        public string OutPath
        {
            get { return _outPath; }
            set
            {
                _outPath = value;
                EncodeCommand.RaiseCanExecuteChanged();
            }
        }

        public bool Looping { get; set; }
        public int LoopStart { get; set; }
        public int LoopEnd { get; set; }

        public StorageFile SaveFile { get; set; }
        public RelayCommand EncodeCommand { get; set; }
        public ICommand InputPathCommand { get; set; }
        public ICommand OutputPathCommand { get; set; }
        private bool _encoding;

        public MainViewModel()
        {
            EncodeCommand = new RelayCommand(Encode, CanEncode);
            InputPathCommand = new RelayCommand(SetInputPath);
            OutputPathCommand = new RelayCommand(SetOutputPath);
        }

        private bool CanEncode() => InPath != null &&
                                    OutPath != null &&
                                    !_encoding;

        private async void SetInputPath()
        {
            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.ComputerFolder
            };
            picker.FileTypeFilter.Add(".wav");

            StorageFile file = await picker.PickSingleFileAsync();
            if (file == null) return;

            StorageApplicationPermissions.
                FutureAccessList.AddOrReplace("Input File", file);
            InPath = file.Path;
        }

        private async void SetOutputPath()
        {
            var savePicker = new FileSavePicker
            {
                SuggestedFileName = Path.GetFileNameWithoutExtension(InPath) + ".dsp"
            };
            savePicker.FileTypeChoices.Add("Nintendo DSP ACPCM Audio File", new List<string> { ".dsp" });

            StorageFile file = await savePicker.PickSaveFileAsync();

            if (file == null) return;

            SaveFile = file;
            OutPath = file.Path;
        }

        public async void Encode()
        {
            AdpcmStream adpcm = null;
            WaveStream wave;
            var watch = new Stopwatch();

            _encoding = true;
            EncodeCommand.RaiseCanExecuteChanged();
            try
            {
                await Task.Run(() =>
                {
                    using (var file = new FileStream(InPath, FileMode.Open))
                    {
                        wave = new WaveStream(file);
                    }

                    watch.Start();

                    adpcm = DspAdpcm.Encode.Adpcm.Encode.PcmToAdpcm(wave);
                    if (Looping)
                    {
                        adpcm.SetLoop(LoopStart, LoopEnd);
                    }

                    watch.Stop();
                });

                Time = watch.Elapsed.TotalSeconds;

                var dsp = new Dsp(adpcm);

                await FileIO.WriteBytesAsync(SaveFile, dsp.GetFile().ToArray());
            }
            catch (Exception ex)
            {
                var messageDialog = new MessageDialog(ex.Message);
                await messageDialog.ShowAsync();
            }
            finally
            {
                _encoding = false;
                EncodeCommand.RaiseCanExecuteChanged();
            }
        }
    }
}
