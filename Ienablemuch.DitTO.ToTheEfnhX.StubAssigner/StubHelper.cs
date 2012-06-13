using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Ienablemuch.DitTO.EntityFramework.StubAssigner;

namespace Ienablemuch.DitTO.ToTheEfnhX.StubAssigner
{
    public static class StubHelper
    {
        public static void AssignStub<T>(this Ienablemuch.ToTheEfnhX.IRepository<T> repo, T obj) where T : class
        {
            if (repo.GetType() == typeof(Ienablemuch.ToTheEfnhX.EntityFramework.Repository<T>))
            {
                System.Data.Entity.DbContext db = (repo as Ienablemuch.ToTheEfnhX.EntityFramework.Repository<T>).DbContext;

                db.AssignStub(obj);
            }
        }
    }
}
