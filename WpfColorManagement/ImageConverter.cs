﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfColorManagement
{
	public class ImageConverter
	{
		public static async Task<BitmapSource> ConvertImageAsync(byte[] sourceData, string colorProfilePath)
		{
			using (var ms = new MemoryStream())
			{
				await ms.WriteAsync(sourceData, 0, sourceData.Length).ConfigureAwait(false);
				ms.Seek(0, SeekOrigin.Begin);

				var frame = BitmapFrame.Create(
					ms,
					BitmapCreateOptions.IgnoreColorProfile | BitmapCreateOptions.PreservePixelFormat,
					BitmapCacheOption.OnLoad);

				var ccb = new ColorConvertedBitmap();
				ccb.BeginInit();
				ccb.Source = frame;

				ccb.SourceColorContext = ((frame.ColorContexts != null) && frame.ColorContexts.Any())
					? frame.ColorContexts.First()
					: new ColorContext(PixelFormats.Bgra32); // Fallback

				ccb.DestinationColorContext = !String.IsNullOrEmpty(colorProfilePath)
					? new ColorContext(new Uri(colorProfilePath))
					: new ColorContext(PixelFormats.Bgra32); // Fallback

				ccb.DestinationFormat = PixelFormats.Bgra32;
				ccb.EndInit();
				ccb.Freeze();

				return ccb;
			}
		}
	}
}