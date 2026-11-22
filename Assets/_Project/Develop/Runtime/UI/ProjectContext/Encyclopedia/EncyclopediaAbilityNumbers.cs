using Assets._Project.Develop.Runtime.Configs.Gameplay.Abilities;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Abilities.Enemies;
using Assets._Project.Develop.Runtime.Utilities.Localization;
using Assets._Project.Develop.Runtime.Utilities.Text;

namespace Assets._Project.Develop.Runtime.UI.ProjectContext.Encyclopedia
{
    // Строка цифр способности под её описанием: числа берутся из полей конфига, в описаниях их нет. У башен —
    // значения заданного уровня способности: в книге первого, в окне способностей боя — покупаемого. Части
    // строки разделены «·» и переносятся только по нему: «раз в 10 с» не разрывается. Шаблоны — русские, они же ключи
    // таблицы перевода; у английского свой порядок слов и единицы («4 dmg every 0.5s for 3s»).
    public static class EncyclopediaAbilityNumbers
    {
        private const int FirstLevel = 1;
        private const char Space = ' ';
        private const string PartsSeparator = "·";

        private const string CooldownTemplate = "Раз в {0} с";
        private const string StunTemplate = "Оглушение на {0} с";
        private const string AdditionalTargetsTemplate = "+{0} {1} за уровень";
        private const string PoisonTemplate = "Яд: {0} урона раз в {1} с, {2} с";
        private const string FragmentsTemplate = "{0} {1} по {2} урона";
        private const string FreezeTemplate = "Заморозка {0} с · раз в {1} с";
        private const string SummonTemplate = "Отряд {0} · раз в {1} с";
        private const string WarCryTemplate = "Радиус {0} · скорость ×{1} на {2} с · раз в {3} с";
        private const string SnowballThrowTemplate = "Дальность {0} · заморозка {1} с · раз в {2} с";

        private const string TargetWordOne = "цель";
        private const string TargetWordFew = "цели";
        private const string TargetWordMany = "целей";
        private const string FragmentWordOne = "осколок";
        private const string FragmentWordFew = "осколка";
        private const string FragmentWordMany = "осколков";

        public static string Format(AbilityConfig ability, LocalizationService localization)
            => Format(ability, FirstLevel, localization);

        public static string Format(AbilityConfig ability, int level, LocalizationService localization)
            => KeepPartsTogether(FormatTowerAbility(ability, level, localization));

        public static string Format(EnemyAbilityConfig ability, LocalizationService localization)
            => KeepPartsTogether(FormatEnemyAbility(ability, localization));

        private static string FormatTowerAbility(AbilityConfig ability, int level, LocalizationService localization)
        {
            switch (ability)
            {
                case AdditionalShootingTargetsAbilityConfig targets:
                    return FormatAdditionalTargets(targets.GetAdditionsTargetsBy(level), localization);

                case PoisonedAttackAbilityConfig poison:
                    return localization.FormatTemplate(
                        PoisonTemplate,
                        localization.FormatDecimal(poison.GetDamageByLevel(level)),
                        localization.FormatDecimal(poison.GetEffectIntervalByLevel(level)),
                        localization.FormatDecimal(poison.GetEffectTimeByLevel(level)));

                case ProjectileFragmentsAbilityConfig fragments:
                    return FormatFragments((int)fragments.FragmentsCount, fragments.GetFragmentsDamageBy(level), localization);

                case AttackStunAbilityConfig attackStun:
                    return localization.FormatTemplate(StunTemplate, localization.FormatDecimal(attackStun.GetStunTimeBy(level)));

                case OneShotAttackAbilityConfig oneShot:
                    return localization.FormatTemplate(CooldownTemplate, localization.FormatDecimal(oneShot.GetCooldownBy(level)));

                case StunAbilityConfig stun:
                    return localization.FormatTemplate(
                        FreezeTemplate,
                        localization.FormatDecimal(stun.GetStunTimeBy(level)),
                        localization.FormatDecimal(stun.Cooldown));

                default:
                    return string.Empty;
            }
        }

        private static string FormatEnemyAbility(EnemyAbilityConfig ability, LocalizationService localization)
        {
            string cooldown = localization.FormatDecimal(ability.Cooldown);

            switch (ability)
            {
                case SummonAbilityConfig summon:
                    return localization.FormatTemplate(SummonTemplate, summon.Count, cooldown);

                case WarCryAbilityConfig warCry:
                    return localization.FormatTemplate(
                        WarCryTemplate,
                        localization.FormatDecimal(warCry.Radius),
                        localization.FormatDecimal(warCry.SpeedBoostIndex),
                        localization.FormatDecimal(warCry.SpeedBoostDuration),
                        cooldown);

                case SnowballThrowAbilityConfig snowballThrow:
                    return localization.FormatTemplate(
                        SnowballThrowTemplate,
                        localization.FormatDecimal(snowballThrow.ThrowRange),
                        localization.FormatDecimal(snowballThrow.FreezeDuration),
                        cooldown);

                default:
                    return localization.FormatTemplate(CooldownTemplate, cooldown);
            }
        }

        // Все пробелы — неразрывные, кроме пробела после разделителя частей.
        private static string KeepPartsTogether(string numbers)
        {
            string nonBreakingSpace = NonBreakingSpace.Character.ToString();
            string glued = numbers.Replace(Space, NonBreakingSpace.Character);

            return glued.Replace(PartsSeparator + nonBreakingSpace, PartsSeparator + Space);
        }

        private static string FormatAdditionalTargets(int targets, LocalizationService localization)
        {
            string noun = localization.SelectPlural(targets, TargetWordOne, TargetWordFew, TargetWordMany);

            return localization.FormatTemplate(AdditionalTargetsTemplate, targets, noun);
        }

        private static string FormatFragments(int count, float damage, LocalizationService localization)
        {
            string noun = localization.SelectPlural(count, FragmentWordOne, FragmentWordFew, FragmentWordMany);

            return localization.FormatTemplate(FragmentsTemplate, count, noun, localization.FormatDecimal(damage));
        }
    }
}
