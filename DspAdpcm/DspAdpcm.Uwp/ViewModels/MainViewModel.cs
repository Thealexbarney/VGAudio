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
using DspAdpcm.Encode.Adpcm.Formats;
using DspAdpcm.Encode.Pcm;
using DspAdpcm.Encode.Pcm.Formats;
using GalaSoft.MvvmLight.Command;
using PropertyChanged;

namespace DspAdpcm.Uwp.ViewModels
{
    [ImplementPropertyChanged]
    public class MainViewModel
    {
        public MainState State { get; set; } = MainState.Opening;
        public AdpcmTypes SelectedFileType { get; set; } = AdpcmTypes.Dsp;

        public bool StateOpening => State == MainState.Opening;
        public bool StateOpenedPcm => State == MainState.OpenedPcm;
        public bool StateEncoded => State == MainState.Encoded;

        public double Time { get; set; }
        public double Samples { get; set; }
        public double SamplesPerMs => Samples / (Time * 1000);

        public static Dictionary<AdpcmTypes, AudioFileType> FileTypes { get; } = new Dictionary<AdpcmTypes, AudioFileType>()
        {
            [AdpcmTypes.Dsp] = new AudioFileType("DSP", ".dsp", "Nintendo DSP ACPCM Audio File",
                fileName => new Dsp(new FileStream(fileName, FileMode.Open)).AudioStream,
                adpcmStream => new Dsp(adpcmStream).GetFile()),
            [AdpcmTypes.Brstm] = new AudioFileType("BRSTM", ".brstm", "BRSTM Audio File",
                fileName => new Brstm(new FileStream(fileName, FileMode.Open)).AudioStream,
                adpcmStream => new Brstm(adpcmStream).GetFile())
        };

        public string InPath { get; set; }

        public IPcmStream PcmStream { get; set; }
        public IAdpcmStream AdpcmStream { get; set; }
        public string InputFileType { get; set; }
        public bool Looping { get; set; }
        public int LoopStart { get; set; }
        public int LoopEnd { get; set; }

        public RelayCommand EncodeCommand { get; set; }
        public ICommand SaveFileCommand { get; set; }
        public ICommand OpenFileCommand { get; set; }
        private bool Encoding { get; set; }

        public MainViewModel()
        {
            EncodeCommand = new RelayCommand(Encode, CanEncode);
            SaveFileCommand = new RelayCommand(SaveFile);
            OpenFileCommand = new RelayCommand(OpenFile);
        }

        private bool CanEncode() => PcmStream != null && !Encoding;

        private async void OpenFile()
        {
            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.ComputerFolder
            };

            picker.FileTypeFilter.Add(".wav");
            picker.FileTypeFilter.Add(".dsp");
            picker.FileTypeFilter.Add(".brstm");

            StorageFile file = await picker.PickSingleFileAsync();

            if (file == null) return;

            try
            {
                StorageApplicationPermissions.
                    FutureAccessList.AddOrReplace("Input File", file);
                InPath = file.Path;

                switch (file.FileType)
                {
                    case ".wav":
                        PcmStream = await Task.Run(() => new Wave(new FileStream(file.Path, FileMode.Open)).AudioStream);
                        AdpcmStream = null;
                        State = MainState.OpenedPcm;
                        InputFileType = ".wav";
                        EncodeCommand.RaiseCanExecuteChanged();
                        break;
                    default:
                        AdpcmTypes fileType = FileTypes.First(x => x.Value.Extension == file.FileType).Key;
                        AdpcmStream = await Task.Run(() => FileTypes[fileType].OpenFunc(file.Path));
                        PcmStream = null;
                        State = MainState.Encoded;
                        if (AdpcmStream.Looping)
                        {
                            Looping = true;
                            LoopStart = AdpcmStream.LoopStart;
                            LoopEnd = AdpcmStream.LoopEnd;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                var messageDialog = new MessageDialog(ex.Message, "Unable to parse file");
                await messageDialog.ShowAsync();
            }
        }

        public async void Encode()
        {
            var watch = new Stopwatch();
            IAdpcmStream adpcm = null;
            Encoding = true;
            EncodeCommand.RaiseCanExecuteChanged();
            try
            {
                await Task.Run(() =>
                {
                    watch.Start();
                    adpcm = DspAdpcm.Encode.Adpcm.Encode.PcmToAdpcmParallel(PcmStream);
                    watch.Stop();
                });

                Time = watch.Elapsed.TotalSeconds;
                Samples = adpcm.NumSamples;
            }
            catch (Exception ex)
            {
                var messageDialog = new MessageDialog(ex.Message, "Error");
                await messageDialog.ShowAsync();
            }
            finally
            {
                Encoding = false;
                EncodeCommand.RaiseCanExecuteChanged();
            }
            AdpcmStream = adpcm;
            State = MainState.Encoded;
            PcmStream = null;
        }

        private async void SaveFile()
        {
            var savePicker = new FileSavePicker
            {
                SuggestedFileName = Path.ChangeExtension(Path.GetFileName(InPath), FileTypes[SelectedFileType].Extension),
            };
            savePicker.FileTypeChoices.Add(FileTypes[SelectedFileType].Description, new List<string> { FileTypes[SelectedFileType].Extension });

            try
            {
                StorageFile saveFile = await savePicker.PickSaveFileAsync();

                if (saveFile == null) return;

                if (Looping)
                {
                    AdpcmStream.SetLoop(LoopStart, LoopEnd);
                }

                IEnumerable<byte> file = FileTypes[SelectedFileType].GetFileFunc(AdpcmStream);

                await FileIO.WriteBytesAsync(saveFile, file.ToArray());
            }
            catch (Exception ex)
            {
                var messageDialog = new MessageDialog(ex.Message, "Error writing file");
                await messageDialog.ShowAsync();
            }
        }

        public enum MainState
        {
            Opening,
            OpenedPcm,
            Encoded
        }

        public enum AdpcmTypes
        {
            Dsp,
            Brstm
        }
    }

    public class AudioFileType
    {
        public string DisplayName { get; }
        public string Extension { get; }
        public string Description { get; }
        public Func<string, IAdpcmStream> OpenFunc { get; }
        public Func<IAdpcmStream, IEnumerable<byte>> GetFileFunc { get; }

        public AudioFileType(string displayName, string extension, string description, Func<string, IAdpcmStream> openFunc, Func<IAdpcmStream, IEnumerable<byte>> getFileFunc)
        {
            DisplayName = displayName;
            Extension = extension;
            Description = description;
            OpenFunc = openFunc;
            GetFileFunc = getFileFunc;
        }
    }
}
