using Baz.ProcessResult;
using Baz.Repository.Common;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Baz.Service.Base
{
    /// <summary>
    /// Ekleme,düzenleme,silme listeleme vb işlemlerin yer aldığı interfacedir.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <seealso cref="System.IDisposable" />
    public interface IService<TEntity> : IDisposable where TEntity : class, Baz.Model.Pattern.IBaseModel
    {
        /// <summary>
        /// Ekleme işleminin yapıldığı methodtur.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns></returns>
        Result<TEntity> Add(TEntity entity);

        //Result<TEntity> Update(TEntity entity)
        /// <summary>
        /// Silme işleminin yapıldığı methodtur.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        Result<TEntity> Delete(int id);

        /// <summary>
        /// Id'ye göre sonucun döndürüldüğü methodtur.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        Result<TEntity> SingleOrDefault(int id);

        //Result<List<TEntity>> List()

        /// <summary>
        /// Alınan parametreye göre listelemenin yapıldığı methodtur.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        Result<List<TEntity>> List(Expression<Func<TEntity, bool>> expression);

        //DataContextConfiguration DataContextConfiguration()
    }
}