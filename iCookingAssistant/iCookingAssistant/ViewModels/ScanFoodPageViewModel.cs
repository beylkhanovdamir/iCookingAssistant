using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Cognitive.CustomVision.Prediction;
using Microsoft.Cognitive.CustomVision.Prediction.Models;
using Microsoft.Cognitive.CustomVision.Training;
using Microsoft.Cognitive.CustomVision.Training.Models;
using Plugin.Media;
using Plugin.Media.Abstractions;
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
		// Other
		private ObservableCollection<string> _predictedFoodObjects;
		public ObservableCollection<string> PredictedFoodObjects
		{
			get => _predictedFoodObjects;
			set => Set(ref _predictedFoodObjects, value);
		}

		public ScanFoodPageViewModel()
		{
			_predictionEndpoint = new PredictionEndpoint() { ApiKey = AIApiKeys.PredictionKey };
			TakePhotoCommand = new Command(async () => await TakePhoto());
		}

		private async Task TakePhoto()
		{
			var options = new StoreCameraMediaOptions { PhotoSize = PhotoSize.Medium };
			var file = await CrossMedia.Current.TakePhotoAsync(options);

			var result = RecognizeFoodObjects(file)
				.Where(p => p.Probability > ProbabilityThreshold)
				.Select(x => new { Name = x.Tag, Precision = x.Probability })
				.GroupBy(x => x.Name)
				.Select(g => g.First().Name)
				.ToList();

			PredictedFoodObjects = new ObservableCollection<string>(result);

		}

		private IList<ImageTagPredictionModel> RecognizeFoodObjects(MediaFile file)
		{
			var result = new List<ImageTagPredictionModel>();
			using (var stream = file.GetStream())
			{
				try
				{
					result = _predictionEndpoint.PredictImage(AIApiKeys.GeneralTrainingProjectId, stream).Predictions.ToList();
				}
				catch (Exception e)
				{
					Debug.WriteLine(e);
				}
				return result;
			}
		}

	}
}
