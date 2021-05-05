using System;

namespace TransformationFunctions.DTOs
{
    public class InputFormat
    {
        public Guid Id { get; set; }
        public string S { get; set; }

        public InputFormat()
        {
        }

        public InputFormat(string s)
        {
            this.Id = Guid.NewGuid();
            this.S = s;
        }
    }
}
