using System.Collections.Generic;
using System.Linq;
using System;

namespace FakeDatabase
{
    public class DbContext
    {
        public IQueryable<Record> Records;
        
        private int numberOfFakeRecords = Convert.ToInt32(Environment.GetEnvironmentVariable("FakeRecordCount"));

        public DbContext() {
            var fakes = new Record[numberOfFakeRecords];
            for (int i = 0; i < numberOfFakeRecords; i++)
            {
                fakes[i] = Record.GenerateRandom();
            }
            this.Records = fakes.AsQueryable();
        }        
    }

    public struct Record {
            public Guid Identifier { get; set; }
            public string Message { get; set; }

            public static Record GenerateRandom() {
                return new Record() {
                    Identifier = System.Guid.NewGuid(),
                    Message = $"It's now {System.DateTime.Now.Millisecond}"
                };
            }
        }    
}