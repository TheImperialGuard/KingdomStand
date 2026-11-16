using System.Collections.Generic;
using Assets._Project.Develop.Runtime.Utilities.DataManagment;
using Assets._Project.Develop.Runtime.Utilities.DataManagment.DataProviders;

namespace Assets._Project.Develop.Runtime.Meta.Features.Tutorial
{
    // Какие части обучения игрок уже видел: показанное больше не всплывает, в том числе при повторе уровня. В сохранениях,
    // сделанных до появления обучения, списка нет — значит, ничего ещё не показано.
    public class TutorialProgressService : IDataReader<PlayerData>, IDataWriter<PlayerData>
    {
        private readonly HashSet<string> _shownIds = new();

        public TutorialProgressService(PlayerDataProvider playerDataProvider)
        {
            playerDataProvider.RegisterWriter(this);
            playerDataProvider.RegisterReader(this);
        }

        public bool IsShown(string id) => _shownIds.Contains(id);

        public void MarkShown(string id) => _shownIds.Add(id);

        public void ReadFrom(PlayerData data)
        {
            _shownIds.Clear();

            if (data.ShownTutorials != null)
                _shownIds.UnionWith(data.ShownTutorials);
        }

        public void WriteTo(PlayerData data) => data.ShownTutorials = new List<string>(_shownIds);
    }
}
