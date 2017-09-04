using System;

namespace HashCalculator.BLL.Models
{
    public class FileInformation : ICloneable
    {
        public string Name { get; set; }

        public string Path { get; set; }

        public double Length { get; set; }

        public string Hash { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
