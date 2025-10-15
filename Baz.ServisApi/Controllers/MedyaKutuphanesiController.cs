using Baz.Service;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Threading;
using Baz.Attributes;
using Baz.ProcessResult;
using Baz.Model.Entity;
using Baz.Mapper;
using Baz.Mapper.Pattern;
using Newtonsoft.Json;
using System.IO;
using Baz.Model.Entity.Constants;
using Baz.Model.Entity.ViewModel;
using Baz.RequestManager;
using Baz.RequestManager.Abstracts;
using Microsoft.Extensions.DependencyInjection;
using Baz.Model.Pattern;
using Microsoft.AspNetCore.Authorization;

namespace Baz.MedyaServiceApi.Controllers
{
    /// <summary>
    /// Medya Kütüphanesi Servisi API işlemlerini yöneten Controller
    /// </summary>
    [ApiController]
    [Route("[Controller]")]
    public class MedyaKutuphanesiController : Controller
    {
        private readonly IParamMedyaTipleriService _paramMedyaTipleriService;
        private readonly IMedyaKutuphanesiService _medyaKutuphanesiService;
        private readonly ILoginUser _loginUser;
        private readonly FileControls fc = new();
        private readonly IServiceProvider _serviceProvider;
        private readonly IRequestHelper _helper;

        /// <summary>
        /// <see cref="MedyaKutuphanesiController"/> için oluşturulan constructor, ilgili class'ı başlatır ve ilgili servislerin injection işlemlerini gerçekleştirir.
        /// </summary>
        /// <param name="medyaKutuphanesiService"> <see cref="IMedyaKutuphanesiService"/> Interface'ine bağlı MedyaKutuphanesi Servisi servis.</param>
        /// <param name="helper"></param>
        /// <param name="paramMedyaTipleriService"> <see cref="IParamMedyaTipleriService"/>  Interface'ine bağlı Param-medyaTipleri Servisi servis.</param>
        /// <param name="serviceProvider"></param>
        /// <param name="loginUser"></param>
        public MedyaKutuphanesiController(IMedyaKutuphanesiService medyaKutuphanesiService, IRequestHelper helper, IParamMedyaTipleriService paramMedyaTipleriService, IServiceProvider serviceProvider, ILoginUser loginUser)
        {
            _helper = helper;
            _loginUser = loginUser;
            _paramMedyaTipleriService = paramMedyaTipleriService;
            _medyaKutuphanesiService = medyaKutuphanesiService;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// API üzerinden gönderilen dosyaları karşılayıp yükleme işlemlerini gerçekleştiren method.
        /// </summary>
        /// <param name="file"> <see cref="IFormFile"/> türünde, WebRequest ile iletilen dosyaları temsil eden interface
        /// yüklenmek istenen dosya parametresi.</param>
        /// <returns>Yükleme işlemi başarılıysai yüklenen dosyanın detaylarını, başarısızsa ilgili hata mesajını döner.</returns>
        [ProcessName(Name = "uzantı ve boyut kontrolü sonrası dosyayı kaydeder.")]
        [HttpPost]
        [Route("Upload")]
        [AllowAnonymous]
        public async Task<Result<MedyaKutuphanesi>> Upload(IFormFile file)
        {
            string extension = fc.ExtensionControl(file);
            List<string> extList = new();
            //yüklenebilen uzantılar listesi
            //resim dosyaları
            extList.Add("png");
            extList.Add("jpeg");
            extList.Add("jpg");

            //doküman dosyaları
            extList.Add("pdf");
            extList.Add("txt");
            extList.Add("xlsx");
            extList.Add("xls");
            extList.Add("docx");
            extList.Add("ppt");
            extList.Add("pptx");

            //video dosyaları
            extList.Add("mp4");
            extList.Add("m4v");
            extList.Add("wmv");
            extList.Add("webm");
            extList.Add("flv");
            extList.Add("mpeg");
            extList.Add("avi");

            //sıkıştırılmış dosyalar
            extList.Add("rar");
            extList.Add("zip");

            var fileSize = file.Length;
            long size = 89128960; //100MB - 1 MB = 1048576 B
            if (extList.Contains(extension.ToLower()))
            {
                if (fileSize <= size)
                {
                    var returnModel = await fc.WriteFile(file);
                    returnModel.Value.KayitTarihi = DateTime.Now;
                    returnModel.Value.SilinmeTarihi = null;
                    returnModel.Value.GuncellenmeTarihi = DateTime.Now;
                    returnModel.Value.PasiflikTarihi = null;
                    returnModel.Value.AktiflikTarihi = null;
                    returnModel.Value.KurumID = _loginUser.KurumID;
                    returnModel.Value.KisiID = _loginUser.KisiID;
                    returnModel.Value.AktifMi = 1;
                    returnModel.Value.SilindiMi = 0;
                    returnModel.Value.MedyaTipiId = _paramMedyaTipleriService.GetIdByName(extension);

                    var medyaModel = _medyaKutuphanesiService.Add(returnModel.Value);
                    return medyaModel;
                }
                else
                {
                    return Results.Fail("size too large", ResultStatusCode.CreateError);
                }
            }
            else
            {
                return Results.Fail("invalid extension", ResultStatusCode.CreateError);
            }
        }

        /// <summary>
        /// API üzerinden gönderilen dosyaları karşılayıp yükleme işlemlerini gerçekleştiren method.
        /// </summary>
        /// <returns>Yükleme işlemi başarılıysai yüklenen dosyanın detaylarını, başarısızsa ilgili hata mesajını döner.</returns>
        [ProcessName(Name = "çoklu dosyaların kaydedilmesi.")]
        [HttpPost]
        [Route("UploadMultiple")]
        public async Task<Result<List<MedyaKutuphanesi>>> UploadMultiple(List<IFormFile> files)
        {
            var returnList = new List<MedyaKutuphanesi>();

            foreach (var file in files)
            {
                var result = await this.Upload(file);
                returnList.Add(result.Value);
            }
            if (returnList.Count == 0)
                return Results.Fail("dosyalar yüklenemedi", ResultStatusCode.CreateError);
            return returnList.ToResult();
        }

        /// <summary>
        /// İstenen dosyaya ait verileri getiren method.
        /// </summary>
        /// <param name="id">Getirilmek istenen dosyaya ait TabloID</param>
        /// <returns>İlgili dosyanın verilerini döndürür.</returns>
        [ProcessName(Name = "id ile medya verilerinin getirilmesi")]
        [Route("Get/{id}")]
        [HttpGet]
        [AllowAnonymous]
        public Result<MedyaKutuphanesi> Get(int id)
        {
            var result = _medyaKutuphanesiService.SingleOrDefault(id);
            if (result.Value == null)
                return Results.Fail("medya bulunamadı.", ResultStatusCode.ReadError);
            return result;
        }

        /// <summary>
        /// id değeri ile istenen medya kaydını silen method.
        /// </summary>
        /// <param name="id">Silinmek istenen dosyaya ait TabloID</param>
        /// <returns>Silme işlemi sonrası sonuç verisini döndürür.</returns>
        [ProcessName(Name = "id ile medya verilerinin silinmesi")]
        [Route("Delete/{id}")]
        [HttpPost]
        public Result<MedyaKutuphanesi> Delete(int id)
        {
            var result = _medyaKutuphanesiService.Delete(id);
            if (result.Value == null)
                return Results.Fail("Silme işlemi başarısız.", ResultStatusCode.DeleteError);
            return result;
        }
        [ProcessName(Name = "uzantı ve boyut kontrolü sonrası dosyayı kaydeder.")]
        [HttpPost]
        [Route("UploadIcerik")]
        [AllowAnonymous]
        public async Task<Result<MedyaKutuphanesi>> UploadIcerik(IFormFile file)
        {
            string extension = fc.ExtensionControl(file);
            List<string> extList = new();
            List<int> medyaIdler = new();
            //yüklenebilen uzantılar listesi
            //resim dosyaları
            extList.Add("png");
            extList.Add("jpeg");
            extList.Add("jpg");

            //doküman dosyaları
            extList.Add("pdf");
            extList.Add("txt");
            extList.Add("xlsx");
            extList.Add("xls");
            extList.Add("docx");
            extList.Add("ppt");
            extList.Add("pptx");

            //video dosyaları
            extList.Add("mp4");
            extList.Add("m4v");
            extList.Add("wmv");
            extList.Add("webm");
            extList.Add("flv");
            extList.Add("mpeg");
            extList.Add("avi");

            //sıkıştırılmış dosyalar
            extList.Add("rar");
            extList.Add("zip");

            var fileSize = file.Length;
            long size = 89128960; //100MB - 1 MB = 1048576 B
            if (extList.Contains(extension.ToLower()))
            {
                if (fileSize <= size)
                {
                    var returnModel = await fc.WriteFile(file);
                    returnModel.Value.KayitTarihi = DateTime.Now;
                    returnModel.Value.SilinmeTarihi = null;
                    returnModel.Value.GuncellenmeTarihi = DateTime.Now;
                    returnModel.Value.PasiflikTarihi = null;
                    returnModel.Value.AktiflikTarihi = null;
                    returnModel.Value.KurumID = _loginUser.KurumID;
                    returnModel.Value.KisiID = _loginUser.KisiID;
                    returnModel.Value.AktifMi = 1;
                    returnModel.Value.SilindiMi = 0;
                    returnModel.Value.MedyaTipiId = _paramMedyaTipleriService.GetIdByName(extension);

                    var medya = _medyaKutuphanesiService.Add(returnModel.Value);
                    var medyaModel = _medyaKutuphanesiService.List(x => x.AktifMi == 1 && x.SilindiMi == 0).Value.LastOrDefault();
                    return medyaModel.ToResult();
                        
                    
                }
                else
                {
                    return Results.Fail("size too large", ResultStatusCode.CreateError);
                }
            }
            else
            {
                return Results.Fail("invalid extension", ResultStatusCode.CreateError);
            }
        }

    }
}