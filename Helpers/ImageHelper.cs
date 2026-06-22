using System;
using System.IO;
using System.Web;

namespace PharmacyProMS.Helpers
{
    public static class ImageHelper
    {
        public static string UploadImage(
            HttpPostedFileBase file,
            string folderName)
        {
            try
            {
                if (file == null || file.ContentLength == 0)
                    return null;

                // Allowed extensions
                string[] allowed = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                string ext = Path.GetExtension(file.FileName).ToLower();

                if (Array.IndexOf(allowed, ext) < 0)
                    return null;

                // Max 5MB
                if (file.ContentLength > 5 * 1024 * 1024)
                    return null;

                // Folder path
                string uploadFolder = HttpContext.Current.Server.MapPath(
                    "~/Content/Uploads/" + folderName + "/");

                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                // Unique file name
                string fileName = Guid.NewGuid().ToString() + ext;
                string fullPath = Path.Combine(uploadFolder, fileName);

                file.SaveAs(fullPath);

                return "/Content/Uploads/" + folderName + "/" + fileName;
            }
            catch
            {
                return null;
            }
        }

        public static void DeleteImage(string imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath)) return;

                string fullPath = HttpContext.Current
                    .Server.MapPath(imagePath);

                if (File.Exists(fullPath))
                    File.Delete(fullPath);
            }
            catch { }
        }
    }
}