using System.Collections.Generic;
using System.Linq;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Levels.Endless;
using Assets._Project.Develop.Runtime.Gameplay.Features.ApplyDamage;
using UnityEngine;
using Random = System.Random;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.EndlessMode
{
    // Состав волны бесконечного режима: сколько врагов, из каких тиров, какие виды.
    //
    // У волны бюджет силы — таблица по волнам, умноженная на «пилу» цикла. Он делится между тирами по их долям; доля
    // тира, которому на этой волне некого выставить, достаётся остальным пропорционально. В тире жребий выбирает виды
    // из годных — открытых и по карману тиру, виды акцента цикла выпадают чаще. Бюджет тира делится между ними поровну,
    // врагов вида — его бюджет / цена, не больше потолка. Остаток округления со знаком переходит следующему виду, затем
    // следующему тиру — от дорогих к дешёвым, — поэтому сила волны сходится с бюджетом при любом жребии. Остаток потолка
    // сначала достаётся ещё одному виду того же тира, если такой по карману.
    //
    // В боссовой волне тяжёлые тиры и эскорт не выходят — их место занимают босс и сопровождение, — а лёгкие тиры
    // получают долю своего обычного бюджета.
    public class EndlessWaveComposer
    {
        private const float NeutralWeight = 1f;
        private const float NeutralCycleStrength = 1f;
        private const int MinKindsPerWave = 1;
        private const int MinGroupSize = 1;
        private const int FirstPlaceInCycle = 1;
        private const int ExtraKindsCount = 1;

        private readonly EndlessModeConfig _config;
        private readonly EndlessCycles _cycles;
        private readonly EndlessRandomStreams _randomStreams;

        public EndlessWaveComposer(EndlessModeConfig config, EndlessCycles cycles, EndlessRandomStreams randomStreams)
        {
            _config = config;
            _cycles = cycles;
            _randomStreams = randomStreams;
        }

        public EndlessWaveComposition Compose(int waveNumber)
        {
            int cycle = _cycles.GetCycle(waveNumber);
            bool hasBoss = TryGetBoss(waveNumber, out EndlessBoss boss);
            float budget = GetBudget(waveNumber);
            float[] tiersBudgets = GetTiersBudgets(waveNumber, budget);

            if (hasBoss)
                ApplyBossWave(tiersBudgets);

            Random random = _randomStreams.CreateWaveRandom(waveNumber);
            IReadOnlyList<EndlessTier> tiers = _config.Tiers;
            List<EndlessWaveKind> kinds = new();
            float carry = 0f;

            for (int i = 0; i < tiers.Count; i++)
            {
                DamageTypes? accent = _cycles.GetAccent(cycle, tiers[i].Road);

                carry = AddTierKinds(kinds, i, waveNumber, tiersBudgets[i], carry, accent, random);
            }

            DamageTypes? lightAccent = _cycles.GetAccent(cycle, EndlessRoadRoles.Light);
            DamageTypes? heavyAccent = _cycles.GetAccent(cycle, EndlessRoadRoles.Heavy);

            return new EndlessWaveComposition(waveNumber, cycle, boss, kinds, budget, lightAccent, heavyAccent);
        }

        // Босс волны, если она боссовая: боссы идут по кругу, начиная с первого в списке
        public bool TryGetBoss(int waveNumber, out EndlessBoss boss)
        {
            boss = null;

            if (IsBossWave(waveNumber) == false)
                return false;

            IReadOnlyList<EndlessBoss> bosses = _config.Bosses;
            int bossOrdinal = waveNumber / _config.BossWavesStep - 1;

            boss = bosses[bossOrdinal % bosses.Count];

            if (boss == null || boss.Boss == null)
            {
                boss = null;
                return false;
            }

            return true;
        }

        private bool IsBossWave(int waveNumber)
        {
            int bossWavesStep = _config.BossWavesStep;

            return _config.Bosses.Count > 0 && bossWavesStep > 0 && waveNumber % bossWavesStep == 0;
        }

        // Бюджет волны с «пилой» цикла: первая волна после босса берёт первый множитель списка
        private float GetBudget(int waveNumber)
        {
            float budget = Mathf.Max(0f, _config.WaveBudget.GetValue(waveNumber));
            int strengthIndex = _cycles.GetPlaceInCycle(waveNumber) - FirstPlaceInCycle;
            IReadOnlyList<float> cycleStrength = _config.CycleStrength;

            if (strengthIndex >= 0 && strengthIndex < cycleStrength.Count)
                return budget * Mathf.Max(0f, cycleStrength[strengthIndex]);

            return budget * NeutralCycleStrength;
        }

        // Бюджеты тиров по долям. Тир, которому некого выставить на свой бюджет, выпадает, и доли пересчитываются, пока
        // все оставшиеся тиры не станут годными
        private float[] GetTiersBudgets(int waveNumber, float budget)
        {
            IReadOnlyList<EndlessTier> tiers = _config.Tiers;
            float[] shares = new float[tiers.Count];
            float[] budgets = new float[tiers.Count];

            for (int i = 0; i < tiers.Count; i++)
            {
                if (tiers[i].Enemies.Any(tierEnemy => tierEnemy.IsOpen(waveNumber)))
                    shares[i] = Mathf.Max(0f, tiers[i].Share.GetValue(waveNumber));
            }

            bool hasDroppedTier = true;

            while (hasDroppedTier)
            {
                hasDroppedTier = false;

                float sharesSum = shares.Sum();

                for (int i = 0; i < tiers.Count; i++)
                    budgets[i] = sharesSum > 0f ? budget * shares[i] / sharesSum : 0f;

                for (int i = 0; i < tiers.Count; i++)
                {
                    if (shares[i] > 0f && HasAffordableEnemy(tiers[i], waveNumber, budgets[i]) == false)
                    {
                        shares[i] = 0f;
                        hasDroppedTier = true;
                    }
                }
            }

            return budgets;
        }

        private void ApplyBossWave(float[] tiersBudgets)
        {
            IReadOnlyList<EndlessTier> tiers = _config.Tiers;

            for (int i = 0; i < tiers.Count; i++)
                tiersBudgets[i] *= tiers[i].Road == EndlessRoadRoles.Light ? _config.BossWaveBudgetShare : 0f;
        }

        private static bool HasAffordableEnemy(EndlessTier tier, int waveNumber, float tierBudget)
            => tier.Enemies.Any(tierEnemy => IsAffordable(tierEnemy, waveNumber, tierBudget));

        private static bool IsAffordable(EndlessTierEnemy tierEnemy, int waveNumber, float tierBudget)
            => tierEnemy.IsOpen(waveNumber) && tierEnemy.Price <= tierBudget;

        // Виды тира в состав. Возвращает остаток, который переходит следующему тиру. Тир, которому в боссовой волне
        // бюджета не хватило ни на один вид, отдаёт свой бюджет дальше целиком
        private float AddTierKinds(
            List<EndlessWaveKind> kinds,
            int tierIndex,
            int waveNumber,
            float tierBudget,
            float carry,
            DamageTypes? accent,
            Random random)
        {
            if (tierBudget <= 0f)
                return carry;

            EndlessTier tier = _config.Tiers[tierIndex];
            List<EndlessTierEnemy> affordableEnemies = tier.Enemies
                .Where(tierEnemy => IsAffordable(tierEnemy, waveNumber, tierBudget))
                .ToList();

            if (affordableEnemies.Count == 0)
                return carry + tierBudget;

            int kindsPerWave = Mathf.Max(MinKindsPerWave, Mathf.RoundToInt(tier.KindsPerWave.GetValue(waveNumber)));
            int kindsCount = Mathf.Min(kindsPerWave, affordableEnemies.Count);
            int groupSize = Mathf.Max(MinGroupSize, Mathf.RoundToInt(tier.GroupSize.GetValue(waveNumber)));

            // Сначала дорогие виды: остаток тогда сходится на дешёвых, и сила ближе к бюджету
            List<EndlessTierEnemy> pickedEnemies = PickWeighted(affordableEnemies, kindsCount, accent, random)
                .OrderByDescending(tierEnemy => tierEnemy.Price)
                .ToList();

            float kindBudget = tierBudget / kindsCount;
            bool isCapped = false;

            foreach (EndlessTierEnemy tierEnemy in pickedEnemies)
            {
                carry = AddKind(kinds, tierEnemy, kindBudget + carry, tierIndex, waveNumber, groupSize, out bool isKindCapped);
                isCapped |= isKindCapped;
            }

            // Потолок вида оставил бюджет тира неистраченным — остаток уходит ещё одному виду тира по жребию, а не
            // следующему тиру: иначе потолок некроманта сбросил бы бюджет тяжёлой дороги на массу лёгкой
            List<EndlessTierEnemy> leftEnemies = affordableEnemies.Except(pickedEnemies).ToList();

            while (isCapped)
            {
                float leftBudget = carry;
                List<EndlessTierEnemy> candidates = leftEnemies.Where(tierEnemy => tierEnemy.Price <= leftBudget).ToList();

                if (candidates.Count == 0)
                    break;

                EndlessTierEnemy extraEnemy = PickWeighted(candidates, ExtraKindsCount, accent, random)[0];

                leftEnemies.Remove(extraEnemy);
                carry = AddKind(kinds, extraEnemy, carry, tierIndex, waveNumber, groupSize, out isCapped);
            }

            return carry;
        }

        // Враги вида на его бюджет, не больше потолка. Возвращает остаток бюджета со знаком
        private float AddKind(
            List<EndlessWaveKind> kinds,
            EndlessTierEnemy tierEnemy,
            float availableBudget,
            int tierIndex,
            int waveNumber,
            int groupSize,
            out bool isCapped)
        {
            int count = Mathf.Max(0, Mathf.RoundToInt(availableBudget / tierEnemy.Price));
            isCapped = false;

            if (tierEnemy.HasMaxPerWave)
            {
                int maxCount = Mathf.Max(0, Mathf.RoundToInt(tierEnemy.MaxPerWave.GetValue(waveNumber)));

                isCapped = count > maxCount;
                count = Mathf.Min(count, maxCount);
            }

            if (count > 0)
            {
                EndlessTier tier = _config.Tiers[tierIndex];

                kinds.Add(new EndlessWaveKind(
                    tierEnemy.EnemyConfig,
                    count,
                    tierEnemy.Price,
                    tierIndex,
                    tier.Road,
                    groupSize,
                    tierEnemy.SpawnsLast));
            }

            return availableBudget - count * tierEnemy.Price;
        }

        // Жребий видов без повторов: вид с сопротивлением акцента весит больше остальных
        private List<EndlessTierEnemy> PickWeighted(
            List<EndlessTierEnemy> candidates,
            int pickCount,
            DamageTypes? accent,
            Random random)
        {
            List<EndlessTierEnemy> leftCandidates = new(candidates);
            List<EndlessTierEnemy> picked = new();

            for (int pick = 0; pick < pickCount; pick++)
            {
                float weightsSum = 0f;

                foreach (EndlessTierEnemy candidate in leftCandidates)
                    weightsSum += GetWeight(candidate, accent);

                double roll = random.NextDouble() * weightsSum;
                int pickedIndex = leftCandidates.Count - 1;

                for (int i = 0; i < leftCandidates.Count; i++)
                {
                    roll -= GetWeight(leftCandidates[i], accent);

                    if (roll < 0d)
                    {
                        pickedIndex = i;
                        break;
                    }
                }

                picked.Add(leftCandidates[pickedIndex]);
                leftCandidates.RemoveAt(pickedIndex);
            }

            return picked;
        }

        private float GetWeight(EndlessTierEnemy tierEnemy, DamageTypes? accent)
        {
            bool isAccented = accent.HasValue && tierEnemy.EnemyConfig.DamageResistanceType == accent.Value;

            return isAccented ? _config.ThemeWeight : NeutralWeight;
        }
    }
}
