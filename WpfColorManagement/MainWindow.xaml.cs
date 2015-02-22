using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfColorManagement
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			this.Drop += OnDrop;
		}

		private async void OnDrop(object sender, DragEventArgs e)
		{
			var filePaths = ((DataObject)e.Data).GetFileDropList().Cast<string>();

			foreach (var filePath in filePaths)
			{
				if (!File.Exists(filePath))
					continue;

				try
				{
					using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					using (var ms = new MemoryStream())
					{
						await fs.CopyToAsync(ms);
						SourceData = ms.ToArray();
					}
				}
				catch
				{
					continue;
				}

				await ConvertImageAsync();
				return;
			}
		}

		public string ColorProfilePath
		{
			get { return (string)GetValue(ColorProfilePathProperty); }
			set { SetValue(ColorProfilePathProperty, value); }
		}
		public static readonly DependencyProperty ColorProfilePathProperty =
			DependencyProperty.Register(
				"ColorProfilePath",
				typeof(string),
				typeof(MainWindow),
				new PropertyMetadata(
					String.Empty,
					async (d, e) => await ((MainWindow)d).ConvertImageAsync()));

		public BitmapSource ConvertedImage
		{
			get { return (BitmapSource)GetValue(ConvertedImageProperty); }
			set { SetValue(ConvertedImageProperty, value); }
		}
		public static readonly DependencyProperty ConvertedImageProperty =
			DependencyProperty.Register(
				"ConvertedImage",
				typeof(BitmapSource),
				typeof(MainWindow),
				new PropertyMetadata(null));

		private byte[] SourceData { get; set; }

		private async Task ConvertImageAsync()
		{
			if (SourceData == null)
				return;

			ConvertedImage = await ImageConverter.ConvertImageAsync(SourceData, ColorProfilePath);
		}
	}
}