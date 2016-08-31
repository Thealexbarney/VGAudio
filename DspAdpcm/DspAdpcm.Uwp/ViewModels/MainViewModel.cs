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
using Windows.UI.Core;
using Windows.UI.Popups;
using DspAdpcm.Adpcm;
using DspAdpcm.Adpcm.Formats;
using DspAdpcm.Pcm;
using DspAdpcm.Pcm.Formats;
using GalaSoft.MvvmLight.Command;
using PropertyChanged;

namespace DspAdpcm.Uwp.ViewModels
{
    [ImplementPropertyChanged]
    public class MainViewModel
    {
        public MainState State { get; set; } = MainState.Opening;

        //Hacky solution due to weird issues in UWP
        //Binding problems with Dictionary<enum, object> but not Dictionary<int, object>
        public Dictionary<int, AudioFileType> FileTypesBinding { get; }
        public int SelectedFileTypeBinding
        {
            get { return (int)SelectedFileType; }
            set { SelectedFileType = (AdpcmTypes)value; }
        }

        public AdpcmTypes SelectedFileType { get; set; } = AdpcmTypes.Dsp;

        public bool StateOpening => State == MainState.Opening;
        public bool StateOpenedPcm => State == MainState.OpenedPcm;
        public bool StateEncoded => State == MainState.Encoded;

        public double Time { get; set; }
        public double Samples { get; set; }
        public double SamplesPerMs => Samples / (Time * 1000);

        public DspConfiguration DspConfiguration { get; set; } = new DspConfiguration();
        public BrstmConfiguration BrstmConfiguration { get; set; } = new BrstmConfiguration();
        public BcstmConfiguration BcstmConfiguration { get; set; } = new BcstmConfiguration();

        public Dictionary<AdpcmTypes, AudioFileType> FileTypes { get; }

        public string InPath { get; set; }

        public PcmStream PcmStream { get; set; }
        public AdpcmStream AdpcmStream { get; set; }
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
            FileTypes = new Dictionary<AdpcmTypes, AudioFileType>()
            {
                [AdpcmTypes.Dsp] = new AudioFileType("DSP", ".dsp", "Nintendo DSP ACPCM Audio File",
                    async fileName =>
                    {
                        var dsp = new Dsp(new FileStream(fileName, FileMode.Open));
                        await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                            CoreDispatcherPriority.Normal, () => { DspConfiguration = dsp.Configuration; });
                        return dsp.AudioStream;
                    },
                    adpcmStream => new Dsp(adpcmStream).GetFile()),
                [AdpcmTypes.Brstm] = new AudioFileType("BRSTM", ".brstm", "BRSTM Audio File",
                    async fileName =>
                    {
                        var brstm = new Brstm(new FileStream(fileName, FileMode.Open));
                        await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                           CoreDispatcherPriority.Normal, () => { BrstmConfiguration = brstm.Configuration; });
                        return brstm.AudioStream;
                    },
                    adpcmStream => new Brstm(adpcmStream, BrstmConfiguration).GetFile()),
                [AdpcmTypes.Bcstm] = new AudioFileType("BCSTM", ".bcstm", "BCSTM Audio File",
                    async fileName =>
                    {
                        var bcstm = new Bcstm(new FileStream(fileName, FileMode.Open));
                        await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                           CoreDispatcherPriority.Normal, () => { BcstmConfiguration = bcstm.Configuration; });
                        return bcstm.AudioStream;
                    },
                    adpcmStream => new Bcstm(adpcmStream, BcstmConfiguration).GetFile())
            };

            FileTypesBinding = FileTypes.ToDictionary(x => (int)x.Key, x => x.Value);

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
            picker.FileTypeFilter.Add(".bcstm");

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
            AdpcmStream adpcm = null;
            Encoding = true;
            EncodeCommand.RaiseCanExecuteChanged();
            try
            {
                await Task.Run(() =>
                {
                    watch.Start();
                    adpcm = DspAdpcm.Adpcm.Encode.PcmToAdpcmParallel(PcmStream);
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
            Brstm,
            Bcstm
        }
    }

    public class AudioFileType
    {
        public string DisplayName { get; }
        public string Extension { get; }
        public string Description { get; }
        public Func<string, Task<AdpcmStream>> OpenFunc { get; }
        public Func<AdpcmStream, IEnumerable<byte>> GetFileFunc { get; }

        public AudioFileType(string displayName, string extension, string description, Func<string, Task<AdpcmStream>> openFunc, Func<AdpcmStream, IEnumerable<byte>> getFileFunc)
        {
            DisplayName = displayName;
            Extension = extension;
            Description = description;
            OpenFunc = openFunc;
            GetFileFunc = getFileFunc;
        }
    }
}
