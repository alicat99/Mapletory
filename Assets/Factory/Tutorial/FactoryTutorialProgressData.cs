using System;
using System.Collections.Generic;

namespace Maptory.Factory
{
    [Serializable]
    public sealed class FactoryTutorialProgressData
    {
        public bool initial_completed;
        public int initial_step;
        public List<string> explained_features = new();

        public bool HasSeen(string feature_id)
        {
            return explained_features.Contains(feature_id);
        }

        public void MarkSeen(string feature_id)
        {
            if (!explained_features.Contains(feature_id)) explained_features.Add(feature_id);
        }
    }
}
