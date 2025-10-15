using Baz.Model.Entity;
using Baz.ProcessResult;
using Baz.RequestManager.Abstracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;

namespace MedyaKutuphanesiUnitTest
{
    /// <summary>
    /// Medya kütüphanesinin testlerinin yapıldığı sınıftır.
    /// </summary>
    [TestClass()]
    public class MedyaKutuphanesiTest
    {
        private readonly HttpClient _client;
        private readonly IRequestHelper _helper;

        private HttpResponseMessage response;

        /// <summary>
        /// Medya kütüphanesinin testlerinin yapıldığı sınıftın yapıcı metodu
        /// </summary>
        public MedyaKutuphanesiTest()
        {
            _helper = TestServerRequestHelper.CreateHelper();
            _client = TestServerRequestHelper.CreateHelperForHttpClient();
        }

        /// <summary>
        /// Medya Kütüphanesi CRUP metotlarının test edildiği test metodu.
        /// </summary>
        [TestMethod()]
        public void UploadTest()
        {
            var direc = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, "mymediadir").Replace("mymediadir", "");
            //Assert-1 Doğru kayıt methodu
            File.WriteAllText(direc + "TestMedyalar/Test.txt", "Unit Test Deneme Dosyası.");
            using (var file = File.OpenRead(direc + "TestMedyalar/Test.txt"))
            using (var content = new StreamContent(file))
            using (var formData = new MultipartFormDataContent())
            {
                formData.Add(content, "file", "Test.txt");
                response = _client.PostAsync($"/MedyaKutuphanesi/Upload", formData).Result;
            }

            Assert.AreEqual(response.StatusCode, HttpStatusCode.OK);
            var responseSTR = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var model = JsonConvert.DeserializeObject<Result<MedyaKutuphanesi>>(responseSTR);
            Assert.IsNotNull(model.Value);

            //Assert-1.1 Farklı dosya uzantısı hata testi
            File.WriteAllText(direc + "TestMedyalar/testFail.js", "Unit Test Fail Deneme Dosyası.");
            using (var file1 = File.OpenRead(direc + "/TestMedyalar/testFail.js"))
            using (var content1 = new StreamContent(file1))
            using (var formData1 = new MultipartFormDataContent())
            {
                formData1.Add(content1, "file", "testFail.js");
                response = _client.PostAsync($"/MedyaKutuphanesi/Upload", formData1).Result;
            }

            Assert.AreEqual(response.StatusCode, HttpStatusCode.OK);
            var responseSTR1 = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var model1 = JsonConvert.DeserializeObject<Result<MedyaKutuphanesi>>(responseSTR1);
            Assert.IsNull(model1.Value);

            //Assert-1.2 izin verilmeyen boyut hata testi
            FileStream fs = new(direc + "TestMedyalar/Bigtest.txt", FileMode.OpenOrCreate);
            fs.Seek(100L * 1024 * 1024, SeekOrigin.Begin);
            fs.WriteByte(0);
            fs.Close();
            using (var file2 = File.OpenRead(direc + "/TestMedyalar/Bigtest.txt"))
            //using (var file2 = new FileStream("bigfile", FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                using var content2 = new StreamContent(file2);
                using var formData2 = new MultipartFormDataContent { { content2, "file", "Bigtest.txt" } };
                response = _client.PostAsync($"/MedyaKutuphanesi/Upload", formData2).Result;
            }

            Assert.AreEqual(response.StatusCode, HttpStatusCode.OK);
            var responseSTR2 = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var model2 = JsonConvert.DeserializeObject<Result<MedyaKutuphanesi>>(responseSTR2);
            Assert.IsNull(model2.Value);

            //Assert-2 getirme testi
            var getresponse = _helper.Get<Result<MedyaKutuphanesi>>($"/MedyaKutuphanesi/Get/" + model.Value.TabloID);

            Assert.AreEqual(getresponse.StatusCode, HttpStatusCode.OK);
            Assert.AreEqual(getresponse.Result.StatusCode, (int)ResultStatusCode.Success);
            Assert.IsNotNull(getresponse.Result.Value);

            //Assert-2.1 getirme testi olumsuz
            var getresponseolumsuz = _helper.Get<Result<MedyaKutuphanesi>>($"/MedyaKutuphanesi/Get/" + 0);

            Assert.AreEqual(getresponseolumsuz.StatusCode, HttpStatusCode.OK);
            Assert.IsNull(getresponseolumsuz.Result.Value);

            //Assert-3 silme testi
            var deleteresponse = _helper.Post<Result<MedyaKutuphanesi>>($"/MedyaKutuphanesi/Delete/" + model.Value.TabloID, model.Value.TabloID);

            Assert.AreEqual(deleteresponse.StatusCode, HttpStatusCode.OK);
            Assert.AreEqual(deleteresponse.Result.StatusCode, (int)ResultStatusCode.Success);
            Assert.IsNotNull(deleteresponse.Result.Value);

            //Assert-3 silme testi olumsuz
            var deleteresponseolumsuz = _helper.Post<Result<MedyaKutuphanesi>>($"/MedyaKutuphanesi/Delete/" + 0, 0);

            Assert.AreEqual(deleteresponseolumsuz.StatusCode, HttpStatusCode.OK);
            Assert.IsNull(deleteresponseolumsuz.Result.Value);

            //Assert-4 çoklu yükleme testi
            File.WriteAllText(direc + "TestMedyalar/CokluTest1.txt", "Unit Test Deneme Dosyası.");
            File.WriteAllText(direc + "TestMedyalar/CokluTest2.txt", "Unit Test Deneme Dosyası.");
            File.WriteAllText(direc + "TestMedyalar/CokluTest3.txt", "Unit Test Deneme Dosyası.");
            var filecoklu1 = File.OpenRead(direc + "/TestMedyalar/CokluTest1.txt");
            var filecoklu2 = File.OpenRead(direc + "/TestMedyalar/CokluTest2.txt");
            var filecoklu3 = File.OpenRead(direc + "/TestMedyalar/CokluTest3.txt");

            var contentcoklu1 = new StreamContent(filecoklu1);
            var contentcoklu2 = new StreamContent(filecoklu2);
            var contentcoklu3 = new StreamContent(filecoklu3);

            var formDataCoklu = new MultipartFormDataContent
            {
                {contentcoklu1, "files", "CokluTest1.txt"},
                {contentcoklu2, "files", "CokluTest2.txt"},
                {contentcoklu3, "files", "CokluTest3.txt"}
            };

            HttpResponseMessage responseCoklu = _client.PostAsync($"/MedyaKutuphanesi/UploadMultiple", formDataCoklu).Result;

            Assert.AreEqual(responseCoklu.StatusCode, HttpStatusCode.OK);
            var responseCokluStr = responseCoklu.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var modelCoklu = JsonConvert.DeserializeObject<Result<List<MedyaKutuphanesi>>>(responseCokluStr);
            Assert.IsNotNull(modelCoklu.Value);
            Assert.AreEqual(3, modelCoklu.Value.Count);

            //Assert-4.1 Çoklu yüklenen verilerin silinmesi
            foreach (var item in modelCoklu.Value)
            {
                var deleteresponseCoklu = _helper.Post<Result<MedyaKutuphanesi>>($"/MedyaKutuphanesi/Delete/" + item.TabloID, item.TabloID);
                Assert.AreEqual(deleteresponseCoklu.StatusCode, HttpStatusCode.OK);
                Assert.AreEqual(deleteresponseCoklu.Result.StatusCode, (int)ResultStatusCode.Success);
                Assert.IsNotNull(deleteresponseCoklu.Result.Value);
            }

            //Assert UploadChunk
            using (var file = File.OpenRead(direc + "TestMedyalar/video.mp4"))
            using (var content = new StreamContent(file))
            using (var formData = new MultipartFormDataContent())
            {
                formData.Add(content, "file", "TestFileName");
                var guid = Guid.NewGuid().ToString();
                formData.Add(new StringContent("Test"), "room");
                formData.Add(new StringContent("True"), "isRecording");
                formData.Add(new StringContent("82"), "kurumId");
                formData.Add(new StringContent("1"), "index");
                formData.Add(new StringContent("129"), "kisiId");
                formData.Add(new StringContent(guid), "randGuid");
                formData.Add(new StringContent("1"), "odaId");
                response = _client.PostAsync($"/MedyaKutuphanesi/UploadChunk", formData).Result;
            }

            Assert.AreEqual(response.StatusCode, HttpStatusCode.OK);

            //Assert UploadChunk-2
            using (var file = File.OpenRead(direc + "TestMedyalar/video.mp4"))
            using (var content = new StreamContent(file))
            using (var formData = new MultipartFormDataContent())
            {
                formData.Add(content, "file", "TestFileName");
                var guid = Guid.NewGuid().ToString();
                formData.Add(new StringContent("Test"), "room");
                formData.Add(new StringContent("True"), "isRecording");
                formData.Add(new StringContent("82"), "kurumId");
                formData.Add(new StringContent("1"), "index");
                formData.Add(new StringContent("129"), "kisiId");
                formData.Add(new StringContent(guid), "randGuid");
                formData.Add(new StringContent("1"), "odaId");
                response = _client.PostAsync($"/MedyaKutuphanesi/UploadChunk", formData).Result;
            }

            Assert.AreEqual(response.StatusCode, HttpStatusCode.OK);

            //Assert UploadChunk-3
            using (var file = File.OpenRead(direc + "TestMedyalar/video.mp4"))
            using (var content = new StreamContent(file))
            using (var formData = new MultipartFormDataContent())
            {
                formData.Add(content, "file", "TestFileName");
                var guid = Guid.NewGuid().ToString();
                formData.Add(new StringContent("Test"), "room");
                formData.Add(new StringContent("True"), "isRecording");
                formData.Add(new StringContent("82"), "kurumId");
                formData.Add(new StringContent("1"), "index");
                formData.Add(new StringContent("129"), "kisiId");
                formData.Add(new StringContent(guid), "randGuid");
                formData.Add(new StringContent("1"), "odaId");
                response = _client.PostAsync($"/MedyaKutuphanesi/UploadChunk", formData).Result;
            }

            Assert.AreEqual(response.StatusCode, HttpStatusCode.OK);

            //Assert UploadChunk-4
            using (var file = File.OpenRead(direc + "TestMedyalar/video.mp4"))
            using (var content = new StreamContent(file))
            using (var formData = new MultipartFormDataContent())
            {
                formData.Add(content, "file", "TestFileName");
                var guid = Guid.NewGuid().ToString();
                formData.Add(new StringContent("Test"), "room");
                formData.Add(new StringContent("False"), "isRecording");
                formData.Add(new StringContent("82"), "kurumId");
                formData.Add(new StringContent("1"), "index");
                formData.Add(new StringContent("129"), "kisiId");
                formData.Add(new StringContent(guid), "randGuid");
                formData.Add(new StringContent("1"), "odaId");
                response = _client.PostAsync($"/MedyaKutuphanesi/UploadChunk", formData).Result;
            }

            Assert.AreEqual(response.StatusCode, HttpStatusCode.OK);

            //Assert DepolamaFGetir-5
            var depolamaAlani = _helper.Get<Result<long>>("/MedyaKutuphanesi/DepolamaFGetir/" + 129);
            Assert.AreEqual(depolamaAlani.StatusCode, HttpStatusCode.OK);
            Assert.AreEqual(depolamaAlani.Result.StatusCode, (int)ResultStatusCode.Success);
            Assert.IsNotNull(depolamaAlani.Result.Value);

            //Assert KalanDepolamaGetir-5
            var kalanDepolamaGetir = _helper.Get<Result<long>>("/MedyaKutuphanesi/KalanDepolamaGetir/" + 82 + "/" + 129);
            Assert.AreEqual(kalanDepolamaGetir.StatusCode, HttpStatusCode.OK);
            Assert.AreEqual(kalanDepolamaGetir.Result.StatusCode, (int)ResultStatusCode.Success);
            Assert.IsNotNull(kalanDepolamaGetir.Result.Value);
        }
    }
}