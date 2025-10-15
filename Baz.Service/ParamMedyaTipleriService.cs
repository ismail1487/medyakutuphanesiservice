using Baz.AOP.Logger.ExceptionLog;
using Baz.Mapper.Pattern;
using Baz.Model.Entity;
using Baz.Repository.Pattern;
using Baz.Service.Base;
using Decor;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baz.Service
{
    /// <summary>
    /// Param-MedyaTipleri  ile ilgili işlevleri barındıran interface.
    /// </summary>
    public interface IParamMedyaTipleriService : IService<ParamMedyaTipleri>
    {
        /// <summary>
        /// İsme göre Id döndüren metod
        /// </summary>
        /// <param name="extension"></param>
        /// <returns></returns>
        public int GetIdByName(string extension);
    }

    /// <summary>
    /// Param-MedyaTipleri ile ilgili işlevleri barındıran, <see cref="IParamMedyaTipleriService"/> interface'ini baz alan class.
    /// </summary>
    public class ParamMedyaTipleriService : Service<ParamMedyaTipleri>, IParamMedyaTipleriService
    {
        /// <summary>
        /// Param-MedyaTipleri ile ilgili işlevleri barındıran servisin yapıcı metodu
        /// <param name="repository"></param>
        /// <param name="dataMapper"></param>
        /// <param name="serviceProvider"></param>
        /// <param name="logger"></param>
        /// </summary>
        public ParamMedyaTipleriService(IRepository<ParamMedyaTipleri> repository, IDataMapper dataMapper, IServiceProvider serviceProvider, ILogger<ParamMedyaTipleriService> logger) : base(repository, dataMapper, serviceProvider, logger)
        {
        }

        /// <summary>
        /// uzanti adına göre ilgili uzantının Id değerini getiren method.
        /// </summary>
        /// <param name="extension"> Uzantı adı.</param>
        /// <returns>İlgili Id değerini döndürür.</returns>
        public int GetIdByName(string extension)
        {
            return List(p => p.ParamTanim == extension).Value.Select(x => x.TabloID).FirstOrDefault();
        }
    }
}