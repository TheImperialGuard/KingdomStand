using Assets._Project.Develop.Runtime.Gameplay.Features.GoldEarning;
using Assets._Project.Develop.Runtime.UI.Core.Presenters;
using Assets._Project.Develop.Runtime.UI.Core.Sounds;

namespace Assets._Project.Develop.Runtime.UI.Gameplay.GoldWallet
{
    // Золото за досрочный вызов волны всплывает «+N» у числа золота и звенит монетами.
    public class GoldGainPresenter : IPresenter
    {
        private readonly GoldGainView _view;
        private readonly EarnGoldOnSkipStageService _earnGoldOnSkipStageService;
        private readonly UISoundPlayer _uiSoundPlayer;

        public GoldGainPresenter(
            GoldGainView view,
            EarnGoldOnSkipStageService earnGoldOnSkipStageService,
            UISoundPlayer uiSoundPlayer)
        {
            _view = view;
            _earnGoldOnSkipStageService = earnGoldOnSkipStageService;
            _uiSoundPlayer = uiSoundPlayer;
        }

        public void Initialize()
        {
            _earnGoldOnSkipStageService.Earned += OnGoldEarned;
        }

        public void Dispose()
        {
            _earnGoldOnSkipStageService.Earned -= OnGoldEarned;
        }

        private void OnGoldEarned(int gold)
        {
            _view.Show(gold);
            _uiSoundPlayer.PlayGoldGain();
        }
    }
}
