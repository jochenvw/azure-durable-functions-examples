using System;

namespace TransformationFunctions.DTOs
{
    public class FormatAInstance
    {
        public Guid Id { get; set; }
        public string N { get; set; }
        public int F { get; set; }
        public int S { get; set; }
        public int T { get; set; }

        public FormatAInstance()
        {
        }

        public FormatAInstance(InputFormat input)
        {
            this.Id = input.Id;
        }

        public override string ToString()
        {
            return $"FormatAInstance - Id {this.Id} - N {this.N} F {this.F} S {this.S} T {this.T}";
        }
    }
}
