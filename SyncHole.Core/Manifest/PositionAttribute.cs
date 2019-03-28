using System;

namespace SyncHole.Core.Manifest
{
    public class PositionAttribute : Attribute
    {
        public uint Index { get; }

        public PositionAttribute(uint index)
        {
            Index = index;
        }
    }
}