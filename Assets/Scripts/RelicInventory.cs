using System.Collections.Generic;
using System.Linq;

namespace Malatro
{
    public sealed class RelicInventory
    {
        public const int MaximumCapacity = 4;

        private readonly List<RelicData> relics = new List<RelicData>();

        public IReadOnlyList<RelicData> Relics => relics;
        public int Count => relics.Count;
        public bool IsFull => relics.Count >= MaximumCapacity;

        public bool Contains(RelicEffectType effectType)
        {
            return relics.Any(relic => relic != null && relic.EffectType == effectType);
        }

        public bool Contains(RelicData relic)
        {
            return relic != null && relics.Contains(relic);
        }

        public bool Add(RelicData relic)
        {
            if (relic == null || IsFull || relics.Contains(relic))
            {
                return false;
            }

            relics.Add(relic);
            return true;
        }

        public bool Remove(RelicData relic)
        {
            return relic != null && relics.Remove(relic);
        }

        public void Clear()
        {
            relics.Clear();
        }
    }
}
