using TaleWorlds.Library;

namespace DellarteDellaGuerra.PoliticalMap.Labels
{
    /// <summary>The political map's region labels, as one movie over the campaign map.</summary>
    public class PoliticalMapLabelsVM : ViewModel
    {
        private bool _isVisible;

        [DataSourceProperty] public MBBindingList<PoliticalMapLabelVM> Labels { get; } = new();

        [DataSourceProperty]
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (value == _isVisible) return;
                _isVisible = value;
                OnPropertyChangedWithValue(value);
            }
        }
    }
}
