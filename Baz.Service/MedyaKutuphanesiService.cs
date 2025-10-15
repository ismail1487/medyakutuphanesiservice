using Baz.Service.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Baz.Model.Entity;
using Baz.Repository.Pattern;
using Baz.Mapper.Pattern;
using Microsoft.Extensions.Logging;

namespace Baz.Service
{
    /// <summary>
    /// MedyaKutuphanesi ile ilgili işlevleri barındıran interface.
    /// </summary>
    public interface IMedyaKutuphanesiService : IService<MedyaKutuphanesi>
    {
    }

    /// <summary>
    /// MedyaKutuphanesi ile ilgili işlevleri barındıran, <see cref="IMedyaKutuphanesiService"/> interface'ini baz alan class.
    /// </summary>
    public class MedyaKutuphanesiService : Service<MedyaKutuphanesi>, IMedyaKutuphanesiService
    {
        /// <summary>
        /// MedyaKutuphanesi ile ilgili işlevleri barındıran servisin yapıcı metodu
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="dataMapper"></param>
        /// <param name="serviceProvider"></param>
        /// <param name="logger"></param>
        public MedyaKutuphanesiService(IRepository<MedyaKutuphanesi> repository, IDataMapper dataMapper, IServiceProvider serviceProvider, ILogger<MedyaKutuphanesiService> logger) : base(repository, dataMapper, serviceProvider, logger)
        {
        }
    }
}