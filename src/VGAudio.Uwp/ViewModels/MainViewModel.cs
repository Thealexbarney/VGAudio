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
using VGAudio.Containers.Bxstm;
using VGAudio.Containers.Dsp;
using VGAudio.Formats;
using VGAudio.Uwp.Audio;
using GalaSoft.MvvmLight.Command;
using PropertyChanged;
using VGAudio.Containers;
using VGAudio.Containers.Idsp;

namespace VGAudio.Uwp.ViewModels
{
    [ImplementPropertyChanged]
    public class MainViewModel
    {
        public MainState State { get; set; } = MainState.NotOpened;

        //Hacky solution due to weird issues in UWP
        //Binding problems with Dictionary<enum, object> but not Dictionary<int, object>
        public Dictionary<int, ContainerType> FileTypesBinding { get; }
        public int SelectedFileTypeBinding
        {
            get => (int)SelectedFileType;
            set => SelectedFileType = (FileType)value;
        }

        public Dictionary<BxstmCodec, string> FormatTypesBinding { get; } = new Dictionary<BxstmCodec, string>
        {
            [BxstmCodec.Adpcm] = "DSP-ADPCM",
            [BxstmCodec.Pcm16Bit] = "16-bit PCM",
            [BxstmCodec.Pcm8Bit] = "8-bit PCM"
        };

        public FileType SelectedFileType { get; set; } = FileType.Dsp;

        public bool StateNotOpened => State == MainState.NotOpened;
        public bool StateOpened => State == MainState.Opened;
        public bool StateSaved => State == MainState.Saved;

        public double Time { get; set; }
        public double Samples { get; set; }
        public double SamplesPerMs => Samples / (Time * 1000);

        public DspConfiguration DspConfiguration { get; set; } = new DspConfiguration();
        public BrstmConfiguration BrstmConfiguration { get; set; } = new BrstmConfiguration();
        public BcstmConfiguration BcstmConfiguration { get; set; } = new BcstmConfiguration();
        public BfstmConfiguration BfstmConfiguration { get; set; } = new BfstmConfiguration();
        public IdspConfiguration IdspConfiguration { get; set; } = new IdspConfiguration();

        public string InPath { get; set; }
        private IAudioFormat InFormat { get; set; }

        public AudioData AudioData { get; set; }
        public bool Looping { get; set; }
        public int LoopStart { get; set; }
        public int LoopEnd { get; set; }

        public RelayCommand SaveFileCommand { get; }
        public ICommand OpenFileCommand { get; }
        private bool Saving { get; set; }

        public MainViewModel()
        {
            FileTypesBinding = AudioInfo.Containers.Where(x => x.Value.GetWriter != null).ToDictionary(x => (int)x.Key, x => x.Value);

            SaveFileCommand = new RelayCommand(SaveFile, CanSave);
            OpenFileCommand = new RelayCommand(OpenFile);
        }

        private bool CanSave() => AudioData != null && !Saving;

        private async void OpenFile()
        {
            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.ComputerFolder
            };

            foreach (string extension in AudioInfo.Extensions.Keys)
            {
                picker.FileTypeFilter.Add("." + extension);
            }

            StorageFile file = await picker.PickSingleFileAsync();

            if (file == null) return;

            try
            {
                StorageApplicationPermissions.
                    FutureAccessList.AddOrReplace("Input File", file);
                InPath = file.Path;

                InFormat = await Task.Run(() => IO.OpenFile(file.Path));
                IAudioFormat format = InFormat;

                LoopStart = format.LoopStart;
                LoopEnd = format.LoopEnd;
                Looping = format.Looping;

                AudioData = new AudioData(format);
                SaveFileCommand.RaiseCanExecuteChanged();
                State = MainState.Opened;
            }
            catch (Exception ex)
            {
                var messageDialog = new MessageDialog(ex.Message, "Unable to parse file");
                await messageDialog.ShowAsync();
            }
        }

        private async void SaveFile()
        {
            var savePicker = new FileSavePicker
            {
                SuggestedFileName = Path.ChangeExtension(Path.GetFileName(InPath), "." + AudioInfo.Containers[SelectedFileType].Extensions.First())
            };
            savePicker.FileTypeChoices.Add(AudioInfo.Containers[SelectedFileType].Description, new List<string> { "." + AudioInfo.Containers[SelectedFileType].Extensions.First() });

            Saving = true;
            SaveFileCommand.RaiseCanExecuteChanged();
            try
            {
                StorageFile saveFile = await savePicker.PickSaveFileAsync();

                if (saveFile == null) return;

                if (Looping)
                {
                    AudioData.SetLoop(Looping, LoopStart, LoopEnd);
                }

                var watch = new Stopwatch();

                byte[] file = null;
                await Task.Run(() =>
                {
                    watch.Start();
                    file = AudioInfo.Containers[SelectedFileType].GetWriter().GetFile(AudioData, GetConfiguration(SelectedFileType));
                    watch.Stop();
                });

                Time = watch.Elapsed.TotalSeconds;
                Samples = AudioData.GetAllFormats().First().SampleCount;

                await FileIO.WriteBytesAsync(saveFile, file);
            }
            catch (Exception ex)
            {
                var messageDialog = new MessageDialog(ex.Message, "Error writing file");
                await messageDialog.ShowAsync();
            }
            finally
            {
                Saving = false;
                SaveFileCommand.RaiseCanExecuteChanged();
            }
        }

        private Configuration GetConfiguration(FileType type)
        {
            switch (type)
            {
                case FileType.Dsp:
                    return DspConfiguration;
                case FileType.Idsp:
                    return IdspConfiguration;
                case FileType.Brstm:
                    return BrstmConfiguration;
                case FileType.Bcstm:
                    return BcstmConfiguration;
                case FileType.Bfstm:
                    return BfstmConfiguration;
                default:
                    return null;
            }
        }

        public enum MainState
        {
            NotOpened,
            Opened,
            Saved
        }
    }
}
