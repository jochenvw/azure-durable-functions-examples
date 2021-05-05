using System.Collections.Generic;
using TransformationFunctions.Data.Model;

namespace TransformationFunctions.Data.Repositories
{
    public interface ILogRepository
    {
        Log GetById(int id);
        int AddBatch(IEnumerable<Log> logs, int commandTimeOut = 30);
    }
}