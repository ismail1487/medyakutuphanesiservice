using Baz.Mapper.Pattern;
using Baz.ProcessResult;
using Baz.Repository.Common;
using Baz.Repository.Pattern;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Baz.Service.Base
{
    /// <summary>
    /// Ekleme,düzenleme,silme listeleme vb işlemlerin yer aldığı sınftır.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <seealso cref="Baz.Service.Base.IService{TEntity}" />
    public class Service<TEntity> : IService<TEntity> where TEntity : class, Baz.Model.Pattern.IBaseModel
    {
        /// <summary>
        /// Reposiitory değişkeni
        /// </summary>
        protected readonly IRepository<TEntity> _repository;

        /// <summary>
        /// Model mapper
        /// </summary>
        protected IDataMapper _dataMapper;

        /// <summary>
        /// Logger
        /// </summary>
        protected ILogger _logger;

        /// <summary>
        /// Servis collector
        /// </summary>
        protected IServiceProvider _serviceProvider;

        private bool _disposed;

        /// <summary>
        /// Ekleme,düzenleme,silme listeleme vb işlemlerin yer aldığı sınftın yapıcı metodu
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="dataMapper"></param>
        /// <param name="serviceProvider"></param>
        /// <param name="logger"></param>
        public Service(IRepository<TEntity> repository, IDataMapper dataMapper, IServiceProvider serviceProvider, ILogger logger)
        {
            _repository = repository;
            _dataMapper = dataMapper;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Ekleme işleminin yapıldığı methodtur.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns></returns>
        public virtual Result<TEntity> Add(TEntity entity)
        {
           
                var result = _repository.Add(entity);
                if (_repository.SaveChanges() > 0)
                    return result.ToResult();
                else
                    return Results.Fail("İşlem yapılamadı");
        }

        /// <summary>
        /// Silme işleminin yapıldığı methodtur.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public virtual Result<TEntity> Delete(int id)
        {
            
                var result = _repository.Delete(id);
                if (_repository.SaveChanges() > 0)
                    return result.ToResult();
                return Results.Fail("İşlem yapılamadı");
        }

        /// <summary>
        /// Alınan parametreye göre listelemenin yapıldığı methodtur.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        public virtual Result<List<TEntity>> List(Expression<Func<TEntity, bool>> expression)
        {
         
                return _repository.List(expression).ToList().ToResult();
        }

        /// <summary>
        /// Id'ye göre sonucun döndürüldüğü methodtur.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public virtual Result<TEntity> SingleOrDefault(int id)
        {
           
                return _repository.SingleOrDefault(id).ToResult();
        }

        /// <summary>
        /// Kullanılmayan kaynakları boşa çıkardıktan sonra sonucu true veya false döndüren methodtur.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposed && disposing)
            {
                _repository.Dispose();
            }
            this._disposed = true;
        }

        /// <summary>
        /// Kullanılmayan kaynakları boşa çıkaran methodtur.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        //public DataContextConfiguration DataContextConfiguration()
        //{   return _repository.DataContextConfiguration() }
    }
}