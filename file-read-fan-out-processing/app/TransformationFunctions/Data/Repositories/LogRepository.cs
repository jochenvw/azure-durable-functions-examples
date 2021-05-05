using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TransformationFunctions.Data.Model;

namespace TransformationFunctions.Data.Repositories
{
    public class LogRepository : ILogRepository
    {
        private FileTransformationContext _context;

        public LogRepository(FileTransformationContext context)
        {
            this._context = context;
        }

        public Log GetById(int id)
        {
            return this._context.Log.SingleOrDefault(log => log.Id == id);
        }

        public int AddBatch(IEnumerable<Log> logs, int commandTimeOut = 30)
        {
            this._context.Log.AddRange(logs);
            this._context.Database.SetCommandTimeout(commandTimeOut);
            return this._context.SaveChanges();
        }
    }
}
