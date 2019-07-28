﻿using System.Collections.Generic;
using System.Linq;
using NAPS2.Images.Transforms;
using NAPS2.Scan;
using NAPS2.Serialization;

namespace NAPS2.Images.Storage
{
    public class StubImageMetadata : IImageMetadata
    {
        public List<Transform> TransformList { get; set; } = new List<Transform>();

        public int TransformState { get; set; }

        public int Index { get; set; }

        public BitDepth BitDepth { get; set; }

        public bool Lossless { get; set; }

        public void Commit()
        {
        }

        public IImageMetadata Clone() =>
            new StubImageMetadata
            {
                TransformList = TransformList.ToList(),
                TransformState = TransformState,
                Index = Index,
                BitDepth = BitDepth,
                Lossless = Lossless
            };

        public void Dispose()
        {
        }
    }
}