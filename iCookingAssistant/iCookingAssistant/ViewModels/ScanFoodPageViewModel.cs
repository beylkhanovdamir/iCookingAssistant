using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Cognitive.CustomVision.Prediction;
using Microsoft.Cognitive.CustomVision.Prediction.Models;
using Plugin.Media;
using Plugin.Media.Abstractions;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace iCookingAssistant.ViewModels
{
	public class ScanFoodPageViewModel : ViewModelBase
	{
		// AI props
		private readonly IPredictionEndpoint _predictionEndpoint;

		private const double ProbabilityThreshold = 0.8;
		// Commands
		public ICommand TakePhotoCommand { get; set; }
		public ICommand RemoveIngredientCommand { get; set; }
		// Other
		private ObservableCollection<string> _predictedFoodIngredients;
		public ObservableCollection<string> PredictedFoodIngredients
		{
			get => _predictedFoodIngredients;
			set => Set(ref _predictedFoodIngredients, value);
		}

		private bool _canTakePhoto = true;
		public bool CanTakePhoto
		{
			get => _canTakePhoto;
			set
			{
				if (Set(ref _canTakePhoto, value))
					RaisePropertyChanged(nameof(ShowSpinner));
			}
		}

		private bool _showRecognizedIngredients;
		public bool ShowRecognizedIngredients
		{
			get => _showRecognizedIngredients;
			set
			{
				if (Set(ref _showRecognizedIngredients, value))
					RaisePropertyChanged(nameof(ShowRecognizedIngredients));
			}
		}

		private bool _apiKeysAreValid;
		public bool ApiKeysAreValid
		{
			get => _apiKeysAreValid;
			set
			{
				if (Set(ref _apiKeysAreValid, value))
					RaisePropertyChanged(nameof(ApiKeysAreValid));
			}
		}


		public bool ShowSpinner => !CanTakePhoto;

		public ScanFoodPageViewModel()
		{
			ApiKeysAreValid = !string.IsNullOrWhiteSpace(AIApiKeys.PredictionKey);
			if (ApiKeysAreValid)
			{
				_predictionEndpoint = new PredictionEndpoint() { ApiKey = AIApiKeys.PredictionKey };
			}
			else
			{
				Task.Run(async () => await SpeakMessage("Seems you are not authorized...Please check your Settings"));//Чувак ты не авторизован. Запили мне мыло уже =)
			}
			TakePhotoCommand = new Command(async () => await TakePhoto(), () => ApiKeysAreValid);
			RemoveIngredientCommand = new Command<string>(RemoveIngredient);
		}

		private void RemoveIngredient(string ingredientName)
		{
			PredictedFoodIngredients.Remove(ingredientName);
			ShowRecognizedIngredients = PredictedFoodIngredients.Any();
		}

		public async Task SpeakMessage(string message)
		{
			var locales = await TextToSpeech.GetLocalesAsync();

			var locale = locales.FirstOrDefault(x => x.Language == "eng");// rus eng.. TODO: ref #7

			var settings = new SpeakSettings()
			{
				Volume = .75F,
				Pitch = 1.0F,
				Locale = locale
			};

			await TextToSpeech.SpeakAsync(message, settings);
		}

		private async Task TakePhoto()
		{
			CanTakePhoto = false;
			var options = new StoreCameraMediaOptions { PhotoSize = PhotoSize.Medium };
			var file = await CrossMedia.Current.TakePhotoAsync(options);

			var result = await RecognizeFoodIngredients(file);

			ShowRecognizedIngredients = result.Count(p => p.Probability > ProbabilityThreshold) > 0;

			if (ShowRecognizedIngredients)
			{
				PredictedFoodIngredients = new ObservableCollection<string>(result.Where(p => p.Probability > ProbabilityThreshold)
					.Select(x => new { Name = x.Tag, Precision = x.Probability })
					.GroupBy(x => x.Name)
					.Select(g => g.First().Name)
					);
				CanTakePhoto = true;
			}
			else
			{
				await SpeakMessage("Please take a photo bit better.").ContinueWith(task =>
				{
					CanTakePhoto = true;
				});
			}
		}

		private async Task<IList<ImageTagPredictionModel>> RecognizeFoodIngredients(MediaFile file)
		{
			var result = new List<ImageTagPredictionModel>();
			try
			{
				result = await Task.Factory.StartNew(() =>
				{
					using (var stream = file.GetStream())
					{
						return _predictionEndpoint.PredictImage(Guid.Parse(AIApiKeys.GeneralTrainingProjectId), stream).Predictions.ToList();
					}
				});
			}
			catch (Exception e)
			{
				await SpeakMessage("I don't see anything which can be cooked.");
			}

			return result;
		}

	}
}
