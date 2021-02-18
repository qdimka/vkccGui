using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using ReactiveUI;
using VkCCGui.Services;
using VkNet;
using VkNet.Model;

namespace VkCCGui.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly VkApi _api = new();
        private readonly VkccService _vkccService;
        private readonly FileService _fileService = new();
        
        private ICommand _openFileCommand;
        private ICommand _openFolderCommand;
        private ICommand _startCommand;
        
        private string _token;
        private string _fromFilePath;
        private string _toFilePath;
        private bool _isStarted;
        private int _linksCount;
        private int _currentProgress;
        private int _delay = 500;
        private ConfigurationService _configurationService;

        public MainWindowViewModel()
        {
            _vkccService = new VkccService(_api);
            _configurationService = new ConfigurationService(
                    Path.Combine(Path.GetTempPath(), "config.json")
                );

            var configuration = _configurationService.Get();
            if (configuration == null) 
                return;
            
            Token = configuration.Token;
            FromFilePath = configuration.From;
            ToFilePath = configuration.To;
            Delay = configuration.Delay;
        }

        public string Token
        {
            get => _token;
            set { _token = value; this.RaisePropertyChanged();}
        }

        public string FromFilePath
        {
            get => _fromFilePath;
            set { _fromFilePath = value; this.RaisePropertyChanged(); }
        }

        public string ToFilePath
        {
            get => _toFilePath;
            set { _toFilePath = value; this.RaisePropertyChanged();}
        }
        
        public int Delay
        {
            get => _delay;
            set { _delay = value; this.RaisePropertyChanged();}
        }

        #region Progress

        public bool IsStarted
        {
            get => _isStarted;
            set { _isStarted = value; this.RaisePropertyChanged();}
        }

        public int LinksCount
        {
            get => _linksCount;
            set { _linksCount = value; this.RaisePropertyChanged();}
        }

        public int CurrentProgress
        {
            get => _currentProgress;
            set { _currentProgress = value; this.RaisePropertyChanged();}
        }

        #endregion

        #region Commands

        public ICommand OpenFileCommand => 
            _openFileCommand ??= ReactiveCommand.CreateFromTask(OpenFile);
        
        public ICommand OpenFolderCommand => 
            _openFolderCommand ??= ReactiveCommand.CreateFromTask(OpenFolder);
        
        public ICommand StartCommand => 
            _startCommand ??= ReactiveCommand.CreateFromTask(Start, 
                this.WhenAnyValue( 
                    model => model.Token, 
                    model => model.ToFilePath, 
                    model => model.FromFilePath,
                    (token, to, from ) => !string.IsNullOrEmpty(token) 
                                          && !string.IsNullOrEmpty(to) 
                                          && !string.IsNullOrEmpty(from)),RxApp.MainThreadScheduler);

        #endregion

        #region CommandHandlers

        private async Task OpenFile()
        {
            var dialog = new OpenFileDialog();
            var result = await dialog.ShowAsync(new Window());

            if (result != null && result.Any()) 
                FromFilePath = result[0];
        }
        
        private async Task OpenFolder()
        {
            var dialog = new OpenFolderDialog();
            var result = await dialog.ShowAsync(new Window());

            if (!string.IsNullOrEmpty(result)) 
                ToFilePath = result;
        }

        private async Task Start()
        {
            if(!_api.IsAuthorized) 
                await _api.AuthorizeAsync(new ApiAuthParams()
                {
                    AccessToken = Token
                });

            _configurationService.Save(new Configuration
            {
                Token = Token,
                From = FromFilePath,
                To = ToFilePath,
                Delay = Delay,
            });
            
            var fileName = Path.GetFileNameWithoutExtension(_fromFilePath);
            
            var urls = _fileService.ReadLines(FromFilePath);
            
            StartCounter(urls.Length);
            
            var shortUrls = new List<string>();
            var failed = new List<string>();

            foreach (var url in urls)
            {
                var result = await _vkccService.GetShortLink(url);
                if (result.IsSuccess)
                    shortUrls.Add(result.ShortUrl);
                else
                    failed.Add(url);
                
                CurrentProgress++;
                
                if(CurrentProgress != 0 && CurrentProgress % 20 == 0)
                    await Task.Delay(1000);
            }

            if (failed.Any())
                _fileService.SaveFile(Path.Combine(ToFilePath, $"{fileName}-failed-{Guid.NewGuid():N}.txt"), failed.ToArray());
            
            _fileService.SaveFile(Path.Combine(ToFilePath, $"{fileName}-{Guid.NewGuid():N}.txt"), shortUrls.ToArray());
            
            ResetCounter();
        }

        private void StartCounter(int maximum)
        {
            IsStarted = true;
            LinksCount = maximum;
        }
        
        private void ResetCounter()
        {
            IsStarted = false;
            CurrentProgress = 0;
            LinksCount = 0;
        }
        
        #endregion
    }
}