using System;

namespace ConstructionSystem
{
    public class ConnectionPointReference
    {
        public Guid Id { get; set; }
        public int SubId { get; set; }
        public int FaceId { get; set; }
        public int Orientation { get; set; }
    }
}