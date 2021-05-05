using System;

namespace TransformationFunctions.DTOs
{
    public class FormatBInstance
    {
        public Guid Id { get; set; }
        public string N { get; set; }
        public int F { get; set; }
        public int S { get; set; }
        public int T { get; set; }
        public int A { get; set; }

        public FormatBInstance()
        {
        }

        public FormatBInstance(FormatAInstance input)
        {
            this.Id = input.Id;
            this.N = input.N;
            this.F = input.F;
            this.S = input.S;
            this.T = input.T;
            this.A = input.F * input.S * input.T;
        }

        public override string ToString()
        {
            return
                $"ID: {this.Id} - First: {this.F}, Second: {this.S}, Third: {this.T} - Additional field: {this.A}";
        }
    }
}
