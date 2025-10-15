using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Baz.Model.Entity;
using Baz.ProcessResult;
using Decor;
using Baz.AOP.Logger.ExceptionLog;

namespace Baz.MedyaServiceApi
{
    /// <summary>
    /// Medya Kütüphanesi dosya yükleme işlemleri ile ilgili kontrol methodlarını içeren class.
    /// </summary>
    public class FileControls
    {
        /// <summary>
        /// İlgili dosyanın uzantısını getiren method.
        /// </summary>
        /// <param name="file"> İlgili dosya parametresi</param>
        /// <returns>İlgili dosyanın uzantısını döndürür.</returns>

        public string ExtensionControl(IFormFile file)
        {
            if (file == null || file.FileName == null)
                return "";
            var extension = /*"." +*/ file.FileName.Split('.')[^1];
            return extension;
        }

        /// <summary>
        /// İlgili dosyanın diske kaydedilmesi işlemini gerçekleştiren method.
        /// </summary>
        /// <param name="file">Yüklenmek istenen dosya parametresi</param>
        /// <returns> Diske kaydedilen dosyaya ait verileri <see cref="MedyaKutuphanesi"/> türünde döndürür.</returns>

        public async Task<Result<MedyaKutuphanesi>> WriteFile(IFormFile file)
        {
            var medya = new MedyaKutuphanesi();
            string fileName;
            var guid = Guid.NewGuid().ToString("N");
            try
            {
                fileName = guid + (file.FileName).Replace(" ", "_");
                medya.MedyaAdi = fileName;
                medya.MedyaUrl = "/MedyaKutuphanesi/" + fileName;

                var pathBuilt = Path.Combine(Environment.CurrentDirectory, "MedyaKutuphanesi");

                if (!Directory.Exists(pathBuilt))
                {
                    Directory.CreateDirectory(pathBuilt);
                }

                var path = Path.Combine(Directory.GetCurrentDirectory(), "MedyaKutuphanesi", fileName);

                await using var stream = new FileStream(path, FileMode.Create);
                await file.CopyToAsync(stream);
                return medya.ToResult();
            }
            catch (Exception ex)
            {
                return medya.ToResult().WithError(ex.Message);
            }
        }
    }
}