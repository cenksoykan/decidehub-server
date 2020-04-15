using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.Primitives;

namespace Decidehub.Web.Extensions
{
    public static class ResizeImageExtentions
    {
        public static string Resize(this IFormFile file, int size)
        {
            using (var image = Image.Load(file.OpenReadStream()))
            {
                int width, height;

                if (image.Width > image.Height)
                {
                    width = size;
                    height = Convert.ToInt32(image.Height * size / (double) image.Width);
                }
                else
                {
                    width = Convert.ToInt32(image.Width * size / (double) image.Height);
                    height = size;
                }

                image.Mutate(x => x.Resize(width, height));


                var base64Str = image.ToBase64String(ImageFormats.Jpeg);

                return base64Str;
            }
        }

        public static void ResizeAndSave(this IFormFile file, int width, int height, string outputPath)
        {
            using (var image = Image.Load(file.OpenReadStream()))
            {
                int imageWidth, imageHeight;
                if (image.Width > image.Height)
                {
                    imageWidth = width;
                    imageHeight = Convert.ToInt32(image.Height * height / (double) image.Width);
                }
                else
                {
                    imageWidth = Convert.ToInt32(image.Width * width / (double) image.Height);
                    imageHeight = height;
                }

                image.Mutate(x => x.Resize(imageWidth, imageHeight));
                image.Save(outputPath);
            }
        }

        public static string CropImage(string path, int t, int l, int h, int w)
        {
            string base64Str;

            using (var stream = File.OpenRead(path))

            using (var image = Image.Load(stream))
            {
                image.Mutate(x => x.Crop(new Rectangle(t, l, w, h)));
                base64Str = image.ToBase64String(ImageFormats.Jpeg);
            }

            return base64Str;
        }
    }
}